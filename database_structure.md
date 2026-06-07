# Chi tiết cơ sở dữ liệu và các phương thức xử lý (Controllers)

Dựa trên cấu trúc của dự án, đặc biệt là file `DataContext.cs` và các file trong thư mục `Controllers`, hệ thống quản lý nhà hàng của bạn được chia thành các bảng và nhóm chức năng chính như sau:

## 1. Quản lý Sản phẩm & Thực đơn (Menu & Products)
### Các bảng (Tables):
- **Products**: Lưu trữ thông tin món ăn/thức uống.
- **Categories**: Danh mục món ăn (ví dụ: Đồ uống, Món chính).
- **ProductQuantities**: Quản lý số lượng/tồn kho của các món ăn.
- **productMaterials**: Cấu hình nguyên liệu cần thiết để làm ra một sản phẩm.

### Controller & Methods (`ProductController`, `CategoryController`):
- `Index()`: Hiển thị danh sách sản phẩm / danh mục.
- `Create()` / `Create(Model)`: Thêm mới một sản phẩm / danh mục.
- `Edit(Id)` / `Edit(Model)`: Cập nhật thông tin.
- `Delete(Id)`: Xóa sản phẩm / danh mục.
- `AddQuantity(id, quantity)`: Thêm số lượng tồn cho sản phẩm.
- `AddMaterial(id, materialIds, quantities)`: Định mức nguyên liệu chế biến món ăn.
- `ToggleModifier(dto)`: Bật/tắt tùy chọn (Modifier) cho món.

---

## 2. Quản lý Tùy chọn món ăn (Modifiers)
### Các bảng (Tables):
- **ModifierGroups**: Nhóm tùy chọn (ví dụ: Chọn kích cỡ, Topping).
- **Modifiers**: Các lựa chọn chi tiết (Size L, Thêm trân châu).
- **ProductModifierMappings (ProModifierGroupModel)**: Bảng trung gian liên kết món ăn với các nhóm tùy chọn.
- **ModifierMaterials**: Bảng định mức nguyên liệu hao hụt khi khách chọn Modifier.

### Controller & Methods (`ModifierGroupController`):
- Thao tác CRUD (Create, Read, Update, Delete) cho các nhóm tùy chọn.
- Quản lý các Modifier bên trong từng nhóm.

---

## 3. Quản lý Đơn hàng (Orders & Kitchen)
### Các bảng (Tables):
- **Orders**: Thông tin hóa đơn/đơn hàng chung.
- **OrderDetails**: Chi tiết từng món trong đơn hàng.
- **OrderDetailModifiers**: Các tùy chọn khách hàng đã chọn kèm theo món ăn trong đơn hàng đó.
- **Kitchen**: Quản lý trạng thái bếp.
- **Reservations**: Quản lý đặt bàn trước.

### Controller & Methods (`OrderController`, `KitchenController`):
- `OrderController`: Quản lý danh sách đơn hàng, xem chi tiết (`ViewOrder`), cập nhật trạng thái hóa đơn (thanh toán, hủy).
- `KitchenController`: 
  - Hiển thị màn hình bếp cho nhân viên chế biến.
  - Các method cập nhật trạng thái món ăn: Đang chờ -> Đang nấu -> Hoàn thành.

---

## 4. Quản lý Bàn và Khu vực (Tables & Zones)
### Các bảng (Tables):
- **Table**: Lưu thông tin từng bàn (Bàn 1, Bàn 2, trạng thái trống/đang phục vụ).
- **Zones**: Khu vực (Tầng 1, Tầng 2, Sân vườn).

### Controller & Methods (`TableAdminController`):
- Thêm/Sửa/Xóa khu vực và các bàn trong khu vực đó.
- Cập nhật trạng thái bàn (mở bàn, đóng bàn).

---

## 5. Quản lý Kho & Nguyên liệu (Warehouse & Materials)
### Các bảng (Tables):
- **Materials**: Danh sách nguyên liệu trong kho.
- **InventoryTransactions**: Lịch sử nhập/xuất kho (phiếu nhập, xuất).
- **Suppliers**: Nhà cung cấp nguyên vật liệu.
- **SupplierCategories**: Nhóm nhà cung cấp.

### Controller & Methods (`WarehouseController`, `SupplierController`, `SupplierCategoryController`):
- `WarehouseController`: Lập phiếu nhập kho, phiếu xuất kho, xem lịch sử tồn kho, thống kê nguyên liệu sắp hết.
- Thao tác CRUD quản lý thông tin của các nhà cung cấp.

---

## 6. Người dùng và Phân quyền (Users & Roles)
### Các bảng (Tables):
- **User / AppUserModel**: Kế thừa từ `IdentityUser`, quản lý thông tin nhân viên, khách hàng.
- Bảng của hệ thống Identity: Quản lý Roles (Quyền hạn).

### Controller & Methods (`UserController`, `RoleController`, `AccountController`):
- Đăng nhập, đăng xuất, quên mật khẩu.
- Phân quyền cho nhân viên (Admin, Nhân viên bếp, Thu ngân, Phục vụ).

---

## 7. Các chức năng Khác (Khuyến mãi, Thống kê, Thông báo)
### Các bảng (Tables):
- **Coupons**: Mã giảm giá/Khuyến mãi.
- **Statisticals**: Lưu trữ số liệu thống kê doanh thu theo thời gian.
- **AdminNotifications**: Thông báo hệ thống cho admin (ví dụ: Đơn hàng mới, nguyên liệu sắp hết).

### Controller & Methods (`CouponController`, `DashboardController`, `NotificationController`):
- `CouponController`: Tạo mã giảm giá, thiết lập điều kiện áp dụng.
- `DashboardController`: Truy xuất dữ liệu `Statisticals` để vẽ biểu đồ doanh thu, số lượng đơn hàng trên trang chủ Admin.
- `NotificationController`: Lấy danh sách thông báo chưa đọc, đánh dấu đã đọc.
