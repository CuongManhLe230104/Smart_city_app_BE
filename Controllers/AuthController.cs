// Controllers/AuthController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCity_BE.Data;
using SmartCity_BE.Models;
using SmartCity_BE.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SmartCity_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;
        private readonly ILogger<AuthController> _logger; // Thêm logger

        public AuthController(ApplicationDbContext context, JwtService jwtService, ILogger<AuthController> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.FullName,
                        u.PhoneNumber,
                        u.CreatedAt
                    })
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();

                return Ok(new
                {
                    message = "Danh sách users",
                    count = users.Count(),
                    users = users
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // Validate
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return BadRequest(new { message = "Email đã được sử dụng" });
                }

                // Hash password
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // Tạo user
                var newUser = new User
                {
                    Email = request.Email,
                    PasswordHash = hashedPassword,
                    FullName = request.FullName,
                    PhoneNumber = request.PhoneNumber,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // Generate JWT token
                var token = _jwtService.GenerateToken(newUser);

                return Ok(new
                {
                    message = "Đăng ký thành công!",
                    token = token,
                    user = new
                    {
                        newUser.Id,
                        newUser.Email,
                        newUser.FullName,
                        newUser.PhoneNumber
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return Unauthorized(new { message = "Email hoặc mật khẩu không đúng" });
                }

                // Tạo JWT token
                var token = _jwtService.GenerateToken(user);

                // ✅ Response phải có cấu trúc đúng
                return Ok(new
                {
                    message = "Đăng nhập thành công",
                    data = new
                    {
                        token = token, // ✅ Phải có token
                        user = new
                        {
                            id = user.Id,
                            email = user.Email,
                            fullName = user.FullName,
                            phoneNumber = user.PhoneNumber
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi: {ex.Message}" });
            }
        }

        // POST: api/Auth/fcm-token
        [HttpPost("fcm-token")]
        [Authorize]
        public async Task<IActionResult> SaveFcmToken([FromBody] FcmTokenDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin user" });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User không tồn tại" });
                }
                user.FcmToken = dto.FcmToken;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Lưu FCM token thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Lỗi: {ex.Message}",
                    detail = ex.InnerException?.Message
                });
            }
        }

        // POST: api/Auth/test-notification
        [HttpPost("test-notification")]
        [Authorize]
        public async Task<IActionResult> TestNotification()
        {
            try
            {
                // ✅ THÊM LOG NGAY ĐẦU METHOD
                _logger.LogInformation("📥 TestNotification endpoint called");

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation($"👤 User ID from JWT: {userIdClaim}");

                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
                {
                    _logger.LogWarning("⚠️ Invalid user ID in JWT");
                    return Unauthorized(new { message = "Không tìm thấy thông tin user" });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"⚠️ User not found: {userId}");
                    return NotFound(new { message = "User không tồn tại" });
                }

                if (string.IsNullOrEmpty(user.FcmToken))
                {
                    _logger.LogWarning($"⚠️ FCM token not found for user: {user.Email}");
                    return NotFound(new
                    {
                        message = "FCM token not found",
                        detail = "User chưa có FCM token. Vui lòng login lại từ app."
                    });
                }

                _logger.LogInformation($"📤 Sending test notification to user: {user.Email}");
                _logger.LogInformation($"📤 FCM Token: {user.FcmToken.Substring(0, 20)}...");

                var notificationService = new NotificationService();
                await notificationService.SendNotificationAsync(
                    user.FcmToken,
                    "🔔 Test Notification",
                    $"Xin chào {user.FullName ?? user.Email}! Đây là thông báo test từ SmartCity Backend.",
                    new Dictionary<string, string>
                    {
                { "type", "test" },
                { "userId", userId.ToString() },
                { "timestamp", DateTime.UtcNow.ToString("O") }
                    }
                );

                _logger.LogInformation("✅ Test notification sent successfully");

                return Ok(new
                {
                    success = true,
                    message = "Notification sent successfully",
                    data = new
                    {
                        userId = user.Id,
                        email = user.Email,
                        fcmToken = user.FcmToken.Substring(0, 20) + "...",
                        sentAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending test notification");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi gửi notification",
                    detail = ex.Message
                });
            }
        }

        // POST: api/Auth/send-notification-to-user/{userId}
        [HttpPost("send-notification-to-user/{userId}")]
        [Authorize]
        public async Task<IActionResult> SendNotificationToUser(long userId, [FromBody] SendNotificationDto dto)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || string.IsNullOrEmpty(user.FcmToken))
                {
                    return NotFound(new { message = "User not found or FCM token missing" });
                }

                _logger.LogInformation($"📤 Sending notification to user: {user.Email}");

                var notificationService = new NotificationService();
                await notificationService.SendNotificationAsync(
                    user.FcmToken,
                    dto.Title,
                    dto.Body,
                    dto.Data ?? new Dictionary<string, string>()
                );

                return Ok(new
                {
                    success = true,
                    message = "Notification sent successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending notification");
                return StatusCode(500, new { message = ex.Message }); // ✅ SỬA: StatusError → StatusCode
            }
        }
        // POST: api/Auth/broadcast-notification
        [HttpPost("broadcast-notification")]
        [Authorize]
        public async Task<IActionResult> BroadcastNotification([FromBody] SendNotificationDto dto)
        {
            try
            {
                var users = await _context.Users
                    .Where(u => !string.IsNullOrEmpty(u.FcmToken))
                    .ToListAsync();

                if (!users.Any())
                {
                    return NotFound(new { message = "No users with FCM tokens found" });
                }

                _logger.LogInformation($"📤 Broadcasting notification to {users.Count} users");

                var notificationService = new NotificationService();
                var tasks = users.Select(user =>
                    notificationService.SendNotificationAsync(
                        user.FcmToken!,
                        dto.Title,
                        dto.Body,
                        dto.Data ?? new Dictionary<string, string>()
                    )
                );

                await Task.WhenAll(tasks);

                return Ok(new
                {
                    success = true,
                    message = $"Notification sent to {users.Count} users"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error broadcasting notification");
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }

    // DTOs
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = default!;

        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;

        [Required]
        public string Password { get; set; } = default!;
    }

    public class FcmTokenDto
    {
        [Required(ErrorMessage = "FCM token là bắt buộc")]
        public string FcmToken { get; set; } = string.Empty;
    }

    public class SendNotificationDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public Dictionary<string, string>? Data { get; set; }
    }
}