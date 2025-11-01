using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("NguoiDung")]
public class NguoiDung
{
    [Key]
    public long Id { get; set; }
    [Required]
    public string Email { get; set; }
    [Required]
    public string MatKhau { get; set; }
    public string? HoTen { get; set; }
    public virtual ICollection<PhanAnh> DanhSachPhanAnh { get; set; } = new HashSet<PhanAnh>();
}