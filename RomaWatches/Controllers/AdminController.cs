using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RomaWatches.Models;

namespace RomaWatches.Controllers
{
    // Controller quản lý khu vực quản trị (Admin).
    // Yêu cầu người dùng phải đăng nhập mới được truy cập.
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger; // Logger để ghi log.
        private readonly UserManager<ApplicationUser> _userManager; // Quản lý người dùng Identity.

        // Constructor injection để lấy các dependencies.
        public AdminController(ILogger<AdminController> logger, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        // Action hiển thị trang Dashboard của Admin.
        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            // Lấy thông tin người dùng hiện tại.
            var user = await _userManager.GetUserAsync(User);
            
            // Kiểm tra nếu người dùng không tồn tại hoặc không phải là admin.
            if (user == null || user.Role != "admin")
            {
                // Chuyển hướng về trang chủ nếu không có quyền.
                return RedirectToAction("Index", "Home");
            }
            
            // Trả về view Dashboard nếu là admin.
            return View();
        }
    }
}
