# Chi tiết Thuộc tính và Phương thức của từng Class (Model)

Trong mô hình MVC sử dụng Entity Framework của dự án, các **Class (Model)** chủ yếu đóng vai trò là các thực thể (Entity) để ánh xạ xuống cơ sở dữ liệu. Do đó, chúng hầu như chỉ chứa các **Thuộc tính (Properties)** để lưu trữ dữ liệu, còn **Phương thức (Methods)** xử lý logic nghiệp vụ sẽ nằm ở các Controllers (như đã phân tích ở tài liệu trước).

Dưới đây là chi tiết các thuộc tính của từng class một (từng bảng):

---

## 1. Hệ thống Sản phẩm (Products & Categories)

### `ProductModel` (Món ăn / Đồ uống)
- **Thuộc tính:**
  - `Id` (int): Khóa chính
  - `Name` (string): Tên món
  - `Description` (string): Mô tả món ăn
  - `Slug` (string): Đường dẫn chuẩn (SEO)
  - `Price` (decimal): Giá bán
  - `CategoryId` (int): Khóa ngoại liên kết tới danh mục
  - `Category` (CategoryModel): Object danh mục
  - `Status` (int): Trạng thái (1: Đang bán)
  - `Sold` (int): Số lượng đã bán (Thống kê)
  - `Image` (string): Đường dẫn ảnh minh họa

### `CategoryModel` (Danh mục món ăn)
- **Thuộc tính:**
  - `Id` (int): Khóa chính
  - `Name` (string): Tên danh mục (Đồ uống, Món lẩu...)
  - `Description` (string): Mô tả
  - `KitchenId` (int): Liên kết bếp chịu trách nhiệm chế biến
  - `Kitchen` (KitchenModel): Object bếp
  - `isAutoFire` (bool): Tự động đẩy đơn xuống bếp hay không
  - `Slug` (string): Đường dẫn URL
  - `Status` (int): Trạng thái

### `ProductQuantityModel` (Tồn kho sản phẩm)
- **Thuộc tính:**
  - `Id` (int)
  - `Quantity` (int): Số lượng hiện có
  - `ProductId` (int): Liên kết đến sản phẩm
  - `DateCreated` (DateTime): Ngày cập nhật tồn kho

### `ProductMaterialsModel` (Định mức nguyên liệu cấu thành món ăn)
- **Thuộc tính:**
  - `ProductId` (int), `Product` (ProductModel)
  - `MaterialId` (int), `Material` (MaterialModel)
  - `QuantityRequired` (decimal): Số lượng nguyên liệu cần để làm ra 1 đơn vị món ăn

---

## 2. Hệ thống Tùy chọn (Modifiers)

### `ModifierGroupModel` (Nhóm tùy chọn)
- **Thuộc tính:**
  - `Id` (int)
  - `Name` (string): Tên nhóm (vd: Kích cỡ, Topping)
  - `IsRequired` (bool): Có bắt buộc chọn hay không
  - `MaxSelect` (int): Số lượng tối đa được chọn
  - `DisplayOrder` (int): Thứ tự hiển thị
  - `Status` (int): Trạng thái
  - `Type` (string): Loại tùy chọn ("Topping", "Size", "Extra")

### `ModifierModel` (Tùy chọn chi tiết)
- **Thuộc tính:**
  - `Id` (int)
  - `Name` (string): Tên lựa chọn (vd: Trân châu trắng, Size L)
  - `Price` (decimal): Giá tiền cộng thêm
  - `ModifierGroupId` (int): Khóa ngoại liên kết nhóm
  - `Status` (int): Trạng thái
  - `Multiplier` (decimal): Hệ số nhân (vd dùng cho Size)

### `ProModifierGroupModel` (Liên kết Món ăn - Nhóm tùy chọn)
- **Thuộc tính:**
  - `ProductId` (int), `Product` (ProductModel)
  - `ModifierGroupId` (int), `ModifierGroup` (ModifierGroupModel)

### `ModifierMaterialModel` (Hao hụt nguyên liệu cho Tùy chọn)
- **Thuộc tính:**
  - `Id` (int)
  - `ModifierId` (int)
  - `MaterialId` (int)
  - `QuantityRequired` (decimal): Số nguyên liệu hao hụt khi khách chọn Option này

---

## 3. Hệ thống Đơn hàng (Orders & Kitchen)

### `OrderModel` (Hóa đơn / Đơn hàng)
- **Thuộc tính:**
  - `Id` (int)
  - `OrderCode` (string): Mã đơn (vd: ORD-1234)
  - `GuestName` (string): Tên khách
  - `CreatedDate` (DateTime): Ngày tạo
  - `Status` (OrderStatus): Trạng thái đơn (Enum: Pending, Processing...)
  - `SubTotal` (decimal): Tổng tiền tạm tính
  - `DiscountAmount` (decimal): Tiền giảm giá
  - `VATRate`, `ServiceRate` (decimal): % Thuế và Phí phục vụ
  - `Method` (PaymentMethod): Phương thức thanh toán (Tiền mặt, Chuyển khoản)
  - `PayStatus` (PaymentStatus): Trạng thái thanh toán (Unpaid, Paid)

### `OrderDetailsModel` (Chi tiết từng món trong hóa đơn)
- **Thuộc tính:**
  - `Id` (int)
  - `OrderId` (int): Thuộc hóa đơn nào
  - `ProductId` (int): Sản phẩm nào
  - `Quantity` (int): Số lượng đặt
  - `UnitPrice` (decimal): Đơn giá thời điểm đặt
  - `CreateDate` (DateTime): Ngày đặt
  - `Status` (StatusProduct): Trạng thái món (Pending, Cooking, Done...)
  - `IsFired` (bool): Đã báo bếp chưa
  - `FireCount` (int): Số lần giục bếp

