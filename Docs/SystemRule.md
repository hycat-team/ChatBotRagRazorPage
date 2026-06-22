# ĐẶC TẢ QUY TẮC & PHÂN QUYỀN HỆ THỐNG
## SYSTEM RULES & RBAC — EDTECH AI CHATBOT RAG

### 1. TỔNG QUAN HỆ THỐNG
* Hệ thống AI Chatbot phục vụ truy xuất kiến thức (RAG) được thiết kế theo mô hình quản lý học liệu mở và công khai (tương tự hệ thống FLM - Framework for Learning Management) của FPT. 
* Toàn bộ danh mục môn học, syllabus, slide bài giảng chuẩn và tài liệu tham khảo được công khai hoàn toàn cho toàn bộ Học sinh trong nhà trường nhằm thúc đẩy tinh thần tự học, tra cứu chéo và chuẩn bị bài trước kỳ học.
* Dữ liệu cấu trúc theo phân cấp quản lý: Hệ thống → Bộ môn → Môn học → Kho Học liệu Chuẩn (Active).
* Hệ thống áp dụng Multi-tenant / Role-Based Access Control (RBAC) ở lớp quản trị (Admin, Head of Department, Lecturer) nhằm phân định rõ trách nhiệm đóng góp dữ liệu, nhưng áp dụng cơ chế mở hoàn toàn đối với tầng end-user (Học sinh) để tối ưu khả năng tiếp cận tri thức.

### 2. MA TRẬN PHÂN QUYỀN (PERMISSION MATRIX)

| Tính năng / Quyền hạn | Admin | Head of Department | Lecturer | Học sinh |
| :--- | :---: | :---: | :---: | :---: |
| Quản lý tài khoản toàn hệ thống | ✔ | – | – | – |
| Tạo và quản lý Bộ môn | ✔ | – | – | – |
| Quản lý Môn học (trong Bộ môn) | - | ✔ | – | – |
| Gán Lecturer vào Môn học | - | ✔ | – | – |
| Thêm tài liệu học tập vào môn | – | ✔ | ✔ (Của mình) | – |
| Xóa tài liệu học tập khỏi môn | - | ✔ | ✔ (Của mình) | – |
| Xem danh sách tài liệu | - | ✔ (Bộ môn của mình) | ✔ (Của mình) | – (*2) |
| Bật / Tắt trạng thái tài liệu | - | ✔ (của mình) | ✔ (Của mình) | – |
| Đổi tên hiển thị tài liệu | - | ✔ (của mình) | ✔ (Của mình) | – |
| Xem toàn bộ danh mục môn học trường | – | – | – | ✔ (*3) |
| Chat với AI (Bất kỳ môn nào) | – | – | – | ✔ (*3) |
| Lọc tài liệu khi chat (Theo Chương/Tên) | – | – | – | ✔ (*2) |
| Xem lịch sử chat cá nhân theo môn | – | – | – | ✔ |
| Xem báo cáo / thống kê bộ môn | ✔ | ✔ | – | – |
| Xem giới hạn & quota hệ thống | ✔ | – | – | – |

**Chú thích:**
* ✔ = Có quyền | ✔ (Của mình) = Chỉ thao tác trên tài liệu do mình tạo | – = Không có quyền.
* (*1) Admin không trực tiếp thêm học liệu vào không gian môn học để bảo toàn tính chuyên môn của Lecturer/Bộ môn, nhưng giữ quyền Xóa để xử lý nội dung vi phạm.
* (*2) Học sinh không xem/tải trực tiếp file vật lý gốc hoặc path nội bộ, nhưng được xem bảng danh mục "Tên hiển thị tài liệu" (Syllabus, Slide chương 1,...) để lựa chọn cấu hình bộ lọc RAG khi chat.
* (*3) Cơ chế mở rộng: Học sinh không cần đăng ký (enroll) từng môn, hệ thống cho phép truy cập, tra cứu và hỏi đáp AI tự do trên toàn bộ danh mục môn học hiện hành.

### 3. ĐẶC TẢ USECASE CHI TIẾT

#### 3.1 Admin — Quản trị viên
* Nắm quyền kiểm soát hạ tầng, quản trị tài khoản và cấu trúc danh mục Bộ môn/Môn học ở mức cao nhất.
* **Quản lý Tài khoản**: Cấp phát, gia hạn hoặc vô hiệu hóa tài khoản của Head of Department, Lecturer, Học sinh.
* **Quản lý Bộ môn**: Khởi tạo, sửa đổi thông tin hoặc thực hiện xóa các Bộ môn trên toàn trường.
* **Super User**: Can thiệp, kiểm tra hệ thống, gỡ bỏ tài liệu lỗi hoặc override trạng thái học liệu khi có yêu cầu từ ban thanh tra trường học.

