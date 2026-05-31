using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLySinhVien.Models
{
    [Table("CauHinhQR")]
    public class CauHinhQR
    {
        [Key]
        public int Id { get; set; }
        public string? TenNganHang { get; set; }
        public string? MaNganHang { get; set; }
        public string? SoTaiKhoan { get; set; }
        public string? TenChuTaiKhoan { get; set; }
    }
}