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
        
        N -->|RAG| O1[Bypass Rewrite Query]
        O1 -->|Giữ nguyên câu hỏi User| O2[Standalone Query]
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

    subgraph LLM [AI Service - Streaming & Retry]
        U --> V1{Gặp lỗi 500/429/Rỗng?}
        V1 -->|Có| V2[Tự động Retry tối đa 3 lần]
        V1 -->|Không| V[Stream phản hồi từng token]
        V2 --> V
        V -->|Tín hiệu Pause từ User| V3[Ngắt Stream ngay lập tức]
        V -->|SignalR: ReceiveToken| W[Hiển thị chữ chạy real-time trên UI]
        V3 --> W
    end

    W -->|Kết thúc Stream / Đã dừng tạo| X[Đóng gói Trích dẫn & Lưu tin nhắn Model vào DB]

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
   * **Bước 3: Lọc & Cấu trúc Truy vấn:**
     * Hệ thống phân tích xem tin nhắn có phải câu chào hỏi xã giao ngắn không. Nếu đúng, trả về câu chào mặc định bằng code (bỏ qua LLM để tiết kiệm token).
     * Bỏ qua bước gọi LLM viết lại câu hỏi (Rewrite Query bị vô hiệu hóa để tiết kiệm token), nên **Standalone Query** chính là nguyên bản câu hỏi của người dùng.
     * Chuyển Standalone Query thành **Vector 768 chiều**.
     * So sánh Vector này với database (Cosine similarity) và áp dụng màng lọc trực tiếp `WHERE DocumentId IN (selectedDocs)`. Lấy ra Top-K Chunks tương đồng.
   * **Bước 4: Xác thực Hallucination & Tạo System Prompt (Grounding & Isolation Rule):**
     * **ZERO_HALLUCINATION_POLICY:** Nếu không tìm thấy Chunks nào phù hợp, hệ thống lập tức ngắt LLM, trả về Fallback: *"Hệ thống không tìm thấy thông tin trong các tài liệu đã chọn"*.
     * **GROUNDING_RULE:** Bơm Top-K Chunks vào System Prompt và ép LLM tuyệt đối chỉ ánh xạ 1:1 với dữ liệu này.
     * **ISOLATION_RULE:** Cô lập hoàn toàn lịch sử chat (`history = null`), chỉ dùng Standalone Query cho LLM sinh câu trả lời để tránh Data Leakage (tránh AI tự lấy lịch sử ra trả lời thay vì dùng tài liệu).
   * **Bước 5: Stream kết quả từ LLM & Xử lý Lỗi/Dừng (Low Temperature):**
     * Gọi API Gemini/OpenAI thông qua Semantic Kernel theo luồng streaming.
     * Có cơ chế **Tự động Retry 3 lần** nếu Google API trả về lỗi 500/429 hoặc trả về dữ liệu rỗng.
     * Với mỗi token trả về từ API, gọi `Clients.Caller.SendAsync("ReceiveToken", token, false)` để đẩy về client real-time.
     * **Nút Dừng tạo (Pause):** Giao diện hiển thị nút Stop. Khi người dùng bấm, Server nhận tín hiệu hủy `CancellationToken`, ngắt luồng stream ngay lập tức và dán nhãn `*(Đã dừng tạo)*`.
   * **Bước 6: Hoàn tất & Ghi nhận Trích dẫn (Citation Injection):**
     * Khi kết thúc stream (hoặc bị ngắt bởi người dùng), server đóng gói toàn bộ nội dung AI đã sinh cùng danh sách nguồn tham khảo.
     * Gửi tín hiệu kết thúc kèm danh sách trích dẫn về client. Lên database ghi nhận thành message của `Model`.
