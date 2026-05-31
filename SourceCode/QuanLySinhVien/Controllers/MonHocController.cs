using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Models;

namespace QuanLySinhVien.Controllers
{
    [Authorize(Roles = "Admin,CanBo,GiangVien")]
    public class MonHocController : Controller
    {
        private readonly QuanLySinhVienContext _db;

        public MonHocController(QuanLySinhVienContext db)
        {
            _db = db;
        }

        private async Task LoadDropdowns(string? maMonHienTai = null)
        {
            ViewBag.DanhSachKhoa = await _db.Khoas
                .OrderBy(k => k.TenKhoa)
                .ToListAsync();

            ViewBag.DanhSachMonTienQuyet = await _db.MonHocs
                .Where(m => string.IsNullOrEmpty(maMonHienTai) || m.MaMon != maMonHienTai)
                .OrderBy(m => m.TenMon)
                .Select(m => new
                {
                    m.MaMon,
                    TenHienThi = m.MaMon + " - " + m.TenMon
                })
                .ToListAsync();
        }

        public async Task<IActionResult> Index(string? tuKhoa, string? maKhoa, int trang = 1)
        {
            int soTrangHienThi = 10;

            var query = _db.MonHocs
                .Include(m => m.MaKhoaNavigation)
                .AsQueryable();

            // BẢO MẬT: Giảng viên chỉ xem môn mình dạy
            if (User.IsInRole("GiangVien"))
            {
                string? username = User.Identity?.Name;

                var danhSachMonDangDay = _db.LopHocPhans
                    .Where(l => l.MaGv == username)
                    .Select(l => l.MaMon)
                    .Distinct()
                    .ToList();

                query = query.Where(m => danhSachMonDangDay.Contains(m.MaMon));
            }

            if (!string.IsNullOrEmpty(tuKhoa))
                query = query.Where(m => m.TenMon.Contains(tuKhoa) || m.MaMon.Contains(tuKhoa));

            if (!string.IsNullOrEmpty(maKhoa))
                query = query.Where(m => m.MaKhoa == maKhoa);

            int tongSo = await query.CountAsync();

            var danhSach = await query
                .OrderBy(m => m.MaMon)
                .Skip((trang - 1) * soTrangHienThi)
                .Take(soTrangHienThi)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.MaKhoa = maKhoa;
            ViewBag.Trang = trang;
            ViewBag.TongTrang = (int)Math.Ceiling((double)tongSo / soTrangHienThi);
            ViewBag.TongSo = tongSo;
            ViewBag.DanhSachKhoa = await _db.Khoas
                .OrderBy(k => k.TenKhoa)
                .ToListAsync();

            return View(danhSach);
        }

        [Authorize(Roles = "Admin,CanBo")]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,CanBo")]
        public async Task<IActionResult> Create(MonHoc mon)
        {
            if (await _db.MonHocs.AnyAsync(m => m.MaMon == mon.MaMon))
                ModelState.AddModelError("MaMon", "Mã môn này đã tồn tại.");

            if (mon.HeSoQt + mon.HeSoCk != 1.00m)
                ModelState.AddModelError("HeSoQt", "Hệ số QT + Hệ số CK phải bằng 1.00");

            ModelState.Remove("MaKhoaNavigation");
            ModelState.Remove("DangKyHocs");
            ModelState.Remove("PhanCongs");
            mon.MaMonTienQuyet = null;

            if (ModelState.IsValid)
            {
                _db.MonHocs.Add(mon);
                await _db.SaveChangesAsync();

                TempData["Success"] = mon.IsDieuKien
                    ? $"Thêm môn điều kiện {mon.TenMon} thành công! Môn này không tính GPA."
                    : $"Thêm môn {mon.TenMon} thành công!";

                return RedirectToAction("Index");
            }

            await LoadDropdowns();
            return View(mon);
        }

        [Authorize(Roles = "Admin,CanBo")]
        public async Task<IActionResult> Edit(string id)
        {
            var mon = await _db.MonHocs.FindAsync(id);
            if (mon == null) return RedirectToAction("Index");

            await LoadDropdowns(id);
            return View(mon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,CanBo")]
        public async Task<IActionResult> Edit(MonHoc mon)
        {

            if (mon.HeSoQt + mon.HeSoCk != 1.00m)
                ModelState.AddModelError("HeSoQt", "Hệ số QT + Hệ số CK phải bằng 1.00");

            ModelState.Remove("MaKhoaNavigation");
            ModelState.Remove("DangKyHocs");
            ModelState.Remove("PhanCongs");
            mon.MaMonTienQuyet = null;

            if (ModelState.IsValid)
            {
                _db.MonHocs.Update(mon);
                await _db.SaveChangesAsync();

                TempData["Success"] = mon.IsDieuKien
                    ? $"Cập nhật môn điều kiện {mon.TenMon} thành công! Môn này không tính GPA."
                    : $"Cập nhật môn {mon.TenMon} thành công!";

                return RedirectToAction("Index");
            }

            await LoadDropdowns(mon.MaMon);
            return View(mon);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var mon = await _db.MonHocs.FindAsync(id);
            if (mon == null) return RedirectToAction("Index");

            bool daSuDung = await _db.DangKyHocs.AnyAsync(d => d.MaMon == id);
            if (daSuDung)
            {
                TempData["Error"] = $"Không thể xoá môn {mon.TenMon} vì đã có sinh viên đăng ký.";
                return RedirectToAction("Index");
            }

            _db.MonHocs.Remove(mon);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã xoá môn {mon.TenMon}.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Detail(string id)
        {
            var mon = await _db.MonHocs
                .Include(m => m.MaKhoaNavigation)
                .FirstOrDefaultAsync(m => m.MaMon == id);

            if (mon == null) return RedirectToAction("Index");

            ViewBag.MonTienQuyet = null;

            if (!string.IsNullOrEmpty(mon.MaMonTienQuyet))
            {
                ViewBag.MonTienQuyet = await _db.MonHocs
                    .Where(m => m.MaMon == mon.MaMonTienQuyet)
                    .Select(m => m.MaMon + " - " + m.TenMon)
                    .FirstOrDefaultAsync();
            }

            return View(mon);
        }
    }
}