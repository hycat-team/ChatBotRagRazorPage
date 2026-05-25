# Hướng dẫn Cấu hình và Khởi chạy (Required Configurations)

Dự án RAG Chatbot đã được xây dựng thành công. Hiện tại, **chỉ có Cơ sở dữ liệu (PostgreSQL + pgvector)** là chạy trong Docker, còn Ứng dụng Web (.NET 8) sẽ chạy ở máy host của bạn (chạy bằng Visual Studio hoặc .NET CLI).

## 1. Cấu hình biến môi trường (`.env`)

Mở file `.env` ở thư mục gốc của dự án (`e:\FPT_EDUCATION\FPTCN7\PRN222\Assiment1\.env`) và điền các giá trị thích hợp:

```env
# Chuỗi kết nối Database trỏ tới localhost
DB_CONNECTION_STRING=Host=localhost;Port=5432;Database=RagChatbotDb;Username=postgres;Password=Password123!

# Mặc định, bạn có thể sử dụng endpoint của OpenAI (bỏ trống OPENAI_ENDPOINT)
# Hoặc nếu sử dụng Groq cho Gemma 27B, hãy điền endpoint tương ứng:
# OPENAI_ENDPOINT=https://api.groq.com/openai/v1

OPENAI_API_KEY=your_api_key_here

# Nếu dùng Groq với Gemma:
# OPENAI_CHAT_MODEL=gemma2-9b-it 
# (hoặc model gemma 26b/27b nếu Groq/vLLM có hỗ trợ, ví dụ: gemma-2-27b-it)

# Vector Embeddings:
# Groq hiện tại chưa hỗ trợ model embedding tốt. Bạn nên dùng text-embedding-3-small của OpenAI 
# hoặc cung cấp một OPENAI_EMBEDDING_MODEL tương ứng từ một provider khác.
```

## 2. Cách khởi chạy dự án

### Bước 1: Khởi động Cơ sở dữ liệu bằng Docker
Mở Terminal / PowerShell tại thư mục `e:\FPT_EDUCATION\FPTCN7\PRN222\Assiment1` và chạy lệnh sau:

```bash
docker compose up -d
```
Lệnh này sẽ khởi chạy Postgres SQL có chứa pgvector ở cổng `5432`.

### Bước 2: Khởi chạy Ứng dụng Web
Hệ thống đã được thiết lập để tự động chạy Migration (tạo bảng trong DB) khi ứng dụng `web` khởi động lần đầu tiên.

Mở thư mục dự án bằng **Visual Studio 2022** (mở file `RagChatbot.sln`), hoặc dùng Terminal đi vào thư mục web và chạy:
```bash
cd RagChatbot.Web
dotnet run
```

## 3. Truy cập ứng dụng

Mở trình duyệt và truy cập vào đường link hiển thị trong console (thường là `http://localhost:5000` hoặc `https://localhost:5001`).

### Quy trình sử dụng mẫu:
1. Chuyển sang tab **Documents** trên thanh điều hướng.
2. Tạo một môn học (Subject) mới, ví dụ: `PRN222` - `Lập trình .NET`.
3. Tải lên một file PDF/DOCX cho môn học đó. Trạng thái file sẽ là `Pending`.
4. Chờ khoảng 10 giây, background job sẽ tự động xử lý. Refresh trang, trạng thái file sẽ chuyển sang `Indexed`.
5. Chuyển về tab **Chat** (Trang chủ).
6. Click chọn môn học bạn vừa tạo ở thanh bên trái.
7. Bắt đầu chat! Bot sẽ phản hồi theo định dạng stream (từng chữ) và kèm theo trích dẫn (Citations).
