using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCity_BE.Data;
using SmartCity_BE.Models;
using SmartCity_BE.DTOs;
using System.Threading.Tasks;

namespace SmartCity_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize] // Yêu cầu xác thực người dùng cho mọi thao tác đặt tour
    public class BookingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/Booking
        // Chức năng: Tạo đơn đặt tour mới
        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] BookingCreateDto request)
        {
            if (!ModelState.IsValid || request.TravelDate.Date < DateTime.Today)
            {
                return BadRequest("Dữ liệu đặt tour không hợp lệ hoặc ngày đi đã qua.");
            }

            // TODO: Lấy User ID từ Claims/Token JWT
            long userId = 1;

            // 1. Kiểm tra Tour có tồn tại và còn chỗ không
            var tour = await _context.TravelTours.FindAsync(request.TourId);
            if (tour == null)
            {
                return NotFound("Tour không tồn tại.");
            }

            if (request.NumberOfPeople <= 0 || request.NumberOfPeople > tour.MaxPeople)
            {
                return BadRequest($"Số lượng người không hợp lệ. Tour này tối đa {tour.MaxPeople} người.");
            }

            // 2. Tính toán tổng giá
            decimal totalPrice = tour.Price * request.NumberOfPeople;

            // 3. Tạo Model Booking
            var newBooking = new Booking
            {
                UserId = userId,
                TourId = request.TourId,
                TravelDate = request.TravelDate,
                NumberOfPeople = request.NumberOfPeople,
                BookingDate = DateTime.UtcNow,
                TotalPrice = totalPrice,
                Status = "Pending",
                SpecialRequests = request.SpecialRequests
            };

            _context.Bookings.Add(newBooking);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBooking), new { id = newBooking.BookingId }, newBooking);
        }

        // GET: api/Booking/{id}
        // Lấy chi tiết đơn đặt hàng (cần xác nhận người dùng sở hữu)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBooking(int id)
        {
            // Lấy User ID từ token
            long userId = 1;

            var booking = await _context.Bookings
                .Include(b => b.Tour) // Load thông tin Tour kèm theo
                .FirstOrDefaultAsync(b => b.BookingId == id && b.UserId == userId);

            if (booking == null)
            {
                return NotFound("Không tìm thấy đơn đặt hàng hoặc bạn không có quyền truy cập.");
            }

            return Ok(booking);
        }
        // GET: api/Booking/all-admin
        // Chức năng: Lấy tất cả các đơn đặt tour (Dành cho Admin)
        [HttpGet("all-admin")]
        public async Task<IActionResult> GetAllBookingsForAdmin()
        {
            try
            {
                var bookings = await _context.Bookings
                    .Include(b => b.Tour)
                    .Include(b => b.User) // ✅ THÊM: Include User
                    .OrderByDescending(b => b.BookingDate)
                    .ToListAsync();

                // ✅ TRANSFORM DATA - Trả về JSON chuẩn
                var result = bookings.Select(b => new
                {
                    bookingId = b.BookingId,
                    tourId = b.TourId,
                    userId = b.UserId,
                    travelDate = b.TravelDate,
                    numberOfPeople = b.NumberOfPeople,
                    totalPrice = b.TotalPrice,
                    status = b.Status, // ✅ Phải là: Pending, Confirmed, Cancelled, Completed
                    specialRequests = b.SpecialRequests,
                    bookingDate = b.BookingDate,

                    // ✅ Thông tin Tour
                    tour = b.Tour != null ? new
                    {
                        id = b.Tour.Id,
                        nameTour = b.Tour.NameTour,
                        coverImageUrl = b.Tour.CoverImageUrl,
                        price = b.Tour.Price
                    } : null,

                    // ✅ Thông tin User
                    user = b.User != null ? new
                    {
                        id = b.User.Id,
                        fullName = b.User.FullName,
                        email = b.User.Email,
                    } : null
                }).ToList();

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách booking thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Lỗi server: {ex.Message}"
                });
            }
        }
        // GET: api/Booking/my-history
        // Lấy lịch sử đặt tour của người dùng hiện tại
        [HttpGet("my-history")]
        public async Task<IActionResult> GetUserBookings()
        {
            // Lấy User ID từ token
            long userId = 1;

            var bookings = await _context.Bookings
                .Where(b => b.UserId == userId)
                .Include(b => b.Tour)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            // ✅ ĐẢM BẢO STATUS LÀ TIẾNG ANH
            var result = bookings.Select(b => new
            {
                bookingId = b.BookingId,
                // ...
                status = b.Status, // ✅ Phải là "Pending", "Confirmed", "Cancelled"
                // ...
            }).ToList();

            return Ok(result);
        }
        // PUT: api/Booking/{id}
        // Cập nhật đơn đặt tour (chỉ cho phép cập nhật số người và trạng thái)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBooking(int id, [FromBody] BookingUpdateDto request)
        {
            // Bắt buộc Include Tour để truy cập Price và MaxPeople cho việc tính toán lại
            var booking = await _context.Bookings
                .Include(b => b.Tour)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
                return NotFound("Không tìm thấy đơn đặt tour.");

            // Lấy thông tin Tour
            var tour = booking.Tour;
            if (tour == null)
            {
                // Đây là lỗi dữ liệu, nên được xử lý
                return StatusCode(500, "Lỗi dữ liệu: Tour liên kết không tồn tại.");
            }

            // ============= 1. Cập nhật số người & Tính lại Giá =============
            if (request.NumberOfPeople.HasValue && request.NumberOfPeople != booking.NumberOfPeople)
            {
                int newNumberOfPeople = request.NumberOfPeople.Value;

                if (newNumberOfPeople <= 0)
                    return BadRequest("Số người không hợp lệ.");

                // Kiểm tra giới hạn MaxPeople của Tour
                if (newNumberOfPeople > tour.MaxPeople)
                {
                    return BadRequest($"Số lượng người không hợp lệ. Tour này tối đa {tour.MaxPeople} người.");
                }

                // Cập nhật số người
                booking.NumberOfPeople = newNumberOfPeople;
                // **Tính toán lại tổng giá**
                booking.TotalPrice = tour.Price * newNumberOfPeople;
            }
            // ===============================================================

            // 2. Cập nhật trạng thái (thường dành cho Admin)
            if (!string.IsNullOrEmpty(request.Status))
            {
                // Tùy chọn: Thêm kiểm tra trạng thái hợp lệ (Pending, Confirmed, Cancelled)
                booking.Status = request.Status;
            }

            // 3. Cập nhật ghi chú
            if (request.SpecialRequests != null)
                booking.SpecialRequests = request.SpecialRequests;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Bookings.Any(e => e.BookingId == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(new
            {
                message = "Cập nhật thành công.",
                booking
            });
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null)
                return NotFound("Không tìm thấy đơn đặt tour.");

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa đơn đặt tour thành công." });
        }
    }
}