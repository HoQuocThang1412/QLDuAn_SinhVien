using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLySinhVien.Models
{
    public partial class ThanhToanHocPhi
    {
        [Key]
        public int MaThanhToan { get; set; }
        public string Mssv { get; set; } = null!;
        public string MaHocKy { get; set; } = null!;
        public decimal SoTienDong { get; set; }
        public DateTime? NgayDong { get; set; }
        public string? HinhThuc { get; set; }
    }
}