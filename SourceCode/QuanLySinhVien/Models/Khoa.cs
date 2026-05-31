using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Phải có dòng này

namespace QuanLySinhVien.Models
{
    public partial class Khoa
    {
        [Key] // 🛑 Thêm dòng này vào
        public string MaKhoa { get; set; } = null!;
        public string TenKhoa { get; set; } = null!;

        public virtual ICollection<GiangVien> GiangViens { get; set; } = new List<GiangVien>();
        public virtual ICollection<LopHoc> LopHocs { get; set; } = new List<LopHoc>();
        public virtual ICollection<MonHoc> MonHocs { get; set; } = new List<MonHoc>();
    }
}