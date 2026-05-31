using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Models;

namespace QuanLySinhVien.Controllers
{
    [Authorize(Roles = "Admin,CanBo")]
    public class HocKyController : Controller
    {
        private readonly QuanLySinhVienContext _db;

        public HocKyController(QuanLySinhVienContext db)
        {
            _db = db;
        }

        private static void ValidateHocKy(HocKy hk, ModelStateDictionary modelState)
        {
            if (!hk.GioiHanTinChi.HasValue)
            {
                modelState.AddModelError("GioiHanTinChi", "Vui lòng nhập giới hạn tín chỉ.");
                return;
            }

            if (hk.GioiHanTinChi.Value < 1)
            {
                modelState.AddModelError("GioiHanTinChi", "Giới hạn tín chỉ phải lớn hơn hoặc bằng 1.");
            }

            if (hk.GioiHanTinChi.Value > 50)
            {
                modelState.AddModelError("GioiHanTinChi", "Giới hạn tín chỉ không được vượt quá 50.");
            }
        }
        // GET: /HocKy/Index
        public async Task<IActionResult> Index()
        {
            var danhSach = await _db.HocKies
                .OrderByDescending(h => h.NgayBatDau)
                .ToListAsync();

            return View(danhSach);
        }

        // GET: /HocKy/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /HocKy/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HocKy hk)
        {
            ValidateHocKy(hk, ModelState);

            if (await _db.HocKies.AnyAsync(h => h.MaHocKy == hk.MaHocKy))
                ModelState.AddModelError("MaHocKy", "Mã học kỳ này đã tồn tại.");

            if (hk.NgayBatDau >= hk.NgayKetThuc)
                ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc phải sau ngày bắt đầu.");

            ModelState.Remove("DangKyHocs");
            ModelState.Remove("PhanCongs");

            if (ModelState.IsValid)
            {
                _db.HocKies.Add(hk);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Thêm học kỳ {hk.TenHocKy} thành công!";
                return RedirectToAction("Index");
            }

            return View(hk);
        }

        // GET: /HocKy/Edit/HK1-2024
        public async Task<IActionResult> Edit(string id)
        {
            var hk = await _db.HocKies.FindAsync(id);
            if (hk == null)
            {
                TempData["Error"] = "Không tìm thấy học kỳ.";
                return RedirectToAction("Index");
            }
            return View(hk);
        }

        // POST: /HocKy/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HocKy hk)
        {
            ValidateHocKy(hk, ModelState);

            if (hk.NgayBatDau >= hk.NgayKetThuc)
                ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc phải sau ngày bắt đầu.");

            ModelState.Remove("DangKyHocs");
            ModelState.Remove("PhanCongs");

            if (ModelState.IsValid)
            {
                _db.HocKies.Update(hk);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Cập nhật học kỳ {hk.TenHocKy} thành công!";
                return RedirectToAction("Index");
            }

            return View(hk);
        }

        // POST: /HocKy/Delete
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var hk = await _db.HocKies.FindAsync(id);
            if (hk == null)
            {
                TempData["Error"] = "Không tìm thấy học kỳ.";
                return RedirectToAction("Index");
            }

            bool daSuDung = await _db.DangKyHocs.AnyAsync(d => d.MaHocKy == id);
            if (daSuDung)
            {
                TempData["Error"] = $"Không thể xoá học kỳ {hk.TenHocKy} vì đã có dữ liệu đăng ký.";
                return RedirectToAction("Index");
            }

            _db.HocKies.Remove(hk);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã xoá học kỳ {hk.TenHocKy}.";
            return RedirectToAction("Index");
        }

        // POST: /HocKy/DoiTrangThai
        [HttpPost]
        public async Task<IActionResult> DoiTrangThai(string id, string trangThai)
        {
            var hk = await _db.HocKies.FindAsync(id);
            if (hk != null)
            {
                hk.TrangThai = trangThai;
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã cập nhật trạng thái học kỳ {hk.TenHocKy}.";
            }
            return RedirectToAction("Index");
        }
    }
}