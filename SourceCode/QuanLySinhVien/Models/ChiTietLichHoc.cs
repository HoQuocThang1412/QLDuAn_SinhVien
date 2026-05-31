using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLySinhVien.Models
{
    [Table("ChiTietLichHoc")]
    public class ChiTietLichHoc
    {
        [Key]
        public int MaChiTiet { get; set; }
        public string MaLhp { get; set; }
        public int Thu { get; set; }
        public int TietBatDau { get; set; }
        public int SoTiet { get; set; }

        [ForeignKey("MaLhp")]
        public virtual LopHocPhan? MaLhpNavigation { get; set; }
    }
}