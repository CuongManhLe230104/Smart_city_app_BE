using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("BusRoutes")] // Tên bảng sẽ là BusRoutes
public class BusRoute
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(10)]
    public string RouteNumber { get; set; } // Số hiệu tuyến, vd: "22"

    [Required]
    [StringLength(255)]
    public string RouteName { get; set; } // Tên tuyến, vd: "Vũng Tàu – Phú Túc"

    [Column(TypeName = "ntext")]
    public string? Schedule { get; set; } // Giờ chạy, lộ trình...
}