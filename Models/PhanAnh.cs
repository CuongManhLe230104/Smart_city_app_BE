// using System.ComponentModel.DataAnnotations;
// using System.ComponentModel.DataAnnotations.Schema;

// [Table("PhanAnh")]
// public class PhanAnh
// {
//     [Key]
//     public long Id { get; set; }
//     [Column(TypeName = "ntext")]
//     public string? NoiDung { get; set; }
//     public string? HinhAnhUrl { get; set; }
//     public DateTime NgayTao { get; set; } = DateTime.Now;
//     public string TrangThai { get; set; } = "CHO_DUYET";

//     public long NguoiDungId { get; set; }
//     [ForeignKey("NguoiDungId")]
//     public virtual NguoiDung NguoiDung { get; set; }
// }