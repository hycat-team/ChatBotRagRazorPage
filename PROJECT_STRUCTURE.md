# Cấu Trúc Kiến Trúc & Cây Thư Mục Dự Án RagChatbot

Tài liệu này mô tả chi tiết kiến trúc phân lớp (N-Tier) và cây thư mục của dự án **RagChatbot** sau khi đã được Refactor chuẩn hóa.

## 1. Cây Thư Mục (Directory Tree)

Dự án được chia thành 3 Project riêng biệt đại diện cho 3 tầng của kiến trúc N-Tier.

```text
CHATBOTRAG
│
├── RagChatbot.Presentation/   (Tầng Giao diện & Điều hướng)
│   ├── Controllers/           (Xử lý HTTP requests: HomeController, DocumentController...)
│   ├── Hubs/                  (Xử lý WebSockets theo thời gian thực: ChatHub)
│   ├── Views/                 (Giao diện người dùng Razor HTML/CSS)
│   ├── wwwroot/               (Chứa các file tĩnh: CSS, JS, thư viện ngoài...)
│   ├── Properties/            (Chứa launchSettings.json)
│   └── Program.cs             (Nơi cấu hình Middleware, Dependency Injection)
│
├── RagChatbot.Business/       (Tầng Xử Lý Nghiệp Vụ - Logic Layer)
│   ├── Interfaces/            (Định nghĩa hợp đồng: IDocumentService, ITextChunkingService...)
│   └── Services/              (Thực thi Logic: ChatService, AiService, TextChunkingService,
│                                DocumentProcessingJob, VectorSearchService...)
│
└── RagChatbot.DataAccess/     (Tầng Truy Xuất Dữ Liệu - Data Access Layer)
    ├── Data/                  (ApplicationDbContext: Cấu hình Entity Framework Core)
    ├── EntityModels/          (Khai báo các thực thể/Bảng DB: Document, ChatSession...)
    ├── Interfaces/            (Định nghĩa hợp đồng cho Repository: IRepository...)
    ├── Repositories/          (Triển khai truy vấn DB: DocumentRepository, AppUserRepository...)
    └── Migrations/            (Lịch sử phiên bản lược đồ CSDL - Database Schema)
```

## 2. Sơ Đồ Liên Kết Kiến Trúc (Architecture Diagram)

Dự án tuân thủ chặt chẽ mô hình **Clean Architecture / N-Tier**. Dữ liệu chỉ chảy theo một chiều từ trên xuống dưới thông qua **Dependency Injection (DI)**, giúp các tầng độc lập với nhau, dễ dàng nâng cấp và viết Unit Test.

```mermaid
graph TD
    %% Định nghĩa các lớp
    subgraph PresentationLayer ["1. Tầng Trình Diễn (RagChatbot.Presentation)"]
        UI["Trình Duyệt (Views / JS / CSS)"]
        Ctrl["Controllers (HTTP)"]
        Hub["ChatHub (SignalR)"]

        UI <-->|HTTP Request/Response| Ctrl
        UI <-->|WebSockets| Hub
    end

    subgraph BusinessLayer ["2. Tầng Nghiệp Vụ (RagChatbot.Business)"]
        ISrv["Các Service Interfaces \n(ISubjectService, IChatService, ...)"]
        Srv["Các Service Implementations \n(Xử lý Logic, Xoay vòng API, RAG)"]

        ISrv -.->|Implement| Srv
    end

    subgraph DataAccessLayer ["3. Tầng Dữ Liệu (RagChatbot.DataAccess)"]
        IRepo["Các Repository Interfaces \n(IRepository)"]
        Repo["Các Repositories \n(Gọi EF Core)"]
        EFCore["Entity Framework Core \n(ApplicationDbContext)"]

        IRepo -.->|Implement| Repo
        Repo --> EFCore
    end

    subgraph External ["4. Các Dịch Vụ Bên Ngoài (External APIs)"]
        GoogleAI["Google AI Studio \n(Gemma 26B, Gemini Embedding)"]
        Drive["Google Drive \n(Lưu Trữ File PDF)"]
        Postgres["PostgreSQL + pgvector \n(Vector Database)"]
    end

    %% Thiết lập luồng dữ liệu / Dependency
    Ctrl -->|Tiêm phụ thuộc (DI)| ISrv
    Hub -->|Tiêm phụ thuộc (DI)| ISrv

    Srv -->|Tiêm phụ thuộc (DI)| IRepo
    Srv -->|HTTP/SDK| GoogleAI
    Srv -->|API| Drive

    EFCore -->|SQL/TCP| Postgres

    %% Style
    classDef pres fill:#e1f5fe,stroke:#03a9f4,stroke-width:2px;
    classDef bus fill:#e8f5e9,stroke:#4caf50,stroke-width:2px;
    classDef data fill:#fff3e0,stroke:#ff9800,stroke-width:2px;
    classDef ext fill:#f3e5f5,stroke:#9c27b0,stroke-width:2px;

    class PresentationLayer pres;
    class BusinessLayer bus;
    class DataAccessLayer data;
    class External ext;
```

