# RAG Chatbot — Hệ thống Chatbot Hỗ trợ Học tập

Ứng dụng Web áp dụng kỹ thuật **RAG (Retrieval-Augmented Generation)** cho phép sinh viên hỏi đáp tự nhiên dựa trên tài liệu môn học (PDF, DOCX). Bot chỉ trả lời trong phạm vi tài liệu được cung cấp và luôn kèm theo trích dẫn nguồn (tên file, số trang).

## Tính Năng Nổi Bật

- **Quản lý Môn học & Tài liệu:** Tạo môn học và upload PDF/DOCX vào từng môn.
- **Xử lý Ngầm (Background Job):** Tài liệu tải lên được tự động trích xuất, chia nhỏ (Text Chunking) và mã hoá thành vector trong nền.
- **Vector Search:** Mỗi chunk được embedding thành vector 768 chiều lưu trong PostgreSQL (`pgvector`), tìm kiếm bằng Cosine Similarity với HNSW Index.
- **Realtime Streaming Chat:** Dùng **SignalR** để stream từng token phản hồi của AI về giao diện (giống ChatGPT).
- **Trích Dẫn Thông Minh (Citations):** Cuối mỗi câu trả lời, Bot chỉ rõ tên file và số trang đã dùng làm ngữ cảnh.
- **Lịch sử Chat:** Hệ thống lưu và tải lại lịch sử hội thoại theo từng môn học.

## Kiến Trúc Kỹ Thuật

| Thành phần | Công nghệ |
|---|---|
| Backend Framework | ASP.NET Core MVC (.NET 8) |
| Cơ sở dữ liệu | PostgreSQL + `pgvector` (Docker) |
| ORM | Entity Framework Core |
| AI / LLM | Google AI Studio (`gemini-1.5-flash`) |
| Embedding Model | `text-embedding-004` (768 chiều) |
| Real-time | ASP.NET Core SignalR |
| File Parsing | `UglyToad.PdfPig` (PDF), `DocumentFormat.OpenXml` (DOCX) |
| Frontend | Razor Views + Tailwind CSS + ViewModels |
| Data Transfer | Data Transfer Objects (DTOs) |

## Cấu Trúc Dự Án (N-Tier Architecture)

```text
CHATBOTRAG/
├── RagChatbot.Presentation/   # Tầng Web (Controllers, Views, Hubs, JS/CSS)
├── RagChatbot.Business/       # Tầng Nghiệp Vụ (Services, Logic AI, RAG)
└── RagChatbot.DataAccess/     # Tầng Dữ Liệu (EF Core, Repositories, Migrations)
```

## Hướng Dẫn Cài Đặt & Khởi Chạy

### Bước 1: Cấu hình biến môi trường

Tạo file `.env` ở thư mục gốc (xem `.env.example` để tham khảo):

```env
DB_CONNECTION_STRING=Host=localhost;Port=5432;Database=RagChatbotDb;Username=postgres;Password=Password123!
GOOGLE_API_KEY=your_google_ai_studio_api_key_here
```

> Lấy API Key miễn phí tại [Google AI Studio](https://aistudio.google.com/apikey).

### Bước 2: Khởi động Database (Docker)

```bash
docker compose up -d
```

Lệnh này khởi chạy PostgreSQL với `pgvector` tại cổng `5432`.

### Bước 3: Chạy Ứng Dụng Web

```bash
cd RagChatbot.Presentation
dotnet run
```

Ứng dụng tự động chạy **EF Core Migration** để khởi tạo schema database ở lần đầu tiên.

### Bước 4: Truy cập

Mở trình duyệt tại `http://localhost:5000` (hoặc URL hiển thị trong console).

## Hướng Dẫn Sử Dụng

1. Vào tab **Documents** → Tạo môn học mới (ví dụ: `PRN222` - `Lập trình .NET`).
2. Upload file PDF/DOCX cho môn học. Trạng thái ban đầu là `Pending`.
3. Chờ ~10 giây để background job xử lý → Refresh trang → Trạng thái chuyển sang `Indexed`.
4. Quay lại tab **Chat** → Chọn môn học ở thanh bên trái → Bắt đầu hỏi!

> Xem chi tiết kiến trúc và tổ chức thư mục tại [PROJECT_STRUCTURE.md](./PROJECT_STRUCTURE.md).
> Khám phá toàn bộ luồng hoạt động chi tiết (Document Ingestion & RAG Chat Flow) tại [SYSTEM_FLOW_DETAILED.md](./SYSTEM_FLOW_DETAILED.md).
