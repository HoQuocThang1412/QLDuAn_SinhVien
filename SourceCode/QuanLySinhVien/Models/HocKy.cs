using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLySinhVien.Models
{
    public partial class HocKy
    {
        [Key]
        public string MaHocKy { get; set; } = null!;

        public string? TenHocKy { get; set; }

        // 🛑 XÓA DẤU "?" ĐỂ FIX LỖI CS1501 BỊ CHỬI TRONG FILE HTML
        public DateOnly NgayBatDau { get; set; }
        public DateOnly NgayKetThuc { get; set; }
        public string? TrangThai { get; set; }
        public int? GioiHanTinChi { get; set; }

        public virtual ICollection<DangKyHoc> DangKyHocs { get; set; } = new List<DangKyHoc>();
        public virtual ICollection<LopHocPhan> LopHocPhans { get; set; } = new List<LopHocPhan>();
    }
}