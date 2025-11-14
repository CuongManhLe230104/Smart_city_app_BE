using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCity_BE.Models  // Chú ý namespace này
{
    [Table("Users")]
    public class User
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = default!;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = default!;

        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(15)]
        public string? PhoneNumber { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}