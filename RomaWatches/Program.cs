using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RomaWatches.Data;
using RomaWatches.Models;

namespace RomaWatches
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Thêm các dịch vụ vào container (Dependency Injection container).
            
            // Lấy chuỗi kết nối từ file cấu hình (appsettings.json).
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            
            // Đăng ký ApplicationDbContext sử dụng SQL Server.
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            
            // Thêm bộ lọc lỗi cho trang phát triển cơ sở dữ liệu.
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // Cấu hình Identity cho hệ thống xác thực và phân quyền người dùng.
            // Sử dụng ApplicationUser làm lớp người dùng tùy chỉnh.
            builder.Services.AddDefaultIdentity<ApplicationUser>(options => 
            {
                // Cấu hình các yêu cầu về mật khẩu và đăng nhập.
                options.SignIn.RequireConfirmedAccount = false; // Không yêu cầu xác nhận email để đăng nhập.
                options.Password.RequireDigit = true; // Yêu cầu có số.
                options.Password.RequireLowercase = true; // Yêu cầu có chữ thường.
                options.Password.RequireUppercase = true; // Yêu cầu có chữ hoa.
                options.Password.RequireNonAlphanumeric = false; // Không bắt buộc ký tự đặc biệt.
                options.Password.RequiredLength = 6; // Độ dài tối thiểu là 6.
            })
            .AddEntityFrameworkStores<ApplicationDbContext>(); // Lưu trữ thông tin Identity trong EF Core.

            // Thêm các dịch vụ cho MVC (Controllers và Views).
            builder.Services.AddControllersWithViews();
            
            // Cấu hình Session để lưu trữ dữ liệu phiên làm việc (ví dụ: giỏ hàng).
            builder.Services.AddDistributedMemoryCache(); // Sử dụng bộ nhớ RAM để lưu cache.
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian hết hạn session là 30 phút.
                options.Cookie.HttpOnly = true; // Cookie chỉ được truy cập qua HTTP, ngăn chặn JS truy cập.
                options.Cookie.IsEssential = true; // Cookie này là thiết yếu cho ứng dụng.
            });

            var app = builder.Build();

            // Cấu hình pipeline xử lý HTTP request (Middleware).
            if (app.Environment.IsDevelopment())
            {
                // Trong môi trường phát triển, hiển thị trang lỗi chi tiết liên quan đến migration.
                app.UseMigrationsEndPoint();
            }
            else
            {
                // Trong môi trường production, chuyển hướng đến trang lỗi chung.
                app.UseExceptionHandler("/Home/Error");
                // Sử dụng HSTS để tăng cường bảo mật (buộc sử dụng HTTPS).
                app.UseHsts();
            }

            // Chuyển hướng HTTP sang HTTPS.
            app.UseHttpsRedirection();
            // Cho phép phục vụ các file tĩnh (CSS, JS, Images) từ thư mục wwwroot.
            app.UseStaticFiles();

            // Kích hoạt tính năng định tuyến (Routing).
            app.UseRouting();
            
            // Kích hoạt Session middleware.
            app.UseSession();

            // Kích hoạt xác thực (Authentication) và phân quyền (Authorization).
            app.UseAuthentication();
            app.UseAuthorization();

            // Định nghĩa route mặc định cho Controller/Action.
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            // Map các Razor Pages (cần thiết cho Identity UI mặc định).
            app.MapRazorPages();

            // Đảm bảo cơ sở dữ liệu được migrate (cập nhật schema) và khởi tạo dữ liệu mẫu (Seed Data).
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    // Tự động áp dụng các migration chưa chạy.
                    await context.Database.MigrateAsync();
                    // Gọi hàm khởi tạo dữ liệu (ví dụ: tạo Admin user).
                    await Data.DbInitializer.InitializeAsync(scope.ServiceProvider);
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }

            // Chạy ứng dụng.
            app.Run();
        }
    }
}