#### 3.2 Head of Department 
* Người kiểm soát nội dung và chất lượng đào tạo của một Bộ môn chuyên ngành cụ thể.
* **Quản lý Môn học**: Tạo mới, sửa đổi hoặc xóa mềm (archive) các Môn học thuộc Bộ môn phụ trách để công khai lên thư viện.
* **Phân công giảng dạy**: Gán hoặc thu hồi quyền đóng góp học liệu của các Lecturer vào từng Môn học tương ứng.
* **Giám sát Học liệu (Read-only)**: Xem danh sách và kiểm tra nội dung toàn bộ học liệu do các Lecturer tải lên. Head of Department không có quyền trực tiếp sửa hay tắt/bật tài liệu cá nhân của Lecturer nhưng có vai trò giám sát chuyên môn.

#### 3.3 Lecturer 
* Chuyên gia học thuật chịu trách nhiệm xây dựng Cơ sở tri thức (Knowledge Base) chuẩn cho AI Chatbot môn học.
* **Truy cập không gian gán**: Chỉ làm việc trên các Môn học được Head of Department phân công.
* **Quản lý học liệu cá nhân**: Upload (PDF, DOCX, Slide bài giảng chuẩn...) hoặc xóa tài liệu học tập của mình. Lecturer chỉ nhìn thấy và quản lý được các file do chính mình upload (Isolated Workspace), tránh việc sửa đổi nhầm lẫn file của Lecturer khác dạy chung môn.
* **Tùy biến tên hiển thị**: Đặt tên trực quan hiển thị ra ngoài giao diện cho Học sinh đọc (ví dụ: 'Slide bài giảng Chương 1 - Mạng máy tính') độc lập với tên file lưu trữ trên server.
* **Kiểm soát trạng thái học liệu**: Bật/Tắt (Toggle Active/Inactive) tài liệu. Khi tắt, hệ thống lập tức loại trừ tài liệu này ra khỏi bối cảnh tìm kiếm RAG của Học sinh.

#### 3.4 Học sinh — Student
* End-user thụ hưởng tài nguyên kiến thức công khai của hệ thống.
* **Tra cứu môn học tự do**: Sau khi xác thực bằng tài khoản trường, Học sinh được xem toàn bộ danh mục các bộ môn và môn học hiện có mà không cần điều kiện enroll.
* **Hỏi đáp AI linh hoạt**: Tự do chọn bất kỳ môn học nào để bắt đầu phiên hỏi đáp thời gian thực với trợ lý AI RAG chuyên môn.
* **Cấu hình bộ lọc tri thức**: Giao diện cung cấp bảng danh sách 'Tên hiển thị' của tất cả tài liệu chuẩn đang Active trong môn học. Học sinh có thể tích chọn lọc (Filter) cụ thể một hoặc vài tài liệu để giới hạn phạm vi AI trả lời (ví dụ: chỉ tra cứu trong phạm vi Giáo trình chuẩn chương 2). Hệ thống ẩn toàn bộ thông tin về tên file gốc, path lưu trữ và Lecturer upload.

### 4. CÁC QUY TẮC NGHIỆP VỤ CỐT LÕI (CORE BUSINESS RULES)

