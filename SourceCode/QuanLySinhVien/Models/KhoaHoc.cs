using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLySinhVien.Models
{
    [Table("KhoaHoc")]
    public class KhoaHoc
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string MaKhoaHoc { get; set; } = null!; 

        [Required]
        [MaxLength(50)]
        public string TenKhoaHoc { get; set; } = null!; 

        public int? NamBatDau { get; set; }
    }
}