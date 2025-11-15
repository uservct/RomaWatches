using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RomaWatches.Data;
using RomaWatches.Models;

namespace RomaWatches.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductController> _logger;

        public ProductController(ApplicationDbContext context, ILogger<ProductController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(
            string? brands,
            string? priceRanges,
            string? movements,
            string? genders,
            string? strapTypes,
            string? waterResistanceAtms,
            string? sortBy = "newest")
        {
            var query = _context.Products.AsQueryable();

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
                    
                    // Apply sorting
                    products = ApplySorting(products.AsQueryable(), sortBy ?? "newest").ToList();
                    
                    return View(products);
                }
            }

            // Apply sorting
            query = ApplySorting(query, sortBy ?? "newest");

            var result = await query.ToListAsync();
            return View(result);
        }

        private IQueryable<Product> ApplySorting(IQueryable<Product> query, string sortBy)
        {
            return sortBy switch
            {
                "price-asc" => query.OrderBy(p => p.Price),
                "price-desc" => query.OrderByDescending(p => p.Price),
                "name" => query.OrderBy(p => p.Name),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts(
            string? brands,
            string? priceRanges,
            string? movements,
            string? genders,
            string? strapTypes,
            string? waterResistanceAtms,
            string? sortBy = "newest")
        {
            var query = _context.Products.AsQueryable();

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

