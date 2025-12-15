using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCity_BE.Data;
using SmartCity_BE.Models;
using SmartCity_BE.Services;
using System.ComponentModel.DataAnnotations; // ✅ THÊM
using System.Security.Claims;

namespace SmartCity_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FloodReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FloodReportsController> _logger;
        private readonly NotificationService _notificationService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public FloodReportsController(
            ApplicationDbContext context,
            ILogger<FloodReportsController> logger,
            NotificationService notificationService,
            IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
            _serviceScopeFactory = serviceScopeFactory;
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
        public async Task<IActionResult> ReviewFloodReport(long id, [FromBody] ReviewFloodReportDto dto)
        {
            try
            {
                var report = await _context.FloodReports.FindAsync(id);

                if (report == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy báo cáo" });
                }

                // ✅ VALIDATE: Nếu duyệt (Approved), bắt buộc phải có waterLevel
                if (dto.Status == "Approved" && string.IsNullOrEmpty(dto.WaterLevel))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Vui lòng chọn mức độ ngập (waterLevel) trước khi duyệt!"
                    });
                }

                // ✅ UPDATE: Status
                report.Status = dto.Status;

                // ✅ UPDATE: WaterLevel (chỉ khi duyệt)
                if (dto.Status == "Approved" && !string.IsNullOrEmpty(dto.WaterLevel))
                {
                    report.WaterLevel = dto.WaterLevel;
                }

                // ✅ UPDATE: AdminNote (optional)
                if (!string.IsNullOrEmpty(dto.AdminNote))
                {
                    report.AdminNote = dto.AdminNote;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "✅ Report {Id} reviewed: Status={Status}, WaterLevel={WaterLevel}",
                    id,
                    report.Status,
                    report.WaterLevel ?? "N/A"
                );

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật báo cáo thành công",
                    data = new
                    {
                        id = report.Id,
                        status = report.Status,
                        waterLevel = report.WaterLevel,
                        adminNote = report.AdminNote
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error reviewing report {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Lỗi cập nhật báo cáo: {ex.Message}"
                });
            }
        }

        // 🔍 Lấy danh sách báo cáo ngập lụt
        [HttpGet]
        public async Task<IActionResult> GetFloodReports([FromQuery] string? status = null)
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

                var reports = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new
                    {
                        id = r.Id,
                        title = r.Title,
                        description = r.Description,
                        address = r.Address,
                        latitude = r.Latitude,
                        longitude = r.Longitude,
                        // ✅ FIX: Replace 10.0.2.2 với localhost
                        imageUrl = !string.IsNullOrEmpty(r.ImageUrl)
                            ? r.ImageUrl.Replace("http://10.0.2.2:5000", "http://localhost:5000")
                            : null,
                        waterLevel = r.WaterLevel,
                        status = r.Status,
                        adminNote = r.AdminNote,
                        createdAt = r.CreatedAt,
                        user = new
                        {
                            id = r.User!.Id,
                            fullName = r.User.FullName,
                            email = r.User.Email,
                        }
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = reports });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting flood reports");
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpDelete("admin/{id}")]
        public async Task<IActionResult> DeleteFloodReport(long id)
        {
            try
            {
                var report = await _context.FloodReports.FindAsync(id);

                if (report == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy báo cáo" });
                }

                _context.FloodReports.Remove(report);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"🗑️ Deleted flood report {id}");

                return Ok(new
                {
                    success = true,
                    message = $"Đã xóa báo cáo: {report.Title}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting flood report {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Lỗi xóa báo cáo: {ex.Message}"
                });
            }
        }

        [HttpPut("admin/{id}")]
        public async Task<IActionResult> UpdateFloodReport(long id, [FromBody] UpdateFloodReportDto dto)
        {
            try
            {
                var report = await _context.FloodReports.FindAsync(id);

                if (report == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy báo cáo" });
                }

                // Update fields
                if (!string.IsNullOrEmpty(dto.Title))
                    report.Title = dto.Title;

                if (!string.IsNullOrEmpty(dto.Description))
                    report.Description = dto.Description;

                if (!string.IsNullOrEmpty(dto.Address))
                    report.Address = dto.Address;

                if (dto.Latitude.HasValue)
                    report.Latitude = dto.Latitude.Value;

                if (dto.Longitude.HasValue)
                    report.Longitude = dto.Longitude.Value;

                if (!string.IsNullOrEmpty(dto.WaterLevel))
                    report.WaterLevel = dto.WaterLevel;

                if (!string.IsNullOrEmpty(dto.Status))
                    report.Status = dto.Status;

                if (!string.IsNullOrEmpty(dto.AdminNote))
                    report.AdminNote = dto.AdminNote;

                report.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Updated flood report {id}");

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật báo cáo thành công",
                    data = new
                    {
                        report.Id,
                        report.Title,
                        report.Description,
                        report.Address,
                        report.Latitude,
                        report.Longitude,
                        report.WaterLevel,
                        report.Status,
                        report.AdminNote,
                        report.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating flood report {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Lỗi cập nhật báo cáo: {ex.Message}"
                });
            }
        }

        // ✅ PUT: /api/FloodReports/{id}/approve
        [HttpPut("{id}/approve")]
        [Authorize]
        public async Task<ActionResult> ApproveFloodReport(long id, [FromBody] ApproveFloodReportDto dto)
        {
            try
            {
                _logger.LogInformation($"📥 Approving flood report {id}");

                var report = await _context.FloodReports
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (report == null)
                {
                    return NotFound(new { message = "Không tìm thấy báo cáo" });
                }

                if (report.Status == "approved")
                {
                    return BadRequest(new { message = "Báo cáo đã được phê duyệt" });
                }

                report.Status = "approved";
                report.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Flood report {id} approved");

                // ✅ GỬI NOTIFICATION
                _ = SendFloodReportNotificationAsync(report, "approved", dto.AdminNote);

                return Ok(new
                {
                    success = true,
                    message = "Phê duyệt báo cáo ngập lụt thành công",
                    data = new { report.Id, report.Status }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving flood report {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ✅ PUT: /api/FloodReports/{id}/reject
        [HttpPut("{id}/reject")]
        [Authorize]
        public async Task<ActionResult> RejectFloodReport(long id, [FromBody] RejectFloodReportDto dto)
        {
            try
            {
                _logger.LogInformation($"📥 Rejecting flood report {id}");

                var report = await _context.FloodReports
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (report == null)
                {
                    return NotFound(new { message = "Không tìm thấy báo cáo" });
                }

                report.Status = "rejected";
                report.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"❌ Flood report {id} rejected");

                // ✅ GỬI NOTIFICATION
                _ = SendFloodReportNotificationAsync(report, "rejected", dto.Reason);

                return Ok(new
                {
                    success = true,
                    message = "Từ chối báo cáo ngập lụt thành công",
                    data = new { report.Id, report.Status }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting flood report {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ✅ PUT: /api/FloodReports/{id}/resolve
        [HttpPut("{id}/resolve")]
        [Authorize]
        public async Task<ActionResult> ResolveFloodReport(long id, [FromBody] ResolveFloodReportDto dto)
        {
            try
            {
                _logger.LogInformation($"📥 Resolving flood report {id}");

                var report = await _context.FloodReports
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (report == null)
                {
                    return NotFound(new { message = "Không tìm thấy báo cáo" });
                }

                report.Status = "resolved";
                report.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Flood report {id} resolved");

                // ✅ GỬI NOTIFICATION
                _ = SendFloodReportNotificationAsync(report, "resolved", dto.Note);

                return Ok(new
                {
                    success = true,
                    message = "Đánh dấu đã xử lý thành công",
                    data = new { report.Id, report.Status }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resolving flood report {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ✅ PRIVATE METHOD: Gửi notification
        private Task SendFloodReportNotificationAsync(FloodReport report, string action, string? note)
        {
            return Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<FloodReportsController>>();

                    logger.LogInformation($"📤 Sending notification for flood report {report.Id} - action: {action}");

                    if (string.IsNullOrEmpty(report.User?.FcmToken))
                    {
                        logger.LogWarning($"⚠️ User has no FCM token");
                        return;
                    }

                    // ✅ SỬA: Dùng Address thay vì Location
                    var location = report.Address ?? "vị trí không xác định";

                    var (title, body) = action switch
                    {
                        "approved" => (
                            "✅ Báo cáo ngập lụt được phê duyệt",
                            $"Báo cáo ngập lụt tại '{location}' đã được tiếp nhận. {note}"
                        ),
                        "rejected" => (
                            "❌ Báo cáo ngập lụt bị từ chối",
                            $"Báo cáo ngập lụt tại '{location}' bị từ chối. Lý do: {note}"
                        ),
                        "resolved" => (
                            "✅ Ngập lụt đã được xử lý",
                            $"Khu vực '{location}' đã được xử lý xong. {note}"
                        ),
                        _ => ("📢 Cập nhật báo cáo ngập lụt", $"Báo cáo tại '{location}' có cập nhật mới")
                    };

                    var notificationService = new NotificationService();
                    await notificationService.SendNotificationAsync(
                        report.User.FcmToken,
                        title,
                        body,
                        new Dictionary<string, string>
                        {
                            { "type", "flood_report" },
                            { "reportId", report.Id.ToString() },
                            { "action", action },
                            { "status", report.Status },
                            { "location", location },
                            { "timestamp", DateTime.UtcNow.ToString("O") }
                        }
                    );

                    logger.LogInformation($"✅ Notification sent to user");
                }
                catch (Exception ex)
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<FloodReportsController>>();
                    logger.LogError(ex, "❌ Error sending flood report notification");
                }
            });
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
        public string? WaterLevel { get; set; } // Low, Medium, High, Dangerous
    }

    // ✅ DTO class
    public class ReviewFloodReportDto
    {
        public string Status { get; set; } = ""; // Required: Approved, Rejected, Pending
        public string? WaterLevel { get; set; } // Optional: Low, Medium, High, Dangerous
        public string? AdminNote { get; set; } // Optional
    }

    public class UpdateFloodReportDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? WaterLevel { get; set; }
        public string? Status { get; set; }
        public string? AdminNote { get; set; }
    }

    // ✅ DTOs
    public class ApproveDto
    {
        public string? AdminNote { get; set; }
    }

    public class RejectDto
    {
        public string Reason { get; set; } = default!;
    }

    public class ResolveDto
    {
        public string? Note { get; set; }
    }

    // ✅ DTOs
    public class ApproveFloodReportDto
    {
        public string? AdminNote { get; set; }
    }

    public class RejectFloodReportDto
    {
        [Required(ErrorMessage = "Vui lòng nhập lý do từ chối")]
        [StringLength(500, ErrorMessage = "Lý do không quá 500 ký tự")]
        public string Reason { get; set; } = default!;
    }

    public class ResolveFloodReportDto
    {
        [StringLength(500)]
        public string? Note { get; set; }
    }
}