using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace QuanLySinhVien.Models
{
    public partial class LopHocPhan
    {
        [Key]
        public string MaLhp { get; set; } = null!;
        public string MaMon { get; set; } = null!;
        public string MaGv { get; set; } = null!;

        // 🛑 ĐÃ SỬA: Cho phép trống (null)
        public string? MaLop { get; set; }

        public string MaHocKy { get; set; } = null!;

        public int Thu { get; set; }
        public int TietBatDau { get; set; }
        public int SoTiet { get; set; }
        public string? PhongHoc { get; set; }
        public int SiSoToiDa { get; set; }

        public virtual GiangVien? MaGvNavigation { get; set; }
        public virtual HocKy? MaHocKyNavigation { get; set; }
        public virtual LopHoc? MaLopNavigation { get; set; }
        public virtual MonHoc? MaMonNavigation { get; set; }
        public virtual ICollection<DangKyHoc> DangKyHocs { get; set; } = new List<DangKyHoc>();
        public string? ThoiKhoaBieu { get; set; }
    }
}