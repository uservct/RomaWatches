using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RomaWatches.Data;
using RomaWatches.Models;

namespace RomaWatches.Controllers
{
    // Controller quản lý sản phẩm (Hiển thị danh sách, lọc, tìm kiếm, chi tiết).
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context; // Context cơ sở dữ liệu.
        private readonly ILogger<ProductController> _logger; // Logger.

        // Constructor injection.
        public ProductController(ApplicationDbContext context, ILogger<ProductController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Action hiển thị trang danh sách sản phẩm (có hỗ trợ lọc và tìm kiếm).
        // GET: /Product
        public async Task<IActionResult> Index(
            string? brands,
            string? priceRanges,
            string? movements,
            string? genders,
            string? strapTypes,
            string? waterResistanceAtms,
            string? sortBy = "newest",
            string? searchQuery = null)
        {
            var query = _context.Products.AsQueryable();

            // Tìm kiếm theo tên hoặc thương hiệu.
            if (!string.IsNullOrEmpty(searchQuery))
            {
                var searchTerm = searchQuery.Trim().ToLower();
                query = query.Where(p => 
                    (p.Name != null && p.Name.ToLower().Contains(searchTerm)) ||
                    (p.Brand != null && p.Brand.ToLower().Contains(searchTerm))
                );
            }

            // Lọc theo thương hiệu.
            if (!string.IsNullOrEmpty(brands))
            {
                var brandList = brands.Split(',').Select(b => b.Trim().ToLower()).ToList();
                query = query.Where(p => brandList.Contains(p.Brand.ToLower()));
            }

            // Lọc theo giới tính.
            if (!string.IsNullOrEmpty(genders))
            {
                var genderList = genders.Split(',').Select(g => g.Trim().ToLower()).ToList();
                query = query.Where(p => p.Gender != null && genderList.Contains(p.Gender.ToLower()));
            }

            // Lọc theo loại máy (Movement).
            if (!string.IsNullOrEmpty(movements))
            {
                var movementList = movements.Split(',').Select(m => m.Trim().ToLower()).ToList();
                query = query.Where(p => movementList.Any(m => p.Movement != null && p.Movement.ToLower().Contains(m)));
            }

            // Lọc theo loại dây đeo.
            if (!string.IsNullOrEmpty(strapTypes))
            {
                var strapList = strapTypes.Split(',').Select(s => s.Trim().ToLower()).ToList();
                query = query.Where(p => strapList.Any(s => p.StrapType != null && p.StrapType.ToLower().Contains(s)));
            }

            // Lọc theo độ chịu nước (ATM).
            if (!string.IsNullOrEmpty(waterResistanceAtms))
            {
                var atmList = waterResistanceAtms.Split(',').Select(a => int.Parse(a.Trim())).ToList();
                query = query.Where(p => p.WaterResistanceAtm.HasValue && atmList.Contains(p.WaterResistanceAtm.Value));
            }

            // Lọc theo khoảng giá.
            // Lưu ý: Việc lọc giá phức tạp hơn vì cần xử lý logic OR giữa các khoảng giá.
            if (!string.IsNullOrEmpty(priceRanges))
            {
                var ranges = priceRanges.Split(',');
                var priceFilters = new List<Func<Product, bool>>();

                foreach (var range in ranges)
                {
                    switch (range.Trim())
                    {
                        case "2-5":
                            priceFilters.Add(p => p.Price >= 2000000 && p.Price < 5000000);
                            break;
                        case "5-10":
                            priceFilters.Add(p => p.Price >= 5000000 && p.Price < 10000000);
                            break;
                        case "10-20":
                            priceFilters.Add(p => p.Price >= 10000000 && p.Price < 20000000);
                            break;
                        case "20-50":
                            priceFilters.Add(p => p.Price >= 20000000 && p.Price < 50000000);
                            break;
                        case "50+":
                            priceFilters.Add(p => p.Price >= 50000000);
                            break;
                    }
                }

                if (priceFilters.Any())
                {
                    // Thực hiện query database trước rồi lọc trên memory (Client evaluation) do logic phức tạp.
                    var products = await query.ToListAsync();
                    products = products.Where(p => priceFilters.Any(filter => filter(p))).ToList();
                    
                    // Áp dụng sắp xếp sau khi lọc.
                    products = ApplySorting(products.AsQueryable(), sortBy ?? "newest").ToList();
                    
                    return View(products);
                }
            }

            // Áp dụng sắp xếp.
            query = ApplySorting(query, sortBy ?? "newest");

            var result = await query.ToListAsync();
            return View(result);
        }

        // Helper method: Áp dụng sắp xếp cho query.
        private IQueryable<Product> ApplySorting(IQueryable<Product> query, string sortBy)
        {
            return sortBy switch
            {
                "price-asc" => query.OrderBy(p => p.Price), // Giá tăng dần.
                "price-desc" => query.OrderByDescending(p => p.Price), // Giá giảm dần.
                "name" => query.OrderBy(p => p.Name), // Tên A-Z.
                "newest" => query.OrderByDescending(p => p.CreatedAt), // Mới nhất.
                _ => query.OrderByDescending(p => p.CreatedAt) // Mặc định là mới nhất.
            };
        }

        // API lấy danh sách sản phẩm (AJAX) - dùng cho việc lọc động mà không reload trang.
        // GET: /Product/GetProducts
        [HttpGet]
        public async Task<IActionResult> GetProducts(
            string? brands,
            string? priceRanges,
            string? movements,
            string? genders,
            string? strapTypes,
            string? waterResistanceAtms,
            string? sortBy = "newest",
            string? searchQuery = null)
        {
            // Logic tương tự như Action Index nhưng trả về JSON.
            var query = _context.Products.AsQueryable();

            // Search by name or brand
            if (!string.IsNullOrEmpty(searchQuery))
            {
                var searchTerm = searchQuery.Trim().ToLower();
                query = query.Where(p => 
                    (p.Name != null && p.Name.ToLower().Contains(searchTerm)) ||
                    (p.Brand != null && p.Brand.ToLower().Contains(searchTerm))
                );
            }

            // Filter by brands
            if (!string.IsNullOrEmpty(brands))
            {
                var brandList = brands.Split(',').Select(b => b.Trim().ToLower()).ToList();
                query = query.Where(p => brandList.Contains(p.Brand.ToLower()));
            }

            // Filter by genders
            if (!string.IsNullOrEmpty(genders))
            {
                var genderList = genders.Split(',').Select(g => g.Trim().ToLower()).ToList();
                query = query.Where(p => p.Gender != null && genderList.Contains(p.Gender.ToLower()));
            }

            // Filter by movements
            if (!string.IsNullOrEmpty(movements))
            {
                var movementList = movements.Split(',').Select(m => m.Trim().ToLower()).ToList();
                query = query.Where(p => movementList.Any(m => p.Movement != null && p.Movement.ToLower().Contains(m)));
            }

            // Filter by strap types
            if (!string.IsNullOrEmpty(strapTypes))
            {
                var strapList = strapTypes.Split(',').Select(s => s.Trim().ToLower()).ToList();
                query = query.Where(p => strapList.Any(s => p.StrapType != null && p.StrapType.ToLower().Contains(s)));
            }

            // Filter by water resistance ATM
            if (!string.IsNullOrEmpty(waterResistanceAtms))
            {
                var atmList = waterResistanceAtms.Split(',').Select(a => int.Parse(a.Trim())).ToList();
                query = query.Where(p => p.WaterResistanceAtm.HasValue && atmList.Contains(p.WaterResistanceAtm.Value));
            }

            // Filter by price ranges
            if (!string.IsNullOrEmpty(priceRanges))
            {
                var ranges = priceRanges.Split(',');
                var priceFilters = new List<Func<Product, bool>>();

                foreach (var range in ranges)
                {
                    switch (range.Trim())
                    {
                        case "2-5":
                            priceFilters.Add(p => p.Price >= 2000000 && p.Price < 5000000);
                            break;
                        case "5-10":
                            priceFilters.Add(p => p.Price >= 5000000 && p.Price < 10000000);
                            break;
                        case "10-20":
                            priceFilters.Add(p => p.Price >= 10000000 && p.Price < 20000000);
                            break;
                        case "20-50":
                            priceFilters.Add(p => p.Price >= 20000000 && p.Price < 50000000);
                            break;
                        case "50+":
                            priceFilters.Add(p => p.Price >= 50000000);
                            break;
                    }
                }

                if (priceFilters.Any())
                {
                    var products = await query.ToListAsync();
                    products = products.Where(p => priceFilters.Any(filter => filter(p))).ToList();
                    products = ApplySorting(products.AsQueryable(), sortBy ?? "newest").ToList();
                    
                    return Json(products);
                }
            }

            query = ApplySorting(query, sortBy ?? "newest");

            var result = await query.ToListAsync();
            return Json(result);
        }

        // Action hiển thị chi tiết sản phẩm.
        // GET: /Product/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products.FindAsync(id);
            
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}

