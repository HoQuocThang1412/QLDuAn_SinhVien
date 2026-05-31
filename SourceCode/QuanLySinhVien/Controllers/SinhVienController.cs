using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.RegularExpressions;

namespace QuanLySinhVien.Controllers
{
    // 🛑 1. MỞ CỬA CHÍNH: Cho phép SinhVien bước vào nhà Controller này
    [Authorize(Roles = "Admin,CanBo,GiangVien,SinhVien")]
    public class SinhVienController : Controller
    {
        private readonly QuanLySinhVienContext _db;

        public SinhVienController(QuanLySinhVienContext db)
        {
            _db = db;
        }

        private static int TinhTuoi(DateOnly ngaySinh, DateOnly ngayHienTai)
        {
            int tuoi = ngayHienTai.Year - ngaySinh.Year;

            if (ngayHienTai < ngaySinh.AddYears(tuoi))
            {
                tuoi--;
            }

            return tuoi;
        }

        private static void ValidateNgaySinhSinhVien(ModelStateDictionary modelState, DateOnly ngaySinh)
        {
            var ngayHienTai = DateOnly.FromDateTime(DateTime.Today);
            int tuoi = TinhTuoi(ngaySinh, ngayHienTai);

            if (tuoi < 18)
            {
                modelState.AddModelError("NgaySinh", "Sinh viên phải đủ 18 tuổi.");
            }
        }

        private static string? ChuanHoaHoTen(string? hoTen)
        {
            if (string.IsNullOrWhiteSpace(hoTen))
                return null;

            return Regex.Replace(hoTen.Trim(), @"\s+", " ");
        }

        private static void ValidateSoDienThoaiSinhVien(ModelStateDictionary modelState, string? soDienThoai)
        {
            if (string.IsNullOrWhiteSpace(soDienThoai))
                return;

            soDienThoai = soDienThoai.Trim();

            if (!Regex.IsMatch(soDienThoai, @"^0\d{9}$"))
            {
                modelState.AddModelError("SoDienThoai", "Số điện thoại phải gồm đúng 10 chữ số và bắt đầu bằng 0.");
            }
        }
        private static void ValidateHoTenSinhVien(ModelStateDictionary modelState, string? hoTen)
        {
            hoTen = ChuanHoaHoTen(hoTen);

            if (string.IsNullOrWhiteSpace(hoTen))
            {
                modelState.AddModelError("HoTen", "Họ tên không được để trống.");
                return;
            }

            if (hoTen.Length > 100)
            {
                modelState.AddModelError("HoTen", "Họ tên không được vượt quá 100 ký tự.");
                return;
            }

            if (!Regex.IsMatch(hoTen, @"^[\p{L}\s'.-]+$"))
            {
                modelState.AddModelError("HoTen", "Họ tên chỉ được chứa chữ cái, khoảng trắng, dấu chấm, dấu gạch nối hoặc dấu nháy.");
                return;
            }

            var words = hoTen.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words.Length < 2)
            {
                modelState.AddModelError("HoTen", "Họ tên phải có ít nhất 2 từ, ví dụ: Nguyễn Văn An.");
                return;
            }

            if (words.Length > 6)
            {
                modelState.AddModelError("HoTen", "Họ tên không được vượt quá 6 từ.");
                return;
            }

            foreach (var word in words)
            {
                if (word.Length < 2)
                {
                    modelState.AddModelError("HoTen", "Mỗi từ trong họ tên phải có ít nhất 2 ký tự.");
                    return;
                }

                if (word.Length > 20)
                {
                    modelState.AddModelError("HoTen", "Mỗi từ trong họ tên không được vượt quá 20 ký tự.");
                    return;
                }
            }

            if (Regex.IsMatch(hoTen, @"(.)\1{3,}"))
            {
                modelState.AddModelError("HoTen", "Họ tên không hợp lệ do có ký tự lặp lại quá nhiều.");
                return;
            }
        }
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

