using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RomaWatches.Models;

namespace RomaWatches.Controllers
{
    // Controller quản lý tài khoản người dùng (Đăng nhập, Đăng ký, Đăng xuất).
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager; // Quản lý thông tin người dùng.
        private readonly SignInManager<ApplicationUser> _signInManager; // Quản lý đăng nhập/đăng xuất.
        private readonly ILogger<AccountController> _logger; // Logger.

        // Constructor injection.
        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // Action xử lý đăng nhập (AJAX).
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous] // Cho phép truy cập không cần đăng nhập.
        [IgnoreAntiforgeryToken] // Bỏ qua kiểm tra CSRF token (lưu ý: chỉ nên dùng nếu thực sự cần thiết, tốt nhất nên dùng ValidateAntiForgeryToken).
        public async Task<IActionResult> Login([FromForm] string email, [FromForm] string password)
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào.
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    return Json(new { success = false, message = "Vui lòng điền email và mật khẩu." });
                }

                // Tìm người dùng theo email.
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return Json(new { success = false, message = "Email hoặc mật khẩu không hợp lệ." });
                }

                // Thực hiện đăng nhập.
                var result = await _signInManager.PasswordSignInAsync(user, password, false, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    // Xác định URL chuyển hướng dựa trên vai trò.
                    var redirectUrl = user.Role == "admin" ? "/Admin/Dashboard" : "/";
                    return Json(new { success = true, message = "Đăng nhập thành công!", redirectUrl = redirectUrl });
                }
                else if (result.IsLockedOut)
                {
                    return Json(new { success = false, message = "Tài khoản đã bị khóa." });
                }
                else
                {
                    return Json(new { success = false, message = "Email hoặc mật khẩu không hợp lệ." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong quá trình đăng nhập");
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
            }
        }

        // Action xử lý đăng ký (AJAX).
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Register([FromForm] string firstName, [FromForm] string lastName, [FromForm] string email, [FromForm] string password, [FromForm] string confirmPassword)
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào.
                if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    return Json(new { success = false, message = "Vui lòng điền đầy đủ các ô." });
                }

                // Kiểm tra mật khẩu xác nhận.
                if (password != confirmPassword)
                {
                    return Json(new { success = false, message = "Mật khẩu không khớp!" });
                }

                // Kiểm tra email đã tồn tại chưa.
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    return Json(new { success = false, message = "Email đã tồn tại." });
                }

                // Tạo đối tượng người dùng mới.
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Role = "user", // Mặc định là user thường.
                    EmailConfirmed = true // Tự động xác nhận email (để đơn giản hóa).
                };

                // Lưu người dùng vào cơ sở dữ liệu.
                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "Đăng ký thành công! Vui lòng đăng nhập." });
                }
                else
                {
                    // Trả về lỗi nếu có (ví dụ: mật khẩu yếu).
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
            }
        }

        // Action xử lý đăng xuất.
        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken] // Bảo vệ chống CSRF.
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync(); // Đăng xuất khỏi hệ thống.
            return RedirectToAction("Index", "Home"); // Quay về trang chủ.
        }
    }
}

