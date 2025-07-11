# Hướng dẫn sử dụng User Profile API

## Vấn đề đã được giải quyết

**Vấn đề trước đây**: Khi xem hồ sơ cá nhân của bản thân thì mới được chỉnh sửa, còn khi xem hồ sơ cá nhân người khác thì không được phép sửa trang cá nhân của họ.

**Giải pháp**: Đã thêm logic phân quyền và thuộc tính `IsOwnProfile` để frontend biết khi nào hiển thị nút chỉnh sửa.

## API Endpoints

### 1. Xem profile của chính mình
```
GET /api/profile/me
Authorization: Bearer <token>
```
**Response**: 
- `IsOwnProfile: true` - Frontend sẽ hiển thị nút chỉnh sửa
- Chứa đầy đủ thông tin profile

### 2. Tìm kiếm profile người khác
```
GET /api/profile/search/{username}
Authorization: Bearer <token>
```
**Response**: 
- `IsOwnProfile: true` nếu đây là profile của chính mình
- `IsOwnProfile: false` nếu đây là profile của người khác
- Frontend chỉ hiển thị nút chỉnh sửa khi `IsOwnProfile: true`

### 3. Cập nhật profile (cách cũ - chỉ cho chính mình)
```
PUT /api/profile/update
Authorization: Bearer <token>
Content-Type: application/json

{
    "fullName": "Tên mới",
    "phoneNumber": "0123456789",
    "address": "Địa chỉ mới"
}
```

### 4. Cập nhật profile qua ID (cách mới - có kiểm tra quyền)
```
PUT /api/profile/{profileId}/update
Authorization: Bearer <token>
Content-Type: application/json

{
    "fullName": "Tên mới",
    "phoneNumber": "0123456789", 
    "address": "Địa chỉ mới"
}
```
**Lưu ý**: Chỉ được cập nhật khi `profileId == currentUserId`

### 5. Đổi mật khẩu (cách cũ)
```
PUT /api/profile/password
Authorization: Bearer <token>
Content-Type: application/json

{
    "currentPassword": "mật khẩu hiện tại",
    "newPassword": "mật khẩu mới"
}
```

### 6. Đổi mật khẩu qua ID (cách mới - có kiểm tra quyền)
```
PUT /api/profile/{profileId}/password
Authorization: Bearer <token>
Content-Type: application/json

{
    "currentPassword": "mật khẩu hiện tại",
    "newPassword": "mật khẩu mới"
}
```
**Lưu ý**: Chỉ được đổi mật khẩu khi `profileId == currentUserId`

## Logic phân quyền

1. **Xem profile**: Tất cả user đều có thể xem profile công khai của nhau
2. **Chỉnh sửa profile**: Chỉ được chỉnh sửa profile của chính mình
3. **Đổi mật khẩu**: Chỉ được đổi mật khẩu của chính mình

## Cách Frontend sử dụng

```javascript
// Khi nhận được response từ API
if (profileData.IsOwnProfile) {
    // Hiển thị nút "Chỉnh sửa profile"
    showEditButton();
} else {
    // Ẩn nút chỉnh sửa
    hideEditButton();
}
```

## Thông báo lỗi

- `"Bạn không có quyền chỉnh sửa hồ sơ của người khác"`
- `"Bạn không có quyền thay đổi mật khẩu của người khác"`
- `"ID hồ sơ không hợp lệ"`