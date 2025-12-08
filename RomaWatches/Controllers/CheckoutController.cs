using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RomaWatches.Data;
using RomaWatches.Models;
using System.Text.Json;

namespace RomaWatches.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<CheckoutController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Checkout
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (cart == null || !cart.CartItems.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            return View(cart);
        }

        // POST: Checkout/Process
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Process([FromBody] CheckoutRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.FullName) ||
                    string.IsNullOrWhiteSpace(request.PhoneNumber) ||
                    string.IsNullOrWhiteSpace(request.Province) ||
                    string.IsNullOrWhiteSpace(request.Ward) ||
                    string.IsNullOrWhiteSpace(request.Address))
                {
                    return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin giao hàng" });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                // Get cart with items
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (cart == null || !cart.CartItems.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng trống" });
                }

                // Calculate totals
                var subtotal = cart.CartItems.Sum(ci => ci.Quantity * ci.Product.Price);
                var shippingFee = 0m; // Có thể tính sau
                var total = subtotal + shippingFee;

                // Parse payment method from string
                if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, out var paymentMethod))
                {
                    return Json(new { success = false, message = "Phương thức thanh toán không hợp lệ" });
                }

                // Determine order status based on payment method
                var status = paymentMethod == PaymentMethod.BankTransfer 
                    ? OrderStatus.Pending 
                    : OrderStatus.Approved;

                // Create order
                var order = new Order
                {
                    UserId = user.Id,
                    FullName = request.FullName.Trim(),
                    PhoneNumber = request.PhoneNumber.Trim(),
                    Province = request.Province.Trim(),
                    Ward = request.Ward.Trim(),
                    Address = request.Address.Trim(),
                    PaymentMethod = paymentMethod,
                    Status = status,
                    TotalAmount = total,
                    ShippingFee = shippingFee,
                    CreatedAt = DateTime.Now
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Create order items
                foreach (var cartItem in cart.CartItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        Price = cartItem.Product.Price, // Lưu giá tại thời điểm đặt hàng
                        CreatedAt = DateTime.Now
                    };
                    _context.OrderItems.Add(orderItem);
                }

                // Clear cart
                _context.CartItems.RemoveRange(cart.CartItems);
                cart.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // Prepare response message
                var message = status == OrderStatus.Pending
                    ? "Đơn hàng đang chờ xét duyệt. Chúng tôi sẽ liên hệ với bạn sớm nhất."
                    : "Đặt hàng thành công! Cảm ơn bạn đã mua sắm tại Roma Watches.";

                return Json(new 
                { 
                    success = true, 
                    message = message,
                    orderId = order.Id,
                    status = status.ToString(),
                    redirectUrl = "/"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing checkout");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xử lý đơn hàng. Vui lòng thử lại." });
            }
        }
    }

    // Request model
    public class CheckoutRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty; // String để parse từ JSON
    }
}

