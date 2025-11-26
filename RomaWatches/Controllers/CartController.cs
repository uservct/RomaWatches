using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RomaWatches.Data;
using RomaWatches.Models;

namespace RomaWatches.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CartController> _logger;

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<CartController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Cart
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Return empty cart for non-authenticated users
                return View(new Cart { CartItems = new List<CartItem>() });
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (cart == null || !cart.CartItems.Any())
            {
                cart = new Cart { UserId = user.Id, CartItems = new List<CartItem>() };
            }

            return View(cart);
        }

        // POST: Cart/Add
        [HttpPost]
        [Authorize]
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

                // Get or create cart
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

                // Check if product already exists in cart
                var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);
                if (existingItem != null)
                {
                    existingItem.Quantity += 1;
                    existingItem.UpdatedAt = DateTime.Now;
                }
                else
                {
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

                // Get updated cart count
                var cartCount = await GetCartCountInternal(user.Id);

                return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng", cartCount = cartCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product to cart");
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm sản phẩm vào giỏ hàng" });
            }
        }

        // POST: Cart/Update
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

                var cartItem = await _context.CartItems
                    .Include(ci => ci.Cart)
                    .Include(ci => ci.Product)
                    .FirstOrDefaultAsync(ci => ci.Id == request.CartItemId && ci.Cart.UserId == user.Id);

                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng" });
                }

                cartItem.Quantity = request.Quantity;
                cartItem.UpdatedAt = DateTime.Now;
                cartItem.Cart.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

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

        // POST: Cart/Remove
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

                var cartItem = await _context.CartItems
                    .Include(ci => ci.Cart)
                    .FirstOrDefaultAsync(ci => ci.Id == request.CartItemId && ci.Cart.UserId == user.Id);

                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng" });
                }

                _context.CartItems.Remove(cartItem);
                cartItem.Cart.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // Get updated cart total
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

        // GET: Cart/GetCartCount
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

        private async Task<int> GetCartCountInternal(string userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            return cart?.CartItems.Sum(ci => ci.Quantity) ?? 0;
        }
    }

    // Request models
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
}

