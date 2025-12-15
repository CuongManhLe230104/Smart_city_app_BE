// File: Models/Booking.cs

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCity_BE.Models
{
    [Table("Bookings")]
    public class Booking
    {
        [Key]
        // ID khóa chính cho đơn đặt hàng
        public int BookingId { get; set; }

        // Khóa ngoại đến bảng User (Người đặt hàng)
        [Required]
        public long UserId { get; set; }

        // Khóa ngoại đến bảng TravelTour
        [Required]
        public int TourId { get; set; }

        // Số lượng người tham gia tour
        [Required]
        public int NumberOfPeople { get; set; }

        // Ngày bắt đầu chuyến đi
        [Required]
        public DateTime TravelDate { get; set; }

        // Ngày tạo đơn đặt hàng (Mặc định là thời điểm hiện tại)
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        // Tổng giá cuối cùng (đã tính toán dựa trên Price của Tour và NumberOfPeople)
        [Column(TypeName = "decimal(18, 2)")]
        [Required]
        public decimal TotalPrice { get; set; }

        // Trạng thái đơn hàng: "Pending", "Confirmed", "Cancelled", "Completed"
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        // Thông tin tùy chọn/yêu cầu đặc biệt
        public string? SpecialRequests { get; set; }

        // --- Navigation Properties ---

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = default!;

        [ForeignKey("TourId")]
        public virtual TravelTour Tour { get; set; } = default!;
    }
}