using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLySinhVien.Models
{
    public partial class DonGiaHocPhi
    {
        [Key]
        public int Id { get; set; }
        public string MaKhoa { get; set; } = null!;
        public string KhoaHoc { get; set; } = null!;
        public decimal SoTienMotTinChi { get; set; }
        public string? GhiChu { get; set; }
    }
}