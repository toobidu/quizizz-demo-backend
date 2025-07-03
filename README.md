# 📚 Dự án Quiz Game Backend

Chào mừng bạn đến với **Quiz Game Backend**! 🎉 Đây là hệ thống backend được xây dựng bằng **.NET** (code thuần, không dùng framework), sử dụng **Docker** để chạy **Database** (PostgreSQL) và **Redis** để quản lý phân quyền dựa trên `user_id` và `permission`. Dự án lấy cảm hứng từ **Quizizz**, hỗ trợ các tính năng như đăng nhập, đăng ký, phòng chơi, chế độ chơi đơn hoặc battle, và bảng xếp hạng dựa trên điểm số và thời gian.

---

## 🚀 Tính năng chính

- **👤 Đăng nhập/Đăng ký**: Tạo tài khoản và đăng nhập để tham gia.
- **🏠 Phòng chơi (Room)**: Người dùng cùng `room_id` được nhóm vào một phòng.
- **🎮 Chế độ chơi**:
    - **Chơi đơn**: Trả lời câu hỏi một mình.
    - **Battle Mode**: Cạnh tranh với người chơi khác trong phòng.
- **❓ Trắc nghiệm**: Chọn đáp án đúng trong 4 lựa chọn (A, B, C, D).
- **🏆 Bảng xếp hạng**: Xếp hạng dựa trên **điểm số** và **thời gian trả lời**.
- **🔐 Phân quyền với Redis**: Lưu `user_id` và `permission` để quản lý vai trò (admin, player).
- **🗄️ Chuẩn 3NF**: Cơ sở dữ liệu được thiết kế độc lập, linh hoạt, giảm dư thừa.

---

## 🛠️ Công nghệ sử dụng

- **Backend**: .NET (code thuần, không dùng ASP.NET). 🖥️
- **Database**: PostgreSQL (chạy trên Docker). 🗃️
- **Redis**: Lưu `user_id` và `permission` cho phân quyền, session tạm thời. 🔑
- **Docker**: Quản lý và chạy các dịch vụ. 🐳
- **Chuẩn 3NF**: Thiết kế database tối ưu, dễ mở rộng. 📋

---

## 🖥️ Hướng dẫn cài đặt

### Yêu cầu
- **Docker** được cài đặt. 🐳
- **.NET SDK** (phiên bản mới nhất, ví dụ: .NET 8.0). 🛠️
- Máy tính có kết nối Internet để kéo image Docker. 🌐

### Các bước cài đặt
1. **Clone repository**:
   ```bash
   git clone <repository_url>
   cd quiz-game-backend
   ```
2. **Khởi chạy dịch vụ**:
    - Chạy database và Redis qua Docker:
      ```bash
      docker-compose up --build
      ```
3. **Kiểm tra**:
    - Truy cập `http://localhost:<port>/api/health` để kiểm tra trạng thái. ✅

---

## 📝 Lưu ý

- **Code thuần**: Backend sử dụng .NET thuần, không phụ thuộc framework như ASP.NET. 🖥️
- **Redis phân quyền**: Lưu `user_id` và `permission` (admin/player) để kiểm soát truy cập. 🔐
- **Chuẩn 3NF**: Các bảng database độc lập, không ràng buộc chặt chẽ, dễ mở rộng. 📋
- **Docker**: Đảm bảo database và Redis chạy ổn định trong container. 🐳
- **Hiệu năng**: Redis tối ưu cho phân quyền và leaderboard tạm thời với TTL. ⚡

---

## 🎮 Cách chơi

1. **Đăng ký/Đăng nhập**: Tạo tài khoản hoặc đăng nhập. 👤
2. **Tham gia phòng**: Nhập `room_id` hoặc tạo phòng mới. 🏠
3. **Chọn chế độ**:
    - **Chơi đơn**: Trả lời câu hỏi, tích điểm. 🎮
    - **Battle**: Cạnh tranh, trả lời nhanh và đúng. ⚔️
4. **Bảng xếp hạng**: Điểm số và thời gian được tính để xếp hạng trong phòng. 🏆

---