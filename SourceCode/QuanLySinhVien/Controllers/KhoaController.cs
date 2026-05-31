using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Models;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLySinhVien.Controllers
{
    // Chỉ có Admin và Cán bộ mới được phép vào quản lý Khoa
    [Authorize(Roles = "Admin,CanBo")]
    public class KhoaController : Controller
    {
        private readonly QuanLySinhVienContext _db;
        public KhoaController(QuanLySinhVienContext db) => _db = db;

        // 1. Xem danh sách Khoa
        public async Task<IActionResult> Index()
        {
            var dsKhoa = await _db.Khoas.OrderBy(k => k.MaKhoa).ToListAsync();
            return View(dsKhoa);
        }

        // 2. Mở form Tạo mới
        public IActionResult Create()
        {
            return View();
        }

        // 3. Xử lý Lưu Tạo mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Khoa khoa)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem mã khoa đã tồn tại chưa
                var checkKhoa = await _db.Khoas.FindAsync(khoa.MaKhoa);
                if (checkKhoa != null)
                {
                    ModelState.AddModelError("MaKhoa", "Mã khoa này đã tồn tại trong hệ thống!");
                    return View(khoa);
                }

                _db.Khoas.Add(khoa);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã thêm khoa mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(khoa);
        }

        // 4. Mở form Sửa
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var khoa = await _db.Khoas.FindAsync(id);
            if (khoa == null) return NotFound();

            return View(khoa);
        }

        // 5. Xử lý Lưu Sửa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Khoa khoa)
        {
            if (id != khoa.MaKhoa) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _db.Update(khoa);
                    await _db.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật thông tin khoa thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KhoaExists(khoa.MaKhoa)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(khoa);
        }

        // 6. Xử lý Xóa (Dùng HttpPost để bảo mật)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var khoa = await _db.Khoas.FindAsync(id);
            if (khoa != null)
            {
                // Kiểm tra xem Khoa này đã có Lớp nào chưa (Ràng buộc khóa ngoại)
                bool coLopThuocKhoa = await _db.LopHocs.AnyAsync(l => l.MaKhoa == id);
                if (coLopThuocKhoa)
                {
                    TempData["Error"] = "Không thể xóa! Khoa này đang chứa các Lớp học sinh hoạt.";
                    return RedirectToAction(nameof(Index));
                }

                _db.Khoas.Remove(khoa);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã xóa khoa thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool KhoaExists(string id)
        {
            return _db.Khoas.Any(e => e.MaKhoa == id);
        }
    }
}