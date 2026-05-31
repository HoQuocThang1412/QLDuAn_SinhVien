using System;
using System.Collections.Generic;

namespace QuanLySinhVien.Models;

public partial class KetQuaHocTap
{
    public int MaKetQua { get; set; }

    public int MaDangKy { get; set; }

    public decimal? DiemQt { get; set; }

    public decimal? DiemThi { get; set; }

    public decimal? DiemTongKet { get; set; }

    public string? XepLoai { get; set; }

    public bool? QuaMon { get; set; }

    public virtual DangKyHoc MaDangKyNavigation { get; set; } = null!;
}