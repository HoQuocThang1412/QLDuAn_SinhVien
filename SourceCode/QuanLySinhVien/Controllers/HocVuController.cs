using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Models;
using QuanLySinhVien.Models.ViewModels;
using System.Text;

namespace QuanLySinhVien.Controllers;

[Authorize]
public class HocVuController : Controller
{
    private readonly QuanLySinhVienContext _db;

    public HocVuController(QuanLySinhVienContext db)
    {
        _db = db;
    }

    [Authorize(Roles = "Admin,CanBo")]
    public async Task<IActionResult> Index(
        string? maHocKy,
        string? maKhoa,
        string? khoaHoc,
        string? maLop,
        string? mucCanhBao)
    {
        var model = await BuildHocVuModelAsync(maHocKy, maKhoa, khoaHoc, maLop, mucCanhBao);
        return View(model);
    }

    [Authorize(Roles = "Admin,CanBo")]
    public async Task<IActionResult> XuatBaoCao(
        string? maHocKy,
        string? maKhoa,
        string? khoaHoc,
        string? maLop,
        string? mucCanhBao)
    {
        var model = await BuildHocVuModelAsync(maHocKy, maKhoa, khoaHoc, maLop, mucCanhBao);

        var sb = new StringBuilder();

        sb.AppendLine("MSSV,HoTen,Lop,KhoaHoc,Khoa,SoMonDangKy,SoMonDaCoDiem,SoMonQua,SoMonRot,SoMonChuaCoDiem,TongTinChiRot,DiemTrungBinh,TiLeQuaMon,MucCanhBao,LyDoCanhBao,MonCanHocLai");

        foreach (var sv in model.DanhSachCanhBao)
        {
            string monHocLai = string.Join(" | ", sv.MonCanHocLai.Select(m =>
                $"{m.MaMon}-{m.TenMon}"));

            sb.AppendLine(string.Join(",",
                Csv(sv.Mssv),
                Csv(sv.HoTen),
                Csv(sv.TenLop),
                Csv(sv.KhoaHoc),
                Csv(sv.TenKhoa),
                sv.SoMonDangKy,
                sv.SoMonDaCoDiem,
                sv.SoMonQua,
                sv.SoMonRot,
                sv.SoMonChuaCoDiem,
                sv.TongTinChiRot,
                sv.DiemTrungBinh?.ToString("0.00") ?? "",
                sv.TiLeQuaMon.ToString("0.##") + "%",
                Csv(sv.MucCanhBao),
                Csv(sv.LyDoCanhBao),
                Csv(monHocLai)
            ));
        }

        byte[] fileBytes = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();

        string hocKy = string.IsNullOrWhiteSpace(model.MaHocKy)
            ? "TatCaHocKy"
            : model.MaHocKy;

        string fileName = $"BaoCao_CanhBaoHocVu_{hocKy}_{DateTime.Now:ddMMyyyy}.csv";

        return File(fileBytes, "text/csv", fileName);
    }

    [Authorize(Roles = "SinhVien")]
    public async Task<IActionResult> Hoclai(string? maHocKy)
    {
        string? mssv = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(mssv))
        {
            return Unauthorized();
        }

        var sinhVien = await _db.SinhViens
            .Include(s => s.MaLopNavigation)
            .FirstOrDefaultAsync(s => s.Mssv == mssv);

        if (sinhVien == null)
        {
            TempData["Error"] = "Không tìm thấy thông tin sinh viên.";
            return RedirectToAction("Index", "Home");
        }

        var dsHocKyDangMo = await _db.HocKies
            .Where(h => h.TrangThai == "Đang diễn ra" || h.TrangThai == "Sắp diễn ra")
            .OrderByDescending(h => h.NgayBatDau)
            .ToListAsync();

        if (string.IsNullOrWhiteSpace(maHocKy))
        {
            maHocKy = dsHocKyDangMo.FirstOrDefault()?.MaHocKy;
        }

        var model = new HocLaiCuaToiViewModel
        {
            Mssv = sinhVien.Mssv,
            HoTen = sinhVien.HoTen,
            Lop = sinhVien.MaLopNavigation?.TenLop ?? sinhVien.MaLop,
            KhoaHoc = sinhVien.MaLopNavigation?.KhoaHoc ?? "",
            MaHocKyDangChon = maHocKy,
            DanhSachHocKyDangMo = dsHocKyDangMo.Select(h => new SelectListItem
            {
                Value = h.MaHocKy,
                Text = h.TenHocKy ?? h.MaHocKy,
                Selected = h.MaHocKy == maHocKy
            }).ToList()
        };

