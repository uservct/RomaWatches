using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RomaWatches.Data;
using RomaWatches.Models;

namespace RomaWatches.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<OrderController> _logger;

        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<OrderController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Order
        public async Task<IActionResult> Index(string? status = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var query = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == user.Id)
                .Where(o => o.Status != OrderStatus.Unconfirmed) // Không hiển thị đơn hàng chưa xác nhận thanh toán
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "processing":
                        // Đang xử lý: Pending hoặc Approved
                        query = query.Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Approved);
                        break;
                    case "completed":
                        query = query.Where(o => o.Status == OrderStatus.Completed);
                        break;
                    case "cancelled":
                        query = query.Where(o => o.Status == OrderStatus.Cancelled);
                        break;
                }
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            return View(orders);
        }

        // GET: Order/Details/{id}
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

            // Load order with OrderItems and Products, verify it belongs to the user
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

        // POST: Order/Cancel
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

                // Verify order belongs to user
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == user.Id);

                if (order == null)
                {
                    return Json(new { success = false, message = "Đơn hàng không tồn tại" });
                }

                // Check if order can be cancelled (not Completed)
                if (order.Status == OrderStatus.Completed)
                {
                    return Json(new { success = false, message = "Không thể hủy đơn hàng đã hoàn thành" });
                }

                // Update order status to Cancelled
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

    // Request model for cancelling order
    public class CancelOrderRequest
    {
        public int OrderId { get; set; }
    }
}


