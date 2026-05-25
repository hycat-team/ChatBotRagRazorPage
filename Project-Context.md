# RagChatbot Project Context & Troubleshooting

Tài liệu này tổng hợp bối cảnh hiện tại của dự án, kiến trúc kỹ thuật, cũng như danh sách các lỗi nghiêm trọng đã gặp phải và cách giải quyết để tiện cho việc theo dõi và bảo trì sau này.

## 1. Bối cảnh dự án (Project Context)
Dự án **RagChatbot** là một hệ thống trò chuyện thông minh dành cho sinh viên, ứng dụng công nghệ **RAG** (Retrieval-Augmented Generation) để AI có thể đọc hiểu và trả lời câu hỏi dựa trên tài liệu môn học nội bộ thay vì tự bịa ra thông tin.

### Stack Công Nghệ Chính:
- **Backend:** ASP.NET Core MVC (.NET 8).
- **Cơ sở dữ liệu:** PostgreSQL chạy qua Docker (`docker-compose.yml`), sử dụng extension `pgvector` để lưu trữ và tìm kiếm vector nhúng (embeddings).
- **AI / LLM Integration:** Dùng thư viện `Microsoft.SemanticKernel`.
- **Nhà cung cấp AI:** Google AI Studio (thông qua chuẩn API tương thích OpenAI).
  - **Chat Model:** `gemini-1.5-flash`
  - **Embedding Model:** `text-embedding-004` (Dimension: 768)
- **Real-time:** Sử dụng SignalR (`ChatHub.cs`) để stream nội dung câu trả lời của AI theo thời gian thực (hiệu ứng gõ chữ) về giao diện web.
- **Frontend:** HTML/Vanilla JS với Tailwind CSS tích hợp trong các file Razor `.cshtml`.

---

## 2. Luồng hoạt động cơ bản (Core Workflow)
1. **Quản lý phiên (Session):** Khi người dùng click vào một môn học (Subject), hệ thống Load lịch sử các tin nhắn cũ từ DB.
2. **RAG Pipeline (Bỏ qua với lời chào):** Nếu người dùng nhắn lời chào ngắn gọn (<15 ký tự), AI sẽ trả lời trực tiếp siêu nhanh. Nếu là câu hỏi, hệ thống sẽ:
   - Dùng Google API tạo Vector nhúng cho câu hỏi.
   - Tìm kiếm các đoạn tài liệu tương đồng nhất trong PostgreSQL bằng Cosine Distance (thông qua index `hnsw`).
   - Ghép tài liệu thành `Context` và gửi cho LLM.
3. **Phản hồi:** LLM stream từng token trả về Client qua SignalR. Tin nhắn sau đó được lưu vào Database để làm "trí nhớ" cho câu hỏi tiếp theo.

---

## 3. Các Lỗi Đã Gặp Phải & Cách Khắc Phục (Troubleshooting Log)

Dưới đây là những "căn bệnh" hệ thống từng mắc phải và đã được chữa trị dứt điểm:

### Lỗi 1: Máy tính bị giật lag toàn hệ thống khi AI tìm kiếm tài liệu
- **Nguyên nhân:** Extension `pgvector` khi tính toán độ tương tự vector trên hàng ngàn dòng dữ liệu đã ngốn 100% CPU của máy host, làm treo máy tính cục bộ.
- **Cách giải quyết:** 
  - Cấu hình lại `docker-compose.yml`, giới hạn container database chỉ được dùng tối đa `1.0 CPU` và `1GB RAM`.
  - Tạo chỉ mục `HNSW Index` cho cột Embedding trong Database để thuật toán tìm kiếm diễn ra cực nhanh mà không cần quét toàn bộ bảng (Full Table Scan).

### Lỗi 2: AI kẹt mãi ở trạng thái "Đang suy nghĩ..." không bao giờ trả lời
- **Nguyên nhân:** File cấu hình gọi nhầm Model `gemma-4-26b-a4b-it` (một model không được Google AI Studio hỗ trợ thông qua API OpenAI). Thay vì báo lỗi JSON đàng hoàng, Google API im lặng ngắt kết nối hoặc treo, khiến backend SignalR của chúng ta chờ mãi mãi.
- **Cách giải quyết:** Sửa mã nguồn `AiService.cs` để trỏ đúng về mô hình `gemini-1.5-flash` và `text-embedding-004` chính chủ của Google.

### Lỗi 3: Lỗi bất đồng bộ SignalR khiến giao diện kẹt chữ "Đang tải lịch sử..."
- **Nguyên nhân (Race Condition):** Khi người dùng F5 tải lại trang, thao tác nhấn vào chọn môn học diễn ra khi kết nối SignalR (`startConnection()`) chưa thực sự thiết lập xong. Lệnh gọi hàm `LoadSubjectHistory` bị ném lỗi ở Client nhưng bị chìm đi (swallowed), làm UI chờ dữ liệu vĩnh viễn.
- **Cách giải quyết:** Viết lại logic Javascript, kiểm tra trạng thái `connection.state`. Nếu chưa Connect thì hoãn việc gọi hàm hoặc bỏ qua, đồng thời tự động reload lại lịch sử ngay sau khi vòng lặp kết nối SignalR báo thành công.

### Lỗi 4: AI báo lỗi "Bad Request" vì truyền sai cấu trúc lịch sử chat
- **Nguyên nhân:** Khi lưu tin nhắn của User vào DB xong, hệ thống truy vấn lại 10 tin nhắn gần nhất để nạp vào Semantic Kernel. Vô tình, câu truy vấn lấy luôn cả tin nhắn vừa lưu, làm chuỗi ngữ cảnh gửi cho AI bị trùng lặp 2 tin nhắn của User liên tiếp. Google API cấm tuyệt đối việc User và AI không nói chuyện luân phiên (nhắn 2 lần liên tục).
- **Cách giải quyết:** Cập nhật EF Core LINQ trong `ChatHub.cs` để thêm điều kiện loại trừ `x.Id != userMessage.Id` khi fetch lịch sử.

### Lỗi 5: Tràn/Lỗi bộ nhớ Vector Dimension Mismatch
- **Nguyên nhân:** Mặc định cột Embedding tạo ra có 3072 chiều. Khi chuyển sang model `text-embedding-004` của Google (chuẩn 768 chiều), Database từ chối insert dữ liệu.
- **Cách giải quyết:** Cập nhật lại EF Core `HasColumnType("vector(768)")` và chạy `dotnet ef database update` để đồng bộ.

---
*Văn bản này được cập nhật tự động để phản ánh kiến trúc và lịch sử sửa lỗi mới nhất của dự án.*
