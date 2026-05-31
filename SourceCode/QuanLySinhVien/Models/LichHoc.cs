using System.ComponentModel.DataAnnotations;

namespace QuanLySinhVien.Models
{
    public partial class LichHoc
    {
        [Key]
        public int MaLichHoc { get; set; }
        public int MaPhanCong { get; set; }
        public int Thu { get; set; }
        public int TietBatDau { get; set; }
        public int SoTiet { get; set; }
        public string? PhongHoc { get; set; }
    }
}