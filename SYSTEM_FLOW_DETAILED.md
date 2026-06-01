# Chi Tiết Luồng Hoạt Động Của Hệ Thống RAG Chatbot

Tài liệu này mô tả chi tiết từ đầu đến cuối (End-to-End) cách hệ thống hoạt động đối với hai luồng nghiệp vụ chính: **Xử lý tài liệu (Document Ingestion)** và **Truy vấn - Trả lời (RAG Chat flow)**.

---

## 1. Luồng Xử Lý Tài Liệu (Document Ingestion Flow)

Khi giảng viên / sinh viên tải lên một tài liệu (PDF, DOCX) vào hệ thống, các bước sau sẽ diễn ra:

### Bước 1: Tiếp nhận file từ người dùng
*   **Thành phần:** `DocumentController` (Tầng Presentation)
*   **Hoạt động:** Người dùng chọn file và ấn Upload. `DocumentController` nhận file qua HTTP POST.
*   **Lưu trữ:** File được `IGoogleDriveService` đẩy lên Google Drive. Nếu lỗi, nó fallback lưu ở Local Storage của server.
*   **Ghi nhận Database:** Thông tin file (Tên, Đường dẫn, SubjectId) được tạo dưới dạng bản ghi `Document` lưu vào PostgreSQL với trạng thái `Status = "Pending"`.

### Bước 2: Kích hoạt Background Job
*   **Thành phần:** `DocumentProcessingJob` (Tầng Business - Background Service)
*   **Hoạt động:** 
    *   Job này chạy định kỳ (vd: mỗi 10 giây). Nó sẽ quét database tìm các tài liệu đang ở trạng thái `Pending`.
    *   Khi tìm thấy, nó cập nhật trạng thái thành `Processing`.

### Bước 3: Trích xuất nội dung (Parsing)
*   **Thành phần:** `PdfPig` (thư viện cho PDF) hoặc `OpenXml` (thư viện cho DOCX)
*   **Hoạt động:** Background Job tải file về server (nếu nằm trên Drive) và bắt đầu đọc. Nó trích xuất toàn bộ text, đồng thời theo dõi văn bản đó thuộc **Trang số mấy (Page Number)**.

### Bước 4: Chia nhỏ văn bản (Text Chunking)
*   **Thành phần:** Thuật toán Text Chunking (trong `DocumentProcessingJob`)
*   **Hoạt động:** Văn bản dài hàng trăm trang được chia thành các đoạn nhỏ (Chunks) mang ngữ nghĩa đầy đủ. 
    *   Kích thước mỗi chunk: Tối đa khoảng 1000 ký tự.
    *   Overlap (chồng lấn): Có một phần văn bản lặp lại giữa chunk trước và chunk sau để giữ trọn vẹn ngữ cảnh.

### Bước 5: Chuyển đổi thành Vector (Embedding)
*   **Thành phần:** `IAiService` -> Google AI Studio (`text-embedding-004`)
*   **Hoạt động:** 
    *   Mỗi chunk văn bản được gửi lên API của Google AI.
    *   Google trả về một **Vector 768 chiều** (một mảng gồm 768 con số thập phân biểu diễn ý nghĩa/ngữ nghĩa của đoạn văn bản đó).

### Bước 6: Lưu trữ Vector
*   **Thành phần:** `IVectorSearchService` -> EF Core -> PostgreSQL (`pgvector`)
*   **Hoạt động:** Đoạn văn bản (text), số trang (page index), và Vector (768 chiều) được lưu thành bản ghi `DocumentChunk` vào database. 
    *   Trạng thái tài liệu cập nhật thành `Indexed`.

---

## 2. Luồng Truy Vấn & Chat (RAG Chat Flow)

Khi người dùng nhắn tin (đặt câu hỏi), luồng sau sẽ diễn ra:

### Bước 1: Gửi tin nhắn qua SignalR
*   **Thành phần:** UI (Trình duyệt) -> `ChatHub` (Tầng Presentation)
*   **Hoạt động:** Giao diện gọi hàm `SendMessage(sessionId, subjectId, message, selectedDocs)` thông qua WebSockets (SignalR).

### Bước 2: Xử lý và Lưu tin nhắn người dùng
*   **Thành phần:** `ChatHub` -> `IChatService`
*   **Hoạt động:** Tin nhắn của User được lưu vào DB (`ChatMessage`) với Role là `User`. 

### Bước 3: Tiền xử lý & Cấu trúc Truy vấn (Bypass Rewrite Query)
*   **Thành phần:** `IAiService` (Query Rewriting)
*   **Hoạt động:**
    1.  **Lấy bối cảnh:** Trích xuất 3 tin nhắn gần nhất từ lịch sử trò chuyện để sử dụng nếu cần thiết.
    2.  **Viết lại câu hỏi (Đã vô hiệu hóa):** Để tiết kiệm token LLM, hệ thống hiện tại **bỏ qua bước viết lại câu hỏi**. Câu hỏi gốc của người dùng được sử dụng trực tiếp làm Standalone Query.
    3.  **Lọc câu chào hỏi:** Nếu câu hỏi chỉ là câu chào ngắn gọn (vd: "chào bạn"), hệ thống tự động trả lời bằng code mà không cần RAG hay LLM.

