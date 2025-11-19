using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartCity_BE.Data;
using SmartCity_BE.Models;
using System.ComponentModel.DataAnnotations;

namespace SmartCity_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FloodReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FloodReportsController> _logger;

        public FloodReportsController(
            ApplicationDbContext context,
            ILogger<FloodReportsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 📤 User gửi báo cáo ngập lụt
        [HttpPost]
        public async Task<IActionResult> CreateFloodReport([FromBody] CreateFloodReportRequest request)
        {
            try
            {
                // ✅ Validate UserId
                var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId);
                if (!userExists)
                {
                    _logger.LogError($"User with Id {request.UserId} not found");
                    return BadRequest(new { message = $"User với Id {request.UserId} không tồn tại" });
                }

                // ✅ Validate WaterLevel
                var validWaterLevels = new[] { "Low", "Medium", "High", "Critical", "Unknown" };
                if (!validWaterLevels.Contains(request.WaterLevel))
                {
                    return BadRequest(new { message = "WaterLevel phải là: Low, Medium, High, Critical, hoặc Unknown" });
                }

                var report = new FloodReport
                {
                    Title = request.Title,
                    Description = request.Description,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    Address = request.Address,
                    ImageUrl = request.ImageUrl,
                    WaterLevel = request.WaterLevel,
                    UserId = request.UserId,
                    Status = "Pending",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.FloodReports.Add(report);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Flood report {report.Id} created successfully");

                return Ok(new
                {
                    message = "Gửi báo cáo ngập lụt thành công! Chờ admin duyệt.",
                    reportId = report.Id
                });
            }
            catch (DbUpdateException dbEx)
            {
                // ✅ Log chi tiết lỗi database
                _logger.LogError(dbEx, "Database error when creating flood report");
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                return BadRequest(new
                {
                    message = "Lỗi database khi lưu báo cáo",
                    error = innerMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating flood report");
                return BadRequest(new
                {
                    message = $"Lỗi: {ex.Message}",
                    innerError = ex.InnerException?.Message
                });
            }
        }

        // 📍 Lấy danh sách các điểm ngập đã được duyệt (hiển thị trên map)
        [HttpGet("approved")]
        public async Task<IActionResult> GetApprovedReports()
        {
            try
            {
                var reports = await _context.FloodReports
                    .Where(f => f.Status == "Approved")
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(f => new
                    {
                        id = f.Id,
                        title = f.Title ?? "",  // ✅ Xử lý NULL
                        description = f.Description ?? "",
                        waterLevel = f.WaterLevel ?? "Low",
                        latitude = f.Latitude,
                        longitude = f.Longitude,
                        address = f.Address ?? "",
                        imageUrl = f.ImageUrl ?? "",
                        userId = f.UserId,
                        status = f.Status ?? "Pending",
                        createdAt = f.CreatedAt,
                        updatedAt = f.UpdatedAt,
                        approvedAt = f.ApprovedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = reports
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                return StatusCode(500, new
                {
                    success = false,
                    message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        // 👤 Lấy báo cáo của user
        [HttpGet("my-reports/{userId}")]
        public async Task<IActionResult> GetMyReports(int userId, [FromQuery] string? status = null)
        {
            try
            {
                var query = _context.FloodReports
                    .Include(f => f.User)
                    .Where(f => f.UserId == userId);

                // Filter theo status nếu có
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(f => f.Status == status);
                }

                var reports = await query
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(f => new
                    {
                        id = f.Id,
                        title = f.Title ?? "",
                        description = f.Description ?? "",
                        waterLevel = f.WaterLevel ?? "Unknown",
                        latitude = f.Latitude,
                        longitude = f.Longitude,
                        address = f.Address ?? "",
                        imageUrl = f.ImageUrl ?? "",
                        userId = f.UserId,
                        status = f.Status ?? "Pending",
                        adminNote = f.AdminNote ?? "",
                        createdAt = f.CreatedAt,
                        updatedAt = f.UpdatedAt,
                        approvedAt = f.ApprovedAt,
                        user = f.User == null ? null : new
                        {
                            id = f.User.Id,
                            fullName = f.User.FullName ?? string.Empty,
                            email = f.User.Email ?? string.Empty
                        }
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = reports
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user reports");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        // 🔐 Admin: Lấy tất cả báo cáo
        [HttpGet("admin/all")]
        public async Task<IActionResult> GetAllReports(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.FloodReports
                    .Include(r => r.User)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(r => r.Status == status);
                }

                var totalCount = await query.CountAsync();

                var reports = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        r.Id,
                        r.Title,
                        r.Description,
                        r.Latitude,
                        r.Longitude,
                        r.Address,
                        r.ImageUrl,
                        r.WaterLevel,
                        r.Status,
                        r.AdminNote,
                        r.CreatedAt,
                        User = new
                        {
                            r.User.Id,
                            r.User.Email,
                            r.User.FullName
                        }
                    })
                    .ToListAsync();

                return Ok(new
                {
                    message = "Lấy danh sách thành công",
                    data = reports,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi: {ex.Message}" });
            }
        }

        // 🔐 Admin: Duyệt báo cáo + Đánh giá mức độ ngập
        [HttpPut("admin/{id}/review")]
        public async Task<IActionResult> ReviewReport(long id, [FromBody] ReviewFloodReportRequest request)
        {
            try
            {
                var report = await _context.FloodReports.FindAsync(id);
                if (report == null)
                {
                    return NotFound(new { message = "Không tìm thấy báo cáo" });
                }

                // ✅ THÊM: Validate WaterLevel
                var validWaterLevels = new[] { "Low", "Medium", "High", "Critical", "Unknown" };
                if (!string.IsNullOrEmpty(request.WaterLevel) && !validWaterLevels.Contains(request.WaterLevel))
                {
                    return BadRequest(new { message = "WaterLevel phải là: Low, Medium, High, Critical, hoặc Unknown" });
                }

                report.Status = request.Status;
                report.AdminNote = request.AdminNote;

                // ✅ THÊM: Cập nhật WaterLevel nếu admin đánh giá
                if (!string.IsNullOrEmpty(request.WaterLevel))
                {
                    report.WaterLevel = request.WaterLevel;
                }

                report.UpdatedAt = DateTime.Now;

                if (request.Status == "Approved")
                {
                    report.ApprovedAt = DateTime.Now;

                    // ✅ THÊM: Validate phải có WaterLevel khi duyệt
                    if (report.WaterLevel == "Unknown")
                    {
                        return BadRequest(new { message = "Vui lòng đánh giá mức độ ngập trước khi duyệt!" });
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Cập nhật báo cáo thành công",
                    report = new
                    {
                        report.Id,
                        report.Status,
                        report.WaterLevel,  // ✅ THÊM
                        report.AdminNote,
                        report.ApprovedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi: {ex.Message}" });
            }
        }
    }

    // DTOs
    public class CreateFloodReportRequest
    {
        [Required(ErrorMessage = "Title là bắt buộc")]
        [StringLength(200)]
        public string Title { get; set; } = default!;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Latitude là bắt buộc")]
        [Range(-90, 90, ErrorMessage = "Latitude phải từ -90 đến 90")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Longitude là bắt buộc")]
        [Range(-180, 180, ErrorMessage = "Longitude phải từ -180 đến 180")]
        public double Longitude { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [Required(ErrorMessage = "ImageUrl là bắt buộc")]
        [StringLength(500)]
        public string ImageUrl { get; set; } = default!;

        [Required(ErrorMessage = "WaterLevel là bắt buộc")]
        public string WaterLevel { get; set; } = "Unknown";

        [Required(ErrorMessage = "UserId là bắt buộc")]
        public long UserId { get; set; }
    }

    public class ReviewFloodReportRequest
    {
        [Required]
        public string Status { get; set; } = default!; // Approved, Rejected

        [StringLength(500)]
        public string? AdminNote { get; set; }

        // ✅ THÊM: Admin đánh giá mức độ ngập
        public string? WaterLevel { get; set; } // Low, Medium, High, Critical
    }
}