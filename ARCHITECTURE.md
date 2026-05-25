# Kiến Trúc & Troubleshooting — RagChatbot

Tài liệu này mô tả chi tiết kiến trúc kỹ thuật, luồng hoạt động, và lịch sử các lỗi nghiêm trọng đã gặp phải trong quá trình phát triển dự án.

## 1. Stack Công Nghệ Chi Tiết

- **Backend:** ASP.NET Core MVC (.NET 8)
- **Cơ sở dữ liệu:** PostgreSQL chạy qua Docker (`docker-compose.yml`), extension `pgvector` để lưu trữ và tìm kiếm vector embedding.
- **AI / LLM Integration:** `Microsoft.SemanticKernel`
- **Nhà cung cấp AI:** Google AI Studio (thông qua API tương thích OpenAI)
  - **Chat Model:** `gemini-1.5-flash`
  - **Embedding Model:** `text-embedding-004` (768 chiều)
- **Real-time:** SignalR (`ChatHub.cs`) — stream từng token phản hồi của AI về giao diện.
- **Frontend:** Razor Views + Tailwind CSS (tích hợp trong file `.cshtml`).

---

## 2. Luồng Hoạt Động Cốt Lõi

### Luồng 1: Ingestion Pipeline (Xử lý tài liệu ngầm)

1. Người dùng upload file qua `DocumentController` → file được upload lên **Google Drive**, ID lưu vào bảng `Documents` với trạng thái `Pending`.
2. `DocumentProcessingJob` (BackgroundService) quét định kỳ:
   - Đổi trạng thái → `Processing`
   - Tải file từ Google Drive → parse text bằng `PdfPig` / `OpenXml`
   - **Text Chunking**: cắt thành đoạn ~1000 ký tự, có overlap để giữ ngữ cảnh
   - Gọi Google Embedding API (batch) → lưu vector vào bảng `DocumentChunks`
   - Đổi trạng thái → `Indexed`

### Luồng 2: RAG Chat Pipeline (Hỏi đáp real-time)

1. Người dùng chọn môn học → SignalR tự động load lịch sử chat gần nhất từ DB.
2. Người dùng gửi câu hỏi qua `ChatHub.SendMessage()`:
   - **Tối ưu lời chào:** Nếu tin nhắn < 15 ký tự và không có `?`, bỏ qua bước Vector Search, trả lời trực tiếp.
   - Tạo embedding cho câu hỏi → tìm kiếm HNSW Cosine Similarity trong `DocumentChunks`.
   - Ghép context từ các chunk tìm được + 10 tin nhắn lịch sử gần nhất.
   - Stream phản hồi từ LLM về client qua SignalR (`ReceiveToken`).
   - Lưu cả câu hỏi + câu trả lời + citations JSON vào `ChatMessages`.

---

## 3. Schema Cơ Sở Dữ Liệu

```sql
-- Môn học
Subjects (Id, Code, Name, CreatedAt)

-- Tài liệu (FilePath lưu Google Drive File ID)
Documents (Id, SubjectId, FileName, FilePath, Status, UploadedAt)

-- Chunks phục vụ RAG
DocumentChunks (Id, DocumentId, Content, PageNumber, Embedding vector(768))

-- Phiên hội thoại
ChatSessions (Id UUID, SubjectId, Title, CreatedAt)

-- Tin nhắn
ChatMessages (Id, SessionId, Role, Content, Citations TEXT, Timestamp)

-- Index tìm kiếm vector
CREATE INDEX ON "DocumentChunks" USING hnsw ("Embedding" vector_cosine_ops);
```

---

## 4. Troubleshooting Log — Các Lỗi Đã Gặp & Cách Khắc Phục

### Lỗi 1: Máy tính bị lag/treo khi AI tìm kiếm tài liệu
- **Nguyên nhân:** `pgvector` tính cosine similarity trên toàn bộ bảng (Full Table Scan) ngốn 100% CPU.
- **Giải pháp:**
  - Giới hạn Docker container: `1.0 CPU`, `1GB RAM` trong `docker-compose.yml`.
  - Tạo **HNSW Index** trên cột `Embedding` để tìm kiếm xấp xỉ nhanh, không quét toàn bảng.

### Lỗi 2: AI kẹt mãi ở "Đang suy nghĩ..." không phản hồi
- **Nguyên nhân:** Gọi nhầm model `gemma-4-26b-a4b-it` không được Google AI Studio hỗ trợ qua API OpenAI. Google API im lặng ngắt kết nối thay vì trả lỗi, khiến SignalR chờ vô hạn.
- **Giải pháp:** Sửa `AiService.cs` trỏ đúng sang `gemini-1.5-flash` và `text-embedding-004`.

### Lỗi 3: Giao diện kẹt chữ "Đang tải lịch sử..." sau khi F5
- **Nguyên nhân (Race Condition):** Người dùng click chọn môn học trước khi kết nối SignalR thiết lập xong. Lệnh `LoadSubjectHistory` bị lỗi ở client nhưng bị nuốt im, UI chờ dữ liệu mãi mãi.
- **Giải pháp:** Viết lại JS — kiểm tra `connection.state` trước khi gọi hub. Tự động reload lịch sử ngay sau khi SignalR báo kết nối thành công.

### Lỗi 4: AI báo "Bad Request" khi trả lời
- **Nguyên nhân:** Khi fetch lịch sử chat để gửi cho AI, truy vấn vô tình lấy luôn tin nhắn User vừa lưu → 2 tin nhắn User liên tiếp → Google API từ chối (yêu cầu User/Assistant phải xen kẽ nhau).
- **Giải pháp:** Thêm `.Where(x => x.Id != userMessage.Id)` vào LINQ query trong `ChatHub.cs`.

### Lỗi 5: Lỗi Vector Dimension Mismatch
- **Nguyên nhân:** Cột `Embedding` được tạo với 3072 chiều (chuẩn OpenAI cũ). Khi chuyển sang `text-embedding-004` của Google (768 chiều), DB từ chối insert.
- **Giải pháp:** Cập nhật `HasColumnType("vector(768)")` trong `ApplicationDbContext.cs` và chạy lại migration.

### Lỗi 6: Lỗi 429 Too Many Requests khi index tài liệu lớn
- **Nguyên nhân:** Gọi Embedding API cho từng chunk riêng lẻ (hàng trăm requests liên tục) → vượt rate limit của Google API.
- **Giải pháp:** Refactor `GoogleEmbeddingService` sang dùng `batchEmbedContents` — gom tất cả chunks thành 1 batch request duy nhất.

---

*Tài liệu được cập nhật để phản ánh kiến trúc và lịch sử sửa lỗi mới nhất.*
