using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCity_BE.Models
{
    [Table("FloodReports")]
    public class FloodReport
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = default!;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; } = default!;

        [StringLength(50)]
        public string WaterLevel { get; set; } = "Unknown";

        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [StringLength(500)]
        public string? AdminNote { get; set; }

        // Foreign Key
        [Required] // ✅ Thêm Required
        public long UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; } // ✅ Thêm ? để nullable

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public DateTime? ApprovedAt { get; set; }
    }
}