        // ==========================================
        // HELPER: LỌC LỚP THEO KHOA + KHÓA HỌC
        // Không hard-code K51.
        // Nếu khoaHoc rỗng thì lấy tất cả khóa.
        // ==========================================
        private IQueryable<LopHoc> LocLopTheoKhoaVaKhoaHoc(IQueryable<LopHoc> query, string? maKhoa, string? khoaHoc)
        {
            if (!string.IsNullOrWhiteSpace(maKhoa))
            {
                query = query.Where(l => l.MaKhoa == maKhoa);
            }

            var maKhoaHoc = ChuanHoaMaKhoaHoc(khoaHoc);

            if (!string.IsNullOrWhiteSpace(maKhoaHoc))
            {
                query = query.Where(l =>
                    l.KhoaHoc == maKhoaHoc ||
                    l.TenLop.Contains(maKhoaHoc) ||
                    l.TenLop.Contains("Khóa " + maKhoaHoc));
            }

            return query;
        }

        // 🛑 2. KHÓA PHÒNG KHÁCH: Cấm Sinh viên xem Danh sách tổng (Chỉ Admin, Cán bộ, Giảng viên)
        [Authorize(Roles = "Admin,CanBo,GiangVien")]
        public async Task<IActionResult> Index(string? tuKhoa, string? maLop, string? maKhoa, string? maChuyenNganh, string? trangThai, string? khoaHoc, int trang = 1)
        {
            int soTrangHienThi = 10;
            var maKhoaHoc = ChuanHoaMaKhoaHoc(khoaHoc);

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.MaLop = maLop;
            ViewBag.MaKhoa = maKhoa;
            ViewBag.MaChuyenNganh = maChuyenNganh;
            ViewBag.TrangThai = trangThai;
            ViewBag.KhoaHoc = maKhoaHoc;
            ViewBag.Trang = trang;

            ViewBag.DanhSachKhoa = await _db.Khoas
                .OrderBy(k => k.TenKhoa)
                .ToListAsync();

            ViewBag.DSKhoaHoc = await _db.KhoaHocs
                .OrderByDescending(k => k.NamBatDau)
                .Select(k => k.MaKhoaHoc)
                .ToListAsync();

            // --- Lọc danh sách Lớp cho dropdown theo Khoa + Khóa học ---
            var queryLop = LocLopTheoKhoaVaKhoaHoc(_db.LopHocs.AsQueryable(), maKhoa, maKhoaHoc);

            ViewBag.DanhSachLop = await queryLop
                .OrderBy(l => l.MaKhoa)
                .ThenByDescending(l => l.KhoaHoc)
                .ThenBy(l => l.TenLop)
                .ToListAsync();

            if (string.IsNullOrEmpty(tuKhoa) && string.IsNullOrEmpty(maLop) &&
                string.IsNullOrEmpty(maKhoa) && string.IsNullOrEmpty(maChuyenNganh) &&
                string.IsNullOrEmpty(trangThai) && string.IsNullOrEmpty(maKhoaHoc))
            {
                ViewBag.TongTrang = 0;
                ViewBag.TongSo = 0;
                return View(new List<SinhVien>());
            }

            var query = _db.SinhViens
                .Include(s => s.MaLopNavigation)
                    .ThenInclude(l => l.MaKhoaNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(tuKhoa))
                query = query.Where(s => s.HoTen.Contains(tuKhoa) || s.Mssv.Contains(tuKhoa));

            if (!string.IsNullOrEmpty(maLop))
                query = query.Where(s => s.MaLop == maLop);

            if (!string.IsNullOrEmpty(maKhoa))
                query = query.Where(s => s.MaLopNavigation.MaKhoa == maKhoa);

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
                    _ => maChuyenNganh
                };
                query = query.Where(s => s.MaLopNavigation.TenLop.Contains(tenCN));
            }

            if (!string.IsNullOrEmpty(trangThai))
                query = query.Where(s => s.TrangThai == trangThai);

            // FIX DYNAMIC QUERY THEO KHÓA HỌC:
            // Nếu maKhoaHoc rỗng => không lọc khóa, hiển thị tất cả.
            // Nếu có maKhoaHoc => lọc theo cột LopHoc.KhoaHoc.
            if (!string.IsNullOrEmpty(maKhoaHoc))
            {
                query = query.Where(s =>
                    s.MaLopNavigation.KhoaHoc == maKhoaHoc ||
                    s.MaLopNavigation.TenLop.Contains(maKhoaHoc) ||
                    s.MaLopNavigation.TenLop.Contains("Khóa " + maKhoaHoc));
            }

            int tongSo = await query.CountAsync();

            var danhSach = await query
                .OrderBy(s => s.MaLopNavigation.MaKhoa)
                .ThenByDescending(s => s.MaLopNavigation.KhoaHoc)
                .ThenBy(s => s.MaLop)
                .ThenBy(s => s.Mssv)
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
            ViewBag.DanhSachLop = await _db.LopHocs
                .OrderBy(l => l.MaKhoa)
                .ThenByDescending(l => l.KhoaHoc)
                .ThenBy(l => l.TenLop)
                .ToListAsync();

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
        public async Task<IActionResult> Create(SinhVien sv, string? khoaHoc)
        {
            var maKhoaHoc = ChuanHoaMaKhoaHoc(khoaHoc);

            sv.HoTen = ChuanHoaHoTen(sv.HoTen) ?? "";
            ValidateHoTenSinhVien(ModelState, sv.HoTen);

            if (string.IsNullOrWhiteSpace(maKhoaHoc))
                ModelState.AddModelError("khoaHoc", "Vui lòng chọn khóa học.");

            if (await _db.SinhViens.AnyAsync(s => s.Mssv == sv.Mssv))
                ModelState.AddModelError("Mssv", "MSSV này đã tồn tại trong hệ thống.");

            if (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == sv.Mssv))
                ModelState.AddModelError("Mssv", "Mã sinh viên này đã được cấp tài khoản trước đó.");

            ValidateNgaySinhSinhVien(ModelState, sv.NgaySinh);

            sv.SoDienThoai = string.IsNullOrWhiteSpace(sv.SoDienThoai) ? null : sv.SoDienThoai.Trim();
            ValidateSoDienThoaiSinhVien(ModelState, sv.SoDienThoai);

            var lopDuocChon = await _db.LopHocs.FirstOrDefaultAsync(l => l.MaLop == sv.MaLop);

            if (lopDuocChon == null)
            {
                ModelState.AddModelError("MaLop", "Vui lòng chọn lớp học hợp lệ.");
            }
            else if (!string.IsNullOrWhiteSpace(maKhoaHoc) &&
                     lopDuocChon.KhoaHoc != maKhoaHoc &&
                     !lopDuocChon.TenLop.Contains(maKhoaHoc) &&
                     !lopDuocChon.TenLop.Contains("Khóa " + maKhoaHoc))
            {
                ModelState.AddModelError("MaLop", "Lớp học đã chọn không thuộc khóa học đã chọn.");
            }

            ModelState.Remove("TrangThai");
            ModelState.Remove("MaLopNavigation");
            ModelState.Remove("MaTaiKhoanNavigation");

            if (ModelState.IsValid)
            {
                using var transaction = await _db.Database.BeginTransactionAsync();
                try
                {
                    var tk = new TaiKhoan
                    {
                        TenDangNhap = sv.Mssv,
                        MatKhauHash = BCrypt.Net.BCrypt.HashPassword(sv.Mssv),
                        VaiTro = "SinhVien",
                        TrangThai = true,
                        LanDangNhapSai = 0
                    };

                    _db.TaiKhoans.Add(tk);
                    await _db.SaveChangesAsync();

                    sv.MaTaiKhoan = tk.MaTaiKhoan;
                    sv.TrangThai = "Đang học";
                    _db.SinhViens.Add(sv);
                    await _db.SaveChangesAsync();

                    await transaction.CommitAsync();

                    TempData["Success"] = $"Đã thêm sinh viên {sv.HoTen}. Tài khoản và mật khẩu mặc định: {sv.Mssv}";
                    return RedirectToAction("Index", new { khoaHoc = maKhoaHoc, maLop = sv.MaLop });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Có lỗi xảy ra khi lưu vào cơ sở dữ liệu: " + ex.Message);
                }
            }

            ViewBag.KhoaHoc = maKhoaHoc;

            ViewBag.DanhSachLop = await _db.LopHocs
                .OrderBy(l => l.MaKhoa)
                .ThenByDescending(l => l.KhoaHoc)
                .ThenBy(l => l.TenLop)
                .ToListAsync();

            ViewBag.DanhSachKhoa = await _db.Khoas
                .OrderBy(k => k.TenKhoa)
                .ToListAsync();

            ViewBag.DSKhoaHoc = await _db.KhoaHocs
                .OrderByDescending(k => k.NamBatDau)
                .Select(k => k.MaKhoaHoc)
                .ToListAsync();

            return View(sv);
        }

        [Authorize(Roles = "Admin,CanBo")]
        public async Task<IActionResult> Edit(string id)
        {
            var sv = await _db.SinhViens.FindAsync(id);
            if (sv == null) return RedirectToAction("Index");

            ViewBag.DanhSachLop = await _db.LopHocs
                .OrderBy(l => l.MaKhoa)
                .ThenByDescending(l => l.KhoaHoc)
                .ThenBy(l => l.TenLop)
                .ToListAsync();

            ViewBag.DanhSachKhoa = await _db.Khoas
                .OrderBy(k => k.TenKhoa)
                .ToListAsync();

            ViewBag.DSKhoaHoc = await _db.KhoaHocs
                .OrderByDescending(k => k.NamBatDau)
                .Select(k => k.MaKhoaHoc)
                .ToListAsync();

            return View(sv);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,CanBo")]
        public async Task<IActionResult> Edit(SinhVien sv)
        {
            sv.HoTen = ChuanHoaHoTen(sv.HoTen) ?? "";
            ValidateHoTenSinhVien(ModelState, sv.HoTen);

            ValidateNgaySinhSinhVien(ModelState, sv.NgaySinh);

            sv.SoDienThoai = string.IsNullOrWhiteSpace(sv.SoDienThoai) ? null : sv.SoDienThoai.Trim();
            ValidateSoDienThoaiSinhVien(ModelState, sv.SoDienThoai);

            ModelState.Remove("TrangThai");
            ModelState.Remove("MaLopNavigation");
            ModelState.Remove("MaTaiKhoanNavigation");

            if (ModelState.IsValid)
            {
                _db.SinhViens.Update(sv);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Cập nhật thông tin {sv.HoTen} thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.DanhSachLop = await _db.LopHocs
                .OrderBy(l => l.MaKhoa)
                .ThenByDescending(l => l.KhoaHoc)
                .ThenBy(l => l.TenLop)
                .ToListAsync();

            ViewBag.DanhSachKhoa = await _db.Khoas
                .OrderBy(k => k.TenKhoa)
                .ToListAsync();

            ViewBag.DSKhoaHoc = await _db.KhoaHocs
                .OrderByDescending(k => k.NamBatDau)
                .Select(k => k.MaKhoaHoc)
                .ToListAsync();

            return View(sv);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var sv = await _db.SinhViens.FindAsync(id);
            if (sv == null) return RedirectToAction("Index");

            _db.SinhViens.Remove(sv);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã xoá sinh viên {sv.HoTen}.";
            return RedirectToAction("Index");
        }

        // 🛑 3. KHÓA PHÒNG CHI TIẾT: Cấm Sinh viên vô xem lén hồ sơ của sinh viên khác!
        [Authorize(Roles = "Admin,CanBo,GiangVien")]
        public async Task<IActionResult> Detail(string id)
        {
            var sv = await _db.SinhViens
                .Include(s => s.MaLopNavigation)
                    .ThenInclude(l => l.MaKhoaNavigation)
                .FirstOrDefaultAsync(s => s.Mssv == id);

            if (sv == null) return RedirectToAction("Index");
            return View(sv);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,CanBo")]
        public async Task<IActionResult> CapTaiKhoan(string mssv)
        {
            var sv = await _db.SinhViens.FirstOrDefaultAsync(s => s.Mssv == mssv);

            if (sv == null)
            {
                TempData["Error"] = "Không tìm thấy sinh viên.";
                return RedirectToAction("Index");
            }

            if (sv.MaTaiKhoan != null)
            {
                TempData["Error"] = $"Sinh viên {sv.HoTen} đã có tài khoản rồi!";
                return RedirectToAction("Index");
            }

            if (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == mssv))
            {
                TempData["Error"] = $"Tên đăng nhập {mssv} đã bị sử dụng bởi người khác.";
                return RedirectToAction("Index");
            }

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var tk = new TaiKhoan
                {
                    TenDangNhap = mssv,
                    MatKhauHash = BCrypt.Net.BCrypt.HashPassword(mssv),
                    VaiTro = "SinhVien",
                    TrangThai = true,
                    LanDangNhapSai = 0
                };

                _db.TaiKhoans.Add(tk);
                await _db.SaveChangesAsync();

                sv.MaTaiKhoan = tk.MaTaiKhoan;
                _db.SinhViens.Update(sv);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["Success"] = $"Đã cấp tài khoản thành công cho {sv.HoTen}. (Tài khoản: {mssv} - Mật khẩu: {mssv})";
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Có lỗi xảy ra khi cấp tài khoản. Vui lòng thử lại.";
            }

            return RedirectToAction("Index");
        }

        // ==========================================
        // 🛑 4. PHÒNG CỦA SINH VIÊN: Chỉ sinh viên mới vào được phòng này
        // ==========================================
        [Authorize(Roles = "SinhVien")]
        public async Task<IActionResult> ThongTin()
        {
            string mssv = User.Identity?.Name;

            if (string.IsNullOrEmpty(mssv))
            {
                return RedirectToAction("Login", "TaiKhoan");
            }

            var sv = await _db.SinhViens
                .Include(s => s.MaLopNavigation)
                .ThenInclude(l => l.MaKhoaNavigation)
                .FirstOrDefaultAsync(s => s.Mssv == mssv);

            if (sv == null)
            {
                return NotFound("Không tìm thấy dữ liệu sinh viên này trong hệ thống.");
            }

            return View(sv);
        }

        // ==========================================
        // DÀNH CHO SINH VIÊN: CẬP NHẬT SĐT VÀ ĐỊA CHỈ
        // ==========================================
        [Authorize(Roles = "SinhVien")]
        public async Task<IActionResult> CapNhatThongTin()
        {
            string mssv = User.Identity?.Name;
            var sv = await _db.SinhViens.FindAsync(mssv);

            if (sv == null) return RedirectToAction("ThongTin");

            return View(sv);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SinhVien")]
        public async Task<IActionResult> CapNhatThongTin(SinhVien model)
        {
            string mssv = User.Identity?.Name;

            if (mssv != model.Mssv) return Forbid();

            var sv = await _db.SinhViens.FindAsync(mssv);
            if (sv == null) return NotFound();

            sv.SoDienThoai = model.SoDienThoai;
            sv.DiaChi = model.DiaChi;

            sv.SoDienThoai = string.IsNullOrWhiteSpace(model.SoDienThoai) ? null : model.SoDienThoai.Trim();
            ValidateSoDienThoaiSinhVien(ModelState, sv.SoDienThoai);

            if (!ModelState.IsValid)
            {
                return View(sv);
            }

            _db.SinhViens.Update(sv);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật thông tin liên lạc thành công!";
            return RedirectToAction("ThongTin");
        }

        // ==========================================
        // DÀNH CHO SINH VIÊN: XEM THỜI KHÓA BIỂU
        // ==========================================
        [Authorize(Roles = "SinhVien")]
        public async Task<IActionResult> ThoiKhoaBieu(string? maHocKy)
        {
            string mssv = User.Identity?.Name;

            var dsHocKy = await _db.HocKies.ToListAsync();
            ViewBag.DanhSachHK = dsHocKy
                .OrderByDescending(h => h.TenHocKy != null && h.TenHocKy.Length >= 9 ? h.TenHocKy.Substring(h.TenHocKy.Length - 9) : h.TenHocKy)
                .ThenByDescending(h => h.TenHocKy)
                .ToList();

            if (string.IsNullOrEmpty(maHocKy))
            {
                var hkHienTai = dsHocKy.FirstOrDefault(h => h.TrangThai == "Đang diễn ra") ?? dsHocKy.FirstOrDefault();
                maHocKy = hkHienTai?.MaHocKy;
            }
            ViewBag.MaHocKy = maHocKy;

            var daDangKy = await _db.DangKyHocs
                .Include(d => d.MaMonNavigation)
                .Include(d => d.MaLhpNavigation)
                    .ThenInclude(l => l.MaGvNavigation)
                .Where(d => d.Mssv == mssv && d.MaHocKy == maHocKy)
                .ToListAsync();

            var listMaLhp = daDangKy.Select(d => d.MaLhp).ToList();
            var listChiTiet = await _db.ChiTietLichHocs
                .Where(c => listMaLhp.Contains(c.MaLhp))
                .ToListAsync();

            ViewBag.ListChiTiet = listChiTiet;

            // 🛑 THUẬT TOÁN MỚI: BẮT CHÍNH XÁC "TỪNG BUỔI" BỊ TRÙNG THAY VÌ BẮT NGUYÊN MÔN
            var listBuoiTrung = new List<string>();

            foreach (var item in daDangKy)
            {
                var lhpNay = item.MaLhpNavigation;
                if (lhpNay == null) continue;

                var cacBuoiHocLopNay = listChiTiet.Where(c => c.MaLhp == lhpNay.MaLhp).ToList();
                if (!cacBuoiHocLopNay.Any() && lhpNay.Thu >= 2 && lhpNay.TietBatDau >= 1)
                    cacBuoiHocLopNay.Add(new ChiTietLichHoc { Thu = lhpNay.Thu, TietBatDau = lhpNay.TietBatDau, SoTiet = lhpNay.SoTiet });

                var cacLopKhac = daDangKy.Where(d => d.MaDangKy != item.MaDangKy).ToList();

                foreach (var lopKhac in cacLopKhac)
                {
                    var lhpKhac = lopKhac.MaLhpNavigation;
                    if (lhpKhac == null) continue;

                    var cacBuoiHocLopKhac = listChiTiet.Where(c => c.MaLhp == lhpKhac.MaLhp).ToList();
                    if (!cacBuoiHocLopKhac.Any() && lhpKhac.Thu >= 2 && lhpKhac.TietBatDau >= 1)
                        cacBuoiHocLopKhac.Add(new ChiTietLichHoc { Thu = lhpKhac.Thu, TietBatDau = lhpKhac.TietBatDau, SoTiet = lhpKhac.SoTiet });

                    foreach (var buoiNay in cacBuoiHocLopNay)
                    {
                        if (buoiNay.Thu < 2 || buoiNay.TietBatDau < 1) continue;

                        foreach (var buoiKhac in cacBuoiHocLopKhac)
                        {
                            if (buoiKhac.Thu < 2 || buoiKhac.TietBatDau < 1) continue;

                            // Trùng giờ và trùng ngày
                            if (buoiNay.Thu == buoiKhac.Thu &&
                                buoiNay.TietBatDau <= (buoiKhac.TietBatDau + buoiKhac.SoTiet - 1) &&
                                buoiKhac.TietBatDau <= (buoiNay.TietBatDau + buoiNay.SoTiet - 1))
                            {
                                // Lưu lại Mã LHP + Thứ + Tiết Bắt Đầu để định vị chính xác ô bị lỗi
                                string keyTrung = $"{lhpNay.MaLhp}_{buoiNay.Thu}_{buoiNay.TietBatDau}";
                                if (!listBuoiTrung.Contains(keyTrung))
                                {
                                    listBuoiTrung.Add(keyTrung);
                                }
                            }
                        }
                    }
                }
            }
            ViewBag.ListBuoiTrung = listBuoiTrung; // Gửi danh sách các Ô bị lỗi sang View

            return View(daDangKy);
        }

        // ==========================================
        // 🛑 API LẤY DANH SÁCH LỚP THEO KHOA VÀ KHÓA DÀNH CHO BỘ LỌC AJAX
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,CanBo,GiangVien")]
        [ActionName("GetLopByKhoa")]
        public async Task<IActionResult> GetLopByKhoaAsync(string? maKhoa, string? khoaHoc)
        {
            var query = LocLopTheoKhoaVaKhoaHoc(_db.LopHocs.AsQueryable(), maKhoa, khoaHoc);

            var dsLop = await query
                .OrderBy(l => l.MaKhoa)
                .ThenByDescending(l => l.KhoaHoc)
                .ThenBy(l => l.TenLop)
                .Select(l => new
                {
                    maLop = l.MaLop,
                    tenLop = l.TenLop,
                    maKhoa = l.MaKhoa,
                    khoaHoc = l.KhoaHoc
                })
                .ToListAsync();

            return Json(dsLop);
        }
    }
}