# SignalR Usage in RagChatbot

Dự án RagChatbot sử dụng **SignalR** để cung cấp các tính năng thời gian thực (real-time) cho người dùng, chủ yếu hỗ trợ trải nghiệm chat trực tuyến mượt mà và thông báo trạng thái của hệ thống.

## Các Hubs hiện có

Hệ thống hiện tại định nghĩa 2 Hub chính trong thư mục `Hubs` của lớp `PresentationRazorPage`:

### 1. `ChatHub` (`/chatHub`)
Đây là Hub cốt lõi phục vụ tính năng RAG Chatbot, xử lý toàn bộ luồng giao tiếp giữa người dùng và AI Assistant. 

**Các chức năng chính trong ChatHub:**
- **`LoadSubjectHistory(int subjectId)`**: Tải lại lịch sử trò chuyện của người dùng đối với một môn học (Subject) cụ thể.
- **`SendMessage(string sessionIdStr, int subjectId, string message, List<int>? documentIds)`**: Gửi tin nhắn mới tới AI. Hàm này sẽ:
  - Kiểm tra và trừ lượt truy vấn (Query Count) của người dùng (giới hạn số câu hỏi miễn phí mỗi ngày theo Role/Subscription).
  - Khởi tạo hoặc lấy ra phiên làm việc (Session).
  - Sinh embedding và tìm kiếm các phần văn bản liên quan (DocumentChunk) bằng `VectorSearchService`.
  - Nếu không có dữ liệu, trả về phản hồi mặc định. Ngược lại, xây dựng Prompt ngữ cảnh (Context) dựa trên tài liệu.
  - Sử dụng tính năng **Streaming** của mô hình AI: Hub sẽ liên tục đẩy các token (từng chữ) về phía client bằng SignalR Client Event: `ReceiveToken`. Client sẽ nhận từng token và hiển thị hiệu ứng typing realtime.
- **`StopGeneration(string sessionIdStr)`**: Hủy quá trình AI đang sinh câu trả lời bằng cách dùng `CancellationTokenSource`.

**Các Event Client lắng nghe (từ ChatHub):**
- `SessionLoaded`: Khi lịch sử chat đã tải xong.
- `SessionCreated`: Khi một phiên chat mới bắt đầu.
- `ReceiveToken`: Nhận token văn bản dạng stream từ AI.
- `ReceiveError`: Nhận thông báo lỗi (ví dụ: tài khoản bị khóa, hết lượt chat).

### 2. `AppNotificationHub` (`/appNotificationHub`)
Đây là Hub chung của hệ thống để quản lý và gửi các thông báo toàn cầu tới client, giúp đồng bộ hóa giao diện người dùng theo thời gian thực mà không cần tải lại trang.

- Hub này được cấu hình như một điểm kết nối để client lắng nghe các sự kiện chung của ứng dụng.
- Server gọi qua `IHubContext<AppNotificationHub>` từ các Background Job hoặc Controller/PageModel để đẩy (push) thông báo.

**Các Event Client lắng nghe (từ AppNotificationHub):**
- `DocumentListChanged`: Bắn ra khi có sự thay đổi về tài liệu. 
  - *Nơi gọi:* `DocumentProcessingJob` (khi xử lý xong tài liệu), `Index.cshtml.cs` của Document (khi người dùng upload, xóa, đổi tên, hoặc thay đổi trạng thái kích hoạt tài liệu).
  - *Mục đích:* Báo cho giao diện quản lý tài liệu tải lại danh sách tài liệu mới nhất.
- `SubjectListChanged`: Bắn ra khi có sự thay đổi về môn học.
  - *Nơi gọi:* `Subjects.cshtml.cs` của Admin (khi thêm, sửa, hoặc vô hiệu hóa môn học).
  - *Mục đích:* Báo cho giao diện quản lý môn học tải lại danh sách môn học mới nhất.

## Cấu hình (Configuration)

SignalR được tích hợp trong ứng dụng bằng các cấu hình ở `Program.cs`:

```csharp
// Đăng ký dịch vụ SignalR
builder.Services.AddSignalR();

// Map Endpoints cho SignalR
app.MapHub<RagChatbot.PresentationRazorPage.Hubs.ChatHub>("/chatHub");
app.MapHub<RagChatbot.PresentationRazorPage.Hubs.AppNotificationHub>("/appNotificationHub");
```

## Tổng kết
Nhờ có SignalR, Chatbot có thể mô phỏng luồng trò chuyện tương tự như ChatGPT thông qua khả năng trả về dữ liệu dạng luồng (streaming) ngay lập tức, cải thiện đáng kể UX so với việc dùng REST API thông thường.
