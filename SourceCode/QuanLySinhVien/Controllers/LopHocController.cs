using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLySinhVien.Controllers
{
    [Authorize(Roles = "Admin,CanBo,GiangVien")]
    public class LopHocController : Controller
    {
        private readonly QuanLySinhVienContext _db;

        public LopHocController(QuanLySinhVienContext db)
        {
            _db = db;
        }

        // ==========================================
        // HELPER: CHUẨN HÓA MÃ KHÓA HỌC
        // Input: "", "51", "K51", "Khóa K51"
        // Output: null hoặc "K51"
        // ==========================================
        private static string? ChuanHoaMaKhoaHoc(string? khoaHoc)
        {
            if (string.IsNullOrWhiteSpace(khoaHoc))
                return null;

            var value = khoaHoc.Trim();

            if (value.StartsWith("Khóa ", StringComparison.OrdinalIgnoreCase))
                value = value.Substring(5).Trim();

            if (!value.StartsWith("K", StringComparison.OrdinalIgnoreCase))
                value = "K" + value;

            return value.ToUpper();
        }

        public async Task<IActionResult> Index(string? tuKhoa, string? maKhoa, string? maChuyenNganh, string? khoaHoc, int trang = 1)
        {
            int soTrangHienThi = 10;
            var maKhoaHoc = ChuanHoaMaKhoaHoc(khoaHoc);

            // 1. CHUẨN BỊ DATA CHO GIAO DIỆN TRƯỚC
            ViewBag.TuKhoa = tuKhoa;
            ViewBag.MaKhoa = maKhoa;
            ViewBag.MaChuyenNganh = maChuyenNganh;
            ViewBag.KhoaHoc = maKhoaHoc;
            ViewBag.Trang = trang;

            ViewBag.DanhSachKhoa = await _db.Khoas
                .OrderBy(k => k.TenKhoa)
                .ToListAsync();

            ViewBag.DSKhoaHoc = await _db.KhoaHocs
                .OrderByDescending(k => k.NamBatDau)
                .Select(k => k.MaKhoaHoc)
                .ToListAsync();

            // 🛑 LOGIC: KHÔNG CHỌN GÌ THÌ TRẢ VỀ DANH SÁCH RỖNG
            if (string.IsNullOrEmpty(tuKhoa) &&
                string.IsNullOrEmpty(maKhoa) &&
                string.IsNullOrEmpty(maChuyenNganh) &&
                string.IsNullOrEmpty(maKhoaHoc))
            {
                ViewBag.TongTrang = 0;
                ViewBag.TongSo = 0;
                return View(new List<LopHoc>());
            }

            var query = _db.LopHocs
                .Include(l => l.MaKhoaNavigation)
                .Include(l => l.SinhViens)
                .AsQueryable();

            if (!string.IsNullOrEmpty(tuKhoa))
                query = query.Where(l => l.TenLop.Contains(tuKhoa) || l.MaLop.Contains(tuKhoa));

            if (!string.IsNullOrEmpty(maKhoa))
                query = query.Where(l => l.MaKhoa == maKhoa);

            if (!string.IsNullOrEmpty(maChuyenNganh))
            {
                var tenCN = maChuyenNganh switch
                {
                    "Toan" => "Toán",
                    "Van" => "Văn",
                    "Anh" => "Anh",
                    "Ly" => "Lý",
                    "Hoa" => "Hóa",
                    "Sinh" => "Sinh",
                    "Dia" => "Địa",
                    "Su" => "Sử",
                    "Tin" => "Tin",
                    "GDCD" => "GDCD",
                    _ => maChuyenNganh,
                };

                query = query.Where(l => l.TenLop.Contains(tenCN));
            }

            // FIX DYNAMIC QUERY THEO KHÓA HỌC:
            // Nếu maKhoaHoc rỗng => không lọc khóa.
            // Nếu có maKhoaHoc => ưu tiên lọc đúng cột LopHoc.KhoaHoc.
            if (!string.IsNullOrEmpty(maKhoaHoc))
            {
                query = query.Where(l =>
                    l.KhoaHoc == maKhoaHoc ||
                    l.TenLop.Contains(maKhoaHoc) ||
                    l.TenLop.Contains("Khóa " + maKhoaHoc));
            }

            int tongSo = await query.CountAsync();

            var danhSach = await query
                .OrderBy(l => l.MaKhoa)
                .ThenByDescending(l => l.KhoaHoc)
                .ThenBy(l => l.MaLop)
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
            ViewBag.DanhSachKhoa = await _db.Khoas
                .OrderBy(k => k.TenKhoa)
                .ToListAsync();

            ViewBag.DSKhoaHoc = await _db.KhoaHocs
                .OrderByDescending(k => k.NamBatDau)
                .Select(k => k.MaKhoaHoc)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,CanBo")]
        public async Task<IActionResult> Create(LopHoc lop)
        {
            lop.KhoaHoc = ChuanHoaMaKhoaHoc(lop.KhoaHoc);

            if (await _db.LopHocs.AnyAsync(l => l.MaLop == lop.MaLop))
                ModelState.AddModelError("MaLop", "Mã lớp này đã tồn tại.");

            if (string.IsNullOrWhiteSpace(lop.KhoaHoc))
                ModelState.AddModelError("KhoaHoc", "Vui lòng chọn khóa học.");

            ModelState.Remove("MaKhoaNavigation");
            ModelState.Remove("SinhViens");
            ModelState.Remove("PhanCongs");
            ModelState.Remove("DangKyHocs");
            ModelState.Remove("LopHocPhans");

            if (ModelState.IsValid)
            {
                _db.LopHocs.Add(lop);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Thêm lớp {lop.TenLop} thành công!";
                return RedirectToAction("Index", new { khoaHoc = lop.KhoaHoc, maKhoa = lop.MaKhoa });
            }

            ViewBag.DanhSachKhoa = await _db.Khoas
                .OrderBy(k => k.TenKhoa)
                .ToListAsync();

            ViewBag.DSKhoaHoc = await _db.KhoaHocs
                .OrderByDescending(k => k.NamBatDau)
                .Select(k => k.MaKhoaHoc)
                .ToListAsync();

            return View(lop);
        }

        [Authorize(Roles = "Admin,CanBo")]
        public async Task<IActionResult> Edit(string id)
        {
            var lop = await _db.LopHocs.FindAsync(id);
            if (lop == null) return RedirectToAction("Index");

            ViewBag.DanhSachKhoa = await _db.Khoas
                .OrderBy(k => k.TenKhoa)
                .ToListAsync();

            ViewBag.DSKhoaHoc = await _db.KhoaHocs
                .OrderByDescending(k => k.NamBatDau)
                .Select(k => k.MaKhoaHoc)
                .ToListAsync();

            return View(lop);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,CanBo")]
        public async Task<IActionResult> Edit(LopHoc lop)
        {
            lop.KhoaHoc = ChuanHoaMaKhoaHoc(lop.KhoaHoc);

            if (string.IsNullOrWhiteSpace(lop.KhoaHoc))
                ModelState.AddModelError("KhoaHoc", "Vui lòng chọn khóa học.");

            ModelState.Remove("MaKhoaNavigation");
            ModelState.Remove("SinhViens");
            ModelState.Remove("PhanCongs");
            ModelState.Remove("DangKyHocs");
            ModelState.Remove("LopHocPhans");

            if (ModelState.IsValid)
            {
                _db.LopHocs.Update(lop);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Cập nhật lớp {lop.TenLop} thành công!";
                return RedirectToAction("Index", new { khoaHoc = lop.KhoaHoc, maKhoa = lop.MaKhoa });
            }

            ViewBag.DanhSachKhoa = await _db.Khoas
                .OrderBy(k => k.TenKhoa)
                .ToListAsync();

            ViewBag.DSKhoaHoc = await _db.KhoaHocs
                .OrderByDescending(k => k.NamBatDau)
                .Select(k => k.MaKhoaHoc)
                .ToListAsync();

            return View(lop);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var lop = await _db.LopHocs.FindAsync(id);
            if (lop == null) return RedirectToAction("Index");

            bool coSinhVien = await _db.SinhViens.AnyAsync(s => s.MaLop == id);
            if (coSinhVien)
            {
                TempData["Error"] = $"Không thể xoá lớp {lop.TenLop} vì còn sinh viên đang học.";
                return RedirectToAction("Index");
            }

            _db.LopHocs.Remove(lop);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã xoá lớp {lop.TenLop}.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Detail(string id)
        {
            var lop = await _db.LopHocs
                .Include(l => l.MaKhoaNavigation)
                .Include(l => l.SinhViens)
                .FirstOrDefaultAsync(l => l.MaLop == id);

            if (lop == null) return RedirectToAction("Index");
            return View(lop);
        }
    }
}