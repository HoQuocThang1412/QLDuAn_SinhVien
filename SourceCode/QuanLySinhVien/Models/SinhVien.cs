using System;
using System.Collections.Generic;

namespace QuanLySinhVien.Models;

public partial class SinhVien
{
    public string Mssv { get; set; } = null!;

    public int? MaTaiKhoan { get; set; }

    public string MaLop { get; set; } = null!;

    public string HoTen { get; set; } = null!;

    public DateOnly NgaySinh { get; set; }

    public string? GioiTinh { get; set; }

    public string? DiaChi { get; set; }

    public string? SoDienThoai { get; set; }

    public string? Email { get; set; }

    public string TrangThai { get; set; } = null!;

    public virtual ICollection<DangKyHoc> DangKyHocs { get; set; } = new List<DangKyHoc>();

    public virtual LopHoc MaLopNavigation { get; set; } = null!;

    public virtual TaiKhoan? MaTaiKhoanNavigation { get; set; }
}
