using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCity_BE.Data;
using SmartCity_BE.Models;
using SmartCity_BE.DTOs;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace SmartCity_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TravelTourController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<TravelTourController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TravelTourController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<TravelTourController> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        // ========================================
        // HELPER: GET BASE URL
        // ========================================
        private string GetBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null) return "http://localhost:5000";
            return $"{request.Scheme}://{request.Host}";
        }

        // ========================================
        // HELPER: CONVERT TO FULL URL
        // ========================================
        private string GetFullImageUrl(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return "";

            // Nếu đã là full URL → return nguyên
            if (imageUrl.StartsWith("http://") || imageUrl.StartsWith("https://"))
                return imageUrl;

            // Nếu là relative path → convert sang full URL
            var baseUrl = GetBaseUrl();
            return $"{baseUrl}{(imageUrl.StartsWith("/") ? "" : "/")}{imageUrl}";
        }

        // ========================================
        // GET: api/TravelTour
        // ========================================
        [HttpGet]
        public async Task<IActionResult> GetTours()
        {
            try
            {
                var tours = await _context.TravelTours.ToListAsync();

                // ✅ CONVERT relative path → full URL
                var result = tours.Select(t => new
                {
                    t.Id,
                    t.UserId,
                    t.NameTour,
                    t.TourType,
                    t.Content,
                    t.Timeline,
                    t.Price,
                    t.MaxPeople,
                    t.Duration,
                    CoverImageUrl = GetFullImageUrl(t.CoverImageUrl), // ✅ FULL URL
                    t.GalleryImageUrls
                }).ToList();

                _logger.LogInformation($"✅ Returning {result.Count} tours with full image URLs");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting tours");
                return StatusCode(500, new { message = "Lỗi lấy danh sách tour" });
            }
        }

        // ========================================
        // GET: api/TravelTour/{id}
        // ========================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTour(int id)
        {
            try
            {
                var tour = await _context.TravelTours.FindAsync(id);

                if (tour == null)
                {
                    return NotFound(new { message = "Không tìm thấy Tour" });
                }

                // ✅ CONVERT relative path → full URL
                var result = new
                {
                    tour.Id,
                    tour.UserId,
                    tour.NameTour,
                    tour.TourType,
                    tour.Content,
                    tour.Timeline,
                    tour.Price,
                    tour.MaxPeople,
                    tour.Duration,
                    CoverImageUrl = GetFullImageUrl(tour.CoverImageUrl), // ✅ FULL URL
                    tour.GalleryImageUrls
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting tour {Id}", id);
                return StatusCode(500, new { message = "Lỗi lấy thông tin tour" });
            }
        }

        // ========================================
        // POST: api/TravelTour - TẠO TOUR MỚI
        // ========================================
        [HttpPost]
        public async Task<IActionResult> CreateTour(
            [FromForm] TravelTourCreateDto tourDto,
            [FromForm] IFormFile? coverImage)
        {
            try
            {
                _logger.LogInformation("📤 Creating tour: {Name}", tourDto.NameTour);

                // ✅ VALIDATE ModelState
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);

                    _logger.LogWarning("❌ ModelState invalid: {Errors}", string.Join(", ", errors));

                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu không hợp lệ",
                        errors = errors
                    });
                }

                // ✅ VALIDATE: Bắt buộc có ảnh khi tạo mới
                if (coverImage == null || coverImage.Length == 0)
                {
                    _logger.LogWarning("❌ No cover image provided");
                    return BadRequest(new
                    {
                        success = false,
                        message = "Vui lòng tải lên ảnh bìa cho Tour!"
                    });
                }

                // ✅ LOG FILE INFO
                _logger.LogInformation("📎 Received file: {FileName} ({Size} bytes, {Type})",
                    coverImage.FileName,
                    coverImage.Length,
                    coverImage.ContentType);

                // ✅ UPLOAD ẢNH
                string coverImageUrl = await SaveImageAsync(coverImage, "tour-covers");
                _logger.LogInformation("✅ Image uploaded successfully: {Url}", coverImageUrl);

                // ✅ TẠO TOUR OBJECT
                long creatorId = 1; // TODO: Lấy từ JWT Claims

                var tour = new TravelTour
                {
                    UserId = creatorId,
                    NameTour = tourDto.NameTour,
                    TourType = tourDto.TourType ?? "",
                    Content = tourDto.Content ?? "",
                    Timeline = tourDto.Timeline ?? "",
                    Price = tourDto.Price,
                    MaxPeople = tourDto.MaxPeople,
                    Duration = tourDto.Duration ?? "",
                    CoverImageUrl = coverImageUrl, // ✅ RELATIVE PATH: /uploads/tour-covers/abc.jpg
                    GalleryImageUrls = tourDto.GalleryImageUrls ?? ""
                };

                _context.TravelTours.Add(tour);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Tour created successfully: ID {Id}", tour.Id);

                // ✅ TRẢ VỀ RESPONSE VỚI FULL URL
                var response = new
                {
                    tour.Id,
                    tour.UserId,
                    tour.NameTour,
                    tour.TourType,
                    tour.Content,
                    tour.Timeline,
                    tour.Price,
                    tour.MaxPeople,
                    tour.Duration,
                    CoverImageUrl = GetFullImageUrl(tour.CoverImageUrl), // ✅ FULL URL
                    tour.GalleryImageUrls
                };

                return CreatedAtAction(nameof(GetTour), new { id = tour.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating tour");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi tạo tour",
                    details = ex.Message
                });
            }
        }

        // ========================================
        // PUT: api/TravelTour/{id} - CẬP NHẬT TOUR
        // ========================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTour(
            int id,
            [FromForm] TravelTourCreateDto tourDto,
            [FromForm] IFormFile? coverImage)
        {
            try
            {
                var tour = await _context.TravelTours.FindAsync(id);

                if (tour == null)
                {
                    return NotFound(new { message = "Không tìm thấy Tour để cập nhật" });
                }

                _logger.LogInformation("📝 Updating tour: ID {Id}", id);

                // ✅ CẬP NHẬT THÔNG TIN
                tour.NameTour = tourDto.NameTour;
                tour.TourType = tourDto.TourType ?? tour.TourType;
                tour.Content = tourDto.Content ?? tour.Content;
                tour.Timeline = tourDto.Timeline ?? tour.Timeline;
                tour.Price = tourDto.Price;
                tour.MaxPeople = tourDto.MaxPeople;
                tour.Duration = tourDto.Duration ?? tour.Duration;
                tour.GalleryImageUrls = tourDto.GalleryImageUrls ?? tour.GalleryImageUrls;

                // ✅ NẾU CÓ ẢNH MỚI → UPLOAD & XÓA ẢNH CŨ
                if (coverImage != null && coverImage.Length > 0)
                {
                    _logger.LogInformation("📎 Updating image: {FileName} ({Size} bytes)",
                        coverImage.FileName, coverImage.Length);

                    // Xóa ảnh cũ
                    if (!string.IsNullOrEmpty(tour.CoverImageUrl))
                    {
                        DeleteImage(tour.CoverImageUrl);
                    }

                    // Upload ảnh mới
                    tour.CoverImageUrl = await SaveImageAsync(coverImage, "tour-covers");
                    _logger.LogInformation("✅ New image uploaded: {Url}", tour.CoverImageUrl);
                }

                _context.Entry(tour).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Tour updated successfully: ID {Id}", id);

                // ✅ TRẢ VỀ RESPONSE VỚI FULL URL
                var response = new
                {
                    tour.Id,
                    tour.UserId,
                    tour.NameTour,
                    tour.TourType,
                    tour.Content,
                    tour.Timeline,
                    tour.Price,
                    tour.MaxPeople,
                    tour.Duration,
                    CoverImageUrl = GetFullImageUrl(tour.CoverImageUrl), // ✅ FULL URL
                    tour.GalleryImageUrls
                };

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật tour thành công",
                    data = response
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.TravelTours.Any(e => e.Id == id))
                {
                    return NotFound(new { message = "Tour không tồn tại" });
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating tour ID {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi cập nhật tour",
                    details = ex.Message
                });
            }
        }

        // ========================================
        // DELETE: api/TravelTour/{id}
        // ========================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTour(int id)
        {
            try
            {
                var tour = await _context.TravelTours.FindAsync(id);

                if (tour == null)
                {
                    return NotFound(new { message = "Không tìm thấy Tour để xóa" });
                }

                // ✅ XÓA ẢNH TRƯỚC KHI XÓA TOUR
                if (!string.IsNullOrEmpty(tour.CoverImageUrl))
                {
                    DeleteImage(tour.CoverImageUrl);
                }

                _context.TravelTours.Remove(tour);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Tour deleted: ID {Id}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting tour ID {Id}", id);
                return StatusCode(500, new
                {
                    message = "Lỗi xóa tour",
                    details = ex.Message
                });
            }
        }

        // ========================================
        // HELPER: SAVE IMAGE
        // ========================================
        private async Task<string> SaveImageAsync(IFormFile file, string folder)
        {
            try
            {
                // 1. Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    throw new Exception($"File type not allowed. Allowed: {string.Join(", ", allowedExtensions)}");
                }

                // 2. Validate file size (max 5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    throw new Exception("File size exceeds 5MB limit");
                }

                // 3. Generate unique filename
                var fileName = $"{Guid.NewGuid()}{fileExtension}";

                // 4. Create upload folder if not exists
                var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", folder);
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                    _logger.LogInformation("📁 Created directory: {Path}", uploadFolder);
                }

                // 5. Full file path
                var filePath = Path.Combine(uploadFolder, fileName);

                // 6. Save file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // 7. Return relative URL
                var imageUrl = $"/uploads/{folder}/{fileName}";

                _logger.LogInformation("💾 File saved: {Path}", filePath);
                _logger.LogInformation("🔗 Image URL: {Url}", imageUrl);

                return imageUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error saving image: {FileName}", file?.FileName);
                throw new Exception($"Lỗi upload ảnh: {ex.Message}");
            }
        }

        // ========================================
        // HELPER: DELETE IMAGE
        // ========================================
        private void DeleteImage(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl)) return;

                // Remove leading slash
                var relativePath = imageUrl.TrimStart('/');

                // Full file path
                var filePath = Path.Combine(_environment.WebRootPath, relativePath);

                // Delete if exists
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation("🗑️ Deleted image: {Path}", filePath);
                }
                else
                {
                    _logger.LogWarning("⚠️ File not found for deletion: {Path}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error deleting image: {Url}", imageUrl);
                // Don't throw - deletion failure shouldn't break the main flow
            }
        }
    }
}