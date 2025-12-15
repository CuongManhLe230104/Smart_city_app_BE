using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCity_BE.Models
{
    [Table("TravelTours")]
    public class TravelTour
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(500)]
        public string CoverImageUrl { get; set; }
        public string GalleryImageUrls { get; set; }

        [Required]
        [MaxLength(255)]
        public string NameTour { get; set; }

        [Required]
        [MaxLength(100)]
        public string TourType { get; set; }

        public string Content { get; set; }

        public string Timeline { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        public int MaxPeople { get; set; }

        [MaxLength(100)]
        public string Duration { get; set; }
        // Foreign Key
        [Required]
        public long UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}