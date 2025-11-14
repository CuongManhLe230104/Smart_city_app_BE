using System.ComponentModel.DataAnnotations;

namespace SmartCity_BE.DTOs
{
    public class BusRouteDto
    {
        public int Id { get; set; }
        public string RouteNumber { get; set; } = default!;
        public string RouteName { get; set; } = default!;
        public string? Schedule { get; set; }
        public string? Description { get; set; }
        public string? StartPoint { get; set; }
        public string? EndPoint { get; set; }
        public decimal? Price { get; set; }
        public string? FirstBusTime { get; set; }
        public string? LastBusTime { get; set; }
        public int? TripDuration { get; set; }
        public List<string>? Stops { get; set; } // Danh sách điểm dừng
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateUpdateBusRouteDto
    {
        [Required]
        public string RouteNumber { get; set; } = default!;

        [Required]
        public string RouteName { get; set; } = default!;

        public string? Schedule { get; set; }
        public string? Description { get; set; }
        public string? StartPoint { get; set; }
        public string? EndPoint { get; set; }
        public decimal? Price { get; set; }
        public string? FirstBusTime { get; set; }
        public string? LastBusTime { get; set; }
        public int? TripDuration { get; set; }
        public List<string>? Stops { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

