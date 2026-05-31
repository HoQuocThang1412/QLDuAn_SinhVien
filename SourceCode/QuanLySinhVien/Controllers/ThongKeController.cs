using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLySinhVien.Controllers
{
    [Authorize(Roles = "Admin,CanBo")]
    public class ThongKeController : Controller
    {
        private readonly QuanLySinhVienContext _db;

        public ThongKeController(QuanLySinhVienContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TongSV = await _db.SinhViens.CountAsync();
            ViewBag.TongGV = await _db.GiangViens.CountAsync();
            ViewBag.TongKhoa = await _db.Khoas.CountAsync();
            ViewBag.TongLhpMo = await _db.LopHocPhans.CountAsync();

            var topSinhVien = await _db.KetQuaHocTaps
                .Include(k => k.MaDangKyNavigation).ThenInclude(d => d.MssvNavigation)
                .Include(k => k.MaDangKyNavigation).ThenInclude(d => d.MaMonNavigation)
                .Include(k => k.MaDangKyNavigation).ThenInclude(d => d.MaLopNavigation)
                .Include(k => k.MaDangKyNavigation).ThenInclude(d => d.MaLhpNavigation)!.ThenInclude(lhp => lhp!.MaGvNavigation)
                .Where(k => k.DiemTongKet >= 8.5m)
                .OrderByDescending(k => k.DiemTongKet)
                .Take(5)
                .ToListAsync();

            ViewBag.TopSinhVien = topSinhVien;

            var thongKeLopHocPhan = await _db.LopHocPhans
                .AsNoTracking()
                .Include(lhp => lhp.MaMonNavigation)
                .Include(lhp => lhp.MaGvNavigation)
                .Include(lhp => lhp.MaLopNavigation)
                .Include(lhp => lhp.MaHocKyNavigation)
                .Select(lhp => new ThongKeLopHocPhanVm
                {
                    MaLhp = lhp.MaLhp,
                    TenMon = lhp.MaMonNavigation != null ? lhp.MaMonNavigation.TenMon : lhp.MaMon,
                    TenGiangVien = lhp.MaGvNavigation != null ? lhp.MaGvNavigation.HoTen : lhp.MaGv,
                    TenLop = lhp.MaLopNavigation != null ? lhp.MaLopNavigation.TenLop : (lhp.MaLop ?? "Không cố định lớp"),
                    TenHocKy = lhp.MaHocKyNavigation != null ? lhp.MaHocKyNavigation.TenHocKy : lhp.MaHocKy,
                    SiSoToiDa = lhp.SiSoToiDa,
                    SoSinhVienDangKy = lhp.DangKyHocs.Count(),
                    SoSinhVienGioi = lhp.DangKyHocs.Count(d =>
                        d.KetQuaHocTap != null &&
                        d.KetQuaHocTap.DiemTongKet >= 8.5m),
                    DiemTrungBinh = lhp.DangKyHocs
                        .Where(d => d.KetQuaHocTap != null && d.KetQuaHocTap.DiemTongKet != null)
                        .Average(d => (decimal?)d.KetQuaHocTap!.DiemTongKet)
                })
                .OrderBy(x => x.TenMon)
                .ThenBy(x => x.TenGiangVien)
                .ThenBy(x => x.MaLhp)
                .ToListAsync();

            ViewBag.ThongKeLopHocPhan = thongKeLopHocPhan;

            ViewBag.ThongKeGiangVien = thongKeLopHocPhan
                .GroupBy(x => x.TenGiangVien)
                .Select(g => new ThongKeGiangVienVm
                {
                    TenGiangVien = g.Key,
                    SoLopHocPhan = g.Count(),
                    SoMonPhuTrach = g.Select(x => x.TenMon).Distinct().Count(),
                    TongSinhVien = g.Sum(x => x.SoSinhVienDangKy),
                    TongSinhVienGioi = g.Sum(x => x.SoSinhVienGioi),
                    DiemTrungBinh = g.Any(x => x.DiemTrungBinh.HasValue)
                        ? g.Where(x => x.DiemTrungBinh.HasValue).Average(x => x.DiemTrungBinh)
                        : null
                })
                .OrderByDescending(x => x.TongSinhVien)
                .ThenBy(x => x.TenGiangVien)
                .ToList();

            var ketQuaGioi = await _db.KetQuaHocTaps
                .AsNoTracking()
                .Include(k => k.MaDangKyNavigation).ThenInclude(d => d.MssvNavigation)
                .Include(k => k.MaDangKyNavigation).ThenInclude(d => d.MaLopNavigation)
                .Include(k => k.MaDangKyNavigation).ThenInclude(d => d.MaMonNavigation)
                .Include(k => k.MaDangKyNavigation).ThenInclude(d => d.MaLhpNavigation)!.ThenInclude(lhp => lhp!.MaGvNavigation)
                .Where(k =>
                    k.DiemTongKet >= 8.5m ||
                    (k.XepLoai != null &&
                        (k.XepLoai.Contains("Giỏi") || k.XepLoai.Contains("Xuất sắc"))))
                .OrderByDescending(k => k.DiemTongKet)
                .ToListAsync();

            ViewBag.SinhVienGioiTheoLopMon = ketQuaGioi
                .GroupBy(k => new
                {
                    MaLop = k.MaDangKyNavigation.MaLop,
                    TenLop = k.MaDangKyNavigation.MaLopNavigation != null
                        ? k.MaDangKyNavigation.MaLopNavigation.TenLop
                        : k.MaDangKyNavigation.MaLop,
                    MaMon = k.MaDangKyNavigation.MaMon,
                    TenMon = k.MaDangKyNavigation.MaMonNavigation != null
                        ? k.MaDangKyNavigation.MaMonNavigation.TenMon
                        : k.MaDangKyNavigation.MaMon,
                    MaLhp = k.MaDangKyNavigation.MaLhp ?? "Chưa gán LHP",
                    TenGiangVien =
                        k.MaDangKyNavigation.MaLhpNavigation != null &&
                        k.MaDangKyNavigation.MaLhpNavigation.MaGvNavigation != null
                            ? k.MaDangKyNavigation.MaLhpNavigation.MaGvNavigation.HoTen
                            : "Chưa gán giảng viên"
                })
                .Select(g => new SinhVienGioiTheoLopMonVm
                {
                    MaLop = g.Key.MaLop,
                    TenLop = g.Key.TenLop,
                    MaMon = g.Key.MaMon,
                    TenMon = g.Key.TenMon,
                    MaLhp = g.Key.MaLhp,
                    TenGiangVien = g.Key.TenGiangVien,
                    SoSinhVienGioi = g.Count(),
                    DiemCaoNhat = g.Max(k => k.DiemTongKet),
                    DanhSachSinhVien = g
                        .OrderByDescending(k => k.DiemTongKet)
                        .Take(5)
                        .Select(k => new SinhVienGioiVm
                        {
                            Mssv = k.MaDangKyNavigation.Mssv,
                            HoTen = k.MaDangKyNavigation.MssvNavigation != null
                                ? k.MaDangKyNavigation.MssvNavigation.HoTen
                                : string.Empty,
                            DiemTongKet = k.DiemTongKet,
                            XepLoai = k.XepLoai
                        })
                        .ToList()
                })
                .OrderBy(x => x.TenLop)
                .ThenBy(x => x.TenMon)
                .ThenBy(x => x.MaLhp)
                .ToList();

            return View();
        }
    }

    public class ThongKeLopHocPhanVm
    {
        public string MaLhp { get; set; } = string.Empty;
        public string TenMon { get; set; } = string.Empty;
        public string TenGiangVien { get; set; } = string.Empty;
        public string TenLop { get; set; } = string.Empty;
        public string TenHocKy { get; set; } = string.Empty;
        public int SiSoToiDa { get; set; }
        public int SoSinhVienDangKy { get; set; }
        public int SoSinhVienGioi { get; set; }
        public decimal? DiemTrungBinh { get; set; }
    }

    public class ThongKeGiangVienVm
    {
        public string TenGiangVien { get; set; } = string.Empty;
        public int SoLopHocPhan { get; set; }
        public int SoMonPhuTrach { get; set; }
        public int TongSinhVien { get; set; }
        public int TongSinhVienGioi { get; set; }
        public decimal? DiemTrungBinh { get; set; }
    }

    public class SinhVienGioiTheoLopMonVm
    {
        public string MaLop { get; set; } = string.Empty;
        public string TenLop { get; set; } = string.Empty;
        public string MaMon { get; set; } = string.Empty;
        public string TenMon { get; set; } = string.Empty;
        public string MaLhp { get; set; } = string.Empty;
        public string TenGiangVien { get; set; } = string.Empty;
        public int SoSinhVienGioi { get; set; }
        public decimal? DiemCaoNhat { get; set; }
        public List<SinhVienGioiVm> DanhSachSinhVien { get; set; } = new();
    }

    public class SinhVienGioiVm
    {
        public string Mssv { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public decimal? DiemTongKet { get; set; }
        public string? XepLoai { get; set; }
    }
}