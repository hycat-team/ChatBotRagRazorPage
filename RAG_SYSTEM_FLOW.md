# Tài Liệu Mô Tả Luồng Xử Lý RAG (Retrieval-Augmented Generation)

Tài liệu này chi tiết hóa luồng xử lý của hệ thống ChatBot RAG khi người dùng tương tác trên giao diện: chọn môn học (đổi tài liệu), tùy chỉnh các file lọc, và gửi câu hỏi (prompt).

---

## 1. Sơ đồ Luồng Hoạt Động (Flowchart)

```mermaid
graph TD
    %% Styling
    classDef client fill:#e0f2fe,stroke:#0284c7,stroke-width:2px,color:#0f172a;
    classDef server fill:#f0fdf4,stroke:#16a34a,stroke-width:2px,color:#0f172a;
    classDef db fill:#fef3c7,stroke:#d97706,stroke-width:2px,color:#0f172a;
    classDef external fill:#faf5ff,stroke:#7c3aed,stroke-width:2px,color:#0f172a;

    subgraph Client [Trình Duyệt - Giao Diện Người Dùng]
        A[Chọn Môn học mới] -->|JS: selectSubject| B[Reset Chat UI, hiển thị trạng thái chờ]
        B --> C[1. Gửi SignalR: LoadSubjectHistory]
        B --> D[2. Fetch HTTP: GetSubjectDocuments]
        D -->|Danh sách file| E[Hiển thị Bộ lọc Tài liệu ở sidebar phải]
        
        E -->|Bật/Tắt Checkbox| F[Cập nhật mảng selectedDocs]
        
        G[Nhập Prompt & bấm Gửi] --> H[Thêm bubble User Message & Hiển thị Thinking Indicator]
        H -->|Gửi SignalR: SendMessage| I[Truyền: SessionId, SubjectId, Message, selectedDocs]
    end

    subgraph Server [Backend ASP.NET Core - ChatHub]
        C -->|Hub: LoadSubjectHistory| J[Lấy lịch sử tin nhắn của Session thuộc Subject này]
        J -->|SignalR: SessionLoaded| B
        
        I -->|Hub: SendMessage| K{Có SessionId chưa?}
        K -->|Chưa có| L[Tạo ChatSession mới & gửi SignalR: SessionCreated]
        K -->|Đã có| M[Lưu tin nhắn User vào database]
        
        M --> N{Có phải câu chào đơn giản?}
        N -->|Không RAG| P[Bỏ qua Vector Search]
        
        N -->|RAG| O1[Rewrite Query LLM]
        O1 -->|Sliding Window History + Query| O2[Standalone Query]
        O2 --> O3[Gọi AiService.GenerateEmbeddingAsync]
        
        O3 --> Q[So sánh Vector cosine similarity]
        Q -->|Truy vấn lọc theo selectedDocs| R[(PostgreSQL + pgvector)]
        R -->|Lấy Top-K Chunks tương đồng| S{Zero Hallucination Policy}
        
        S -->|Chunks = 0| S1[Bắn thẳng Fallback Message]
        S -->|Chunks > 0| S2[Bơm Chunks vào Context]
        
        S2 & P --> T[Ghép context vào System Prompt Template (Grounding Rule)]
        T -->|Isolation Rule: history=null| U[Gọi LLM Streaming Generation]
        S1 --> W
    end

    subgraph LLM [AI Service - Streaming]
        U --> V[Stream phản hồi từng token]
        V -->|SignalR: ReceiveToken| W[Hiển thị chữ chạy real-time trên UI]
    end

    W -->|Kết thúc Stream| X[Đóng gói Trích dẫn & Lưu tin nhắn Model vào DB]

    class A,B,C,D,E,F,G,H,I,W client;
    class J,K,L,M,N,O,P,Q,S,T,U,X server;
    class R db;
    class V external;
```

---

## 2. Chi Tiết Các Kịch Bản Nghiệp Vụ

### Kịch Bản A: Người dùng chọn Môn học (Đổi Môn học & Đổi Tài liệu)

Khi người dùng nhấn chọn một môn học bất kỳ từ danh sách Sidebar trái:

1. **Giao diện Client (`Index.cshtml`):**
   * Hàm JavaScript `selectSubject(id, name, el)` được kích hoạt.
   * **Giao diện Chat** được làm mới: Clear màn hình chat cũ, thay đổi tiêu đề chat theo môn học hiện tại, đổi placeholder ở ô nhập prompt và kích hoạt các nút/ô nhập liệu.
   * **Nạp lịch sử trò chuyện (Chat History):** Gửi yêu cầu qua kết nối SignalR:
     ```javascript
     connection.invoke("LoadSubjectHistory", id);
     ```
     Server nhận yêu cầu, tìm bản ghi `ChatSession` gần nhất của người dùng cho môn học này, lấy danh sách `ChatMessage` cũ và đẩy ngược lại về giao diện qua sự kiện `SessionLoaded`.
   * **Nạp bộ lọc tài liệu (`loadDocFilter`):** Gửi yêu cầu HTTP GET đến endpoint:
     ```
     GET /Document/GetSubjectDocuments?subjectId={subjectId}
     ```
     API trả về danh sách toàn bộ các file tài liệu đã được chỉ mục hóa (indexed) thành công trong môn học đó.
     * **Sidebar phải (`#docFilterPanel`)** xuất hiện và vẽ danh sách checkbox tương ứng với các file.
     * Mặc định ban đầu, **tất cả** checkbox tài liệu đều được tích chọn (`checked`).