### Bước 4: Đánh giá & Lọc dữ liệu (Vector Search)
*   **Thành phần:** `IAiService` (Embedding) và `IVectorSearchService` (Tìm kiếm)
*   **Hoạt động:**
    1.  **Embed Câu hỏi:** Standalone Query được biến thành một Vector 768 chiều.
    2.  **Pre-filtering & Similarity:** Vector câu hỏi được đưa xuống PostgreSQL. Extension `pgvector` sử dụng HNSW index so sánh với các DocumentChunks, đồng thời áp dụng màng lọc cứng `WHERE DocumentId IN (selectedDocs)`.
    3.  **Lấy Top K:** PostgreSQL trả về Top các đoạn văn bản (chunks) tương đồng nhất.

### Bước 5: Kiểm soát rủi ro & Tạo Prompt (Zero Hallucination & Grounding)
*   **Thành phần:** `IChatService`
*   **Hoạt động:** 
    *   **ZERO_HALLUCINATION_POLICY:** Nếu không có chunk nào tương đồng, hệ thống không gọi LLM sinh văn bản mà trả về ngay câu Fallback: *"Hệ thống không tìm thấy thông tin trong các tài liệu đã chọn"*.
    *   **GROUNDING_RULE:** Bơm các chunks tìm được vào ngữ cảnh. Ép LLM chỉ được phép trả lời dựa trên những chunks này.

### Bước 6: Sinh câu trả lời (LLM Streaming, Retry & Isolation Rule)
*   **Thành phần:** `IAiService` -> Google AI Studio
*   **Hoạt động:**
    *   **ISOLATION_RULE:** Tách biệt hoàn toàn lịch sử chat (`history = null`) khỏi bước sinh văn bản cuối cùng để tránh hiện tượng Data Leakage (AI học lỏm kiến thức từ chat history).
    *   **Cơ chế Retry (Kháng lỗi):** LLM xử lý Standalone Query dựa trên ngữ cảnh được cấp. Nếu API của Google trả về lỗi 500, 429 hoặc trả về văn bản rỗng, hệ thống sẽ **tự động gọi lại (Retry) tối đa 3 lần** trước khi bỏ cuộc.
    *   LLM stream trả về từng token.

### Bước 7: Đẩy dữ liệu về Giao diện & Dừng tạo (Real-time Streaming & Pause)
*   **Thành phần:** `ChatHub` -> UI
*   **Hoạt động:** 
    *   Mỗi khi LLM sinh ra một token, `ChatHub` gửi ngay lập tức tín hiệu `ReceiveToken` về giao diện.
    *   Giao diện hiển thị chữ chạy giống hệt ChatGPT mà không cần đợi load xong toàn bộ.
    *   **Dừng tạo (Pause):** Tại bất kỳ thời điểm nào, người dùng có thể bấm nút "Dừng tạo". SignalR sẽ gửi lệnh hủy thông qua `CancellationToken`, ngắt ngay lập tức tiến trình stream từ API của Google, lưu trữ câu trả lời dang dở và báo *"(Đã dừng tạo)"*.

### Bước 8: Xử lý Trích dẫn & Lưu trữ (Citation Injection)
*   **Thành phần:** `IChatService` -> `ChatHub`
*   **Hoạt động:**
    *   Bắt buộc đính kèm Metadata (Tên file, Index trang) vào các node thông tin trả về.
    *   Lưu toàn bộ tin trả lời của AI cùng JSON trích dẫn vào database với Role là `Model`.
    *   Gửi tín hiệu kết thúc về giao diện kèm theo danh sách trích dẫn để hiển thị.

---

## Tóm Lược Các Công Nghệ Trọng Điểm Tham Gia Vào Luồng

1.  **Dữ liệu luân chuyển (DTOs / ViewModels):** Tất cả dữ liệu đi từ Database lên giao diện đều được bọc trong các Data Transfer Objects (DTO) tại Business Layer, và bọc trong ViewModels tại Presentation Layer để bảo mật và tối ưu giao diện.
2.  **Entity Framework Core & pgvector:** Nóng cốt của hệ thống RAG nằm ở khả năng lưu trữ mảng vector và tính toán khoảng cách (distance) trực tiếp bằng ngôn ngữ SQL thông qua EF Core.
3.  **SignalR:** Đảm nhận kết nối 2 chiều (Duplex) giúp trải nghiệm chat cực kỳ mượt mà, không bị giật lag khi tải các câu trả lời dài.
4.  **Kiến trúc N-Tier:** Việc chia tách giúp code cực kỳ gọn gàng. `DocumentProcessingJob` không dính dáng gì đến Controllers. Việc thay đổi AI từ Google sang OpenAI trong tương lai chỉ cần viết lại `AiService` mà không ảnh hưởng tới bất kỳ thành phần nào khác.
