using System;
using System.Collections.Generic;

namespace QuanLySinhVien.Models;

public partial class LopHoc
{
    public string MaLop { get; set; } = null!;

    public string TenLop { get; set; } = null!;

    public string MaKhoa { get; set; } = null!;

    public string? KhoaHoc { get; set; }

    public virtual ICollection<DangKyHoc> DangKyHocs { get; set; } = new List<DangKyHoc>();

    public virtual Khoa MaKhoaNavigation { get; set; } = null!;

    public virtual ICollection<SinhVien> SinhViens { get; set; } = new List<SinhVien>();
}
