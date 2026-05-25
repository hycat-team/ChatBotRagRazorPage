# RAG Chatbot System

Đây là một hệ thống RAG (Retrieval-Augmented Generation) Chatbot được xây dựng bằng **ASP.NET Core MVC (.NET 8)**, hỗ trợ tìm kiếm tài liệu theo ngữ cảnh và trả lời câu hỏi bằng AI với khả năng Stream realtime. Hệ thống tận dụng sức mạnh của **PostgreSQL** kết hợp với tiện ích mở rộng **pgvector** để lưu trữ và truy vấn Vector Embeddings.

## 🌟 Tính Năng Nổi Bật

*   **Quản Lý Tài Liệu & Môn Học:** Người dùng có thể tạo môn học và upload các tài liệu định dạng PDF, DOCX vào từng môn học.
*   **Xử Lý Background (Ngầm):** Các tài liệu tải lên sẽ được tự động trích xuất nội dung (sử dụng PdfPig và OpenXml) và chia nhỏ (Text Chunking).
*   **Vector Search:** Mỗi chunk văn bản được mã hóa (embedding) thành vector 1536 chiều và lưu trữ vào CSDL Postgres thông qua `pgvector`. Tính năng tìm kiếm sử dụng thuật toán tính khoảng cách Cosine Similarity (HNSW Index).
*   **Realtime Streaming Chat:** Sử dụng thư viện **SignalR** kết hợp với **Semantic Kernel** của Microsoft, cho phép chatbot stream từng kí tự trực tiếp về phía giao diện (giống như ChatGPT).
*   **Trích Dẫn Thông Minh (Citations):** Các câu trả lời của Bot đều dựa trên nội dung tài liệu người dùng đã đăng tải. Ở cuối câu trả lời, Bot sẽ trích dẫn chính xác trang và tên tài liệu mà nó đã sử dụng làm ngữ cảnh.
*   **Giao Diện Hiện Đại:** Sử dụng **Tailwind CSS** đem lại cảm giác tối giản, chuyên nghiệp và mượt mà.

## 🛠 Kiến Trúc Kỹ Thuật

*   **Backend Framework:** ASP.NET Core MVC (.NET 8)
*   **Cơ Sở Dữ Liệu:** PostgreSQL (chạy trong Docker container)
*   **ORM:** Entity Framework Core (hỗ trợ `pgvector`)
*   **AI Integration:** `Microsoft.SemanticKernel` (Tương thích với OpenAI, có thể cấu hình để chạy các mô hình nguồn mở như **Gemma 4 26B** qua các API của Groq, vLLM hoặc LM Studio).
*   **Document Parsers:** `UglyToad.PdfPig` (xử lý PDF), `DocumentFormat.OpenXml` (xử lý DOCX).
*   **Real-time Communication:** SignalR.

## 📁 Cấu Trúc Dự Án

*   `Controllers/`: Quản lý các endpoints điều hướng giao diện (Home, Document).
*   `Models/Entities/`: Cấu trúc dữ liệu trong Database (Subject, Document, DocumentChunk, ChatSession, ChatMessage).
*   `Services/`: Nơi chứa logic cốt lõi.
    *   `AiService`: Kết nối LLM và Embedding qua Semantic Kernel.
    *   `DocumentExtractionService`: Đọc chữ từ file.
    *   `TextChunkingService`: Cắt đoạn văn bản.
    *   `VectorSearchService`: Query vector tương đồng.
    *   `DocumentProcessingJob`: Worker ngầm tự động index tài liệu.
*   `Hubs/`: `ChatHub` chịu trách nhiệm mở WebSocket, nhận request và trả luồng (stream) kết quả về phía client.

## 🚀 Cách Khởi Chạy

Dự án sử dụng mô hình Hybrid: **Database chạy trong Docker** và **Application chạy dưới máy Host**.

1. Cấu hình file `.env` (bao gồm API Key và Connection String).
2. Chạy cơ sở dữ liệu: `docker compose up -d`.
3. Chạy ứng dụng web: `dotnet run` (tại thư mục `RagChatbot.Web`). Ứng dụng sẽ tự động chạy migrations để khởi tạo database ở lần đầu tiên.