* **BR-01: Quy tắc Sở hữu Dữ liệu (Data Ownership Rule)** — Tài liệu thuộc sở hữu của người tải lên. Lecturer A không thể xem, xóa hay bật/tắt tài liệu của Lecturer B trong cùng một không gian môn học. Quy tắc này đảm bảo phân định rõ trách nhiệm đóng góp nội dung của từng cá nhân.
* **BR-02: Quy tắc Vòng đời & Trạng thái Học liệu (Lifecycle & State Rule)** — Học liệu khi tải lên hệ thống sẽ mặc định ở trạng thái kích hoạt. Tuy nhiên, luồng xử lý kỹ thuật bắt buộc phải trải qua trạng thái trung gian: Khi vừa upload, file giữ trạng thái [Processing]. Hệ thống tiến hành chunking và embedding; nếu thành công sẽ tự động chuyển đổi sang [Active] để RAG quét dữ liệu. Nếu tiến trình lỗi, chuyển sang trạng thái [Failed] và đẩy thông báo lỗi chi tiết ra màn hình của Lecturer sở hữu để kịp thời tải lại.
* **BR-03: Quy tắc Bối cảnh RAG môn học mở (RAG Open Context Rule)** — Khi Học sinh đặt câu hỏi tại giao diện một Môn học, hệ thống RAG thực thi quét và truy xuất kiến thức dựa trên các điều kiện đồng thời sau:
  * Quét toàn bộ dữ liệu nằm trong Môn học học sinh đang tương tác (Tuyệt đối không quét chéo môn).
  * Tài liệu phải ở trạng thái Active (Bật).
  * **Kỹ thuật Truy xuất Hybrid Search & Ranking:** Để tối ưu hóa độ chính xác và khả năng tìm kiếm, hệ thống AI Chatbot áp dụng cơ chế Hybrid Search (Tìm kiếm lai) kết hợp song song giữa tìm kiếm theo từ khóa (BM25) và tìm kiếm theo ngữ nghĩa (Vector Search). Sau khi tập hợp kết quả từ cả hai luồng, hệ thống sẽ đưa qua mô hình Ranking (Re-ranker) để chấm điểm và sắp xếp lại các Chunk dữ liệu theo mức độ liên quan chặt chẽ nhất với truy vấn của người dùng, trước khi nạp vào bối cảnh (context) cho LLM sinh câu trả lời. 
  * **Quy tắc Gộp kho tài liệu môn học chung**: Vì hệ thống public tài liệu chuẩn, bối cảnh RAG mặc định sẽ quét qua tài liệu Active của TẤT CẢ Lecturer được gán quyền đóng góp vào môn học đó. Kho tri thức sẽ được tổng hợp đầy đủ nhất cho Học sinh. Trong trường hợp Học sinh áp dụng tính năng bộ lọc tài liệu, RAG chỉ thực hiện truy xuất thông tin trong tập con các file được chọn.
  * **Quy tắc xử lý rỗng (Fallback)**: Nếu môn học chưa được tải lên tài liệu nào, hoặc toàn bộ tài liệu bị chuyển sang Inactive/Processing Failed, hệ thống CẤM gọi mô hình LLM tự do để ngăn chặn hiện tượng bịa đặt kiến thức (Hallucination). Hệ thống bắt buộc phải ngắt luồng và hiển thị thông báo: "Hiện tại môn học chưa có tài liệu học tập được kích hoạt trên hệ thống. Vui lòng quay lại sau hoặc liên hệ Bộ môn phụ trách để biết thêm chi tiết.".
* **BR-04: Quy tắc Tiếp cận Tự do (Open Access Rule)** — Mô hình mở rộng - Gỡ bỏ hoàn toàn rào cản Đăng ký môn học. Học sinh có toàn quyền tiếp cận, tra cứu danh mục học liệu và chat với AI tại tất cả các môn học có trên hệ thống mà không cần phê duyệt enroll từ Admin.
* **BR-05: Quy tắc Đồng bộ hóa Chỉ mục RAG (Vector Sync Rule)** — Đảm bảo tính chính xác tức thì cho dữ liệu tìm kiếm ngữ nghĩa:
  * Khi tài liệu bị chuyển sang trạng thái Inactive: Hệ thống ngay lập tức đánh cờ (flag) loại trừ tài liệu khỏi tập dữ liệu truy vấn RAG, có hiệu lực ngay ở câu hỏi tiếp theo của học sinh.
  * Khi tài liệu bị xóa vĩnh viễn: Hệ thống bắt buộc phải thực hiện lệnh gọi API xóa toàn bộ tập vector embedding tương ứng trên Vector Database trước khi tiến hành xóa bản ghi dữ liệu vật lý khỏi hệ thống.
* **BR-06: Quy tắc Xử lý Tháp dòng & Khóa tài khoản (Cascade & Alert Rule)** — Định nghĩa rõ luồng xử lý tháp dòng tự động bảo toàn tài nguyên hệ thống:
  * **Xóa Bộ môn / Môn học**: Toàn bộ học liệu liên quan lập tức bị gỡ vector chỉ mục khỏi RAG Index và chuyển toàn bộ file vật lý vào phân vùng Lưu trữ tạm (Archive) tối thiểu 30 ngày trước khi xóa sạch hoàn toàn.
  * **Vô hiệu hóa tài khoản Lecturer**: Khi tài khoản Lecturer đóng góp học liệu bị Admin khóa, hệ thống tự động gửi thông báo (Alert Notification) đến Admin và Head of Department phụ trách để đưa ra quyết định: Chuyển giao quyền sở hữu tài liệu (Transfer Ownership) cho Lecturer khác tiếp quản, hoặc chuyển toàn bộ học liệu của Lecturer đó sang Inactive/Archive nhằm tránh việc tài liệu mồ côi (orphaned data) tiếp tục vận hành tự động mà không có nhân sự kiểm soát.
  * **Vô hiệu hóa tài khoản Học sinh**: Đóng toàn bộ quyền truy cập hệ thống ngay lập tức; dọn dẹp các session hoạt động và đưa lịch sử chat vào vùng Archive bảo mật.
