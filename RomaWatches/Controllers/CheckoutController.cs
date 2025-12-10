using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RomaWatches.Data;
using RomaWatches.Models;
using System.Text.Json;

namespace RomaWatches.Controllers
{
    // Controller xử lý quá trình thanh toán (Checkout).
    // Yêu cầu người dùng phải đăng nhập.
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context; // Context cơ sở dữ liệu.
        private readonly UserManager<ApplicationUser> _userManager; // Quản lý người dùng.
        private readonly ILogger<CheckoutController> _logger; // Logger.

        // Constructor injection.
        public CheckoutController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<CheckoutController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Action hiển thị trang thanh toán.
        // GET: /Checkout
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy giỏ hàng của người dùng.
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            // Nếu giỏ hàng trống, thử khôi phục từ Session (trường hợp "Mua ngay").
            if (cart == null || !cart.CartItems.Any())
            {
                var restored = await RestoreCartIfExists(user.Id);
                
                if (restored)
                {
                    // Tải lại giỏ hàng sau khi khôi phục.
                    cart = await _context.Carts
                        .Include(c => c.CartItems)
                            .ThenInclude(ci => ci.Product)
                        .FirstOrDefaultAsync(c => c.UserId == user.Id);
                }
                
                // Nếu vẫn trống sau khi thử khôi phục, chuyển hướng về trang giỏ hàng.
                if (cart == null || !cart.CartItems.Any())
                {
                    return RedirectToAction("Index", "Cart");
                }
            }

