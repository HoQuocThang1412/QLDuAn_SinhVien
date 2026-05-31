// File: SourceCode/QuanLySinhVien/Controllers/LopHocPhanController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuanLySinhVien.Controllers
{
    [Authorize(Roles = "Admin,CanBo,GiangVien")]
    public class LopHocPhanController : Controller
    {
        private readonly QuanLySinhVienContext _db;

        public LopHocPhanController(QuanLySinhVienContext db)
        {
            _db = db;
        }

        private static void ValidateLopHocPhan(LopHocPhan lhp, ModelStateDictionary modelState)
        {
            if (lhp.SiSoToiDa < 1)
            {
                modelState.AddModelError("SiSoToiDa", "Sĩ số tối đa phải lớn hơn hoặc bằng 1.");
            }

            if (lhp.SiSoToiDa > 500)
            {
                modelState.AddModelError("SiSoToiDa", "Sĩ số tối đa không được vượt quá 500.");
            }
        }

        private static bool TrungTiet(int tietBatDauA, int soTietA, int tietBatDauB, int soTietB)
        {
            int tietKetThucA = tietBatDauA + soTietA - 1;
            int tietKetThucB = tietBatDauB + soTietB - 1;

            return tietBatDauA <= tietKetThucB && tietBatDauB <= tietKetThucA;
        }

        private async Task<List<string>> KiemTraTrungLichAsync(
            string maGv,
            string maHocKy,
            string? phongHoc,
            List<ChiTietLichHoc> lichMoi,
            string? maLhpBoQua = null)
        {
            var loi = new List<string>();

            if (lichMoi == null || !lichMoi.Any())
            {
                loi.Add("Vui lòng chọn ít nhất một buổi học.");
                return loi;
            }

            var lichDangCo = await _db.ChiTietLichHocs
                .Include(x => x.MaLhpNavigation)
                    .ThenInclude(lhp => lhp!.MaMonNavigation)
                .Include(x => x.MaLhpNavigation)
                    .ThenInclude(lhp => lhp!.MaGvNavigation)
                .Where(x =>
                    x.MaLhpNavigation != null &&
                    x.MaLhpNavigation.MaHocKy == maHocKy &&
                    (maLhpBoQua == null || x.MaLhp != maLhpBoQua))
                .ToListAsync();

            foreach (var lich in lichMoi)
            {
                var trungGiangVien = lichDangCo
                    .Where(x =>
                        x.MaLhpNavigation != null &&
                        x.MaLhpNavigation.MaGv == maGv &&
                        x.Thu == lich.Thu &&
                        TrungTiet(x.TietBatDau, x.SoTiet, lich.TietBatDau, lich.SoTiet))
                    .ToList();

                foreach (var item in trungGiangVien)
                {
                    string tenMon = item.MaLhpNavigation?.MaMonNavigation?.TenMon ?? item.MaLhp;
                    string tenGv = item.MaLhpNavigation?.MaGvNavigation?.HoTen ?? maGv;

                    loi.Add(
                        $"Giảng viên {tenGv} bị trùng lịch với lớp học phần {item.MaLhp} - {tenMon}, " +
                        $"thứ {item.Thu}, tiết {item.TietBatDau}-{item.TietBatDau + item.SoTiet - 1}."
                    );
                }

                if (!string.IsNullOrWhiteSpace(phongHoc))
                {
                    var trungPhong = lichDangCo
                        .Where(x =>
                            x.MaLhpNavigation != null &&
                            x.MaLhpNavigation.PhongHoc != null &&
                            x.MaLhpNavigation.PhongHoc.Trim().ToLower() == phongHoc.Trim().ToLower() &&
                            x.Thu == lich.Thu &&
                            TrungTiet(x.TietBatDau, x.SoTiet, lich.TietBatDau, lich.SoTiet))
                        .ToList();

                    foreach (var item in trungPhong)
                    {
                        string tenMon = item.MaLhpNavigation?.MaMonNavigation?.TenMon ?? item.MaLhp;

                        loi.Add(
                            $"Phòng {phongHoc} bị trùng với lớp học phần {item.MaLhp} - {tenMon}, " +
                            $"thứ {item.Thu}, tiết {item.TietBatDau}-{item.TietBatDau + item.SoTiet - 1}."
                        );
                    }
                }
            }

            return loi.Distinct().ToList();
        }

        private async Task LoadViewBagFormAsync(string? tuKhoa = null, string? maKhoa = null, string? khoaHoc = null)
        {
            var dsHocKy = await _db.HocKies.ToListAsync();

            ViewBag.DanhSachHK = dsHocKy
                .OrderByDescending(h => h.TenHocKy != null && h.TenHocKy.Length >= 9
                    ? h.TenHocKy.Substring(h.TenHocKy.Length - 9)
                    : h.TenHocKy)
                .ThenByDescending(h => h.TenHocKy)
                .ToList();

            ViewBag.DanhSachKhoa = await _db.Khoas
                .OrderBy(k => k.TenKhoa)
                .ToListAsync();

            ViewBag.DanhSachMon = await _db.MonHocs
                .OrderBy(m => m.TenMon)
                .ToListAsync();

            ViewBag.DanhSachLop = await _db.LopHocs
                .OrderBy(l => l.MaKhoa)
                .ThenByDescending(l => l.KhoaHoc)
                .ThenBy(l => l.MaLop)
                .ToListAsync();

            ViewBag.DanhSachGV = await _db.GiangViens
                .OrderBy(g => g.HoTen)
                .ToListAsync();

            ViewBag.DSKhoaHoc = await _db.KhoaHocs
                .OrderByDescending(k => k.NamBatDau)
                .Select(k => k.MaKhoaHoc)
                .ToListAsync();

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.MaKhoa = maKhoa;
            ViewBag.KhoaHoc = ChuanHoaMaKhoaHoc(khoaHoc);
        }

        private static string? ChuanHoaMaKhoaHoc(string? khoaHoc)
        {
            if (string.IsNullOrWhiteSpace(khoaHoc))
            {
                return null;
            }

            var value = khoaHoc.Trim();

            if (value.StartsWith("Khóa ", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(5).Trim();
            }

            if (!value.StartsWith("K", StringComparison.OrdinalIgnoreCase))
            {
                value = "K" + value;
            }

            return value.ToUpper();
        }

        public async Task<IActionResult> Index(string? tuKhoa, string? maKhoa, string? khoaHoc, int trang = 1)
        {
            int soTrangHienThi = 10;
            var maKhoaHoc = ChuanHoaMaKhoaHoc(khoaHoc);

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.MaKhoa = maKhoa;
            ViewBag.KhoaHoc = maKhoaHoc;
            ViewBag.Trang = trang;

            ViewBag.DanhSachKhoa = await _db.Khoas
                .OrderBy(k => k.TenKhoa)
                .ToListAsync();

            ViewBag.DSKhoaHoc = await _db.KhoaHocs
                .OrderByDescending(k => k.NamBatDau)
                .Select(k => k.MaKhoaHoc)
                .ToListAsync();

            var query = _db.LopHocPhans
                .Include(l => l.MaMonNavigation)
                    .ThenInclude(m => m.MaKhoaNavigation)
                .Include(l => l.MaLopNavigation)
                .Include(l => l.MaGvNavigation)
                .AsQueryable();

            if (User.IsInRole("GiangVien"))
            {
                string? username = User.Identity?.Name;
                query = query.Where(l => l.MaGv == username);
            }

            if (!string.IsNullOrEmpty(tuKhoa))
            {
                query = query.Where(l =>
                    l.MaLhp.Contains(tuKhoa) ||
                    l.MaMonNavigation.TenMon.Contains(tuKhoa));
            }

            if (!string.IsNullOrEmpty(maKhoa))
            {
                query = query.Where(l => l.MaMonNavigation.MaKhoa == maKhoa);
            }

            if (!string.IsNullOrEmpty(maKhoaHoc))
            {
                query = query.Where(l =>
                    l.MaLopNavigation != null &&
                    (
                        l.MaLopNavigation.KhoaHoc == maKhoaHoc ||
                        l.MaLopNavigation.TenLop.Contains(maKhoaHoc) ||
                        l.MaLopNavigation.TenLop.Contains("Khóa " + maKhoaHoc)
                    ));
            }

            int tongSo = await query.CountAsync();

            var danhSach = await query
                .OrderByDescending(l => l.MaHocKy)
                .ThenByDescending(l => l.MaLhp)
                .Skip((trang - 1) * soTrangHienThi)
                .Take(soTrangHienThi)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.TongTrang = (int)Math.Ceiling((double)tongSo / soTrangHienThi);
            ViewBag.TongSo = tongSo;

            return View(danhSach);
        }

        [Authorize(Roles = "Admin,CanBo")]
        public async Task<IActionResult> Create()
        {
            await LoadViewBagFormAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,CanBo")]
        public async Task<IActionResult> Create(LopHocPhan lhp, string LichHocJson)
        {
            ValidateLopHocPhan(lhp, ModelState);

            if (await _db.LopHocPhans.AnyAsync(l => l.MaLhp == lhp.MaLhp))
            {
                ModelState.AddModelError("MaLhp", "Mã LHP này đã tồn tại.");
            }

            ModelState.Remove("MaGvNavigation");
            ModelState.Remove("MaHocKyNavigation");
            ModelState.Remove("MaLopNavigation");
            ModelState.Remove("MaMonNavigation");
            ModelState.Remove("DangKyHocs");

            ModelState.Remove("MaLop");
            ModelState.Remove("Thu");
            ModelState.Remove("TietBatDau");
            ModelState.Remove("SoTiet");

            List<ChiTietLichHoc>? details = null;

            if (!string.IsNullOrEmpty(LichHocJson))
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                details = JsonSerializer.Deserialize<List<ChiTietLichHoc>>(LichHocJson, options);
            }

            if (details == null || !details.Any())
            {
                ModelState.AddModelError("", "LỖI: Bắt buộc phải nhấp chuột vào lưới thời khóa biểu để chọn ít nhất 1 buổi học!");
            }
            else
            {
                var loiTrungLich = await KiemTraTrungLichAsync(
                    lhp.MaGv,
                    lhp.MaHocKy,
                    lhp.PhongHoc,
                    details
                );

                foreach (var loi in loiTrungLich)
                {
                    ModelState.AddModelError("", loi);
                }
            }

            if (ModelState.IsValid)
            {
                lhp.Thu = details![0].Thu;
                lhp.TietBatDau = details[0].TietBatDau;
                lhp.SoTiet = details[0].SoTiet;

                lhp.MaLop = null;

                _db.LopHocPhans.Add(lhp);
                await _db.SaveChangesAsync();

                foreach (var item in details)
                {
                    item.MaLhp = lhp.MaLhp;
                    _db.ChiTietLichHocs.Add(item);
                }

                await _db.SaveChangesAsync();

                TempData["Success"] = "Đã xếp lịch và mở lớp học phần thành công!";
                return RedirectToAction("Index");
            }

            await LoadViewBagFormAsync();
            return View(lhp);
        }

        [Authorize(Roles = "Admin,CanBo")]
        public async Task<IActionResult> Edit(string id, string? tuKhoa, string? maKhoa, string? khoaHoc)
        {
            var maKhoaHoc = ChuanHoaMaKhoaHoc(khoaHoc);

            var lhp = await _db.LopHocPhans.FindAsync(id);
            if (lhp == null)
            {
                return RedirectToAction("Index");
            }

            await LoadViewBagFormAsync(tuKhoa, maKhoa, maKhoaHoc);
            return View(lhp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,CanBo")]
        public async Task<IActionResult> Edit(
            LopHocPhan lhp,
            string? tuKhoa,
            string? maKhoa,
            string? khoaHoc,
            string LichHocJson)
        {
            var maKhoaHoc = ChuanHoaMaKhoaHoc(khoaHoc);

            ValidateLopHocPhan(lhp, ModelState);

            ModelState.Remove("MaGvNavigation");
            ModelState.Remove("MaHocKyNavigation");
            ModelState.Remove("MaLopNavigation");
            ModelState.Remove("MaMonNavigation");
            ModelState.Remove("DangKyHocs");

            ModelState.Remove("MaLop");
            ModelState.Remove("Thu");
            ModelState.Remove("TietBatDau");
            ModelState.Remove("SoTiet");

            List<ChiTietLichHoc>? details = null;

            if (!string.IsNullOrEmpty(LichHocJson))
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                details = JsonSerializer.Deserialize<List<ChiTietLichHoc>>(LichHocJson, options);
            }

            if (details == null || !details.Any())
            {
                ModelState.AddModelError("", "LỖI: Bắt buộc phải nhấp chuột vào lưới thời khóa biểu để chọn ít nhất 1 buổi học!");
            }
            else
            {
                var loiTrungLich = await KiemTraTrungLichAsync(
                    lhp.MaGv,
                    lhp.MaHocKy,
                    lhp.PhongHoc,
                    details,
                    lhp.MaLhp
                );

                foreach (var loi in loiTrungLich)
                {
                    ModelState.AddModelError("", loi);
                }
            }

            if (ModelState.IsValid)
            {
                lhp.Thu = details![0].Thu;
                lhp.TietBatDau = details[0].TietBatDau;
                lhp.SoTiet = details[0].SoTiet;

                lhp.MaLop = null;

                _db.LopHocPhans.Update(lhp);

                var oldDetails = _db.ChiTietLichHocs.Where(x => x.MaLhp == lhp.MaLhp);
                _db.ChiTietLichHocs.RemoveRange(oldDetails);

                await _db.SaveChangesAsync();

                foreach (var item in details)
                {
                    item.MaLhp = lhp.MaLhp;
                    _db.ChiTietLichHocs.Add(item);
                }

                await _db.SaveChangesAsync();

                TempData["Success"] = "Cập nhật lớp học phần và thời khóa biểu thành công!";
                return RedirectToAction("Index", new
                {
                    tuKhoa = tuKhoa,
                    maKhoa = maKhoa,
                    khoaHoc = maKhoaHoc
                });
            }

            await LoadViewBagFormAsync(tuKhoa, maKhoa, maKhoaHoc);
            return View(lhp);
        }

        [Authorize(Roles = "GiangVien")]
        public async Task<IActionResult> LichDay(string? maHocKy)
        {
            string? maGv = User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(maGv))
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            ViewBag.MaGv = maGv;
            ViewBag.MaHocKy = maHocKy;

            ViewBag.DanhSachHocKy = await _db.HocKies
                .OrderByDescending(h => h.TenHocKy)
                .ToListAsync();

            var query = _db.ChiTietLichHocs
                .Include(x => x.MaLhpNavigation)
                    .ThenInclude(lhp => lhp!.MaMonNavigation)
                .Include(x => x.MaLhpNavigation)
                    .ThenInclude(lhp => lhp!.MaGvNavigation)
                .Include(x => x.MaLhpNavigation)
                    .ThenInclude(lhp => lhp!.MaHocKyNavigation)
                .Where(x =>
                    x.MaLhpNavigation != null &&
                    x.MaLhpNavigation.MaGv == maGv)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(maHocKy))
            {
                query = query.Where(x =>
                    x.MaLhpNavigation != null &&
                    x.MaLhpNavigation.MaHocKy == maHocKy);
            }

            var lichDay = await query
                .OrderBy(x => x.Thu)
                .ThenBy(x => x.TietBatDau)
                .AsNoTracking()
                .ToListAsync();

            return View(lichDay);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id, string? tuKhoa, string? maKhoa, string? khoaHoc)
        {
            var maKhoaHoc = ChuanHoaMaKhoaHoc(khoaHoc);

            var lhp = await _db.LopHocPhans.FindAsync(id);
            if (lhp == null)
            {
                return RedirectToAction("Index");
            }

            bool coSinhVien = await _db.DangKyHocs.AnyAsync(d => d.MaLhp == id);
            if (coSinhVien)
            {
                TempData["Error"] = "Không thể xóa vì đã có sinh viên!";
                return RedirectToAction("Index", new
                {
                    tuKhoa = tuKhoa,
                    maKhoa = maKhoa,
                    khoaHoc = maKhoaHoc
                });
            }

            var details = _db.ChiTietLichHocs.Where(x => x.MaLhp == id);
            _db.ChiTietLichHocs.RemoveRange(details);

            _db.LopHocPhans.Remove(lhp);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã xóa lớp học phần.";
            return RedirectToAction("Index", new
            {
                tuKhoa = tuKhoa,
                maKhoa = maKhoa,
                khoaHoc = maKhoaHoc
            });
        }
    }
}