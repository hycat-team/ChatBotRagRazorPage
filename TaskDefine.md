```python
markdown_content = """# TÀI LIỆU TẢI CHI TIẾT DỰ ÁN (PROJECT SPECIFICATION)
## HỆ THỐNG CHATBOT HỖ TRỢ HỌC TẬP DỰA TRÊN TÀI LIỆU MÔN HỌC (RAG CHATBOT)

### 1. TỔNG QUAN DỰ ÁN (PROJECT OVERVIEW)
Hệ thống là một ứng dụng Web dựa trên kiến trúc **ASP.NET Core MVC (.NET 8)**, áp dụng kỹ thuật **RAG (Retrieval-Augmented Generation)** để cho phép sinh viên hỏi đáp tự nhiên dựa trên tài liệu môn học (PDF, DOCX, Slide bài giảng) do Giảng viên/Quản trị viên cung cấp. Hệ thống giới hạn phạm vi trả lời trong bộ tài liệu được chỉ định và cung cấp nguồn trích dẫn cụ thể (Tên file, số trang/đoạn).

---

### 2. KIẾN TRÚC & CÔNG NGHỆ CHỦ CHỐT (TECH STACK)
* **Framework chính:** ASP.NET Core MVC (.NET 8)
* **Cơ sở dữ liệu (Relational & Vector DB):** PostgreSQL tích hợp extension `pgvector` (Chạy hoàn toàn trên Docker).
* **Giao diện (Frontend):** Razor Views + Tailwind CSS (Cấu hình qua npm/CDN).
* **Real-time Streaming:** ASP.NET Core SignalR (Dùng để stream từng từ của câu trả lời từ LLM về giao diện chat của sinh viên).
* **AI Orchestration / Vectorization:** HttpClient / Microsoft Semantic Kernel kết nối với các mô hình LLM (Gemini API / OpenAI API) và Embedding Model (`text-embedding-3-small` hoặc tương đương).
* **Xử lý tệp tin (File Parsing):** `UglyToad.PdfPig` (Dành cho PDF) và `DocumentFormat.OpenXml` (Dành cho DOCX/PPTX).
* **Xử lý tác vụ nền (Background Jobs):** .NET `BackgroundService` (Hosted Service) kết hợp với hàng đợi trong database để xử lý tách text, chunking và embedding bất đồng bộ mà không gây nghẽn luồng xử lý chính.

---

### 3. THIẾT KẾ CƠ SỞ DỮ LIỆU (DATABASE SCHEMA)

Hệ thống sử dụng một database PostgreSQL duy nhất chứa cả dữ liệu quan hệ và dữ liệu Vector phục vụ RAG.


```

````text
File saved successfully to rag-chatbot-specification.md

