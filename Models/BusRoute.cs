// Models/BusRoute.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCity_BE.Models
{
    [Table("BusRoutes")]
    public class BusRoute
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string RouteNumber { get; set; } = default!;

        [Required]
        [StringLength(255)]
        public string RouteName { get; set; } = default!;

        [StringLength(100)]
        public string? Schedule { get; set; }

        // ⭐ Thêm các trường mới
        [StringLength(500)]
        public string? Description { get; set; } // Mô tả tuyến

        [StringLength(100)]
        public string? StartPoint { get; set; } // Điểm đầu

        [StringLength(100)]
        public string? EndPoint { get; set; } // Điểm cuối

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Price { get; set; } // Giá vé

        [StringLength(50)]
        public string? FirstBusTime { get; set; } // Giờ xe đầu

        [StringLength(50)]
        public string? LastBusTime { get; set; } // Giờ xe cuối

        public int? TripDuration { get; set; } // Thời gian hành trình (phút)

        [StringLength(1000)]
        public string? StopsJson { get; set; } // JSON các điểm dừng

        [StringLength(500)]
        public string? ImageUrl { get; set; } // Ảnh minh họa

        public bool IsActive { get; set; } = true; // Tuyến đang hoạt động
    }
}