2. **Khi thay đổi tài liệu (Tích / Bỏ tích các checkbox tài liệu ở Sidebar phải):**
   * Mỗi hành động tích chọn/bỏ tích sẽ kích hoạt hàm `updateFilterInfo()`.
   * Mảng danh sách file được chọn sẽ được thu thập thông qua hàm `getSelectedDocIds()` bằng cách quét các checkbox đang được chọn:
     ```javascript
     const selectedDocs = [...document.querySelectorAll('.doc-checkbox:checked')].map(c => parseInt(c.value));
     ```
   * Dữ liệu này chỉ được lưu trữ tạm thời trên Client và sẽ được gửi lên Server mỗi khi người dùng gửi prompt mới.

---

### Kịch Bản B: Người dùng nhập Prompt & Gửi câu hỏi

Khi người dùng nhập văn bản vào ô chat và nhấn gửi (hoặc nhấn Enter):

1. **Giao diện Client gửi yêu cầu:**
   * Lấy prompt của người dùng và thêm ngay lập tức vào màn hình chat dưới dạng bubble tin nhắn của `user`.
   * Hiển thị trạng thái "AI đang xử lý" (Thinking Indicator) cùng hiệu ứng ba chấm chuyển động.
   * Thu thập danh sách `selectedDocs` (mảng ID các tài liệu đang được tích chọn).
   * Gửi gói tin thông qua kết nối SignalR đến `ChatHub`:
     ```javascript
     connection.send("SendMessage", currentSessionId, currentSubjectId, msg, selectedDocs);
     ```

2. **Server-side xử lý tại `ChatHub.cs` (`SendMessage`):**
   * **Bước 1: Khởi tạo/Xác thực Session:** Nếu chưa có Session ID (phiên chat mới), hệ thống tự động tạo một `ChatSession` mới trong DB và gửi ID về client qua sự kiện `SessionCreated`.
   * **Bước 2: Lưu tin nhắn của User:** Bản ghi tin nhắn mới của người dùng được lưu vào bảng `ChatMessage`.
   * **Bước 3: Lọc & Cấu trúc Truy vấn (Context Sliding Window & Rewrite Query):**
     * Hệ thống phân tích xem tin nhắn có phải câu chào hỏi xã giao ngắn không. Nếu đúng, bỏ qua bước tìm kiếm.
     * Nếu là câu hỏi kiến thức, hệ thống lấy đúng **3 tin nhắn gần nhất** (Sliding Window) kết hợp với câu hỏi hiện tại gửi cho LLM để viết lại thành một **Standalone Query** (Truy vấn độc lập).
     * Chuyển Standalone Query thành **Vector 768 chiều**.
     * So sánh Vector này với database (Cosine similarity) và áp dụng màng lọc trực tiếp `WHERE DocumentId IN (selectedDocs)`. Lấy ra Top-K Chunks tương đồng.
   * **Bước 4: Xác thực Hallucination & Tạo System Prompt (Grounding & Isolation Rule):**
     * **ZERO_HALLUCINATION_POLICY:** Nếu không tìm thấy Chunks nào phù hợp, hệ thống lập tức ngắt LLM, trả về Fallback: *"Hệ thống không tìm thấy thông tin trong các tài liệu đã chọn"*.
     * **GROUNDING_RULE:** Bơm Top-K Chunks vào System Prompt và ép LLM tuyệt đối chỉ ánh xạ 1:1 với dữ liệu này.
     * **ISOLATION_RULE:** Cô lập hoàn toàn lịch sử chat (`history = null`), chỉ dùng Standalone Query cho LLM sinh câu trả lời để tránh Data Leakage (tránh AI tự lấy lịch sử ra trả lời thay vì dùng tài liệu).
   * **Bước 5: Stream kết quả từ LLM (Low Temperature):**
     * Gọi API Gemini/OpenAI thông qua Semantic Kernel theo luồng streaming với tham số `temperature` cực thấp để đảm bảo tính chính xác và không sáng tạo thừa.
     * Với mỗi token trả về từ API, gọi `Clients.Caller.SendAsync("ReceiveToken", token, false)` để đẩy về client real-time.
   * **Bước 6: Hoàn tất & Ghi nhận Trích dẫn (Citation Injection):**
     * Khi kết thúc stream, server đóng gói toàn bộ nội dung AI cùng danh sách nguồn tham khảo (tên file, số trang) bằng JSON.
     * Gửi tín hiệu kết thúc kèm danh sách trích dẫn về client để hiển thị link tham khảo trực quan. Lên database ghi nhận thành message của `Model`.
