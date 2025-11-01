using System.ComponentModel.DataAnnotations;

// 1. Thêm namespace
namespace SmartCity_BE.DTOs
{
    // DTO để hiển thị thông tin tuyến xe
    public class BusRouteDto
    {
        public int Id { get; set; }
        public string RouteNumber { get; set; }
        public string RouteName { get; set; }
        public string? Schedule { get; set; }
    }

    // DTO để tạo mới hoặc cập nhật
    public class CreateUpdateBusRouteDto
    {
        [Required]
        public string RouteNumber { get; set; }
        [Required]
        public string RouteName { get; set; }
        public string? Schedule { get; set; }
    }
}

