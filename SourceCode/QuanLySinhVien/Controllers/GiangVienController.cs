using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Models;

namespace QuanLySinhVien.Controllers
{
    [Authorize(Roles = "Admin,CanBo,GiangVien")]
    public class GiangVienController : Controller
    {
        private readonly QuanLySinhVienContext _db;

        public GiangVienController(QuanLySinhVienContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string? tuKhoa, string? maKhoa, int trang = 1)
        {
            int soTrangHienThi = 10;

            // 1. Gửi data ra View trước
            ViewBag.TuKhoa = tuKhoa;
            ViewBag.MaKhoa = maKhoa;
            ViewBag.Trang = trang;
            ViewBag.DanhSachKhoa = await _db.Khoas.ToListAsync();

            // 🛑 LOGIC MỚI: Nếu không chọn gì -> Trả về danh sách rỗng (Chưa lọc)
            if (string.IsNullOrEmpty(tuKhoa) && string.IsNullOrEmpty(maKhoa))
            {
                ViewBag.TongTrang = 0;
                ViewBag.TongSo = 0;
                return View(new List<GiangVien>());
            }

            // 2. Query dữ liệu
            var query = _db.GiangViens.Include(g => g.MaKhoaNavigation).AsQueryable();

            // 🛑 BẢO MẬT: Giảng viên chỉ xem được chính mình
            if (User.IsInRole("GiangVien"))
            {
                string username = User.Identity?.Name;
                query = query.Where(g => g.MaGv == username);
            }

            if (!string.IsNullOrEmpty(tuKhoa))
                query = query.Where(g => g.HoTen.Contains(tuKhoa) || g.MaGv.Contains(tuKhoa));

            if (!string.IsNullOrEmpty(maKhoa))
                query = query.Where(g => g.MaKhoa == maKhoa);

            // 3. Phân trang và SẮP XẾP MỚI: Ưu tiên Khoa -> Tới Mã GV
            int tongSo = await query.CountAsync();
            var danhSach = await query
                .OrderBy(g => g.MaKhoa) // Gọn gàng theo Khoa
                .ThenBy(g => g.MaGv)    // Sau đó xếp theo Mã
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
            ViewBag.DanhSachKhoa = await _db.Khoas.ToListAsync();
            return View();
        }

        // POST: /GiangVien/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,CanBo")]
        public async Task<IActionResult> Create(GiangVien gv)
        {
            // Kiểm tra xem Mã GV đã tồn tại chưa
            if (await _db.GiangViens.AnyAsync(g => g.MaGv == gv.MaGv))
                ModelState.AddModelError("MaGv", "Mã giảng viên này đã tồn tại.");

            // Kiểm tra xem Tên đăng nhập đã bị trùng chưa (tránh lỗi hệ thống)
            if (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == gv.MaGv))
                ModelState.AddModelError("MaGv", "Tài khoản cho mã giảng viên này đã tồn tại.");

            ModelState.Remove("MaKhoaNavigation");
            ModelState.Remove("MaTaiKhoanNavigation");

            if (ModelState.IsValid)
            {
                // 🌟 BƯỚC 1: TẠO TÀI KHOẢN TỰ ĐỘNG 🌟
                var taiKhoanMoi = new TaiKhoan
                {
                    TenDangNhap = gv.MaGv,
                    // Đã bọc hàm mã hóa BCrypt vào đây
                    MatKhauHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    VaiTro = "GiangVien",
                    TrangThai = true,
                    LanDangNhapSai = 0
                };

                _db.TaiKhoans.Add(taiKhoanMoi);
                await _db.SaveChangesAsync(); // Phải Save để Database sinh ra số MaTaiKhoan tự động

                // 🌟 BƯỚC 2: LIÊN KẾT TÀI KHOẢN CHO GIẢNG VIÊN 🌟
                gv.MaTaiKhoan = taiKhoanMoi.MaTaiKhoan;

                _db.GiangViens.Add(gv);
                await _db.SaveChangesAsync();

                TempData["Success"] = $"Thêm giảng viên {gv.HoTen} thành công! Đã cấp tài khoản: {gv.MaGv} / Mật khẩu: 123456";
                return RedirectToAction("Index");
            }

            ViewBag.DanhSachKhoa = await _db.Khoas.ToListAsync();
            return View(gv);
        }

        [Authorize(Roles = "Admin,CanBo")]
        public async Task<IActionResult> Edit(string id)
        {
            var gv = await _db.GiangViens.FindAsync(id);
            if (gv == null) return RedirectToAction("Index");
            ViewBag.DanhSachKhoa = await _db.Khoas.ToListAsync();
            return View(gv);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,CanBo")]
        public async Task<IActionResult> Edit(GiangVien gv)
        {
            ModelState.Remove("MaKhoaNavigation");
            ModelState.Remove("MaTaiKhoanNavigation");

            if (ModelState.IsValid)
            {
                _db.GiangViens.Update(gv);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Cập nhật thông tin {gv.HoTen} thành công!";
                return RedirectToAction("Index");
            }
            ViewBag.DanhSachKhoa = await _db.Khoas.ToListAsync();
            return View(gv);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var gv = await _db.GiangViens.FindAsync(id);
            if (gv == null) return RedirectToAction("Index");

            // 🛑 TUYỆT CHIÊU: Lấy lại cái URL hiện tại (chứa thông tin đang lọc Khoa nào, trang mấy...)
            string urlTruocDo = Request.Headers["Referer"].ToString();

            // Kiểm tra ràng buộc
            bool dangDayLhp = await _db.LopHocPhans.AnyAsync(l => l.MaGv == id);
            try
            {
                _db.GiangViens.Remove(gv);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã xoá giảng viên {gv.HoTen} thành công.";
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                TempData["Error"] = "Lỗi Database: Giảng viên này đang dính dữ liệu ở bảng khác, không thể xóa!";
            }

            // 🛑 Thay vì bay về Index trắng bóc, mình ném sếp về lại đúng cái URL có chứa bộ lọc
            return !string.IsNullOrEmpty(urlTruocDo) ? Redirect(urlTruocDo) : RedirectToAction("Index");
        }

        public async Task<IActionResult> Detail(string id)
        {
            var gv = await _db.GiangViens
                .Include(g => g.MaKhoaNavigation)
                .FirstOrDefaultAsync(g => g.MaGv == id);

            if (gv == null) return RedirectToAction("Index");
            return View(gv);
        }
    }
}