```sql
-- Kích hoạt extension pgvector
CREATE EXTENSION IF NOT EXISTS vector;

-- 1. Bảng Môn học
CREATE TABLE "Subjects" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Code" VARCHAR(50) NOT NULL UNIQUE,
    "Name" VARCHAR(255) NOT NULL,
    "CreatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 2. Bảng Tài liệu
CREATE TABLE "Documents" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "SubjectId" INT REFERENCES "Subjects"("Id") ON DELETE CASCADE,
    "FileName" VARCHAR(255) NOT NULL,
    "FilePath" VARCHAR(500) NOT NULL,
    "Status" VARCHAR(50) NOT NULL, -- 'Pending', 'Processing', 'Indexed', 'Failed'
    "UploadedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 3. Bảng Các đoạn văn bản đã chia nhỏ (Chunks) phục vụ RAG
CREATE TABLE "DocumentChunks" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "DocumentId" INT REFERENCES "Documents"("Id") ON DELETE CASCADE,
    "Content" TEXT NOT NULL,
    "PageNumber" INT, -- Số trang của chunk (nếu có từ PDF)
    "Embedding" vector(1536) -- Độ dài vector tùy thuộc vào model embedding (ví dụ 1536 cho OpenAI, 768 cho Gemini)
);

-- 4. Bảng Phiên hội thoại (Chat Sessions)
CREATE TABLE "ChatSessions" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "SubjectId" INT REFERENCES "Subjects"("Id") ON DELETE CASCADE,
    "Title" VARCHAR(255) NOT NULL,
    "CreatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 5. Bảng Chi tiết tin nhắn (Chat Messages)
CREATE TABLE "ChatMessages" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "SessionId" UUID REFERENCES "ChatSessions"("Id") ON DELETE CASCADE,
    "Role" VARCHAR(50) NOT NULL, -- 'user' hoặc 'assistant'
    "Content" TEXT NOT NULL,
    "Citations" TEXT, -- Chuỗi JSON lưu trữ thông tin trích dẫn nguồn (ví dụ: [{"FileName": "Agile.pdf", "Page": 3}])
    "Timestamp" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tạo Index tìm kiếm Vector tương đồng (Cosine Distance)
CREATE INDEX ON "DocumentChunks" USING hnsw ("Embedding" vector_cosine_ops);

````

---

### 4. LUỒNG XỬ LÝ CHÍNH CỦA HỆ THỐNG (SYSTEM WORKFLOWS)

#### Luồng 1: Xử lý tài liệu ngầm (Ingestion Pipeline)

1. Người dùng upload file thông qua giao diện Web (`DocumentController` -> `Upload`).
2. Hệ thống lưu file vào thư mục local lưu trữ tạm thời và ghi nhận một bản ghi vào bảng `Documents` với trạng thái `Pending`.
3. Một dịch vụ ngầm (`DocumentProcessingJob`) quét các file `Pending`:

- Chuyển trạng thái thành `Processing`.
- Đọc nội dung chữ từ file bằng `PdfPig` hoặc `OpenXml`.
- Thực hiện **Text Chunking**: Chia text thành các đoạn nhỏ (khoảng 1000 ký tự / ~300 tokens), cấu hình độ đè dịch (Overlap) khoảng 10-20% để không mất ngữ cảnh giữa các đoạn liền kề.
- Gọi API Embedding để biến đổi từng Chunk văn bản thành Vector số thực.
- Lưu toàn bộ Content, PageNumber, và Vector Embedding vào bảng `DocumentChunks`.
- Cập nhật trạng thái `Documents` thành `Indexed`.

#### Luồng 2: Hỏi đáp RAG kết hợp Real-time Streaming

1. Sinh viên mở giao diện chat, chọn một Môn học và bắt đầu gửi câu hỏi thông qua kết nối **SignalR**.
2. Khi nhận được câu hỏi từ Client tại `ChatHub`:

- Hệ thống lưu câu hỏi của User vào bảng `ChatMessages`.
- Lấy danh sách 3-5 câu hỏi/trả lời gần nhất từ `ChatMessages` thuộc `SessionId` đó để làm ngữ cảnh hội thoại dài hạn.
- Gọi API Embedding để chuyển câu hỏi hiện tại thành Vector.
- Thực hiện **Vector Search**: Truy vấn bảng `DocumentChunks` để tìm các đoạn văn bản thuộc môn học đó có khoảng cách Cosine nhỏ nhất (độ tương đồng cao nhất) với Vector câu hỏi. Lấy ra top 3-5 đoạn phù hợp nhất.
- Xây dựng **System Prompt** nghiêm ngặt cho LLM:

```text
Bạn là trợ lý học tập thông minh. Hãy trả lời câu hỏi của sinh viên dựa trên ngữ cảnh được cung cấp dưới đây.
Nếu câu trả lời không có trong ngữ cảnh, hãy trả lời: 'Tôi không tìm thấy thông tin này trong tài liệu môn học'.
Tuyệt đối không sử dụng kiến thức bên ngoài để tự bịa câu trả lời.

[NGỮ CẢNH TÀI LIỆU]:
... (Nội dung của top các chunks tìm được kèm theo Tên file và Số trang) ...

[LỊCH SỬ TRÒ CHUYỆN]:
... (3 câu thoại gần nhất) ...

[CÂU HỎI CỦA SINH VIÊN]:
...

