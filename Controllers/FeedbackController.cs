// Controllers/FeedbackController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCity_BE.Data;
using SmartCity_BE.Models;
using System.Security.Claims;

namespace SmartCity_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FeedbackController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Gửi phản ánh (User)
        [HttpPost]
        public async Task<IActionResult> CreateFeedback([FromBody] CreateFeedbackRequest request)
        {
            try
            {
                var feedback = new Feedback
                {
                    Title = request.Title,
                    Description = request.Description,
                    Category = request.Category,
                    Location = request.Location,
                    ImageUrl = request.ImageUrl,
                    UserId = request.UserId, // Tạm thời dùng UserId từ request, sau này sẽ lấy từ JWT
                    Status = "Pending",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Gửi phản ánh thành công! Chúng tôi sẽ xem xét và phản hồi sớm nhất.",
                    feedbackId = feedback.Id,
                    status = feedback.Status
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi: {ex.Message}" });
            }
        }

        // Lấy danh sách phản ánh của user
        [HttpGet("my-feedbacks/{userId}")]
        public async Task<IActionResult> GetMyFeedbacks(long userId)
        {
            try
            {
                var feedbacks = await _context.Feedbacks
                    .Where(f => f.UserId == userId)
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(f => new
                    {
                        f.Id,
                        f.Title,
                        f.Description,
                        f.Category,
                        f.Location,
                        f.ImageUrl,
                        f.Status,
                        f.AdminResponse,
                        f.CreatedAt,
                        f.ResolvedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    message = "Lấy danh sách phản ánh thành công",
                    data = feedbacks
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi: {ex.Message}" });
            }
        }

        // Admin: Lấy tất cả phản ánh
        [HttpGet("admin/all")]
        public async Task<IActionResult> GetAllFeedbacks(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.Feedbacks
                    .Include(f => f.User)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(f => f.Status == status);
                }

                var totalCount = await query.CountAsync();

                var feedbacks = await query
                    .OrderByDescending(f => f.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(f => new
                    {
                        f.Id,
                        f.Title,
                        f.Description,
                        f.Category,
                        f.Location,
                        f.ImageUrl,
                        f.Status,
                        f.AdminResponse,
                        f.CreatedAt,
                        f.UpdatedAt,
                        f.ResolvedAt,
                        User = new
                        {
                            f.User.Id,
                            f.User.Email,
                            f.User.FullName
                        }
                    })
                    .ToListAsync();

                return Ok(new
                {
                    message = "Lấy danh sách phản ánh thành công",
                    data = feedbacks,
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

        // Admin: Xử lý phản ánh
        [HttpPut("admin/{id}/respond")]
        public async Task<IActionResult> RespondToFeedback(long id, [FromBody] AdminResponseRequest request)
        {
            try
            {
                var feedback = await _context.Feedbacks.FindAsync(id);
                if (feedback == null)
                {
                    return NotFound(new { message = "Không tìm thấy phản ánh" });
                }

                feedback.Status = request.Status;
                feedback.AdminResponse = request.Response;
                feedback.UpdatedAt = DateTime.Now;

                if (request.Status == "Resolved")
                {
                    feedback.ResolvedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Cập nhật phản ánh thành công",
                    feedback = new
                    {
                        feedback.Id,
                        feedback.Status,
                        feedback.AdminResponse,
                        feedback.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi: {ex.Message}" });
            }
        }

        // Lấy thống kê
        [HttpGet("admin/statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var totalFeedbacks = await _context.Feedbacks.CountAsync();
                var pendingCount = await _context.Feedbacks.CountAsync(f => f.Status == "Pending");
                var processingCount = await _context.Feedbacks.CountAsync(f => f.Status == "Processing");
                var resolvedCount = await _context.Feedbacks.CountAsync(f => f.Status == "Resolved");
                var rejectedCount = await _context.Feedbacks.CountAsync(f => f.Status == "Rejected");

                var todayFeedbacks = await _context.Feedbacks
                    .CountAsync(f => f.CreatedAt.Date == DateTime.Today);

                var categoryStats = await _context.Feedbacks
                    .GroupBy(f => f.Category)
                    .Select(g => new
                    {
                        category = g.Key,
                        count = g.Count()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    message = "Lấy thống kê thành công",
                    statistics = new
                    {
                        totalFeedbacks,
                        pendingCount,
                        processingCount,
                        resolvedCount,
                        rejectedCount,
                        todayFeedbacks,
                        categoryStats
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi: {ex.Message}" });
            }
        }

        // ⭐ Thêm endpoint mới - Lấy tất cả phản ánh công khai
        [HttpGet("public")]
        public async Task<IActionResult> GetPublicFeedbacks(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? category = null,
            [FromQuery] string? status = null)
        {
            try
            {
                var query = _context.Feedbacks
                    .Include(f => f.User)
                    .AsQueryable();

                // Filter by category
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(f => f.Category == category);
                }

                // Filter by status
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(f => f.Status == status);
                }

                var totalCount = await query.CountAsync();

                var feedbacks = await query
                    .OrderByDescending(f => f.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(f => new
                    {
                        f.Id,
                        f.Title,
                        f.Description,
                        f.Category,
                        f.Location,
                        f.ImageUrl,
                        f.Status,
                        f.AdminResponse,
                        f.CreatedAt,
                        f.ResolvedAt,
                        User = new
                        {
                            f.User.Id,
                            f.User.FullName,
                            // Ẩn email để bảo mật
                        }
                    })
                    .ToListAsync();

                return Ok(new
                {
                    message = "Lấy danh sách phản ánh thành công",
                    data = feedbacks,
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
                Console.WriteLine($"❌ Error: {ex.Message}");
                return BadRequest(new { message = $"Lỗi: {ex.Message}" });
            }
        }
    }

    // DTOs
    public class CreateFeedbackRequest
    {
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Category { get; set; } = default!;
        public string? Location { get; set; }
        public string? ImageUrl { get; set; }
        public long UserId { get; set; }
    }

    public class AdminResponseRequest
    {
        public string Status { get; set; } = default!; // Processing, Resolved, Rejected
        public string Response { get; set; } = default!;
    }
}