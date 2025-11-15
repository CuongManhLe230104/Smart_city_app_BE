using Microsoft.AspNetCore.Mvc;

namespace SmartCity_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<UploadController> _logger;

        public UploadController(
            IWebHostEnvironment environment,
            ILogger<UploadController> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        // 📤 Upload ảnh
        [HttpPost("image")]
        [RequestSizeLimit(10_000_000)] // 10MB
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "Không có file được chọn" });
                }

                // ✅ Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new { message = "Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png, .gif)" });
                }

                // ✅ Validate file size (max 10MB)
                if (file.Length > 10_000_000)
                {
                    return BadRequest(new { message = "File quá lớn. Tối đa 10MB" });
                }

                // ✅ Tạo thư mục uploads nếu chưa có
                var uploadsFolder = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads", "flood-images");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // ✅ Tạo tên file unique
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // ✅ Lưu file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // ✅ Trả về URL
                var fileUrl = $"{Request.Scheme}://{Request.Host}/uploads/flood-images/{uniqueFileName}";

                _logger.LogInformation($"File uploaded: {uniqueFileName}");

                return Ok(new
                {
                    success = true,
                    message = "Upload ảnh thành công",
                    url = fileUrl,
                    fileName = uniqueFileName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Lỗi upload: {ex.Message}"
                });
            }
        }

        // 🗑️ Xóa ảnh
        [HttpDelete("image/{fileName}")]
        public IActionResult DeleteImage(string fileName)
        {
            try
            {
                var filePath = Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot",
                    "uploads",
                    "flood-images",
                    fileName
                );

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    return Ok(new { message = "Xóa ảnh thành công" });
                }

                return NotFound(new { message = "Không tìm thấy file" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image");
                return StatusCode(500, new { message = $"Lỗi: {ex.Message}" });
            }
        }
    }
}