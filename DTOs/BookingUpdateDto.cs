namespace SmartCity_BE.DTOs
{
    public class BookingUpdateDto
    {
        public int? NumberOfPeople { get; set; }
        public string? Status { get; set; }
        public string? SpecialRequests { get; set; }
    }
}