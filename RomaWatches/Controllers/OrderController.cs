using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RomaWatches.Data;
using RomaWatches.Models;

namespace RomaWatches.Controllers
{
    // Controller quản lý đơn hàng của người dùng.
    // Yêu cầu đăng nhập.
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context; // Context cơ sở dữ liệu.
        private readonly UserManager<ApplicationUser> _userManager; // Quản lý người dùng.
        private readonly ILogger<OrderController> _logger; // Logger.

        // Constructor injection.
        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<OrderController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Action hiển thị danh sách đơn hàng (Lịch sử mua hàng).
        // GET: /Order
        public async Task<IActionResult> Index(string? status = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Truy vấn đơn hàng của người dùng.
            var query = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == user.Id)
                .AsQueryable();

            // Lọc theo trạng thái đơn hàng.
            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "processing":
                        // Đang xử lý: Bao gồm Unconfirmed (Chờ xác nhận), Pending (Đã xác nhận) và Approved (Đang giao hàng).
                        query = query.Where(o => o.Status == OrderStatus.Unconfirmed || o.Status == OrderStatus.Pending || o.Status == OrderStatus.Approved);
                        break;
                    case "completed":
                        // Đã hoàn thành.
                        query = query.Where(o => o.Status == OrderStatus.Completed);
                        break;
                    case "cancelled":
                        // Đã hủy.
                        query = query.Where(o => o.Status == OrderStatus.Cancelled);
                        break;
                }
            }

            // Sắp xếp theo thời gian tạo mới nhất.
            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            return View(orders);
        }

        // Action hiển thị chi tiết đơn hàng.
        // GET: /Order/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Tải thông tin đơn hàng và chi tiết sản phẩm.
            // Đảm bảo đơn hàng thuộc về người dùng hiện tại.
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // Action hủy đơn hàng (AJAX).
        // POST: /Order/Cancel
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Cancel([FromBody] CancelOrderRequest request)
        {
            try
            {
                if (request == null || request.OrderId <= 0)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                // Kiểm tra đơn hàng thuộc về người dùng.
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == user.Id);

                if (order == null)
                {
                    return Json(new { success = false, message = "Đơn hàng không tồn tại" });
                }

                // Kiểm tra xem đơn hàng có thể hủy được không (chỉ hủy được nếu chưa hoàn thành).
                if (order.Status == OrderStatus.Completed)
                {
                    return Json(new { success = false, message = "Không thể hủy đơn hàng đã hoàn thành" });
                }

                // Cập nhật trạng thái thành Đã hủy.
                order.Status = OrderStatus.Cancelled;
                order.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đơn hàng đã được hủy thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order");
                return Json(new { success = false, message = "Có lỗi xảy ra khi hủy đơn hàng. Vui lòng thử lại." });
            }
        }
    }

    // Class DTO cho request hủy đơn hàng.
    public class CancelOrderRequest
    {
        public int OrderId { get; set; }
    }
}


