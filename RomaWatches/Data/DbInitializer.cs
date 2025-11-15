using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RomaWatches.Models;

namespace RomaWatches.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("DbInitializer");

            try
            {
                // Ensure database is created and migrated
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrated successfully.");

                // Create Products table if not exists
                var tableExists = await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
                    BEGIN
                        CREATE TABLE Products (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            Name NVARCHAR(200) NOT NULL,
                            CaseMaterial NVARCHAR(100) NULL,
                            CaseDiameter NVARCHAR(50) NULL,
                            Dial NVARCHAR(100) NULL,
                            Movement NVARCHAR(100) NULL,
                            PowerReserve NVARCHAR(50) NULL,
                            WaterResistance NVARCHAR(50) NULL,
                            Crystal NVARCHAR(100) NULL,
                            Gender NVARCHAR(50) NULL,
                            StrapType NVARCHAR(100) NULL,
                            WaterResistanceAtm INT NULL,
                            Description NVARCHAR(MAX) NULL,
                            Brand NVARCHAR(100) NOT NULL,
                            Price DECIMAL(18,2) NOT NULL,
                            ImageUrl NVARCHAR(500) NULL,
                            CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                            UpdatedAt DATETIME2 NULL
                        );
                        
                        CREATE INDEX IX_Products_Brand ON Products(Brand);
                        CREATE INDEX IX_Products_Price ON Products(Price);
                        CREATE INDEX IX_Products_CreatedAt ON Products(CreatedAt);
                        CREATE INDEX IX_Products_Gender ON Products(Gender);
                        CREATE INDEX IX_Products_StrapType ON Products(StrapType);
                        CREATE INDEX IX_Products_WaterResistanceAtm ON Products(WaterResistanceAtm);
                    END
                ");
                logger.LogInformation("Products table checked/created successfully.");


                // Create admin user if not exists
                var adminEmail = "admin@example.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);

                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        FirstName = "Admin",
                        LastName = "Account",
                        Role = "admin",
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(adminUser, "Admin@123");
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Admin user created successfully.");
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        logger.LogError($"Failed to create admin user: {errors}");
                        throw new Exception($"Failed to create admin user: {errors}");
                    }
                }
                else
                {
                    // Update role and password if user exists
                    if (adminUser.Role != "admin")
                    {
                        adminUser.Role = "admin";
                        await userManager.UpdateAsync(adminUser);
                        logger.LogInformation("Admin user role updated.");
                    }
                    
                    // Reset password to ensure it's correct
                    var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                    var resetResult = await userManager.ResetPasswordAsync(adminUser, token, "Admin@123");
                    if (resetResult.Succeeded)
                    {
                        logger.LogInformation("Admin user password reset successfully.");
                    }
                }

                // Seed Products
                if (!context.Products.Any())
                {
                    logger.LogInformation("Seeding products...");
                    var products = new List<Product>
                    {
                        new Product
                        {
                            Name = "Seamaster Diver 300M",
                            Brand = "Omega",
                            CaseMaterial = "Thép không gỉ",
                            CaseDiameter = "42mm",
                            Dial = "Xanh dương",
                            Movement = "Đồng hồ cơ",
                            PowerReserve = "60 giờ",
                            WaterResistance = "300m",
                            WaterResistanceAtm = 20,
                            Crystal = "Sapphire",
                            Gender = "Nam",
                            StrapType = "Thép không rỉ",
                            Price = 3500000,
                            ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuB2ViSgHC7A9NF9q6e-2xHDnYfEiO5WFaGo0FW9A_Iu0SvfUWx_1GuXRfnZC6jNk7pHWLEpZeeT-szB-PLifCWyHgvgnIRQJWMwQRbhOMKqkTLmzgnJwJ12oYXZXtbSkHOoSL9LbGpJMZiSBggrIPpyFtJHVRIuqJ0_rzRw22ApWhi9a5X4LU3iz2yKP7G_TMjBbF65ZBbtQb435t4NpTJ313AJW8aj1iQieCmSyUyMnCi6tW9YkBeObad8rtpj1wrE4j_hZ3cx13A",
                            Description = "Đồng hồ lặn biểu tượng của Omega"
                        },
                        new Product
                        {
                            Name = "Submariner Date",
                            Brand = "Rolex",
                            CaseMaterial = "Thép Oystersteel",
                            CaseDiameter = "41mm",
                            Dial = "Đen",
                            Movement = "Đồng hồ cơ",
                            PowerReserve = "70 giờ",
                            WaterResistance = "300m",
                            WaterResistanceAtm = 20,
                            Crystal = "Sapphire chống xước",
                            Gender = "Nam",
                            StrapType = "Thép không rỉ",
                            Price = 8500000,
                            ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuCqOQHJqH6cQKj-2xHDnYfEiO5WFaGo0FW9A_Iu0SvfUWx_1GuXRfnZC6jNk7pHWLEpZeeT-szB-PLifCWyHgvgnIRQJWMwQRbhOMKqkTLmzgnJwJ12oYXZXtbSkHOoSL9LbGpJMZiSBggrIPpyFtJHVRIuqJ0_rzRw22ApWhi9a5X4LU3iz2yKP7G_TMjBbF65ZBbtQb435t4NpTJ313AJW8aj1iQieCmSyUyMnCi6tW9YkBeObad8rtpj1wrE4j",
                            Description = "Đồng hồ lặn huyền thoại của Rolex"
                        },
                        new Product
                        {
                            Name = "Nautilus 5711",
                            Brand = "Patek Philippe",
                            CaseMaterial = "Thép không gỉ",
                            CaseDiameter = "40mm",
                            Dial = "Xanh dương",
                            Movement = "Đồng hồ cơ",
                            PowerReserve = "45 giờ",
                            WaterResistance = "120m",
                            WaterResistanceAtm = 10,
                            Crystal = "Sapphire",
                            Gender = "Nam",
                            StrapType = "Thép không rỉ",
                            Price = 95000000,
                            ImageUrl = "https://images.unsplash.com/photo-1587836374616-0f4a2f6c8e1d",
                            Description = "Biểu tượng sang trọng thể thao của Patek Philippe"
                        },
                        new Product
                        {
                            Name = "Royal Oak 15500ST",
                            Brand = "Audemars Piguet",
                            CaseMaterial = "Thép không gỉ",
                            CaseDiameter = "41mm",
                            Dial = "Xanh dương",
                            Movement = "Đồng hồ cơ",
                            PowerReserve = "60 giờ",
                            WaterResistance = "50m",
                            WaterResistanceAtm = 5,
                            Crystal = "Sapphire chống lóa",
                            Gender = "Nam",
                            StrapType = "Thép không rỉ",
                            Price = 72000000,
                            ImageUrl = "https://images.unsplash.com/photo-1594534475808-b18fc33b045e",
                            Description = "Thiết kế biểu tượng với vỏ bát giác"
                        },
                        new Product
                        {
                            Name = "Santos de Cartier",
                            Brand = "Cartier",
                            CaseMaterial = "Thép và vàng",
                            CaseDiameter = "39.8mm",
                            Dial = "Trắng",
                            Movement = "Đồng hồ cơ",
                            PowerReserve = "48 giờ",
                            WaterResistance = "100m",
                            WaterResistanceAtm = 10,
                            Crystal = "Sapphire",
                            Gender = "Đôi",
                            StrapType = "Dây da",
                            Price = 15500000,
                            ImageUrl = "https://images.unsplash.com/photo-1523170335258-f5ed11844a49",
                            Description = "Đồng hồ bay đầu tiên của thế giới"
                        },
                        new Product
                        {
                            Name = "Big Bang Unico",
                            Brand = "Hublot",
                            CaseMaterial = "Ceramic",
                            CaseDiameter = "45mm",
                            Dial = "Đen",
                            Movement = "Đồng hồ cơ",
                            PowerReserve = "72 giờ",
                            WaterResistance = "100m",
                            WaterResistanceAtm = 10,
                            Crystal = "Sapphire",
                            Gender = "Nam",
                            StrapType = "Dây silicone",
                            Price = 39500000,
                            ImageUrl = "https://images.unsplash.com/photo-1622434641406-a158123450f9",
                            Description = "Thiết kế táo bạo và hiện đại"
                        },
                        new Product
                        {
                            Name = "Speedmaster Professional",
                            Brand = "Omega",
                            CaseMaterial = "Thép không gỉ",
                            CaseDiameter = "42mm",
                            Dial = "Đen",
                            Movement = "Đồng hồ cơ",
                            PowerReserve = "48 giờ",
                            WaterResistance = "50m",
                            WaterResistanceAtm = 5,
                            Crystal = "Hesalite",
                            Gender = "Nam",
                            StrapType = "Dây dù",
                            Price = 4500000,
                            ImageUrl = "https://images.unsplash.com/photo-1614164185128-e4ec99c436d7",
                            Description = "Moonwatch - Đồng hồ lên mặt trăng"
                        },
                        new Product
                        {
                            Name = "Daytona Cosmograph",
                            Brand = "Rolex",
                            CaseMaterial = "Vàng Everose",
                            CaseDiameter = "40mm",
                            Dial = "Chocolate",
                            Movement = "Đồng hồ cơ",
                            PowerReserve = "72 giờ",
                            WaterResistance = "100m",
                            WaterResistanceAtm = 10,
                            Crystal = "Sapphire chống xước",
                            Gender = "Nam",
                            StrapType = "Thép không rỉ",
                            Price = 85000000,
                            ImageUrl = "https://images.unsplash.com/photo-1587836374616-0f4a2f6c8e1d",
                            Description = "Đồng hồ đua xe huyền thoại"
                        },
                        new Product
                        {
                            Name = "Lady-Datejust",
                            Brand = "Rolex",
                            CaseMaterial = "Vàng trắng",
                            CaseDiameter = "28mm",
                            Dial = "Hồng perlamut",
                            Movement = "Đồng hồ cơ",
                            PowerReserve = "55 giờ",
                            WaterResistance = "100m",
                            WaterResistanceAtm = 10,
                            Crystal = "Sapphire",
                            Gender = "Nữ",
                            StrapType = "Thép không rỉ",
                            Price = 32000000,
                            ImageUrl = "https://images.unsplash.com/photo-1594576722512-582bcd46fba3",
                            Description = "Đồng hồ nữ sang trọng của Rolex"
                        },
                        new Product
                        {
                            Name = "Oyster Perpetual",
                            Brand = "Rolex",
                            CaseMaterial = "Thép Oystersteel",
                            CaseDiameter = "36mm",
                            Dial = "Xanh lá cây",
                            Movement = "Đồng hồ cơ",
                            PowerReserve = "70 giờ",
                            WaterResistance = "100m",
                            WaterResistanceAtm = 10,
                            Crystal = "Sapphire",
                            Gender = "Đôi",
                            StrapType = "Thép không rỉ",
                            Price = 16500000,
                            ImageUrl = "https://images.unsplash.com/photo-1524805444758-089113d48a6d",
                            Description = "Đồng hồ cổ điển với mặt số màu sắc"
                        },
                        new Product
                        {
                            Name = "Constellation Manhattan",
                            Brand = "Omega",
                            CaseMaterial = "Vàng và thép",
                            CaseDiameter = "29mm",
                            Dial = "Trắng ngọc trai",
                            Movement = "Đồng hồ cơ",
                            PowerReserve = "55 giờ",
                            WaterResistance = "50m",
                            WaterResistanceAtm = 5,
                            Crystal = "Sapphire",
                            Gender = "Nữ",
                            StrapType = "Thép không rỉ",
                            Price = 18500000,
                            ImageUrl = "https://images.unsplash.com/photo-1611694517597-0a2542664fd5",
                            Description = "Đồng hồ nữ tinh tế với kim cương"
                        },
                        new Product
                        {
                            Name = "Tank Must",
                            Brand = "Cartier",
                            CaseMaterial = "Thép không gỉ",
                            CaseDiameter = "33.7mm",
                            Dial = "Xanh dương",
                            Movement = "Đồng hồ cơ",
                            PowerReserve = "38 giờ",
                            WaterResistance = "30m",
                            WaterResistanceAtm = 3,
                            Crystal = "Sapphire",
                            Gender = "Nữ",
                            StrapType = "Dây da",
                            Price = 7800000,
                            ImageUrl = "https://images.unsplash.com/photo-1532667449560-72a95c8d381b",
                            Description = "Thiết kế hình chữ nhật biểu tượng"
                        },
                        new Product
                        {
                            Name = "Ballon Bleu",
                            Brand = "Cartier",
                            CaseMaterial = "Thép không gỉ",
                            CaseDiameter = "36mm",
                            Dial = "Bạc guilloche",
                            Movement = "Đồng hồ cơ",
                            PowerReserve = "42 giờ",
                            WaterResistance = "30m",
                            WaterResistanceAtm = 3,
                            Crystal = "Sapphire",
                            Gender = "Nữ",
                            StrapType = "Dây da",
                            Price = 12500000,
                            ImageUrl = "https://images.unsplash.com/photo-1515562141207-7a88fb7ce338",
                            Description = "Mặt số cong độc đáo với xanh Cartier"
                        },
                        new Product
                        {
                            Name = "Classic Fusion",
                            Brand = "Hublot",
                            CaseMaterial = "Titanium",
                            CaseDiameter = "42mm",
                            Dial = "Đen skeleton",
                            Movement = "Đồng hồ cơ",
                            PowerReserve = "42 giờ",
                            WaterResistance = "50m",
                            WaterResistanceAtm = 5,
                            Crystal = "Sapphire",
                            Gender = "Nam",
                            StrapType = "Dây da",
                            Price = 24500000,
                            ImageUrl = "https://images.unsplash.com/photo-1547996160-81dfa63595aa",
                            Description = "Thiết kế thanh lịch với vỏ titanium"
                        },
                        new Product
                        {
                            Name = "Calatrava",
                            Brand = "Patek Philippe",
                            CaseMaterial = "Vàng trắng",
                            CaseDiameter = "39mm",
                            Dial = "Trắng",
                            Movement = "Đồng hồ cơ",
                            PowerReserve = "65 giờ",
                            WaterResistance = "30m",
                            WaterResistanceAtm = 3,
                            Crystal = "Sapphire",
                            Gender = "Nam",
                            StrapType = "Dây da",
                            Price = 67500000,
                            ImageUrl = "https://images.unsplash.com/photo-1509048191080-d2984bad6ae5",
                            Description = "Đồng hồ dress watch cổ điển nhất"
                        },
                        new Product
                        {
                            Name = "Royal Oak Offshore",
                            Brand = "Audemars Piguet",
                            CaseMaterial = "Ceramic",
                            CaseDiameter = "44mm",
                            Dial = "Đen Méga Tapisserie",
                            Movement = "Đồng hồ cơ",
                            PowerReserve = "65 giờ",
                            WaterResistance = "100m",
                            WaterResistanceAtm = 10,
                            Crystal = "Sapphire chống lóa",
                            Gender = "Nam",
                            StrapType = "Dây dù",
                            Price = 89000000,
                            ImageUrl = "https://images.unsplash.com/photo-1622434641406-a158123450f9",
                            Description = "Phiên bản thể thao mạnh mẽ của Royal Oak"
                        },
                        new Product
                        {
                            Name = "Aqua Terra 150M",
                            Brand = "Omega",
                            CaseMaterial = "Thép không gỉ",
                            CaseDiameter = "38mm",
                            Dial = "Xanh dương teak",
                            Movement = "Đồng hồ cơ",
                            PowerReserve = "55 giờ",
                            WaterResistance = "150m",
                            WaterResistanceAtm = 15,
                            Crystal = "Sapphire",
                            Gender = "Đôi",
                            StrapType = "Thép không rỉ",
                            Price = 6800000,
                            ImageUrl = "https://images.unsplash.com/photo-1533139502658-0198f920d8e8",
                            Description = "Đồng hồ thể thao thanh lịch hàng ngày"
                        },
                        new Product
                        {
                            Name = "Oysterquartz Datejust",
                            Brand = "Rolex",
                            CaseMaterial = "Thép Oystersteel",
                            CaseDiameter = "36mm",
                            Dial = "Xanh dương",
                            Movement = "Đồng hồ điện tử",
                            PowerReserve = "Pin 5 năm",
                            WaterResistance = "100m",
                            WaterResistanceAtm = 10,
                            Crystal = "Sapphire",
                            Gender = "Nam",
                            StrapType = "Thép không rỉ",
                            Price = 12000000,
                            ImageUrl = "https://images.unsplash.com/photo-1524805444758-089113d48a6d",
                            Description = "Đồng hồ quartz chính xác cao của Rolex"
                        },
                        new Product
                        {
                            Name = "Seamaster Aqua Terra Quartz",
                            Brand = "Omega",
                            CaseMaterial = "Thép không gỉ",
                            CaseDiameter = "38mm",
                            Dial = "Trắng",
                            Movement = "Đồng hồ điện tử",
                            PowerReserve = "Pin 4 năm",
                            WaterResistance = "150m",
                            WaterResistanceAtm = 15,
                            Crystal = "Sapphire",
                            Gender = "Nữ",
                            StrapType = "Dây da",
                            Price = 7500000,
                            ImageUrl = "https://images.unsplash.com/photo-1533139502658-0198f920d8e8",
                            Description = "Đồng hồ quartz nữ thanh lịch"
                        },
                        new Product
                        {
                            Name = "Tank Solo Quartz",
                            Brand = "Cartier",
                            CaseMaterial = "Thép không gỉ",
                            CaseDiameter = "31mm",
                            Dial = "Trắng",
                            Movement = "Đồng hồ điện tử",
                            PowerReserve = "Pin 3 năm",
                            WaterResistance = "30m",
                            WaterResistanceAtm = 3,
                            Crystal = "Sapphire",
                            Gender = "Nữ",
                            StrapType = "Dây da",
                            Price = 4200000,
                            ImageUrl = "https://images.unsplash.com/photo-1532667449560-72a95c8d381b",
                            Description = "Thiết kế cổ điển với bộ máy quartz"
                        },
                        new Product
                        {
                            Name = "Big Bang Quartz",
                            Brand = "Hublot",
                            CaseMaterial = "Ceramic",
                            CaseDiameter = "41mm",
                            Dial = "Đen",
                            Movement = "Đồng hồ điện tử",
                            PowerReserve = "Pin 3 năm",
                            WaterResistance = "100m",
                            WaterResistanceAtm = 10,
                            Crystal = "Sapphire",
                            Gender = "Nam",
                            StrapType = "Dây silicone",
                            Price = 28000000,
                            ImageUrl = "https://images.unsplash.com/photo-1622434641406-a158123450f9",
                            Description = "Thiết kế hiện đại với bộ máy quartz"
                        },
                        new Product
                        {
                            Name = "Royal Oak Quartz",
                            Brand = "Audemars Piguet",
                            CaseMaterial = "Thép không gỉ",
                            CaseDiameter = "33mm",
                            Dial = "Xanh dương",
                            Movement = "Đồng hồ điện tử",
                            PowerReserve = "Pin 4 năm",
                            WaterResistance = "50m",
                            WaterResistanceAtm = 5,
                            Crystal = "Sapphire",
                            Gender = "Nữ",
                            StrapType = "Thép không rỉ",
                            Price = 18500000,
                            ImageUrl = "https://images.unsplash.com/photo-1594534475808-b18fc33b045e",
                            Description = "Royal Oak phiên bản quartz nữ"
                        },
                        new Product
                        {
                            Name = "Twenty-4 Quartz",
                            Brand = "Patek Philippe",
                            CaseMaterial = "Thép không gỉ",
                            CaseDiameter = "30mm",
                            Dial = "Trắng ngọc trai",
                            Movement = "Đồng hồ điện tử",
                            PowerReserve = "Pin 3 năm",
                            WaterResistance = "30m",
                            WaterResistanceAtm = 3,
                            Crystal = "Sapphire",
                            Gender = "Nữ",
                            StrapType = "Dây da",
                            Price = 55000000,
                            ImageUrl = "https://images.unsplash.com/photo-1509048191080-d2984bad6ae5",
                            Description = "Đồng hồ nữ sang trọng với bộ máy quartz"
                        },
                    };

                    context.Products.AddRange(products);
                    await context.SaveChangesAsync();
                    logger.LogInformation($"Seeded {products.Count} products successfully.");
                }
                else
                {
                    logger.LogInformation("Products already exist, skipping seed.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database.");
                throw;
            }
        }
    }
}

