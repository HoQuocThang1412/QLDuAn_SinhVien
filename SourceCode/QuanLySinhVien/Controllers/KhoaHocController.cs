using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Models;

namespace QuanLySinhVien.Controllers
{
    [Authorize(Roles = "Admin")]
    public class KhoaHocController : Controller
    {
        private readonly QuanLySinhVienContext _db;
        public KhoaHocController(QuanLySinhVienContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var list = await _db.KhoaHocs.OrderBy(k => k.MaKhoaHoc).ToListAsync();
            return View(list);
        }

        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KhoaHoc kh)
        {
            if (await _db.KhoaHocs.AnyAsync(k => k.MaKhoaHoc == kh.MaKhoaHoc))
                ModelState.AddModelError("MaKhoaHoc", "Mã khóa học đã tồn tại.");

            if (ModelState.IsValid)
            {
                _db.KhoaHocs.Add(kh);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã thêm {kh.TenKhoaHoc}!";
                return RedirectToAction(nameof(Index));
            }
            return View(kh);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var kh = await _db.KhoaHocs.FindAsync(id);
            if (kh == null) return RedirectToAction(nameof(Index));
            return View(kh);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(KhoaHoc kh)
        {
            if (ModelState.IsValid)
            {
                _db.KhoaHocs.Update(kh);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Cập nhật thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(kh);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var kh = await _db.KhoaHocs.FindAsync(id);
            if (kh != null)
            {
                _db.KhoaHocs.Remove(kh);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã xóa {kh.TenKhoaHoc}!";
            }
            return RedirectToAction(nameof(Index));
        }

        // API cho các dropdown trên toàn hệ thống
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _db.KhoaHocs
                .OrderBy(k => k.MaKhoaHoc)
                .Select(k => new { k.MaKhoaHoc, k.TenKhoaHoc })
                .ToListAsync();
            return Json(list);
        }
    }
}