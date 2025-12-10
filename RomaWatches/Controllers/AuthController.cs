using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RomaWatches.Models;

namespace RomaWatches.Controllers
{
    // API Controller xử lý xác thực từ bên thứ 3 (ví dụ: Google).
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration; // Cấu hình hệ thống.
        private readonly UserManager<ApplicationUser> _userManager; // Quản lý người dùng.
        private readonly SignInManager<ApplicationUser> _signInManager; // Quản lý đăng nhập.
        private readonly ILogger<AuthController> _logger; // Logger.

        // Constructor injection.
        public AuthController(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // API xử lý đăng nhập bằng Google.
        // POST: api/Auth/google
        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] IdTokenRequest request)
        {
            try
            {
                // Xác thực token Google gửi lên.
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.id_token, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _configuration["Google:ClientId"] } // Kiểm tra Client ID.
                });

                // Lấy thông tin user từ payload của Google.
                var email = payload.Email;
                var name = payload.Name;
                var googleId = payload.Subject;

                _logger.LogInformation("Google login attempt for email: {Email}", email);

                // Tìm user trong database theo email.
                var user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    // Tạo user mới nếu chưa tồn tại.
                    var nameParts = name.Split(' ', 2);
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true, // Email từ Google đã được xác thực.
                        FirstName = nameParts.Length > 0 ? nameParts[0] : name,
                        LastName = nameParts.Length > 1 ? nameParts[1] : "",
                        Role = "user"
                    };

                    var result = await _userManager.CreateAsync(user);
                    if (!result.Succeeded)
                    {
                        _logger.LogError("Failed to create user from Google login: {Errors}", 
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                        return BadRequest(new { ok = false, message = "Không thể tạo tài khoản." });
                    }

                    _logger.LogInformation("Created new user from Google login: {Email}", email);
                }

                // Đăng nhập user vào hệ thống (tạo cookie).
                await _signInManager.SignInAsync(user, isPersistent: true);
                _logger.LogInformation("User logged in successfully via Google: {Email}", email);

                return Ok(new { ok = true, name = $"{user.FirstName} {user.LastName}".Trim(), email = user.Email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google authentication");
                return Unauthorized(new { ok = false, message = "Xác thực Google thất bại." });
            }
        }
    }

    // Class DTO nhận token từ client.
    public class IdTokenRequest
    {
        public string id_token { get; set; } = string.Empty;
    }
}
