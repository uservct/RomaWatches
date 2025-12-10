using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RomaWatches.Data;
using RomaWatches.Models;
using System.Text.Json;

namespace RomaWatches.Controllers
{
    // Controller quản lý giỏ hàng.
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context; // Context cơ sở dữ liệu.
        private readonly UserManager<ApplicationUser> _userManager; // Quản lý người dùng.
        private readonly ILogger<CartController> _logger; // Logger.

        // Constructor injection.
        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<CartController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Action hiển thị trang giỏ hàng.
        // GET: /Cart
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Trả về giỏ hàng rỗng cho người dùng chưa đăng nhập.
                return View(new Cart { CartItems = new List<CartItem>() });
            }

            // Lấy giỏ hàng của người dùng từ database.
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            // Tự động khôi phục giỏ hàng đã lưu (nếu có) nếu giỏ hàng hiện tại đang trống.
            // (Thường dùng sau khi chức năng "Mua ngay" đã tạm thời xóa giỏ hàng cũ).
            if (cart == null || !cart.CartItems.Any())
            {
                await RestoreCartIfExists(user.Id);
                
                // Tải lại giỏ hàng sau khi khôi phục.
                cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);
            }

            // Nếu vẫn chưa có giỏ hàng, tạo mới một đối tượng rỗng để hiển thị.
            if (cart == null)
            {
                cart = new Cart { UserId = user.Id, CartItems = new List<CartItem>() };
            }

            return View(cart);
        }

        // Action thêm sản phẩm vào giỏ hàng (AJAX).
        // POST: /Cart/Add
        [HttpPost]
        [Authorize] // Yêu cầu đăng nhập.
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Add([FromBody] AddToCartRequest request)
        {
            try
            {
                if (request == null || request.ProductId <= 0)
                {
                    return Json(new { success = false, message = "Thông tin sản phẩm không hợp lệ" });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng" });
                }

                var product = await _context.Products.FindAsync(request.ProductId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                }

                // Lấy hoặc tạo mới giỏ hàng.
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserId = user.Id,
                        CreatedAt = DateTime.Now
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                // Kiểm tra xem sản phẩm đã có trong giỏ hàng chưa.
                var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);
                if (existingItem != null)
                {
                    // Nếu có rồi thì tăng số lượng.
                    existingItem.Quantity += 1;
                    existingItem.UpdatedAt = DateTime.Now;
                }
                else
                {
                    // Nếu chưa có thì thêm mới.
                    var cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = request.ProductId,
                        Quantity = 1,
                        CreatedAt = DateTime.Now
                    };
                    cart.CartItems.Add(cartItem);
                }

                cart.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // Lấy tổng số lượng sản phẩm trong giỏ để cập nhật UI.
                var cartCount = await GetCartCountInternal(user.Id);

                return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng", cartCount = cartCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product to cart");
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm sản phẩm vào giỏ hàng" });
            }
        }

        // Action cập nhật số lượng sản phẩm trong giỏ hàng (AJAX).
        // POST: /Cart/Update
        [HttpPost]
        [Authorize]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Update([FromBody] UpdateCartItemRequest request)
        {
            try
            {
                if (request == null || request.CartItemId <= 0 || request.Quantity < 1)
                {
                    return Json(new { success = false, message = "Số lượng phải lớn hơn 0" });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                // Tìm sản phẩm trong giỏ hàng.
                var cartItem = await _context.CartItems
                    .Include(ci => ci.Cart)
                    .Include(ci => ci.Product)
                    .FirstOrDefaultAsync(ci => ci.Id == request.CartItemId && ci.Cart.UserId == user.Id);

                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng" });
                }

                // Cập nhật số lượng.
                cartItem.Quantity = request.Quantity;
                cartItem.UpdatedAt = DateTime.Now;
                cartItem.Cart.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // Tính toán lại tổng tiền.
                var subtotal = cartItem.Quantity * cartItem.Product.Price;
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                var total = cart?.CartItems.Sum(ci => ci.Quantity * ci.Product.Price) ?? 0;

                return Json(new { 
                    success = true, 
                    message = "Đã cập nhật số lượng", 
                    subtotal = subtotal,
                    total = total
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart item");
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật số lượng" });
            }
        }

        // Action xóa sản phẩm khỏi giỏ hàng (AJAX).
        // POST: /Cart/Remove
        [HttpPost]
        [Authorize]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Remove([FromBody] RemoveCartItemRequest request)
        {
            try
            {
                if (request == null || request.CartItemId <= 0)
                {
                    return Json(new { success = false, message = "Thông tin không hợp lệ" });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                // Tìm sản phẩm cần xóa.
                var cartItem = await _context.CartItems
                    .Include(ci => ci.Cart)
                    .FirstOrDefaultAsync(ci => ci.Id == request.CartItemId && ci.Cart.UserId == user.Id);

                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng" });
                }

                // Xóa sản phẩm.
                _context.CartItems.Remove(cartItem);
                cartItem.Cart.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // Tính toán lại tổng tiền và số lượng.
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                var total = cart?.CartItems.Sum(ci => ci.Quantity * ci.Product.Price) ?? 0;
                var cartCount = await GetCartCountInternal(user.Id);

                return Json(new { 
                    success = true, 
                    message = "Đã xóa sản phẩm khỏi giỏ hàng",
                    total = total,
                    cartCount = cartCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa sản phẩm" });
            }
        }

        // Action lấy số lượng sản phẩm trong giỏ hàng (AJAX).
        // GET: /Cart/GetCartCount
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCartCount()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { count = 0 });
                }

                var count = await GetCartCountInternal(user.Id);
                return Json(new { count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart count");
                return Json(new { count = 0 });
            }
        }

        // Action "Mua ngay" (Buy Now).
        // Chức năng này sẽ lưu giỏ hàng hiện tại vào session, xóa giỏ hàng, và thêm sản phẩm "Mua ngay" vào.
        // POST: /Cart/BuyNow
        [HttpPost]
        [Authorize]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> BuyNow([FromBody] AddToCartRequest request)
        {
            try
            {
                if (request == null || request.ProductId <= 0)
                {
                    return Json(new { success = false, message = "Thông tin sản phẩm không hợp lệ" });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để mua sản phẩm" });
                }

                var product = await _context.Products.FindAsync(request.ProductId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                }

                // Lấy hoặc tạo giỏ hàng.
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserId = user.Id,
                        CreatedAt = DateTime.Now
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                // Lưu các sản phẩm hiện có trong giỏ hàng vào Session (để khôi phục sau).
                if (cart.CartItems.Any())
                {
                    var savedCartItems = cart.CartItems.Select(ci => new
                    {
                        ProductId = ci.ProductId,
                        Quantity = ci.Quantity
                    }).ToList();
                    
                    HttpContext.Session.SetString($"SavedCart_{user.Id}", JsonSerializer.Serialize(savedCartItems));
                }

                // Xóa tất cả sản phẩm hiện tại trong giỏ hàng.
                _context.CartItems.RemoveRange(cart.CartItems);

                // Thêm sản phẩm "Mua ngay" vào giỏ hàng.
                var cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = request.ProductId,
                    Quantity = 1,
                    CreatedAt = DateTime.Now
                };
                cart.CartItems.Add(cartItem);

                cart.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                var cartCount = await GetCartCountInternal(user.Id);

                return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng", cartCount = cartCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BuyNow");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xử lý đơn hàng" });
            }
        }

        // Action khôi phục giỏ hàng đã lưu từ Session (AJAX).
        // POST: /Cart/RestoreCart
        [HttpPost]
        [Authorize]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RestoreCart()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                // Lấy dữ liệu giỏ hàng đã lưu từ Session.
                var savedCartJson = HttpContext.Session.GetString($"SavedCart_{user.Id}");
                if (string.IsNullOrEmpty(savedCartJson))
                {
                    return Json(new { success = false, message = "Không có giỏ hàng đã lưu" });
                }

                var savedCartItems = JsonSerializer.Deserialize<List<SavedCartItem>>(savedCartJson);
                if (savedCartItems == null || !savedCartItems.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng đã lưu trống" });
                }

                // Lấy hoặc tạo giỏ hàng.
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserId = user.Id,
                        CreatedAt = DateTime.Now
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                // Xóa các sản phẩm hiện tại (ví dụ: sản phẩm vừa "Mua ngay" xong).
                _context.CartItems.RemoveRange(cart.CartItems);

                // Khôi phục các sản phẩm từ Session.
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

                // Xóa dữ liệu trong Session sau khi đã khôi phục.
                HttpContext.Session.Remove($"SavedCart_{user.Id}");

                var cartCount = await GetCartCountInternal(user.Id);

                return Json(new { success = true, message = "Đã khôi phục giỏ hàng", cartCount = cartCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring cart");
                return Json(new { success = false, message = "Có lỗi xảy ra khi khôi phục giỏ hàng" });
            }
        }

        // Helper method: Tự động khôi phục giỏ hàng nếu tồn tại trong session.
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

                // Chỉ khôi phục nếu giỏ hàng hiện tại đang trống (để tránh ghi đè dữ liệu mới).
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

        // Helper method: Lấy tổng số lượng sản phẩm trong giỏ hàng.
        private async Task<int> GetCartCountInternal(string userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            return cart?.CartItems.Sum(ci => ci.Quantity) ?? 0;
        }
    }

    // Các class DTO (Data Transfer Object) cho request.
    public class AddToCartRequest
    {
        public int ProductId { get; set; }
    }

    public class UpdateCartItemRequest
    {
        public int CartItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class RemoveCartItemRequest
    {
        public int CartItemId { get; set; }
    }
    
    // Class hỗ trợ lưu trữ tạm thời trong Session.
    public class SavedCartItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}

