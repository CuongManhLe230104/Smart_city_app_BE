// Models/Feedback.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCity_BE.Models
{
    [Table("Feedbacks")]
    public class Feedback
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = default!;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = default!;

        [StringLength(50)]
        public string Category { get; set; } = default!; // Giao thông, Môi trường, Hạ tầng, Khác

        [StringLength(200)]
        public string? Location { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Processing, Resolved, Rejected

        [StringLength(500)]
        public string? AdminResponse { get; set; }

        // Foreign Key to User
        public long UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public DateTime? ResolvedAt { get; set; }
    }
}