```

- Gọi LLM với chế độ **Streaming**. Khi LLM trả về từng token (từng từ), `ChatHub` sử dụng SignalR để đẩy ngay từ đó xuống Client (`Clients.Caller.SendAsync("ReceiveToken", token)`), tạo hiệu ứng gõ chữ thời gian thực.
- Khi kết thúc stream, hệ thống lấy toàn bộ nội dung câu trả lời hoàn chỉnh cùng thông tin trích dẫn nguồn (metadata của các chunks đã dùng) để lưu vào database làm lịch sử.

---

### 5. DANH SÁCH TASK CHI TIẾT CHO AGENT ANTIGRAVITY (WBS)

#### PHASE 1: KHỞI TẠO MÔI TRƯỜNG & DỰ ÁN (SETUP ENV & INITIALIZATION)

- [ ] **Task 1.1:** Tạo Docker Compose file cấu hình dịch vụ PostgreSQL, kích hoạt sẵn extension `pgvector` và ánh xạ volume để lưu trữ dữ liệu bền vững.
- [ ] **Task 1.2:** Khởi tạo Solution .NET 8 với kiến trúc ASP.NET Core MVC. Chia thư mục rõ ràng: `Controllers`, `Models` (bao gồm ViewModels và DomainModels), `Views`, `Services`, `Hubs`.
- [ ] **Task 1.3:** Cài đặt các NuGet Packages cần thiết: `Npgsql.EntityFrameworkCore.PostgreSQL`, `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite` (hoặc cấu hình kiểu vector thủ công), `Microsoft.AspNetCore.SignalR.Common`, `UglyToad.PdfPig`, `DocumentFormat.OpenXml`.
- [ ] **Task 1.4:** Cấu hình Tailwind CSS thông qua npm hoặc tích hợp vào quy trình build của dự án (Asset Pipeline) để phục vụ giao diện Razor Views.

#### PHASE 2: THIẾT KẾ DATA LAYER & MIGRATION

- [ ] **Task 2.1:** Thiết kế các Entity Class tương ứng với cơ sở dữ liệu đã mô tả (Subject, Document, DocumentChunk, ChatSession, ChatMessage).
- [ ] **Task 2.2:** Cấu hình `DbContext` của Entity Framework Core. Lưu ý cấu hình trường dữ liệu `Embedding` thuộc loại mảng số thực `float[]` ứng với kiểu dữ liệu `vector` trong Postgres.
- [ ] **Task 2.3:** Tạo Migration ban đầu và cập nhật database lên Docker Postgres container, kiểm tra xem các bảng và chỉ mục HNSW đã được tạo thành công chưa.

#### PHASE 3: PIPELINE XỬ LÝ TÀI LIỆU (DOCUMENT INGESTION SERVICE)

- [ ] **Task 3.1:** Viết `DocumentController` và View để quản lý danh sách tài liệu môn học, cho phép chọn môn học và upload tệp PDF/DOCX.
- [ ] **Task 3.2:** Triển khai `DocumentExtractionService` để parse text từ tệp tin, trích xuất text kèm theo thông tin số trang (đặc biệt đối với file PDF).
- [ ] **Task 3.3:** Triển khai `TextChunkingService` đảm nhận thuật toán cắt chuỗi ký tự cố định kèm overlap thích hợp để chuẩn bị dữ liệu đầu vào cho embedding.
- [ ] **Task 3.4:** Viết một .NET `BackgroundService` chạy ngầm. Dịch vụ này định kỳ quét bảng `Documents` tìm trạng thái `Pending`, thực hiện parse text -> băm nhỏ -> gọi API tạo vector embedding từ LLM -> lưu kết quả vào database.

#### PHASE 4: LẬP TRÌNH CORE RAG PIPELINE

- [ ] **Task 4.1:** Viết hàm tính toán tìm kiếm Vector tương đồng bằng SQL thuần (Raw SQL) hoặc tích hợp qua EF Core với hàm tính khoảng cách Cosine (`<=>` trong pgvector) để lấy ra Top K đoạn văn bản tương thích nhất từ bảng `DocumentChunks`.
- [ ] **Task 4.2:** Viết dịch vụ cấu trúc Prompt (Prompt Engineering Service) tự động lắp ghép thông tin cấu trúc bao gồm System Instruction, Ngữ cảnh lấy từ Vector DB, Lịch sử trò chuyện cũ và câu hỏi hiện tại.

#### PHASE 5: REAL-TIME CHAT VỚI SIGNALR & STREAMING

- [ ] **Task 5.1:** Tạo một SignalR Hub (`ChatHub.cs`) chứa method `SendMessage(Guid sessionId, string message)`.
- [ ] **Task 5.2:** Tích hợp HttpClient để gọi API của LLM (ví dụ OpenAI hoặc Gemini) theo chế độ stream nhận kết quả liên tục (Server-Sent Events).
- [ ] **Task 5.3:** Trong luồng đọc stream của LLM, mỗi khi nhận được một cụm chữ mới, gọi `Clients.Caller.SendAsync("ReceiveChunk", chunk)` để đẩy ngay về client. Khi kết thúc stream, tiến hành lưu toàn bộ câu trả lời kèm mảng JSON trích dẫn nguồn vào DB.

#### PHASE 6: PHÁT TRIỂN FRONTEND VÀ HOÀN THIỆN UI/UX

- [ ] **Task 6.1:** Xây dựng giao diện Khung Chat bằng Razor View phối hợp Tailwind CSS. Giao diện bao gồm: Thanh bên hiển thị lịch sử các Phiên chat theo môn học, khung chính hiển thị hội thoại trò chuyện sinh động.
- [ ] **Task 6.2:** Viết JavaScript kết nối tới SignalR Hub, xử lý sự kiện gửi tin nhắn, append tin nhắn của User vào màn hình, thu nhận các token phản hồi thời gian thực từ Hub và cập nhật hiệu ứng gõ chữ (typing effect).
- [ ] **Task 6.3:** Hiển thị khu vực Trích dẫn nguồn (Citations) một cách tinh tế ngay dưới mỗi câu trả lời của Bot, cho phép nhấp vào để xem chi tiết đoạn văn bản gốc trích xuất từ file nào, trang số mấy.

---

### 6. TIÊU CHÍ NGHIỆM THU (ACCEPTANCE CRITERIA FOR AGENT)

1. **Tính năng Tài liệu:** Hệ thống phải upload được một file PDF dài, xử lý ngầm thành công, lưu dữ liệu Vector vào Postgres mà không gây đứng luồng giao diện Web. Danh sách tài liệu phải chuyển từ `Processing` sang `Indexed`.
2. **Tính năng Chat:** Bot không được trả lời các câu hỏi ngoài phạm vi tài liệu (Nếu hỏi ngoài lề, bot bắt buộc phải từ chối theo System Prompt đã định nghĩa).
3. **Trải nghiệm Real-time:** Tin nhắn từ Bot phải được hiển thị dạng stream từng chữ một thông qua SignalR, không được đợi load toàn bộ câu rồi mới hiển thị. Sau khi chat xong, F5 lại trang vẫn phải giữ được lịch sử chat và các nguồn trích dẫn.
   """

file_path = "rag-chatbot-specification.md"
with open(file_path, "w", encoding="utf-8") as f:
f.write(markdown_content)

print(f"File saved successfully to {file_path}")

```
File Markdown của bạn đã sẵn sàng
[file-tag: code-generated-file-0-1779643753090038859]

Mình đã tạo xong tài liệu mô tả chi tiết dự án (Project Specification) ở định dạng Markdown theo đúng yêu cầu tech stack của bạn bao gồm: ASP.NET Core MVC (.NET 8), PostgreSQL (tích hợp pgvector), Tailwind CSS, SignalR và Docker.

Trong tài liệu này, mình đã phân chia cấu trúc rõ ràng từ kiến trúc tổng thể, thiết kế cơ sở dữ liệu cho đến một danh sách công việc (WBS - Work Breakdown Structure) với 6 giai đoạn cụ thể để agent antigravity có thể dễ dàng đọc hiểu và tiến hành code từng bước một cách chuẩn xác. Bạn có thể tải file về và chuyển trực tiếp cho agent nhé!

```