        var dsMonRot = await _db.DangKyHocs
            .Include(d => d.MaMonNavigation)
            .Include(d => d.MaHocKyNavigation)
            .Include(d => d.KetQuaHocTap)
            .Where(d => d.Mssv == mssv)
            .Where(d => d.KetQuaHocTap != null && d.KetQuaHocTap.QuaMon == false)
            .AsNoTracking()
            .ToListAsync();

        var maMonRot = dsMonRot
            .Select(d => d.MaMon)
            .Distinct()
            .ToList();

        var dsLopDaDangKy = await _db.DangKyHocs
            .Where(d => d.Mssv == mssv)
            .Select(d => d.MaLhp)
            .ToListAsync();

        var lopHocPhanDangMoQuery = _db.LopHocPhans
            .Include(l => l.MaMonNavigation)
            .Include(l => l.MaHocKyNavigation)
            .Include(l => l.MaGvNavigation)
            .Include(l => l.DangKyHocs)
            .Where(l => maMonRot.Contains(l.MaMon))
            .Where(l => l.MaHocKyNavigation != null &&
                (l.MaHocKyNavigation.TrangThai == "Đang diễn ra"
                || l.MaHocKyNavigation.TrangThai == "Sắp diễn ra"))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(maHocKy))
        {
            lopHocPhanDangMoQuery = lopHocPhanDangMoQuery
                .Where(l => l.MaHocKy == maHocKy);
        }

        var lopHocPhanDangMo = await lopHocPhanDangMoQuery
            .OrderByDescending(l => l.MaHocKyNavigation!.NgayBatDau)
            .ThenBy(l => l.MaMon)
            .ThenBy(l => l.MaLhp)
            .ToListAsync();

        model.DanhSachMonRot = dsMonRot
            .GroupBy(d => d.MaMon)
            .Select(g =>
            {
                var monRotGanNhat = g
                    .OrderByDescending(x => x.MaHocKyNavigation != null
                        ? x.MaHocKyNavigation.NgayBatDau
                        : DateOnly.MinValue)
                    .First();

                var dsLopGoiY = lopHocPhanDangMo
                    .Where(l => l.MaMon == monRotGanNhat.MaMon)
                    .Where(l => !dsLopDaDangKy.Contains(l.MaLhp))
                    .Select(l => new LopHocPhanDangMoViewModel
                    {
                        MaLhp = l.MaLhp,
                        MaHocKy = l.MaHocKy,
                        TenHocKy = l.MaHocKyNavigation?.TenHocKy ?? l.MaHocKy,
                        TenGiangVien = l.MaGvNavigation?.HoTen ?? l.MaGv,
                        LichHoc = TaoLichHoc(l),
                        PhongHoc = l.PhongHoc ?? "",
                        SiSoToiDa = l.SiSoToiDa,
                        SiSoDaDangKy = l.DangKyHocs?.Count ?? 0
                    })
                    .ToList();

                return new MonRotCuaToiViewModel
                {
                    MaMon = monRotGanNhat.MaMon,
                    TenMon = monRotGanNhat.MaMonNavigation?.TenMon ?? monRotGanNhat.MaMon,
                    SoTinChi = monRotGanNhat.MaMonNavigation?.SoTinChi ?? 0,
                    DiemTongKet = monRotGanNhat.KetQuaHocTap?.DiemTongKet,
                    XepLoai = monRotGanNhat.KetQuaHocTap?.XepLoai ?? "",
                    HocKyRot = monRotGanNhat.MaHocKyNavigation?.TenHocKy ?? monRotGanNhat.MaHocKy,
                    LopHocPhanDangMo = dsLopGoiY
                };
            })
            .OrderBy(m => m.MaMon)
            .ToList();

        return View(model);
    }

    [Authorize(Roles = "Admin,CanBo")]
    public async Task<IActionResult> ChiTietSinhVien(string mssv, string? maHocKy)
    {
        if (string.IsNullOrWhiteSpace(mssv))
        {
            return RedirectToAction(nameof(Index));
        }

        var sinhVien = await _db.SinhViens
            .Include(s => s.MaLopNavigation)
                .ThenInclude(l => l.MaKhoaNavigation)
            .FirstOrDefaultAsync(s => s.Mssv == mssv);

        if (sinhVien == null)
        {
            TempData["Error"] = "Không tìm thấy sinh viên.";
            return RedirectToAction(nameof(Index));
        }

        string tenHocKy = "Tất cả học kỳ";

        if (!string.IsNullOrWhiteSpace(maHocKy))
        {
            tenHocKy = await _db.HocKies
                .Where(h => h.MaHocKy == maHocKy)
                .Select(h => h.TenHocKy ?? h.MaHocKy)
                .FirstOrDefaultAsync() ?? maHocKy;
        }

        var dangKyQuery = _db.DangKyHocs
            .Include(d => d.MaMonNavigation)
            .Include(d => d.MaHocKyNavigation)
            .Include(d => d.MaLhpNavigation)
            .Include(d => d.KetQuaHocTap)
            .Where(d => d.Mssv == mssv)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(maHocKy))
        {
            dangKyQuery = dangKyQuery.Where(d => d.MaHocKy == maHocKy);
        }

        var dangKys = await dangKyQuery
            .AsNoTracking()
            .ToListAsync();

        var dsDaCoDiem = dangKys
            .Where(d => d.KetQuaHocTap?.DiemTongKet != null)
            .ToList();

        var dsQua = dangKys
            .Where(d => d.KetQuaHocTap?.QuaMon == true)
            .ToList();

        var dsRot = dangKys
            .Where(d => d.KetQuaHocTap?.QuaMon == false)
            .ToList();

        int tongTinChiCoDiem = dsDaCoDiem
            .Sum(d => d.MaMonNavigation?.SoTinChi ?? 0);

        decimal? diemTrungBinh = null;

        if (tongTinChiCoDiem > 0)
        {
            decimal tongDiemHeSo = dsDaCoDiem.Sum(d =>
                (d.KetQuaHocTap?.DiemTongKet ?? 0)
                * (d.MaMonNavigation?.SoTinChi ?? 0));

            diemTrungBinh = Math.Round(
                tongDiemHeSo / tongTinChiCoDiem,
                2,
                MidpointRounding.AwayFromZero);
        }

        decimal tiLeQuaMon = dsDaCoDiem.Count == 0
            ? 0
            : Math.Round(
                dsQua.Count * 100m / dsDaCoDiem.Count,
                2,
                MidpointRounding.AwayFromZero);

        var phanLoai = PhanLoaiCanhBao(
            dangKys.Count,
            dsDaCoDiem.Count,
            dsRot.Count,
            diemTrungBinh);

        var maMonRot = dsRot
            .Select(d => d.MaMon)
            .Distinct()
            .ToList();

        var lopHocPhanGoiY = await _db.LopHocPhans
            .Include(l => l.MaHocKyNavigation)
            .Include(l => l.MaMonNavigation)
            .Include(l => l.DangKyHocs)
            .Where(l => maMonRot.Contains(l.MaMon))
            .Where(l => l.MaHocKyNavigation != null &&
                (l.MaHocKyNavigation.TrangThai == "Đang diễn ra"
                || l.MaHocKyNavigation.TrangThai == "Sắp diễn ra"))
            .OrderByDescending(l => l.MaHocKyNavigation!.NgayBatDau)
            .ThenBy(l => l.MaMon)
            .ThenBy(l => l.MaLhp)
            .ToListAsync();

        var monCanHocLai = dsRot
            .GroupBy(d => d.MaMon)
            .Select(g =>
            {
                var monRotGanNhat = g
                    .OrderByDescending(x => x.MaHocKyNavigation != null
                        ? x.MaHocKyNavigation.NgayBatDau
                        : DateOnly.MinValue)
                    .First();

                var goiY = lopHocPhanGoiY
                    .Where(l => l.MaMon == monRotGanNhat.MaMon)
                    .Where(l => !dangKys.Any(d => d.MaLhp == l.MaLhp))
                    .Take(3)
                    .Select(l => new LopHocPhanGoiYViewModel
                    {
                        MaLhp = l.MaLhp,
                        TenHocKy = l.MaHocKyNavigation?.TenHocKy ?? l.MaHocKy,
                        LichHoc = TaoLichHoc(l),
                        PhongHoc = l.PhongHoc ?? "",
                        SiSoToiDa = l.SiSoToiDa,
                        SiSoDaDangKy = l.DangKyHocs?.Count ?? 0
                    })
                    .ToList();

                return new MonHocLaiViewModel
                {
                    Mssv = sinhVien.Mssv,
                    HoTen = sinhVien.HoTen,
                    MaMon = monRotGanNhat.MaMon,
                    TenMon = monRotGanNhat.MaMonNavigation?.TenMon ?? monRotGanNhat.MaMon,
                    SoTinChi = monRotGanNhat.MaMonNavigation?.SoTinChi ?? 0,
                    DiemTongKet = monRotGanNhat.KetQuaHocTap?.DiemTongKet,
                    XepLoai = monRotGanNhat.KetQuaHocTap?.XepLoai ?? "",
                    LopHocPhanGoiY = goiY
                };
            })
            .ToList();

        var model = new ChiTietHocVuSinhVienViewModel
        {
            Mssv = sinhVien.Mssv,
            HoTen = sinhVien.HoTen,
            Lop = sinhVien.MaLopNavigation?.TenLop ?? sinhVien.MaLop,
            KhoaHoc = sinhVien.MaLopNavigation?.KhoaHoc ?? "",
            Khoa = sinhVien.MaLopNavigation?.MaKhoaNavigation?.TenKhoa
                ?? sinhVien.MaLopNavigation?.MaKhoa
                ?? "",

            MaHocKy = maHocKy,
            TenHocKy = tenHocKy,

            SoMonDangKy = dangKys.Count,
            SoMonDaCoDiem = dsDaCoDiem.Count,
            SoMonQua = dsQua.Count,
            SoMonRot = dsRot.Count,
            SoMonChuaCoDiem = dangKys.Count - dsDaCoDiem.Count,
            TongTinChiRot = dsRot.Sum(d => d.MaMonNavigation?.SoTinChi ?? 0),

            DiemTrungBinh = diemTrungBinh,
            TiLeQuaMon = tiLeQuaMon,

            MucCanhBao = phanLoai.Muc,
            CssClass = phanLoai.Css,
            LyDoCanhBao = phanLoai.LyDo,

            DanhSachMonHoc = dangKys
                .OrderByDescending(d => d.MaHocKyNavigation?.NgayBatDau)
                .ThenBy(d => d.MaMon)
                .Select(d => new MonHocChiTietHocVuViewModel
                {
                    MaMon = d.MaMon,
                    TenMon = d.MaMonNavigation?.TenMon ?? d.MaMon,
                    MaLhp = d.MaLhp,
                    TenHocKy = d.MaHocKyNavigation?.TenHocKy ?? d.MaHocKy,
                    SoTinChi = d.MaMonNavigation?.SoTinChi ?? 0,
                    DiemQt = d.KetQuaHocTap?.DiemQt,
                    DiemThi = d.KetQuaHocTap?.DiemThi,
                    DiemTongKet = d.KetQuaHocTap?.DiemTongKet,
                    XepLoai = d.KetQuaHocTap?.XepLoai ?? "",
                    QuaMon = d.KetQuaHocTap?.QuaMon
                })
                .ToList(),

            MonCanHocLai = monCanHocLai
        };

        return View(model);
    }

    private async Task<HocVuIndexViewModel> BuildHocVuModelAsync(
        string? maHocKy,
        string? maKhoa,
        string? khoaHoc,
        string? maLop,
        string? mucCanhBao)
    {
        var model = new HocVuIndexViewModel
        {
            MaHocKy = maHocKy,
            MaKhoa = maKhoa,
            KhoaHoc = khoaHoc,
            MaLop = maLop,
            MucCanhBao = mucCanhBao
        };

        await NapDanhSachLocAsync(model);

        var sinhVienQuery = _db.SinhViens
            .Include(s => s.MaLopNavigation)
                .ThenInclude(l => l.MaKhoaNavigation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(model.MaKhoa))
        {
            sinhVienQuery = sinhVienQuery
                .Where(s => s.MaLopNavigation.MaKhoa == model.MaKhoa);
        }

        if (!string.IsNullOrWhiteSpace(model.KhoaHoc))
        {
            sinhVienQuery = sinhVienQuery
                .Where(s => s.MaLopNavigation.KhoaHoc == model.KhoaHoc);
        }

        if (!string.IsNullOrWhiteSpace(model.MaLop))
        {
            sinhVienQuery = sinhVienQuery
                .Where(s => s.MaLop == model.MaLop);
        }

        var sinhViens = await sinhVienQuery
            .OrderBy(s => s.MaLopNavigation.MaKhoa)
            .ThenBy(s => s.MaLop)
            .ThenBy(s => s.Mssv)
            .ToListAsync();

        var mssvList = sinhViens
            .Select(s => s.Mssv)
            .ToList();

        var dangKyQuery = _db.DangKyHocs
            .Include(d => d.MaMonNavigation)
            .Include(d => d.MaHocKyNavigation)
            .Include(d => d.KetQuaHocTap)
            .Where(d => mssvList.Contains(d.Mssv))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(model.MaHocKy))
        {
            dangKyQuery = dangKyQuery
                .Where(d => d.MaHocKy == model.MaHocKy);
        }

        var dangKys = await dangKyQuery
            .AsNoTracking()
            .ToListAsync();

        var maMonRot = dangKys
            .Where(d => d.KetQuaHocTap != null && d.KetQuaHocTap.QuaMon == false)
            .Select(d => d.MaMon)
            .Distinct()
            .ToList();

        var lopHocPhanGoiY = await _db.LopHocPhans
            .Include(l => l.MaHocKyNavigation)
            .Include(l => l.MaMonNavigation)
            .Include(l => l.DangKyHocs)
            .Where(l => maMonRot.Contains(l.MaMon))
            .ToListAsync();

        lopHocPhanGoiY = lopHocPhanGoiY
            .OrderByDescending(l => l.MaHocKyNavigation?.TrangThai == "Đang diễn ra")
            .ThenByDescending(l => l.MaHocKyNavigation?.TrangThai == "Sắp diễn ra")
            .ThenByDescending(l => l.MaHocKyNavigation?.NgayBatDau)
            .ThenBy(l => l.MaMon)
            .ThenBy(l => l.MaLhp)
            .ToList();

        var danhSachCanhBao = new List<SinhVienCanhBaoViewModel>();

        foreach (var sv in sinhViens)
        {
            var dsDk = dangKys
                .Where(d => d.Mssv == sv.Mssv)
                .ToList();

            var dsDaCoDiem = dsDk
                .Where(d => d.KetQuaHocTap?.DiemTongKet != null)
                .ToList();

            var dsQua = dsDk
                .Where(d => d.KetQuaHocTap?.QuaMon == true)
                .ToList();

            var dsRot = dsDk
                .Where(d => d.KetQuaHocTap?.QuaMon == false)
                .ToList();

            int tongTinChiCoDiem = dsDaCoDiem
                .Sum(d => d.MaMonNavigation?.SoTinChi ?? 0);

            decimal? diemTrungBinh = null;

            if (tongTinChiCoDiem > 0)
            {
                decimal tongDiemHeSo = dsDaCoDiem.Sum(d =>
                    (d.KetQuaHocTap?.DiemTongKet ?? 0)
                    * (d.MaMonNavigation?.SoTinChi ?? 0));

                diemTrungBinh = Math.Round(
                    tongDiemHeSo / tongTinChiCoDiem,
                    2,
                    MidpointRounding.AwayFromZero);
            }

            decimal tiLeQuaMon = dsDaCoDiem.Count == 0
                ? 0
                : Math.Round(
                    dsQua.Count * 100m / dsDaCoDiem.Count,
                    2,
                    MidpointRounding.AwayFromZero);

            var monCanHocLai = dsRot
                .GroupBy(d => d.MaMon)
                .Select(g =>
                {
                    var monRotGanNhat = g
                        .OrderByDescending(x => x.MaHocKy)
                        .First();

                    var goiY = lopHocPhanGoiY
                        .Where(l => l.MaMon == monRotGanNhat.MaMon)
                        .Where(l => !dsDk.Any(d => d.MaLhp == l.MaLhp))
                        .Take(3)
                        .Select(l => new LopHocPhanGoiYViewModel
                        {
                            MaLhp = l.MaLhp,
                            TenHocKy = l.MaHocKyNavigation?.TenHocKy ?? l.MaHocKy,
                            LichHoc = TaoLichHoc(l),
                            PhongHoc = l.PhongHoc ?? "",
                            SiSoToiDa = l.SiSoToiDa,
                            SiSoDaDangKy = l.DangKyHocs?.Count ?? 0
                        })
                        .ToList();

                    return new MonHocLaiViewModel
                    {
                        Mssv = sv.Mssv,
                        HoTen = sv.HoTen,
                        MaMon = monRotGanNhat.MaMon,
                        TenMon = monRotGanNhat.MaMonNavigation?.TenMon ?? monRotGanNhat.MaMon,
                        SoTinChi = monRotGanNhat.MaMonNavigation?.SoTinChi ?? 0,
                        DiemTongKet = monRotGanNhat.KetQuaHocTap?.DiemTongKet,
                        XepLoai = monRotGanNhat.KetQuaHocTap?.XepLoai ?? "",
                        LopHocPhanGoiY = goiY
                    };
                })
                .ToList();

            var phanLoai = PhanLoaiCanhBao(
                dsDk.Count,
                dsDaCoDiem.Count,
                dsRot.Count,
                diemTrungBinh);

            danhSachCanhBao.Add(new SinhVienCanhBaoViewModel
            {
                Mssv = sv.Mssv,
                HoTen = sv.HoTen,

                MaLop = sv.MaLop,
                TenLop = sv.MaLopNavigation?.TenLop ?? sv.MaLop,
                KhoaHoc = sv.MaLopNavigation?.KhoaHoc ?? "",

                MaKhoa = sv.MaLopNavigation?.MaKhoa ?? "",
                TenKhoa = sv.MaLopNavigation?.MaKhoaNavigation?.TenKhoa
                    ?? sv.MaLopNavigation?.MaKhoa
                    ?? "",

                SoMonDangKy = dsDk.Count,
                SoMonDaCoDiem = dsDaCoDiem.Count,
                SoMonQua = dsQua.Count,
                SoMonRot = dsRot.Count,
                SoMonChuaCoDiem = dsDk.Count - dsDaCoDiem.Count,
                TongTinChiRot = dsRot.Sum(d => d.MaMonNavigation?.SoTinChi ?? 0),

                DiemTrungBinh = diemTrungBinh,
                TiLeQuaMon = tiLeQuaMon,

                MucCanhBao = phanLoai.Muc,
                CssClass = phanLoai.Css,
                LyDoCanhBao = phanLoai.LyDo,

                MonCanHocLai = monCanHocLai
            });
        }

        if (!string.IsNullOrWhiteSpace(model.MucCanhBao))
        {
            danhSachCanhBao = danhSachCanhBao
                .Where(x => x.MucCanhBao == model.MucCanhBao)
                .ToList();
        }

        model.DanhSachCanhBao = danhSachCanhBao
            .OrderByDescending(x => x.SoMonRot)
            .ThenBy(x => x.DiemTrungBinh ?? 999)
            .ThenBy(x => x.Mssv)
            .ToList();

        model.DanhSachMonHocLai = model.DanhSachCanhBao
            .SelectMany(x => x.MonCanHocLai)
            .OrderBy(x => x.Mssv)
            .ThenBy(x => x.MaMon)
            .ToList();

        model.TongSinhVien = model.DanhSachCanhBao.Count;
        model.SoBinhThuong = model.DanhSachCanhBao.Count(x => x.MucCanhBao == "Bình thường");
        model.SoCanTheoDoi = model.DanhSachCanhBao.Count(x => x.MucCanhBao == "Cần theo dõi");
        model.SoCanhBaoHocVu = model.DanhSachCanhBao.Count(x => x.MucCanhBao == "Cảnh báo học vụ");
        model.SoNguyCoHocVu = model.DanhSachCanhBao.Count(x => x.MucCanhBao == "Nguy cơ học vụ");
        model.SoChuaDuDuLieu = model.DanhSachCanhBao.Count(x => x.MucCanhBao == "Chưa đủ dữ liệu");

        int soCanhBao = model.SoCanTheoDoi
            + model.SoCanhBaoHocVu
            + model.SoNguyCoHocVu;

        model.TiLeSinhVienCanhBao = model.TongSinhVien == 0
            ? 0
            : Math.Round(
                soCanhBao * 100m / model.TongSinhVien,
                2,
                MidpointRounding.AwayFromZero);

        return model;
    }

    private async Task NapDanhSachLocAsync(HocVuIndexViewModel model)
    {
        model.DanhSachHocKy = await _db.HocKies
            .OrderByDescending(h => h.NgayBatDau)
            .Select(h => new SelectListItem
            {
                Value = h.MaHocKy,
                Text = h.TenHocKy ?? h.MaHocKy,
                Selected = h.MaHocKy == model.MaHocKy
            })
            .ToListAsync();

        model.DanhSachKhoa = await _db.Khoas
            .OrderBy(k => k.TenKhoa)
            .Select(k => new SelectListItem
            {
                Value = k.MaKhoa,
                Text = k.TenKhoa,
                Selected = k.MaKhoa == model.MaKhoa
            })
            .ToListAsync();

        var khoaHocQuery = _db.LopHocs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(model.MaKhoa))
        {
            khoaHocQuery = khoaHocQuery.Where(l => l.MaKhoa == model.MaKhoa);
        }

        model.DanhSachKhoaHoc = await khoaHocQuery
            .Where(l => l.KhoaHoc != null && l.KhoaHoc != "")
            .Select(l => l.KhoaHoc!)
            .Distinct()
            .OrderBy(kh => kh)
            .Select(kh => new SelectListItem
            {
                Value = kh,
                Text = kh,
                Selected = kh == model.KhoaHoc
            })
            .ToListAsync();

        var lopQuery = _db.LopHocs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(model.MaKhoa))
        {
            lopQuery = lopQuery.Where(l => l.MaKhoa == model.MaKhoa);
        }

        if (!string.IsNullOrWhiteSpace(model.KhoaHoc))
        {
            lopQuery = lopQuery.Where(l => l.KhoaHoc == model.KhoaHoc);
        }

        if (!string.IsNullOrWhiteSpace(model.MaLop))
        {
            bool lopHopLe = await lopQuery.AnyAsync(l => l.MaLop == model.MaLop);

            if (!lopHopLe)
            {
                model.MaLop = null;
            }
        }

        model.DanhSachLop = await lopQuery
            .OrderBy(l => l.TenLop)
            .Select(l => new SelectListItem
            {
                Value = l.MaLop,
                Text = string.IsNullOrEmpty(l.KhoaHoc)
                    ? l.TenLop
                    : l.TenLop + " - " + l.KhoaHoc,
                Selected = l.MaLop == model.MaLop
            })
            .ToListAsync();

        model.DanhSachMucCanhBao = new List<SelectListItem>
        {
            new() { Value = "Bình thường", Text = "Bình thường", Selected = model.MucCanhBao == "Bình thường" },
            new() { Value = "Cần theo dõi", Text = "Cần theo dõi", Selected = model.MucCanhBao == "Cần theo dõi" },
            new() { Value = "Cảnh báo học vụ", Text = "Cảnh báo học vụ", Selected = model.MucCanhBao == "Cảnh báo học vụ" },
            new() { Value = "Nguy cơ học vụ", Text = "Nguy cơ học vụ", Selected = model.MucCanhBao == "Nguy cơ học vụ" },
            new() { Value = "Chưa đủ dữ liệu", Text = "Chưa đủ dữ liệu", Selected = model.MucCanhBao == "Chưa đủ dữ liệu" }
        };
    }

    private static (string Muc, string Css, string LyDo) PhanLoaiCanhBao(
        int soMonDangKy,
        int soMonDaCoDiem,
        int soMonRot,
        decimal? diemTrungBinh)
    {
        if (soMonDangKy == 0 || soMonDaCoDiem == 0)
        {
            return ("Chưa đủ dữ liệu", "secondary", "Sinh viên chưa có dữ liệu điểm để đánh giá.");
        }

        if (soMonRot >= 3 || diemTrungBinh < 4)
        {
            return ("Nguy cơ học vụ", "danger", "Rớt từ 3 môn trở lên hoặc điểm trung bình dưới 4.0.");
        }

        if (soMonRot == 2 || diemTrungBinh < 5)
        {
            return ("Cảnh báo học vụ", "warning", "Rớt 2 môn hoặc điểm trung bình dưới 5.0.");
        }

        if (soMonRot == 1 || diemTrungBinh < 6.5m)
        {
            return ("Cần theo dõi", "info", "Rớt 1 môn hoặc điểm trung bình dưới 6.5.");
        }

        return ("Bình thường", "success", "Kết quả học tập đang ở mức ổn định.");
    }

    private static string TaoLichHoc(LopHocPhan lhp)
    {
        string thu = lhp.Thu == 8
            ? "Chủ nhật"
            : $"Thứ {lhp.Thu}";

        int tietKetThuc = lhp.TietBatDau + lhp.SoTiet - 1;

        return $"{thu}, tiết {lhp.TietBatDau}-{tietKetThuc}";
    }

    private static string Csv(string? value)
    {
        value ??= "";
        value = value.Replace("\"", "\"\"");
        return $"\"{value}\"";
    }
}