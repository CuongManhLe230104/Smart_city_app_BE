// Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCity_BE.Data;
using SmartCity_BE.Models;
using SmartCity_BE.Services;
using System.ComponentModel.DataAnnotations;

namespace SmartCity_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(ApplicationDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
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
}