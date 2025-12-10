using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RomaWatches.Data;
using RomaWatches.Models;

namespace RomaWatches.Controllers
{
    // Controller quản lý trang chủ và các trang chung.
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger; // Logger để ghi log.
        private readonly ApplicationDbContext _context; // Context cơ sở dữ liệu.

        // Constructor injection.
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Action hiển thị trang chủ.
        // GET: /
        public async Task<IActionResult> Index()
        {
            // Lấy 4 sản phẩm mới nhất từ cơ sở dữ liệu để hiển thị.
            var latestProducts = await _context.Products
                .OrderByDescending(p => p.Id) // Sắp xếp giảm dần theo ID (mới nhất lên đầu).
                .Take(4) // Lấy 4 sản phẩm.
                .ToListAsync();
            
            // Trả về view Index kèm theo danh sách sản phẩm.
            return View(latestProducts);
        }

        // Action hiển thị trang chính sách quyền riêng tư.
        // GET: /Home/Privacy
        public IActionResult Privacy()
        {
            return View();
        }

        // Action xử lý lỗi.
        // ResponseCache: Không lưu cache cho trang lỗi.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Trả về view Error với thông tin về RequestId để debug.
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
