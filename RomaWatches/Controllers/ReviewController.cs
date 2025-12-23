using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RomaWatches.Data;
using RomaWatches.Models;

namespace RomaWatches.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ReviewController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /Review/GetReviews?productId=1
        // Lấy danh sách đánh giá của sản phẩm (public, không cần auth)
        [HttpGet]
        public async Task<IActionResult> GetReviews(int productId)
        {
            try
            {
                var reviews = await _context.Reviews
                    .Include(r => r.User)
                    .Where(r => r.ProductId == productId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new
                    {
                        r.Id,
                        r.Rating,
                        r.Comment,
                        r.CreatedAt,
                        r.UpdatedAt,
                        UserName = r.User.FirstName + " " + r.User.LastName,
                        UserId = r.UserId
                    })
                    .ToListAsync();

                return Json(new { success = true, reviews });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reviews for product {ProductId}", productId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải đánh giá" });
            }
        }

        // GET: /Review/CanReview?productId=1
        // Kiểm tra user có thể đánh giá không (cần auth)
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> CanReview(int productId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { canReview = false, message = "Vui lòng đăng nhập" });
                }

                // Kiểm tra user đã mua sản phẩm chưa
                var hasPurchased = await HasUserPurchasedProduct(user.Id, productId);
                if (!hasPurchased)
                {
                    return Json(new { canReview = false, message = "Bạn cần mua sản phẩm này trước khi đánh giá" });
                }

                // Kiểm tra user đã đánh giá chưa
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == user.Id);

                return Json(new
                {
                    canReview = true,
                    hasReviewed = existingReview != null,
                    existingReview = existingReview != null ? new
                    {
                        existingReview.Id,
                        existingReview.Rating,
                        existingReview.Comment
                    } : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user can review product {ProductId}", productId);
                return Json(new { canReview = false, message = "Có lỗi xảy ra" });
            }
        }

        // POST: /Review/Create
        // Tạo đánh giá mới (cần auth)
        [HttpPost]
        [Authorize]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Create([FromBody] CreateReviewRequest request)
        {
            try
            {
                if (request == null || request.ProductId <= 0 || request.Rating < 1 || request.Rating > 5)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                // Kiểm tra user đã mua sản phẩm chưa
                var hasPurchased = await HasUserPurchasedProduct(user.Id, request.ProductId);
                if (!hasPurchased)
                {
                    return Json(new { success = false, message = "Bạn cần mua sản phẩm này trước khi đánh giá" });
                }

                // Kiểm tra đã đánh giá chưa
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.ProductId == request.ProductId && r.UserId == user.Id);

                if (existingReview != null)
                {
                    return Json(new { success = false, message = "Bạn đã đánh giá sản phẩm này rồi. Vui lòng chỉnh sửa đánh giá hiện tại." });
                }

                // Kiểm tra sản phẩm tồn tại
                var product = await _context.Products.FindAsync(request.ProductId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                }

                var review = new Review
                {
                    ProductId = request.ProductId,
                    UserId = user.Id,
                    Rating = request.Rating,
                    Comment = request.Comment?.Trim(),
                    CreatedAt = DateTime.Now
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Đánh giá đã được gửi thành công",
                    review = new
                    {
                        review.Id,
                        review.Rating,
                        review.Comment,
                        review.CreatedAt,
                        UserName = user.FirstName + " " + user.LastName,
                        UserId = user.Id
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo đánh giá" });
            }
        }

        // PUT: /Review/Update/1
        // Cập nhật đánh giá (cần auth, chỉ owner)
        [HttpPut]
        [Authorize]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateReviewRequest request)
        {
            try
            {
                if (request == null || request.Rating < 1 || request.Rating > 5)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                var review = await _context.Reviews.FindAsync(id);
                if (review == null)
                {
                    return Json(new { success = false, message = "Đánh giá không tồn tại" });
                }

                // Kiểm tra quyền sở hữu
                if (review.UserId != user.Id)
                {
                    return Json(new { success = false, message = "Bạn không có quyền chỉnh sửa đánh giá này" });
                }

                review.Rating = request.Rating;
                review.Comment = request.Comment?.Trim();
                review.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Đánh giá đã được cập nhật",
                    review = new
                    {
                        review.Id,
                        review.Rating,
                        review.Comment,
                        review.CreatedAt,
                        review.UpdatedAt,
                        UserName = user.FirstName + " " + user.LastName,
                        UserId = user.Id
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review {ReviewId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật đánh giá" });
            }
        }

        // DELETE: /Review/Delete/1
        // Xóa đánh giá (cần auth, chỉ owner)
        [HttpDelete]
        [Authorize]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                var review = await _context.Reviews.FindAsync(id);
                if (review == null)
                {
                    return Json(new { success = false, message = "Đánh giá không tồn tại" });
                }

                // Kiểm tra quyền sở hữu
                if (review.UserId != user.Id)
                {
                    return Json(new { success = false, message = "Bạn không có quyền xóa đánh giá này" });
                }

                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đánh giá đã được xóa" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review {ReviewId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa đánh giá" });
            }
        }

        // Helper method: Kiểm tra user đã mua sản phẩm với status Completed
        private async Task<bool> HasUserPurchasedProduct(string userId, int productId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .AnyAsync(o =>
                    o.UserId == userId &&
                    o.Status == OrderStatus.Completed &&
                    o.OrderItems.Any(oi => oi.ProductId == productId));
        }
    }

    // DTOs
    public class CreateReviewRequest
    {
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class UpdateReviewRequest
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}

