using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCity_BE.Models
{
    [Table("EventBanners")]
    public class EventBanner
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = default!;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public string ImageUrl { get; set; } = default!;
    }
}