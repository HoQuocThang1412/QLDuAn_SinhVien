using System;
using System.Collections.Generic;

namespace QuanLySinhVien.Models;

public partial class GiangVien
{
    public string MaGv { get; set; } = null!;

    public int? MaTaiKhoan { get; set; }

    public string MaKhoa { get; set; } = null!;

    public string HoTen { get; set; } = null!;

    public string? Email { get; set; }

    public string? HocVi { get; set; }

    public virtual Khoa MaKhoaNavigation { get; set; } = null!;

    public virtual TaiKhoan? MaTaiKhoanNavigation { get; set; }
}
