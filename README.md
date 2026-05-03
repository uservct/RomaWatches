# RomaWatches - Ứng dụng E-commerce Bán Đồng Hồ

RomaWatches là một ứng dụng web thương mại điện tử được xây dựng trên nền tảng **ASP.NET Core MVC 8.0**. Dự án cung cấp một hệ thống cửa hàng trực tuyến với đầy đủ tính năng dành cho người dùng mua sắm đồng hồ và quản trị viên quản lý cửa hàng.

## 🚀 Công Nghệ Sử Dụng

- **Backend Framework:** ASP.NET Core MVC 8.0
- **Cơ sở dữ liệu:** SQL Server
- **ORM:** Entity Framework Core
- **Xác thực & Phân quyền:** ASP.NET Core Identity (Hỗ trợ xác thực cục bộ và Google Login)
- **Quản lý trạng thái:** Session (Dành cho giỏ hàng)
- **Giao diện (Frontend):** Razor Views, Bootstrap, Tailwind CSS, jQuery

## 📋 Cấu Trúc Thư Mục Chính

- `Controllers/`: Chứa các bộ điều khiển xử lý logic nghiệp vụ và định tuyến (Admin, Cart, Checkout, Order, Product, v.v.).
- `Models/`: Chứa các lớp định nghĩa cấu trúc dữ liệu và ViewModel (Product, Order, CartItem, ApplicationUser, v.v.).
- `Views/`: Chứa các giao diện người dùng được chia thành các thư mục tương ứng với Controllers.
- `Data/`: Chứa cấu hình kết nối Database (`ApplicationDbContext`) và lớp khởi tạo dữ liệu mẫu (`DbInitializer`).
- `wwwroot/`: Chứa các tài nguyên tĩnh như CSS, JavaScript, hình ảnh và thư viện front-end.

## ✨ Các Tính Năng Nổi Bật

### 1. Phía Người Dùng (Khách hàng)
- **Duyệt sản phẩm:** Xem danh sách đồng hồ, xem chi tiết sản phẩm bao gồm hình ảnh, giá cả, và thông số kỹ thuật (Chất liệu vỏ, máy, độ chịu nước, v.v.).
- **Giỏ hàng (Cart):** Thêm, sửa, xóa sản phẩm trong giỏ hàng (sử dụng Session memory).
- **Thanh toán (Checkout):** Hỗ trợ nhiều phương thức thanh toán (COD, Chuyển khoản ngân hàng, Nhận tại cửa hàng).
- **Quản lý tài khoản:** Đăng ký, đăng nhập, theo dõi lịch sử đơn hàng.
- **Đánh giá (Review):** Khách hàng có thể để lại đánh giá cho các sản phẩm đã mua.

### 2. Phía Quản Trị Viên (Admin)
- **Bảng điều khiển (Dashboard):** Thống kê tổng quan về doanh thu, đơn hàng mới, sản phẩm bán chạy trong 30 ngày qua.
- **Quản lý đơn hàng:** Xem danh sách, chi tiết đơn hàng, tìm kiếm và cập nhật trạng thái đơn hàng (Đang chờ, Đã xác nhận, Đang giao, Đã giao, Đã hủy).
- *(Các tính năng quản lý sản phẩm, tin tức, hỗ trợ khách hàng được xây dựng qua các controllers tương ứng).*

## 🛠 Hướng Dẫn Cài Đặt và Chạy Dự Án

### Yêu Cầu Hệ Thống
- [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- SQL Server (hoặc SQL Server Express / LocalDB)
- Visual Studio 2022 hoặc Visual Studio Code

### Các Bước Cài Đặt

1. **Khôi phục các gói NuGet:**
   Chạy lệnh sau trong thư mục chứa file `.csproj`:
   ```bash
   dotnet restore
   ```

2. **Cấu hình chuỗi kết nối Database:**
   Mở file `appsettings.json` và cập nhật chuỗi kết nối `DefaultConnection` cho phù hợp với SQL Server của bạn.

3. **Cập nhật Database (Migration):**
   Mở terminal / Package Manager Console và chạy:
   ```bash
   dotnet ef database update
   ```
   *(Hệ thống sẽ tự động tạo cơ sở dữ liệu và thêm dữ liệu mẫu nếu có thông qua `DbInitializer`).*

4. **Chạy dự án:**
   ```bash
   dotnet run
   ```
   Hoặc chạy trực tiếp từ Visual Studio. Mặc định ứng dụng sẽ chạy trên địa chỉ hiển thị trong terminal (thường là `https://localhost:xxxx`).

---
*Dự án RomaWatches được phát triển hướng tới mục tiêu cung cấp một trải nghiệm mua sắm đồng hồ trực tuyến mượt mà và quản lý bán hàng hiệu quả.*
