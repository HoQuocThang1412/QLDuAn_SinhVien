using Microsoft.AspNetCore.Mvc.Rendering;

namespace QuanLySinhVien.Models.ViewModels;

public class HocVuIndexViewModel
{
    public string? MaHocKy { get; set; }
    public string? MaKhoa { get; set; }
    public string? KhoaHoc { get; set; }
    public string? MaLop { get; set; }
    public string? MucCanhBao { get; set; }

    public List<SelectListItem> DanhSachHocKy { get; set; } = new();
    public List<SelectListItem> DanhSachKhoa { get; set; } = new();
    public List<SelectListItem> DanhSachKhoaHoc { get; set; } = new();
    public List<SelectListItem> DanhSachLop { get; set; } = new();
    public List<SelectListItem> DanhSachMucCanhBao { get; set; } = new();

    public int TongSinhVien { get; set; }
    public int SoBinhThuong { get; set; }
    public int SoCanTheoDoi { get; set; }
    public int SoCanhBaoHocVu { get; set; }
    public int SoNguyCoHocVu { get; set; }
    public int SoChuaDuDuLieu { get; set; }

    public decimal TiLeSinhVienCanhBao { get; set; }

    public List<SinhVienCanhBaoViewModel> DanhSachCanhBao { get; set; } = new();
    public List<MonHocLaiViewModel> DanhSachMonHocLai { get; set; } = new();
}

public class SinhVienCanhBaoViewModel
{
    public string Mssv { get; set; } = "";
    public string HoTen { get; set; } = "";

    public string MaLop { get; set; } = "";
    public string TenLop { get; set; } = "";
    public string KhoaHoc { get; set; } = "";

    public string MaKhoa { get; set; } = "";
    public string TenKhoa { get; set; } = "";

    public int SoMonDangKy { get; set; }
    public int SoMonDaCoDiem { get; set; }
    public int SoMonQua { get; set; }
    public int SoMonRot { get; set; }
    public int SoMonChuaCoDiem { get; set; }
    public int TongTinChiRot { get; set; }

    public decimal? DiemTrungBinh { get; set; }
    public decimal TiLeQuaMon { get; set; }

    public string MucCanhBao { get; set; } = "";
    public string CssClass { get; set; } = "";
    public string LyDoCanhBao { get; set; } = "";

    public List<MonHocLaiViewModel> MonCanHocLai { get; set; } = new();
}

public class MonHocLaiViewModel
{
    public string Mssv { get; set; } = "";
    public string HoTen { get; set; } = "";

    public string MaMon { get; set; } = "";
    public string TenMon { get; set; } = "";

    public int SoTinChi { get; set; }
    public decimal? DiemTongKet { get; set; }
    public string XepLoai { get; set; } = "";

    public List<LopHocPhanGoiYViewModel> LopHocPhanGoiY { get; set; } = new();
}

public class LopHocPhanGoiYViewModel
{
    public string MaLhp { get; set; } = "";
    public string TenHocKy { get; set; } = "";
    public string LichHoc { get; set; } = "";
    public string PhongHoc { get; set; } = "";

    public int SiSoToiDa { get; set; }
    public int SiSoDaDangKy { get; set; }

    public int SoChoConLai => Math.Max(SiSoToiDa - SiSoDaDangKy, 0);
}

public class HocLaiCuaToiViewModel
{
    public string Mssv { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string Lop { get; set; } = "";
    public string KhoaHoc { get; set; } = "";

    public string? MaHocKyDangChon { get; set; }

    public List<SelectListItem> DanhSachHocKyDangMo { get; set; } = new();
    public List<MonRotCuaToiViewModel> DanhSachMonRot { get; set; } = new();

    public int TongMonRot => DanhSachMonRot.Count;
    public int TongLopCoTheDangKy => DanhSachMonRot.Sum(m => m.LopHocPhanDangMo.Count);
}

public class MonRotCuaToiViewModel
{
    public string MaMon { get; set; } = "";
    public string TenMon { get; set; } = "";
    public int SoTinChi { get; set; }

    public decimal? DiemTongKet { get; set; }
    public string XepLoai { get; set; } = "";
    public string HocKyRot { get; set; } = "";

    public List<LopHocPhanDangMoViewModel> LopHocPhanDangMo { get; set; } = new();
}

public class LopHocPhanDangMoViewModel
{
    public string MaLhp { get; set; } = "";
    public string MaHocKy { get; set; } = "";
    public string TenHocKy { get; set; } = "";
    public string TenGiangVien { get; set; } = "";
    public string LichHoc { get; set; } = "";
    public string PhongHoc { get; set; } = "";

    public int SiSoToiDa { get; set; }
    public int SiSoDaDangKy { get; set; }

    public int SoChoConLai => Math.Max(SiSoToiDa - SiSoDaDangKy, 0);
    public bool ConCho => SoChoConLai > 0;
}

public class ChiTietHocVuSinhVienViewModel
{
    public string Mssv { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string Lop { get; set; } = "";
    public string KhoaHoc { get; set; } = "";
    public string Khoa { get; set; } = "";

    public string? MaHocKy { get; set; }
    public string TenHocKy { get; set; } = "Tất cả học kỳ";

    public int SoMonDangKy { get; set; }
    public int SoMonDaCoDiem { get; set; }
    public int SoMonQua { get; set; }
    public int SoMonRot { get; set; }
    public int SoMonChuaCoDiem { get; set; }
    public int TongTinChiRot { get; set; }

    public decimal? DiemTrungBinh { get; set; }
    public decimal TiLeQuaMon { get; set; }

    public string MucCanhBao { get; set; } = "";
    public string CssClass { get; set; } = "";
    public string LyDoCanhBao { get; set; } = "";

    public List<MonHocChiTietHocVuViewModel> DanhSachMonHoc { get; set; } = new();
    public List<MonHocLaiViewModel> MonCanHocLai { get; set; } = new();
}

public class MonHocChiTietHocVuViewModel
{
    public string MaMon { get; set; } = "";
    public string TenMon { get; set; } = "";
    public string MaLhp { get; set; } = "";
    public string TenHocKy { get; set; } = "";

    public int SoTinChi { get; set; }

    public decimal? DiemQt { get; set; }
    public decimal? DiemThi { get; set; }
    public decimal? DiemTongKet { get; set; }

    public string XepLoai { get; set; } = "";
    public bool? QuaMon { get; set; }

    public string TrangThaiText
    {
        get
        {
            if (DiemTongKet == null) return "Chưa có điểm";
            return QuaMon == true ? "Đạt" : "Không đạt";
        }
    }

    public string BadgeClass
    {
        get
        {
            if (DiemTongKet == null) return "secondary";
            return QuaMon == true ? "success" : "danger";
        }
    }
}