### `OrderDetailModifierModel` (Tùy chọn kèm theo chi tiết món)
- **Thuộc tính:**
  - `Id` (int)
  - `OrderDetailId` (int)
  - `ModifierId` (int), `Modifier` (ModifierModel)
  - `ModifierPrice` (decimal): Giá option tại thời điểm đặt

### `KitchenModel` (Quản lý Bếp)
- **Thuộc tính:**
  - `Id` (int)
  - `Name` (string): Tên bếp (vd: Bếp nóng, Bếp lạnh, Quầy Bar)
  - `IsActive` (bool): Đang hoạt động
  - `SortOrder` (int): Thứ tự hiển thị

### `KitchenOrderModel` (Hiển thị màn hình Bếp - Không lưu DB)
- **Thuộc tính:**
  - `OrderCode`, `ProductName`, `Quantity`, `CategoryName`, `Status`

---

## 4. Hệ thống Khu vực & Bàn (Zones & Tables)

### `ZoneModel` (Khu vực)
- **Thuộc tính:**
  - `ZoneId` (int)
  - `ZoneName` (string): Tên khu (Sân vườn, VIP)
  - `ZoneDescription` (string): Mô tả
  - `ZoneStatus` (ZoneStatus): Trạng thái
  - `Width`, `Height` (int): Kích thước khu vực (dùng cho map 2D)

### `TableModel` (Bàn ăn)
- **Thuộc tính:**
  - `Id` (int)
  - `TableName` (string): Tên bàn (Bàn 1, Bàn 2)
  - `Status` (TableStatus): Trạng thái (Trống, Đang phục vụ...)
  - `PosX`, `PosY` (float): Tọa độ X, Y trên sơ đồ
  - `Width`, `Height` (float): Kích thước bàn trên sơ đồ
  - `Capacity` (int): Số ghế/sức chứa
  - `ZoneId` (int): Thuộc khu vực nào

### `ReservationModel` (Đặt bàn)
- **Thuộc tính:**
  - `Id` (int)
  - `TableId` (int)
  - `CustomerName`, `Phone` (string): Tên, số ĐT khách
  - `PeopleCount` (int): Số người
  - `ReserveTime` (DateTime): Thời gian khách đến
  - `Status` (ReservationStatus): Trạng thái (Pending, Confirmed, Cancelled)
  - `CreatedAt` (DateTime): Thời gian tạo

---

## 5. Hệ thống Kho & Nhà cung cấp (Warehouse & Suppliers)

### `MaterialModel` (Nguyên vật liệu)
- **Thuộc tính:**
  - `Id` (int)
  - `Name` (string): Tên (Thịt bò, Trứng...)
  - `Unit` (string): Đơn vị tính (Kg, Lít, Quả)
  - `CurrentQuantity` (decimal): Tồn kho hiện tại
  - `ReorderLevel` (decimal): Mức cảnh báo sắp hết
  - `Status` (int): Trạng thái
  - `SupplierId` (int): Thuộc nhà cung cấp nào

### `InventoryTransactionModel` (Lịch sử Nhập/Xuất kho)
- **Thuộc tính:**
  - `Id` (int)
  - `MaterialId` (int)
  - `DateCreated` (DateTime): Ngày giao dịch
  - `Quantity` (decimal): Số lượng nhập/xuất
  - `UnitPrice` (decimal): Đơn giá nhập/xuất
  - `TotalPrice` (decimal): Tổng tiền
  - `Type` (string): Loại giao dịch ("Nhập" hoặc "Xuất")

### `SupplierCategoryModel` (Nhóm nhà cung cấp)
- **Thuộc tính:** `SupplierCategoryId` (int), `Name` (string), `IsActive` (bool)

### `SupplierModel` (Nhà cung cấp)
- **Thuộc tính:**
  - `SupplierId` (int)
  - `SupplierName` (string): Tên NCC
  - `Status` (SupplierStatus): Trạng thái
  - `SupplierCategoryId` (int)

---

## 6. Người dùng, Khuyến mãi & Thống kê

### `AppUserModel` (Kế thừa từ IdentityUser)
- **Thuộc tính bổ sung:**
  - `Occupation` (string): Chức vụ
  - `RoleId` (string): Phân quyền
  - `CreatedAt` (DateTime): Ngày tạo tài khoản

### `CouponModel` (Mã giảm giá)
- **Thuộc tính:**
  - `Id` (int)
  - `Code` (string): Mã nhập (vd: SALE10)
  - `DiscountAmount` (decimal): Số tiền giảm
  - `IsActive` (bool)

### `StatisticalModel` (Thống kê)
- **Thuộc tính:**
  - `Id` (int)
  - `Quantity` (int): Số đơn hàng
  - `Sold` (int): Số sản phẩm bán ra
  - `Revenue` (decimal): Doanh thu
  - `Profit` (decimal): Lợi nhuận
  - `DateCreated` (DateTime): Ngày thống kê

### `AdminNotificationModel` (Thông báo cho Quản lý)
- **Thuộc tính:**
  - `Id` (int)
  - `TableId`, `OrderId`, `OrderDetailId` (int): Liên kết tham chiếu nếu có
  - `Message` (string): Nội dung thông báo
  - `ProductName` (string): Tên sản phẩm liên quan
  - `IsRead` (bool): Đã đọc chưa
  - `CreatedAt` (DateTime)
