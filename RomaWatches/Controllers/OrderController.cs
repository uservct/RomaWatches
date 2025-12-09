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
    }
}


