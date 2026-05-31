using System;
using System.Collections.Generic;

namespace QuanLySinhVien.Models;

public partial class TaiKhoan
{
    public int MaTaiKhoan { get; set; }

    public string TenDangNhap { get; set; } = null!;

    public string MatKhauHash { get; set; } = null!;

    public string VaiTro { get; set; } = null!;

    public bool TrangThai { get; set; }

    public int LanDangNhapSai { get; set; }

    public virtual GiangVien? GiangVien { get; set; }

    public virtual SinhVien? SinhVien { get; set; }
}
