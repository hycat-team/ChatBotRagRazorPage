# ĐẶC TẢ QUY TẮC VÀ PHÂN QUYỀN HỆ THỐNG (SYSTEM RULES & RBAC)
**Dự án:** Hệ thống AI Chatbot RAG Giáo dục (EdTech)
**Mô hình:** Multi-tenant / Role-Based Access Control (RBAC)

---

## TỔNG QUAN HỆ THỐNG
Hệ thống AI Chatbot phục vụ truy xuất kiến thức (RAG) dành riêng cho môi trường học đường. Dữ liệu được phân mảnh và phân lập bảo mật theo phân cấp: Hệ thống -> Bộ môn -> Môn học -> Giảng viên -> Học liệu.

---

## MA TRẬN PHÂN QUYỀN NGƯỜI DÙNG (ACTORS)

| Tính năng / Quyền hạn | Admin | Trưởng bộ môn | Giảng viên | Học sinh |
| :--- | :---: | :---: | :---: | :---: |
| Quản lý Tài khoản toàn hệ thống | ✔ | - | - | - |
| Tạo và Quản lý Bộ môn | ✔ | - | - | - |
| Quản lý Môn học (trong Bộ môn) | ✔ | ✔ | - | - |
| Gán Giảng viên vào Môn học | ✔ | ✔ | - | - |
| Thêm/Xóa tài liệu học tập | ✔ | - | ✔ (Chỉ của mình) | - |
| Xem tài liệu trong Môn học | ✔ | ✔ (Tất cả) | ✔ (Chỉ của mình) | - |
| Bật/Tắt (Toggle) trạng thái tài liệu | ✔ | - | ✔ (Chỉ của mình) | - |
| Đổi tên hiển thị của tài liệu | ✔ | - | ✔ (Chỉ của mình) | - |
| Chat với AI và Lọc tài liệu | - | - | - | ✔ |

---

## ĐẶC TẢ USECASE CHI TIẾT

### Nhóm 1: Admin (Quản trị viên)
Đại diện cho phòng IT hoặc ban quản trị nhà trường, nắm toàn quyền kiểm soát cơ sở hạ tầng.
* **Quản lý Tài khoản:** Cấp phát, vô hiệu hóa, cập nhật thông tin tài khoản cho Trưởng bộ môn, Giảng viên và Học sinh.
* **Quản lý Bộ môn:** Khởi tạo các "Bộ môn" (Ví dụ: Công nghệ Thông tin, Kinh tế, Ngôn ngữ).
* **Toàn quyền (Super User):** Có thể can thiệp vào bất kỳ thực thể nào trong hệ thống khi cần thiết.

### Nhóm 2: Trưởng bộ môn (Head of Department)
Người quản lý chuyên môn của một Khoa/Bộ môn cụ thể.
* **Quản lý Môn học:** Tạo mới, chỉnh sửa thông tin, hoặc xóa các "Môn học" thuộc phạm vi Bộ môn mình quản lý.
* **Phân công Giảng dạy:** Gán quyền (Assign) các Giảng viên vào từng Môn học tương ứng.
* **Giám sát Học liệu (Read-only):** Được quyền **XEM** danh sách toàn bộ tài liệu do tất cả Giảng viên tải lên trong các môn học thuộc bộ môn của mình, nhưng không có quyền xóa hay chỉnh sửa tài liệu của Giảng viên.

### Nhóm 3: Giảng viên (Teacher)
Người trực tiếp cung cấp tri thức (Knowledge Base) cho AI.
* **Truy cập biệt lập:** Chỉ nhìn thấy các Môn học mà mình đã được Trưởng bộ môn gán vào.
* **Quản lý Tài liệu cá nhân:** Tải lên (Upload) hoặc Xóa tài liệu (PDF, Docx...) vào Môn học. Tuyệt đối **chỉ nhìn thấy và thao tác trên tài liệu do chính mình tải lên** (Isolated Workspace). Không nhìn thấy tài liệu của giảng viên khác dù dạy chung một môn.
* **Tùy biến hiển thị:** Đổi tên hiển thị (Display Name) của tài liệu trên giao diện hệ thống mà không làm thay đổi tên file vật lý gốc.
* **Kiểm soát trạng thái học liệu:** Bật/Tắt (Toggle Active/Inactive) tài liệu của chính mình. Khi tài liệu bị tắt, AI sẽ không đọc và không sử dụng dữ liệu từ tài liệu đó để trả lời Học sinh.

### Nhóm 4: Học sinh (Student)
Người dùng cuối (End-user) tiêu thụ tài nguyên AI.
* **Xác thực:** Đăng nhập bằng tài khoản được nhà trường cung cấp.
* **Trải nghiệm Chatbot:** Chọn một Môn học cụ thể để bắt đầu luồng hỏi đáp với AI.
* **Lọc không gian tri thức:** Trước hoặc trong khi chat, có quyền chọn (Filter) cụ thể các tài liệu muốn AI tham chiếu để trả lời, giúp thu hẹp phạm vi kiến thức và tăng độ chính xác.

---

## CÁC QUY TẮC NGHIỆP VỤ CỐT LÕI (CORE BUSINESS RULES)

* **Quy tắc Sở hữu Dữ liệu (Data Ownership Rule):** Tài liệu thuộc sở hữu của người tải lên. Giảng viên A không thể xem, xóa hay tắt/bật tài liệu của Giảng viên B.
* **Quy tắc Hiển thị (Visibility Rule):** Document khi được tải lên sẽ mặc định ở trạng thái Active (Bật). 
* **Quy tắc Bối cảnh AI (RAG Context Rule):** Khi Học sinh đặt câu hỏi trong một Môn học, hệ thống RAG chỉ được phép quét và truy xuất các tài liệu thỏa mãn ĐỒNG THỜI hai điều kiện:
    * Nằm trong môn học đó.
    * Đang ở trạng thái Active (Bật) bởi Giảng viên.

---

## ĐỊNH HƯỚNG MỞ RỘNG TƯƠNG LAI (FUTURE ROADMAP)

* **Tính năng Subscription / Giới hạn truy vấn:**
    * Tích hợp hệ thống đếm Token/Query cho lớp Học sinh.
    * Trường dữ liệu dự kiến: `RemainingQueries` (Số lượt hỏi còn lại), `SubscriptionPlan` (Gói cước: Free/Premium), `ResetCycle` (Chu kỳ làm mới: Ngày/Tháng).
    * Hệ thống tự động ngắt kết nối Chatbot và hiển thị thông báo nâng cấp gói khi Học sinh dùng hết lượt hỏi định mức.