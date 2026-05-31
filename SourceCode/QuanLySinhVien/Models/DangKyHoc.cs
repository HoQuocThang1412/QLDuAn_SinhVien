using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLySinhVien.Models
{
    public partial class DangKyHoc
    {
        [Key]
        public int MaDangKy { get; set; }
        public string Mssv { get; set; } = null!;
        public string MaMon { get; set; } = null!;
        public string MaLop { get; set; } = null!;
        public string MaHocKy { get; set; } = null!;
        public string? MaLhp { get; set; }
        public int? LanHoc { get; set; }

        [ForeignKey("MaHocKy")]
        public virtual HocKy? MaHocKyNavigation { get; set; }
        [ForeignKey("MaLhp")]
        public virtual LopHocPhan? MaLhpNavigation { get; set; }
        [ForeignKey("MaLop")]
        public virtual LopHoc? MaLopNavigation { get; set; }
        [ForeignKey("MaMon")]
        public virtual MonHoc? MaMonNavigation { get; set; }
        [ForeignKey("Mssv")]
        public virtual SinhVien? MssvNavigation { get; set; }
        public virtual KetQuaHocTap? KetQuaHocTap { get; set; }
    }
}