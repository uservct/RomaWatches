using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RomaWatches.Data;
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
        private readonly ApplicationDbContext _context; // Context cơ sở dữ liệu.

        // Constructor injection để lấy các dependencies.
        public AdminController(ILogger<AdminController> logger, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;
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
            
            // Tính toán thống kê 30 ngày qua
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            
            // Tổng doanh thu (chỉ tính đơn hàng đã hoàn thành hoặc đã duyệt)
            var totalRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= thirtyDaysAgo && 
                           (o.Status == OrderStatus.Approved || o.Status == OrderStatus.Completed))
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            
            // Đơn hàng mới (30 ngày qua, không tính Unconfirmed)
            var newOrdersCount = await _context.Orders
                .Where(o => o.CreatedAt >= thirtyDaysAgo && o.Status != OrderStatus.Unconfirmed)
                .CountAsync();
            
            // Tổng sản phẩm đã bán (30 ngày qua, từ đơn hàng đã duyệt/hoàn thành)
            var totalProductsSold = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.CreatedAt >= thirtyDaysAgo && 
                            (oi.Order.Status == OrderStatus.Approved || oi.Order.Status == OrderStatus.Completed))
                .SumAsync(oi => (int?)oi.Quantity) ?? 0;
            
            // Đơn hàng gần đây (5 đơn hàng mới nhất, không tính Unconfirmed)
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .Where(o => o.Status != OrderStatus.Unconfirmed)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync();
            
            // Sản phẩm bán chạy nhất (top 5, tính từ OrderItems của đơn hàng đã duyệt/hoàn thành)
            var topProducts = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.Status == OrderStatus.Approved || oi.Order.Status == OrderStatus.Completed)
                .GroupBy(oi => new { oi.ProductId, oi.Product.Name, oi.Product.ImageUrl, oi.Product.Price })
                .Select(g => new TopProductViewModel
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name ?? string.Empty,
                    ProductImageUrl = g.Key.ImageUrl ?? string.Empty,
                    ProductPrice = g.Key.Price,
                    TotalSold = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToListAsync();
            
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.NewOrdersCount = newOrdersCount;
            ViewBag.TotalProductsSold = totalProductsSold;
            ViewBag.RecentOrders = recentOrders;
            ViewBag.TopProducts = topProducts;
            
            // Trả về view Dashboard nếu là admin.
            return View();
        }

        // Helper method: Kiểm tra quyền admin
        private async Task<bool> IsAdminAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user != null && user.Role == "admin";
        }

        // Action hiển thị danh sách đơn hàng.
        // GET: /Admin/Orders
        public async Task<IActionResult> Orders(string? search = null)
        {
            if (!await IsAdminAsync())
            {
                return RedirectToAction("Index", "Home");
            }

            var query = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .AsQueryable();

            // Tìm kiếm theo mã đơn hàng hoặc tên khách hàng
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                // Tìm theo mã đơn hàng (#RW{id}) hoặc tên khách hàng
                query = query.Where(o => 
                    o.FullName.ToLower().Contains(searchLower) ||
                    ($"#RW{o.Id}").ToLower().Contains(searchLower)
                );
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            ViewBag.Search = search;
            return View("Orders/Index", orders);
        }

        // Action hiển thị chi tiết đơn hàng.
        // GET: /Admin/Orders/Details/{id}
        public async Task<IActionResult> OrderDetails(int id)
        {
            if (!await IsAdminAsync())
            {
                return RedirectToAction("Index", "Home");
            }

            if (id <= 0)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View("Orders/Details", order);
        }

        // Action cập nhật trạng thái đơn hàng (AJAX).
        // POST: /Admin/UpdateOrderStatus hoặc /Admin/Orders/UpdateStatus
        [HttpPost]
        [Route("/Admin/UpdateOrderStatus")]
        [Route("/Admin/Orders/UpdateStatus")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                _logger.LogInformation("UpdateOrderStatus called with request: OrderId={OrderId}, NewStatus={NewStatus}, NewStatusString={NewStatusString}", 
                    request?.OrderId ?? 0, request?.NewStatus.ToString() ?? "null", request?.NewStatusString ?? "null");

                if (!await IsAdminAsync())
                {
                    _logger.LogWarning("Unauthorized attempt to update order status");
                    return Json(new { success = false, message = "Không có quyền truy cập" });
                }

                if (request == null)
                {
                    _logger.LogWarning("UpdateOrderStatus: Request is null");
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ: Request null" });
                }

                if (request.OrderId <= 0)
                {
                    _logger.LogWarning("UpdateOrderStatus: Invalid OrderId: {OrderId}", request.OrderId);
                    return Json(new { success = false, message = "Mã đơn hàng không hợp lệ" });
                }

                // Parse enum từ string - ưu tiên các field string
                OrderStatus newStatus;
                string? statusString = request.NewStatusString;
                
                if (!string.IsNullOrEmpty(statusString))
                {
                    // Parse từ string (có thể là "Unconfirmed", "Pending", etc.)
                    if (!Enum.TryParse<OrderStatus>(statusString, true, out newStatus))
                    {
                        _logger.LogWarning("UpdateOrderStatus: Invalid status string: {Status}", statusString);
                        return Json(new { success = false, message = $"Trạng thái không hợp lệ: {statusString}" });
                    }
                    _logger.LogInformation("UpdateOrderStatus: Parsed status from string '{StatusString}' to {Status}", statusString, newStatus);
                }
                else if (request.NewStatus != default(OrderStatus))
                {
                    // Sử dụng enum trực tiếp nếu có
                    newStatus = request.NewStatus;
                    _logger.LogInformation("UpdateOrderStatus: Using enum status: {Status}", newStatus);
                }
                else
                {
                    _logger.LogWarning("UpdateOrderStatus: No valid status provided. NewStatus={NewStatus}, NewStatusString={NewStatusString}", 
                        request.NewStatus, request.NewStatusString);
                    return Json(new { success = false, message = "Trạng thái không được cung cấp" });
                }

                var order = await _context.Orders.FindAsync(request.OrderId);
                if (order == null)
                {
                    _logger.LogWarning("UpdateOrderStatus: Order not found: {OrderId}", request.OrderId);
                    return Json(new { success = false, message = "Đơn hàng không tồn tại" });
                }

                var oldStatus = order.Status;
                _logger.LogInformation("UpdateOrderStatus: Order {OrderId} - Changing status from {OldStatus} to {NewStatus}", 
                    request.OrderId, oldStatus, newStatus);

                // Validate chuyển đổi trạng thái hợp lệ
                if (!IsValidStatusTransition(oldStatus, newStatus))
                {
                    _logger.LogWarning("UpdateOrderStatus: Invalid status transition from {OldStatus} to {NewStatus}", 
                        oldStatus, newStatus);
                    return Json(new { 
                        success = false, 
                        message = $"Không thể chuyển từ trạng thái \"{GetOrderStatusText(oldStatus)}\" sang \"{GetOrderStatusText(newStatus)}\"" 
                    });
                }

                // Cập nhật trạng thái
                order.Status = newStatus;
                order.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation("UpdateOrderStatus: Successfully updated order {OrderId} from {OldStatus} to {NewStatus}", 
                    request.OrderId, oldStatus, newStatus);

                return Json(new { 
                    success = true, 
                    message = $"Cập nhật trạng thái thành công: {GetOrderStatusText(newStatus)}", 
                    newStatus = newStatus.ToString() 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for OrderId: {OrderId}", request?.OrderId ?? 0);
                return Json(new { success = false, message = $"Có lỗi xảy ra khi cập nhật trạng thái: {ex.Message}" });
            }
        }

        // Helper method: Kiểm tra chuyển đổi trạng thái hợp lệ
        // Admin có thể chuyển đổi linh hoạt giữa các trạng thái, trừ Completed và Cancelled
        private bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
        {
            // Không cho phép chuyển từ Completed hoặc Cancelled
            if (currentStatus == OrderStatus.Completed || currentStatus == OrderStatus.Cancelled)
            {
                return false;
            }

            // Không cho phép chuyển sang chính nó
            if (currentStatus == newStatus)
            {
                return false;
            }

            // Cho phép hủy từ bất kỳ trạng thái nào (trừ Completed và Cancelled)
            if (newStatus == OrderStatus.Cancelled)
            {
                return true;
            }

            // Không cho phép chuyển sang Completed từ Unconfirmed (phải qua Pending hoặc Approved trước)
            if (newStatus == OrderStatus.Completed && currentStatus == OrderStatus.Unconfirmed)
            {
                return false;
            }

            // Cho phép tất cả các chuyển đổi khác giữa Unconfirmed, Pending, Approved, Completed
            // (trừ các trường hợp đã được xử lý ở trên)
            return true;
        }

        // Helper method: Lấy text trạng thái thanh toán
        public static string GetPaymentStatusText(Order order)
        {
            if (order.Status == OrderStatus.Cancelled)
            {
                return "Đã hủy";
            }

            if (order.PaymentMethod == PaymentMethod.BankTransfer)
            {
                if (order.Status == OrderStatus.Unconfirmed || order.Status == OrderStatus.Pending)
                {
                    return "Chờ thanh toán";
                }
                return "Đã thanh toán";
            }
            else // COD hoặc InStore
            {
                if (order.Status == OrderStatus.Approved || order.Status == OrderStatus.Completed)
                {
                    return "Đã thanh toán";
                }
                return "Chờ thanh toán";
            }
        }

        // Helper method: Lấy class màu cho badge trạng thái thanh toán
        public static string GetPaymentStatusColor(Order order)
        {
            if (order.Status == OrderStatus.Cancelled)
            {
                return "bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300";
            }

            if (order.PaymentMethod == PaymentMethod.BankTransfer)
            {
                if (order.Status == OrderStatus.Unconfirmed || order.Status == OrderStatus.Pending)
                {
                    return "bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300";
                }
                return "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300";
            }
            else
            {
                if (order.Status == OrderStatus.Approved || order.Status == OrderStatus.Completed)
                {
                    return "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300";
                }
                return "bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300";
            }
        }

        // Helper method: Lấy text trạng thái đơn hàng
        public static string GetOrderStatusText(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Unconfirmed => "Đang chờ xác nhận",
                OrderStatus.Pending => "Đã xác nhận",
                OrderStatus.Approved => "Đang giao hàng",
                OrderStatus.Completed => "Đã giao hàng",
                OrderStatus.Cancelled => "Đã hủy",
                _ => status.ToString()
            };
        }

        // Helper method: Lấy class màu cho badge trạng thái đơn hàng
        public static string GetOrderStatusColor(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Unconfirmed => "bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300",
                OrderStatus.Pending => "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300",
                OrderStatus.Approved => "bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300",
                OrderStatus.Completed => "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300",
                OrderStatus.Cancelled => "bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300",
                _ => "bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-300"
            };
        }
    }

    // DTO cho request cập nhật trạng thái đơn hàng
    public class UpdateOrderStatusRequest
    {
        public int OrderId { get; set; }
        public OrderStatus NewStatus { get; set; }
        public string? NewStatusString { get; set; } // Hỗ trợ parse từ string
    }
}

// ViewModel cho sản phẩm bán chạy
namespace RomaWatches.Models
{
    public class TopProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductImageUrl { get; set; } = string.Empty;
        public decimal ProductPrice { get; set; }
        public int TotalSold { get; set; }
    }
}
