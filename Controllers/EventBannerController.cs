using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCity_BE.Data;
using SmartCity_BE.DTOs;
using SmartCity_BE.Models;
using Microsoft.Extensions.Logging;
using SmartCity_BE.Services;

namespace Smartcity_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventBannersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EventBannersController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _env;
        private readonly NotificationService _notificationService;
        private readonly IServiceScopeFactory _serviceScopeFactory; 

        public EventBannersController(
            ApplicationDbContext context,
            ILogger<EventBannersController> logger,
            IHttpContextAccessor httpContextAccessor,
            IWebHostEnvironment env,
            NotificationService notificationService, 
            IServiceScopeFactory serviceScopeFactory) 
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _env = env;
            _notificationService = notificationService; 
            _serviceScopeFactory = serviceScopeFactory; 
        }

        // ✅ Helper: Lấy base URL động
        private string GetBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null) return "http://localhost:5000";

            var scheme = request.Scheme;
            var host = request.Host.Value;
            return $"{scheme}://{host}";
        }

        // ✅ Helper: Convert relative path thành full URL
        private string GetFullImageUrl(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return string.Empty;

            // Nếu đã là full URL → giữ nguyên
            if (imageUrl.StartsWith("http://") || imageUrl.StartsWith("https://"))
            {
                return imageUrl;
            }

            // Nếu là relative path → thêm base URL
            var baseUrl = GetBaseUrl();
            return $"{baseUrl}{(imageUrl.StartsWith("/") ? "" : "/")}{imageUrl}";
        }

        // ✅ 1. GET: /api/EventBanners
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventBannerDto>>> GetEventBanners()
        {
            try
            {
                _logger.LogInformation("📥 GET /api/EventBanners");

                var banners = await _context.EventBanners
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();

                var result = banners.Select(b => new EventBannerDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Description = b.Description,
                    ImageUrl = GetFullImageUrl(b.ImageUrl) // ✅ Convert sang full URL
                }).ToList();

                _logger.LogInformation($"✅ Returning {result.Count} banners");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting event banners");
                return StatusCode(500, new { message = $"Lỗi: {ex.Message}" });
            }
        }

        // ✅ 2. GET: /api/EventBanners/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<EventBannerDto>> GetEventBanner(int id)
        {
            try
            {
                var banner = await _context.EventBanners.FindAsync(id);

                if (banner == null)
                {
                    return NotFound(new { message = "Không tìm thấy banner" });
                }

                return Ok(new EventBannerDto
                {
                    Id = banner.Id,
                    Title = banner.Title,
                    Description = banner.Description,
                    ImageUrl = GetFullImageUrl(banner.ImageUrl)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting banner {id}");
                return StatusCode(500, new { message = $"Lỗi: {ex.Message}" });
            }
        }

        // ✅ 3. POST: /api/EventBanners
        [HttpPost]
        public async Task<ActionResult<EventBanner>> CreateEventBanner([FromBody] CreateEventBannerDto dto)
        {
            try
            {
                _logger.LogInformation("📥 POST /api/EventBanners");
                _logger.LogInformation($"📥 Received: Title={dto.Title}, ImageUrl={dto.ImageUrl}");

                if (string.IsNullOrWhiteSpace(dto.Title))
                {
                    _logger.LogWarning("⚠️ Title is empty");
                    return BadRequest(new { message = "Title không được để trống" });
                }

                if (string.IsNullOrWhiteSpace(dto.ImageUrl))
                {
                    _logger.LogWarning("⚠️ ImageUrl is empty");
                    return BadRequest(new { message = "ImageUrl không được để trống" });
                }

                var imageUrl = dto.ImageUrl.Trim();
                if (imageUrl.StartsWith("http://") || imageUrl.StartsWith("https://"))
                {
                    var uri = new Uri(imageUrl);
                    imageUrl = uri.AbsolutePath;
                }

                var banner = new EventBanner
                {
                    Title = dto.Title.Trim(),
                    Description = dto.Description?.Trim(),
                    ImageUrl = imageUrl,
                    CreatedAt = DateTime.UtcNow
                };

                _context.EventBanners.Add(banner);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Created banner {banner.Id}: {banner.Title}");

                var bannerId = banner.Id;
                var bannerTitle = banner.Title;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventBannersController>>();

                        logger.LogInformation("📤 Starting notification broadcast for banner {BannerId}", bannerId);

                        var users = await context.Users
                            .Where(u => !string.IsNullOrEmpty(u.FcmToken))
                            .ToListAsync();

                        logger.LogInformation($"📤 Found {users.Count} users with FCM tokens");

                        if (users.Count == 0)
                        {
                            logger.LogWarning("⚠️ No users with FCM tokens found");
                            return;
                        }

                        var notificationService = new NotificationService();
                        var successCount = 0;
                        var failCount = 0;

                        foreach (var user in users)
                        {
                            logger.LogInformation($"📤 Sending to {user.Email}");
                            logger.LogInformation($"   FCM Token: {user.FcmToken.Substring(0, 30)}...");

                            try
                            {
                                await notificationService.SendNotificationAsync(
                                    user.FcmToken!,
                                    "🎉 Sự kiện mới",
                                    bannerTitle,
                                    new Dictionary<string, string>
                                    {
                                        { "type", "event" },
                                        { "eventId", bannerId.ToString() },
                                        { "title", bannerTitle },
                                        { "timestamp", DateTime.UtcNow.ToString("O") }
                                    }
                                );

                                successCount++;
                                logger.LogInformation($"✅ Sent successfully to {user.Email}");
                            }
                            catch (Exception ex)
                            {
                                failCount++;
                                logger.LogError(ex, $"❌ Failed to send to {user.Email}: {ex.Message}");
                            }
                        }

                        logger.LogInformation($"✅ Notification broadcast completed: {successCount} success, {failCount} failed");
                    }
                    catch (Exception ex)
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventBannersController>>();
                        logger.LogError(ex, "❌ Error in notification broadcast: {Message}", ex.Message);
                        logger.LogError("   StackTrace: {StackTrace}", ex.StackTrace);
                    }
                });

                return CreatedAtAction(
                    nameof(GetEventBanner),
                    new { id = banner.Id },
                    new EventBannerDto
                    {
                        Id = banner.Id,
                        Title = banner.Title,
                        Description = banner.Description,
                        ImageUrl = GetFullImageUrl(banner.ImageUrl)
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event banner");
                return StatusCode(500, new { message = $"Lỗi: {ex.Message}" });
            }
        }

        // ✅ 4. PUT: /api/EventBanners/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateEventBanner(int id, [FromBody] CreateEventBannerDto dto) // ✅ Giữ nguyên FromBody
        {
            try
            {
                var banner = await _context.EventBanners.FindAsync(id);

                if (banner == null)
                {
                    return NotFound(new { message = "Không tìm thấy banner" });
                }

                if (string.IsNullOrWhiteSpace(dto.Title))
                {
                    return BadRequest(new { message = "Title không được để trống" });
                }

                if (string.IsNullOrWhiteSpace(dto.ImageUrl))
                {
                    return BadRequest(new { message = "ImageUrl không được để trống" });
                }

                // ✅ Convert sang relative path nếu cần
                var imageUrl = dto.ImageUrl.Trim();
                if (imageUrl.StartsWith("http://") || imageUrl.StartsWith("https://"))
                {
                    var uri = new Uri(imageUrl);
                    imageUrl = uri.AbsolutePath;
                }

                banner.Title = dto.Title.Trim();
                banner.Description = dto.Description?.Trim();
                banner.ImageUrl = imageUrl;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Updated banner {id}");

                return Ok(new
                {
                    message = "Cập nhật banner thành công",
                    banner = new EventBannerDto
                    {
                        Id = banner.Id,
                        Title = banner.Title,
                        Description = banner.Description,
                        ImageUrl = GetFullImageUrl(banner.ImageUrl)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating banner {id}");
                return StatusCode(500, new { message = $"Lỗi: {ex.Message}" });
            }
        }

        // ✅ 5. DELETE: /api/EventBanners/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteEventBanner(int id)
        {
            try
            {
                var banner = await _context.EventBanners.FindAsync(id);

                if (banner == null)
                {
                    return NotFound(new { message = "Không tìm thấy banner" });
                }

                // ✅ Xóa file ảnh nếu tồn tại
                if (!string.IsNullOrEmpty(banner.ImageUrl))
                {
                    var imagePath = Path.Combine(_env.WebRootPath, banner.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                        _logger.LogInformation($"🗑️ Deleted image file: {imagePath}");
                    }
                }

                _context.EventBanners.Remove(banner);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"🗑️ Deleted banner {id}: {banner.Title}");

                return Ok(new { message = $"Đã xóa banner: {banner.Title}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting banner {id}");
                return StatusCode(500, new { message = $"Lỗi: {ex.Message}" });
            }
        }

        // ✅ 6. POST: /api/EventBanners/delete-multiple
        [HttpPost("delete-multiple")]
        public async Task<ActionResult> DeleteMultipleBanners([FromBody] DeleteMultipleRequest request)
        {
            try
            {
                if (request.Ids == null || request.Ids.Count == 0)
                {
                    return BadRequest(new { message = "Danh sách ID không được để trống" });
                }

                var banners = await _context.EventBanners
                    .Where(b => request.Ids.Contains(b.Id))
                    .ToListAsync();

                if (banners.Count == 0)
                {
                    return NotFound(new { message = "Không tìm thấy banner nào" });
                }

                // Xóa files
                foreach (var banner in banners)
                {
                    if (!string.IsNullOrEmpty(banner.ImageUrl))
                    {
                        var imagePath = Path.Combine(_env.WebRootPath, banner.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }
                }

                _context.EventBanners.RemoveRange(banners);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"🗑️ Deleted {banners.Count} banners");

                return Ok(new
                {
                    message = $"Đã xóa {banners.Count} banner",
                    deletedIds = banners.Select(b => b.Id).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting multiple banners");
                return StatusCode(500, new { message = $"Lỗi: {ex.Message}" });
            }
        }
    }

    public class DeleteMultipleRequest
    {
        public List<int> Ids { get; set; } = new();
    }
}