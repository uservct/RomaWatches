using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RomaWatches.Models;

namespace RomaWatches.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Login([FromForm] string email, [FromForm] string password)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    return Json(new { success = false, message = "Vui lòng điền email và mật khẩu." });
                }

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return Json(new { success = false, message = "Email hoặc mật khẩu không hợp lệ." });
                }

                var result = await _signInManager.PasswordSignInAsync(user, password, false, lockoutOnFailure: false);
                if (result.Succeeded)
                {
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

        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Register([FromForm] string firstName, [FromForm] string lastName, [FromForm] string email, [FromForm] string password, [FromForm] string confirmPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    return Json(new { success = false, message = "Vui lòng điền đầy đủ các ô." });
                }

                if (password != confirmPassword)
                {
                    return Json(new { success = false, message = "Mật khẩu không khớp!" });
                }

                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    return Json(new { success = false, message = "Email đã tồn tại." });
                }

                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Role = "user",
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "Đăng ký thành công! Vui lòng đăng nhập." });
                }
                else
                {
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}