            return View(cart);
        }


        // Action xử lý đặt hàng (AJAX).
        // POST: /Checkout/Process
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

                // Kiểm tra các trường thông tin bắt buộc.
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

                // Lấy giỏ hàng và sản phẩm.
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (cart == null || !cart.CartItems.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng trống" });
                }

                // Tính toán tổng tiền.
                var subtotal = cart.CartItems.Sum(ci => ci.Quantity * ci.Product.Price);
                var shippingFee = 0m; // Phí vận chuyển (có thể tính toán phức tạp hơn sau này).
                var total = subtotal + shippingFee;

                // Parse phương thức thanh toán từ chuỗi.
                if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, out var paymentMethod))
                {
                    return Json(new { success = false, message = "Phương thức thanh toán không hợp lệ" });
                }

                // Tạo đơn hàng mới
                Order order;
                if (paymentMethod == PaymentMethod.BankTransfer)
                {
                    // Tạo đơn hàng với status Unconfirmed cho BankTransfer (chờ admin xác nhận)
                    order = new Order
                    {
                        UserId = user.Id,
                        FullName = request.FullName.Trim(),
                        PhoneNumber = request.PhoneNumber.Trim(),
                        Province = request.Province.Trim(),
                        Ward = request.Ward.Trim(),
                        Address = request.Address.Trim(),
                        PaymentMethod = paymentMethod,
                        Status = OrderStatus.Unconfirmed,
                        TotalAmount = total,
                        ShippingFee = shippingFee,
                        CreatedAt = DateTime.Now
                    };
                }
                else
                {
                    // Tạo đơn hàng với status Approved cho COD hoặc InStore
                    order = new Order
                    {
                        UserId = user.Id,
                        FullName = request.FullName.Trim(),
                        PhoneNumber = request.PhoneNumber.Trim(),
                        Province = request.Province.Trim(),
                        Ward = request.Ward.Trim(),
                        Address = request.Address.Trim(),
                        PaymentMethod = paymentMethod,
                        Status = OrderStatus.Approved,
                        TotalAmount = total,
                        ShippingFee = shippingFee,
                        CreatedAt = DateTime.Now
                    };
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Tạo chi tiết đơn hàng (OrderItems) từ giỏ hàng.
                foreach (var cartItem in cart.CartItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        Price = cartItem.Product.Price,
                        CreatedAt = DateTime.Now
                    };
                    _context.OrderItems.Add(orderItem);
                }

                await _context.SaveChangesAsync();

                // Xóa giỏ hàng sau khi đặt hàng thành công.
                _context.CartItems.RemoveRange(cart.CartItems);
                cart.UpdatedAt = DateTime.Now;
                
                // Xóa giỏ hàng đã lưu trong Session.
                HttpContext.Session.Remove($"SavedCart_{user.Id}");

                await _context.SaveChangesAsync();

                // Chuẩn bị thông báo phản hồi.
                var message = order.Status == OrderStatus.Unconfirmed
                    ? "Đơn hàng đang chờ xác nhận thanh toán. Chúng tôi sẽ kiểm tra và xác nhận đơn hàng của bạn sớm nhất."
                    : "Đặt hàng thành công! Cảm ơn bạn đã mua sắm tại Roma Watches.";

                // Tất cả các phương thức đều redirect đến trang success.
                var redirectUrl = $"/Checkout/Success?orderId={order.Id}";

                // Nếu là chuyển khoản ngân hàng, trả về thông tin mã QR để thanh toán.
                if (paymentMethod == PaymentMethod.BankTransfer)
                {
                    var accountNumber = "62688888888686";
                    var bank = "MB";
                    var amount = total.ToString("F0");
                    var description = "thanh toan don hang RomaWatches";
                    
                    // Tạo link QR code SePay/VietQR với nội dung cố định.
                    var qrCodeUrl = $"https://qr.sepay.vn/img?acc={accountNumber}&bank={bank}&amount={amount}&des={Uri.EscapeDataString(description)}";
                    
                    return Json(new 
                    { 
                        success = true, 
                        message = message,
                        orderId = order.Id,
                        status = order.Status.ToString(),
                        redirectUrl = redirectUrl,
                        paymentMethod = "BankTransfer",
                        qrCodeUrl = qrCodeUrl,
                        accountNumber = "626 8888 8888 686",
                        accountHolder = "VU CHI THANH",
                        amount = total
                    });
                }

                return Json(new 
                { 
                    success = true, 
                    message = message,
                    orderId = order.Id,
                    status = order.Status.ToString(),
                    redirectUrl = redirectUrl,
                    paymentMethod = paymentMethod.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing checkout");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xử lý đơn hàng. Vui lòng thử lại." });
            }
        }

        // Action hiển thị trang "Đặt hàng thành công".
        // GET: /Checkout/Success
        public async Task<IActionResult> Success(int orderId)
        {
            if (orderId <= 0)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Tải thông tin đơn hàng cùng với chi tiết sản phẩm.
            // Đảm bảo đơn hàng thuộc về người dùng hiện tại.
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // Action xác nhận đã thanh toán (cho phương thức chuyển khoản).
        // POST: /Checkout/ConfirmPayment
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
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

                // Không cần xử lý gì vì đơn hàng đã được tạo với status Pending

                // Xóa giỏ hàng sau khi người dùng xác nhận thanh toán.
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (cart != null && cart.CartItems.Any())
                {
                    _context.CartItems.RemoveRange(cart.CartItems);
                    cart.UpdatedAt = DateTime.Now;
                }
                
                // Xóa giỏ hàng lưu trong Session.
                HttpContext.Session.Remove($"SavedCart_{user.Id}");

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = "Xác nhận thanh toán thành công",
                    redirectUrl = $"/Checkout/Success?orderId={order.Id}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xác nhận thanh toán. Vui lòng thử lại." });
            }
        }

        // Helper method: Tự động khôi phục giỏ hàng từ Session nếu có.
        private async Task<bool> RestoreCartIfExists(string userId)
        {
            try
            {
                var savedCartJson = HttpContext.Session.GetString($"SavedCart_{userId}");
                if (string.IsNullOrEmpty(savedCartJson))
                {
                    return false;
                }

                var savedCartItems = JsonSerializer.Deserialize<List<SavedCartItem>>(savedCartJson);
                if (savedCartItems == null || !savedCartItems.Any())
                {
                    HttpContext.Session.Remove($"SavedCart_{userId}");
                    return false;
                }

                // Lấy hoặc tạo giỏ hàng.
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserId = userId,
                        CreatedAt = DateTime.Now
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                // Chỉ khôi phục nếu giỏ hàng hiện tại trống.
                if (!cart.CartItems.Any())
                {
                    foreach (var savedItem in savedCartItems)
                    {
                        var cartItem = new CartItem
                        {
                            CartId = cart.Id,
                            ProductId = savedItem.ProductId,
                            Quantity = savedItem.Quantity,
                            CreatedAt = DateTime.Now
                        };
                        cart.CartItems.Add(cartItem);
                    }

                    cart.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                // Xóa session sau khi khôi phục.
                HttpContext.Session.Remove($"SavedCart_{userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-restoring cart for user {UserId}", userId);
                return false;
            }
        }
    }

    // Class DTO cho request đặt hàng.
    public class CheckoutRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty; // String để parse từ JSON.
    }

    // Class DTO cho request xác nhận thanh toán.
    public class ConfirmPaymentRequest
    {
        public int OrderId { get; set; }
    }
}

