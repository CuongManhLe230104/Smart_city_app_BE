namespace SmartCity_BE.DTOs
{
    // DTO để gửi dữ liệu banner "sạch" cho frontend
    public class EventBannerDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public string ImageUrl { get; set; } = default!;
    }
}