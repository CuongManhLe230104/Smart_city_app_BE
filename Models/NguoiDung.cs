// using System.ComponentModel.DataAnnotations;
// using System.ComponentModel.DataAnnotations.Schema;

// namespace SmartCity_BE.Models // Thêm namespace
// {
//     [Table("NguoiDung")]
//     public class NguoiDung
//     {
//         [Key]
//         public long Id { get; set; }
//         [Required]
//         public string Email { get; set; } = default!; // <-- SỬA
//         [Required]
//         public string MatKhau { get; set; } = default!; // <-- SỬA
//         public string? HoTen { get; set; }
//         public virtual ICollection<PhanAnh> DanhSachPhanAnh { get; set; } = new HashSet<PhanAnh>();
//     }
// }