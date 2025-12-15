using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace SmartCity_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<UploadController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UploadController(
            IWebHostEnvironment env,
            ILogger<UploadController> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _env = env;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        // ✅ Helper: Lấy base URL động
        private string GetBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null) return "http://localhost:5000";

            var scheme = request.Scheme; // http hoặc https
            var host = request.Host.Value; // 10.0.2.2:5000 hoặc 192.168.1.177:5000

            return $"{scheme}://{host}";
        }

        // ✅ Upload feedback image
        [HttpPost("feedback")]
        public async Task<IActionResult> UploadFeedbackImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Không có file" });
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new { success = false, message = "Chỉ chấp nhận file ảnh" });
                }

                if (file.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new { success = false, message = "File quá lớn (tối đa 5MB)" });
                }

                var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "feedback-images");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                var uniqueFileName = $"feedback_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString().Substring(0, 8)}{extension}";
                var filePath = Path.Combine(uploadFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // ✅ CHỈ TRẢ RELATIVE PATH
                var fileUrl = $"/uploads/feedback-images/{uniqueFileName}";

                _logger.LogInformation($"✅ Uploaded: {fileUrl}");

                return Ok(new
                {
                    success = true,
                    message = "Upload thành công",
                    url = fileUrl // ✅ Relative path
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading feedback image");
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // ✅ Upload flood image
        [HttpPost("image")]
        public async Task<IActionResult> UploadFloodImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Không có file" });
                }

                _logger.LogInformation($"📤 Uploading flood image: {file.FileName}");

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new { success = false, message = "Chỉ chấp nhận file ảnh" });
                }

                if (file.Length > 10 * 1024 * 1024)
                {
                    return BadRequest(new { success = false, message = "File quá lớn (tối đa 10MB)" });
                }

                var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "flood-images");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                var uniqueFileName = $"flood_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString().Substring(0, 8)}{extension}";
                var filePath = Path.Combine(uploadFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // ✅ FULL URL
                var fileUrl = $"/uploads/flood-images/{uniqueFileName}";

                _logger.LogInformation($"✅ Uploaded: {fileUrl}");

                return Ok(new
                {
                    success = true,
                    message = "Upload thành công",
                    url = fileUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading flood image");
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // ✅ Upload event banner
        [HttpPost("event-banner")]
        public async Task<IActionResult> UploadEventBanner(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Không có file" });
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new { success = false, message = "Chỉ chấp nhận file ảnh" });
                }

                if (file.Length > 10 * 1024 * 1024)
                {
                    return BadRequest(new { success = false, message = "File quá lớn (tối đa 10MB)" });
                }

                var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "eventbanner-images");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                var uniqueFileName = $"banner_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString().Substring(0, 8)}{extension}";
                var filePath = Path.Combine(uploadFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // ✅ FULL URL
                var fileUrl = $"/uploads/eventbanner-images/{uniqueFileName}";

                _logger.LogInformation($"✅ Uploaded banner: {fileUrl}");

                return Ok(new
                {
                    success = true,
                    message = "Upload banner thành công",
                    url = fileUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading event banner");
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // ✅ Upload profile image
        [HttpPost("profile")]
        public async Task<IActionResult> UploadProfileImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Không có file" });
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new { success = false, message = "Chỉ chấp nhận file ảnh" });
                }

                if (file.Length > 2 * 1024 * 1024)
                {
                    return BadRequest(new { success = false, message = "File quá lớn (tối đa 2MB)" });
                }

                var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "profile-images");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                var uniqueFileName = $"profile_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString().Substring(0, 8)}{extension}";
                var filePath = Path.Combine(uploadFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // ✅ FULL URL
                var fileUrl = $"/uploads/profile-images/{uniqueFileName}";

                _logger.LogInformation($"✅ Uploaded profile: {fileUrl}");

                return Ok(new
                {
                    success = true,
                    message = "Upload ảnh thành công",
                    url = fileUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile image");
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
    }
}