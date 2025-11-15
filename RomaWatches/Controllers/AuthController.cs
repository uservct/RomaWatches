using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RomaWatches.Models;

namespace RomaWatches.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AuthController> _logger;

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

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] IdTokenRequest request)
        {
            try
            {
                // Validate Google token
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.id_token, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _configuration["Google:ClientId"] }
                });

                // Lấy thông tin user từ Google
                var email = payload.Email;
                var name = payload.Name;
                var googleId = payload.Subject;

                _logger.LogInformation("Google login attempt for email: {Email}", email);

                // Tìm user trong database theo email
                var user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    // Tạo user mới nếu chưa tồn tại
                    var nameParts = name.Split(' ', 2);
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
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

                // Đăng nhập user
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

    public class IdTokenRequest
    {
        public string id_token { get; set; } = string.Empty;
    }
}