* **BR-07: Quy tắc Thời hạn Lưu trữ Lịch sử Chat (Chat Log Retention Rule)** — Lịch sử hội thoại của Học sinh được tổ chức hiển thị theo từng phiên chat (Session) cục bộ trong từng môn học. Nhằm tối ưu hóa tài nguyên lưu trữ và bộ nhớ đệm cho hệ thống RAG, thời hạn lưu trữ mặc định cho lịch sử chat của Học sinh hoạt động bình thường là 1 Học kỳ (tương đương 6 tháng). Hết thời hạn trên, hệ thống chạy cron job tự động chuyển các phiên chat cũ vào phân vùng Lưu trữ lịch sử (Archived Log) và không tải lên UI sử dụng hàng ngày.

### 5. QUY TẮC BẢO MẬT & PHI TIẾT LỘ

#### 5.1 Audit Log (Nhật ký kiểm toán)
* Hệ thống ghi nhận tập trung, ghi đè/xóa là bất khả thi đối với các thao tác cấu trúc dữ liệu chính:
* **Hành động của Admin**: Cấp/khóa tài khoản, tạo/xóa Bộ môn, các thao tác override hệ thống.
* **Hành động của Head of Department**: Khởi tạo môn học, cấu hình xóa môn học, gán hoặc thu hồi quyền giảng dạy của Lecturer.
* **Hành động của Lecturer**: Các thao tác Upload file, Xóa file, Thay đổi tên hiển thị học liệu, và tắt/bật trạng thái Active/Inactive.
* Mỗi log record bắt buộc bao gồm: Actor ID, Timestamp, Target Object ID, và Chi tiết hành động thực hiện để phục vụ công tác rà soát lỗi hệ thống.

#### 5.2 Data Isolation (Phân lập dữ liệu Bộ môn)
* Hệ thống cam kết kiểm soát tuyệt đối luồng dữ liệu (Zero cross-tenant leak). 
* Tri thức thuộc không gian Bộ môn A không bao giờ xuất hiện trong kết quả trả về của mô hình RAG thuộc Bộ môn B. 
* Học sinh và Lecturer bị chặn quyền tương tác trực tiếp với tầng cơ sở dữ liệu Vector (Vector DB APIs) mà bắt buộc phải thông qua lớp xác thực (Authorization/Middleware Layer) của hệ thống Server.

#### 5.3 Quy định Hiển thị Trích dẫn Nguồn (Citation Constraints)
* Khi AI Chatbot đưa ra câu trả lời cho Học sinh dựa trên tài liệu, hệ thống bắt buộc phải render trích dẫn nguồn ngay dưới câu trả lời để phục vụ việc đối chiếu kiểm chứng. 
* Định dạng hiển thị được chuẩn hóa nghiêm ngặt:
  * **Nguồn tham chiếu:** [Tên hiển thị học liệu do GV đặt] — Đoạn tham chiếu [Số]
* Hệ thống tuyệt đối bảo mật, không hiển thị ra ngoài giao diện Học sinh các thông tin: Tên file vật lý gốc (.pdf/.docx lưu trên server), Danh tính Lecturer tải lên file đó, và Cấu trúc đường dẫn thư mục lưu trữ nội bộ.