### Giải thích Luồng Hoạt Động (Flow)

1. **Người dùng** thao tác trên trình duyệt (Upload file, gửi tin nhắn).
2. Yêu cầu được gửi đến **Controllers** (nếu upload) hoặc **ChatHub** (nếu chat).
3. Controllers/Hub không tự xử lý mà gọi xuống **Tầng Business** (ví dụ: `IDocumentService`, `IAiService`, `ITextChunkingService`) để thực thi logic nghiệp vụ.
4. **Xử lý Chunking thông minh:** `DocumentProcessingJob` điều phối quy trình xử lý tài liệu ngầm. Khi ghép nối text giữa các trang, nó dùng thuật toán quét ngược thông minh bỏ qua dấu chấm số (numeric period) để tránh cắt ngang các con số tài chính. Sau đó, `TextChunkingService` mask toàn bộ dấu chấm giữa chữ số bằng `ALPHANUMERICDOTMASK` trước khi chia nhỏ, đảm bảo tính toàn vẹn 100% cho các số như `43.000` hay `10.000.000`.
5. Nếu Tầng Business cần đọc/ghi dữ liệu, nó sẽ gọi xuống **Tầng Data Access** thông qua các `Repository`. Tầng Data Access sẽ dùng Entity Framework Core để biến đổi code thành lệnh SQL chạy trên PostgreSQL.
6. Nếu Tầng Business cần xử lý AI hoặc lưu trữ Cloud, nó sẽ gọi ra các API bên ngoài như Google AI Studio hoặc Google Drive.

## 3. Kiến Trúc Giao Tiếp Mức Cao (High-level Communication Flow)

Dưới đây là sơ đồ luồng giao tiếp hệ thống mức cao giữa các tầng (Layers) và các dịch vụ bên ngoài (External Services) bằng plaintext.

```text
    [Người dùng cuối]
       │       ▲
 (Nhập liệu)   │ (Hiển thị / Phản hồi)
       ▼       │
┌─────────────────────────────────────────────────────────┐
│               PRESENTATION LAYER (WebMVC & SignalR)     │
│                                                         │
│   [View / UI] <────(Dữ liệu)────> [Controller / Hub]    │
└───────────────────────┬─────────────────▲───────────────┘
                        │                 │
             (Truyền tham số / DTO)  (Trả về DTO / ViewModel)
                        │                 │
                        ▼                 │
┌─────────────────────────────────────────────────────────┐
│                BUSINESS LAYER (Services)                │
│                                                         │
│                     [Services]                          │
│           (ChatService, DocumentService...)             │
└──────┬────────────────┬─────────────────▲───────────────┘
       │                │                 │
       │         (Gọi hàm xử lý)   (Trả về Entity)
       │                │                 │
       │                ▼                 │
       │ ┌──────────────────────────────────────────────┐
       │ │            DATA ACCESS LAYER (DAL)           │
       │ │                                              │
       │ │                [Repositories]                │
       │ │                      ↕                       │
       │ │                 [DbContext]                  │
       │ └──────────────────────┬───────────────▲───────┘
       │                        │               │        
(Gọi API bên ngoài)      (Gửi câu lệnh SQL) (Dữ liệu thô)
       │                        │               │        
       ▼                        ▼               │        
┌────────────────┐       ┌──────────────────────┴─────────┐
│ [External APIs]│       │                                │
│ - Google AI    │       │       [(Cơ sở dữ liệu)]        │
│ - Google Drive │       │     PostgreSQL + pgvector      │
└────────────────┘       └────────────────────────────────┘
```
