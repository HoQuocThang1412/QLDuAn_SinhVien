using System;
using System.Collections.Generic;

namespace QuanLySinhVien.Models;

public partial class MonHoc
{
    public string MaMon { get; set; } = null!;

    public string TenMon { get; set; } = null!;

    public string MaKhoa { get; set; } = null!;

    public int SoTinChi { get; set; }

    public decimal HeSoQt { get; set; }

    public decimal HeSoCk { get; set; }

    public bool IsDieuKien { get; set; }

    public string? MaMonTienQuyet { get; set; }

    public virtual ICollection<DangKyHoc> DangKyHocs { get; set; } = new List<DangKyHoc>();

    public virtual Khoa MaKhoaNavigation { get; set; } = null!;
}