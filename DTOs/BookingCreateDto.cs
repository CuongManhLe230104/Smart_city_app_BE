// File: DTOs/BookingCreateDto.cs

using System.ComponentModel.DataAnnotations;

namespace SmartCity_BE.DTOs
{
    public class BookingCreateDto
    {
        [Required]
        public int TourId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng người phải lớn hơn 0.")]
        public int NumberOfPeople { get; set; }

        [Required]
        // Đảm bảo ngày đi không phải là ngày trong quá khứ
        public DateTime TravelDate { get; set; }

        public string? SpecialRequests { get; set; }
    }
}