### 6. ĐỊNH HƯỚNG MỞ RỘNG TƯƠNG LAI (FUTURE ROADMAP)
* **6.1 Hạn mức Tài nguyên & Quota truy vấn (Học sinh)**: Thiết lập cơ chế đếm số lượt câu hỏi (Query rate limiting) hoặc kiểm soát dung lượng token tiêu thụ theo ngày của tài khoản Học sinh nhằm kiểm soát bài toán chi phí vận hành API dịch vụ LLM. Cấu hình dự kiến: RemainingQueries, DailyQuotaLimits, ResetCycle (Tự động làm mới vào lúc 00:00 hàng ngày).
* **6.2 Giới hạn Dung lượng Lưu trữ Học liệu (Lecturer)**: Giới hạn dung lượng lưu trữ học liệu tối đa mà một Lecturer được phép tải lên hệ thống môn học để chống quá tải bộ nhớ vật lý của Server. Các trường dữ liệu dự kiến: UsedStorage (MB), MaxStorageCapacity (MB), tự động bắn thông báo cảnh báo khi đạt ngưỡng dung lượng 80%.
* **6.3 Thống kê & Dashboard Thông minh**: Xây dựng hệ thống bảng biểu trực quan: Head of Department theo dõi được xu hướng tự học của sinh viên, top các môn học hoặc chương tài liệu được đặt câu hỏi nhiều nhất để đánh giá độ khó kiến thức; Admin quản trị hiệu năng và kiểm soát chi phí API toàn trường.

### 7. BẢNG ƯU TIÊN TRIỂN KHAI (IMPLEMENTATION ROADMAP)

| Mức độ ưu tiên | Hạng mục triển khai | Chi tiết kỹ thuật / Luồng nghiệp vụ |
| :--- | :--- | :--- |
| 🔴 CAO | Cấu hình luồng mở danh mục & Fallback (BR-03, BR-04) | Mở cổng truy cập tự do cho Học sinh vào mọi môn học. Xây dựng bộ lọc RAG theo kho tài liệu gộp chung của môn học, tích hợp hàm chặn ngắt luồng (Fallback rỗng) khi môn học trống học liệu. |
| 🔴 CAO | Đồng bộ hóa Vector DB & Vòng đời (BR-02, BR-05) | Xử lý triệt để trạng thái học liệu [Processing] -> [Active/Failed]. Đảm bảo khi Lecturer tắt/xóa học liệu, chỉ mục Vector DB được cập nhật realtime. |
| 🔴 CAO | Cảnh báo Khóa tài khoản Lecturer (BR-06) | Xây dựng luồng thông báo tự động cho Admin/Head of Department để xử lý tập tài liệu mồ côi ngay khi vô hiệu hóa tài khoản Lecturer đóng góp. |
| 🟡 TRUNG BÌNH | Hệ thống ghi nhận Audit Log toàn diện (Section 5.1) | Thiết lập hệ thống ghi nhật ký không thể sửa xóa cho mọi hành động cấu trúc của Admin và Head of Department. |
| 🟡 TRUNG BÌNH | Bộ lọc UI Học sinh & Citation định dạng (5.3, 3.4) | Xây dựng giao diện cho Học sinh tích chọn bộ lọc dựa trên Tên hiển thị học liệu; hiển thị citation chuẩn hóa giấu tên file gốc. |
| 🟢 THẤP | Rate Limiting & Quota Token theo ngày (Section 6.1) | Tích hợp bộ đếm câu hỏi tự động refresh theo chu kỳ ngày để chống spam tài nguyên API. |
| 🟢 THẤP | Cron Job dọn dẹp và Lưu trữ Lịch sử chat (BR-07) | Cài đặt cron tác vụ tự động quét dữ liệu định kỳ, chuyển các session chat quá 6 tháng vào phân vùng Archived Log. |# Danh sách Tài khoản Hệ thống

Dưới đây là các tài khoản mặc định (Seed Data) đã được khởi tạo sẵn trong cơ sở dữ liệu của dự án. 

| STT | Vai trò (Role) | Email | Mật khẩu mặc định | Ghi chú |
| :--- | :--- | :--- | :--- | :--- |
| 1 | **Admin** | `admin@gmail.com` | `@Admin1` | Có toàn quyền quản trị hệ thống. |
| 2 | **Trưởng Bộ môn** | `hod@gmail.com` | `@Hod1` | Được tự động gán vào bộ môn **Công nghệ Thông tin**. |
| 4 | **Học sinh 1** | `student1@gmail.com` | `@Cus1` | Tài khoản học sinh để hỏi đáp tài liệu. |
| 5 | **Học sinh 2** | `student2@gmail.com` | `@Cus2` | Tài khoản học sinh để hỏi đáp tài liệu. |

> [!NOTE]
> Mọi tài khoản mới được tạo ra từ màn hình của Admin thông qua chức năng **Tạo Tài khoản (bằng file)** hoặc **Tạo HOD** thì đều có chung mật khẩu mặc định là: `123456`.
>
> Khi ấn "Đặt lại mật khẩu" trong trang quản trị, tài khoản đó cũng sẽ quay về mật khẩu mặc định: `123456`.
