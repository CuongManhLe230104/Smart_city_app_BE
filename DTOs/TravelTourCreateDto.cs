// File: DTOs/TravelTourCreateDto.cs

using System.ComponentModel.DataAnnotations;

namespace SmartCity_BE.DTOs
{
    public class TravelTourCreateDto
    {
        // Lưu ý: UserId sẽ được lấy từ JWT/Token, không phải từ Body Request

        [Required(ErrorMessage = "Tên tour là bắt buộc")]
        [MaxLength(200)]
        public string NameTour { get; set; } = string.Empty; // ✅ PascalCase

        [MaxLength(100)]
        public string? TourType { get; set; } // ✅ PascalCase

        public string? Content { get; set; } // ✅ PascalCase

        public string? Timeline { get; set; } // ✅ PascalCase

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public decimal Price { get; set; } // ✅ PascalCase

        [Range(1, 1000, ErrorMessage = "Số người phải từ 1-1000")]
        public int MaxPeople { get; set; } // ✅ PascalCase

        [MaxLength(100)]
        public string? Duration { get; set; } // ✅ PascalCase

        public string? GalleryImageUrls { get; set; } // ✅ PascalCase
    }
}