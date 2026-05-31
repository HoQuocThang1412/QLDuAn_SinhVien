using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuanLySinhVien.Controllers
{
    public class HocPhiController : BaseController
    {
        public HocPhiController(QuanLySinhVienContext db) : base(db) { }

        private static bool DonGiaHopLe(DonGiaHocPhi donGia, out string error)
        {
            error = "";

            if (donGia.SoTienMotTinChi <= 0)
            {
                error = "Số tiền một tín chỉ phải lớn hơn 0.";
                return false;
            }

            if (donGia.SoTienMotTinChi > 10000000)
            {
                error = "Số tiền một tín chỉ không được vượt quá 10.000.000.";
                return false;
            }

            return true;
        }

        private static string ChuanHoaMaKhoa(string? maKhoa)
        {
            return string.IsNullOrWhiteSpace(maKhoa)
                ? ""
                : maKhoa.Trim().ToUpper();
        }

        private static string ChuanHoaKhoaHoc(string? khoaHoc)
        {
            if (string.IsNullOrWhiteSpace(khoaHoc))
                return "";

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

        private static string LayKhoaHocTuMssv(string? mssv)
        {
            if (string.IsNullOrWhiteSpace(mssv))
                return "";

            var match = Regex.Match(mssv.Trim(), @"^(\d{2})");

            if (!match.Success)
                return "";

            return "K" + match.Groups[1].Value;
        }

        private static string LayKhoaHocSinhVien(SinhVien sinhVien)
        {
            string khoaHocTuLop = ChuanHoaKhoaHoc(sinhVien.MaLopNavigation?.KhoaHoc);

            if (!string.IsNullOrEmpty(khoaHocTuLop))
                return khoaHocTuLop;

            string tenLop = sinhVien.MaLopNavigation?.TenLop ?? "";
            var matchTenLop = Regex.Match(tenLop, @"K\d{2}", RegexOptions.IgnoreCase);

            if (matchTenLop.Success)
                return ChuanHoaKhoaHoc(matchTenLop.Value);

            return LayKhoaHocTuMssv(sinhVien.Mssv);
        }

        private async Task<DonGiaHocPhi?> LayDonGiaChoSinhVienAsync(SinhVien sinhVien)
        {
            string maKhoaSinhVien = ChuanHoaMaKhoa(sinhVien.MaLopNavigation?.MaKhoa);
            string khoaHocSinhVien = LayKhoaHocSinhVien(sinhVien);

            var dsDonGia = await _db.DonGiaHocPhis.ToListAsync();

            // 1. Match chuẩn: đúng khoa + đúng khóa
            var donGia = dsDonGia.FirstOrDefault(d =>
                ChuanHoaMaKhoa(d.MaKhoa) == maKhoaSinhVien &&
                ChuanHoaKhoaHoc(d.KhoaHoc) == khoaHocSinhVien);

            if (donGia != null)
                return donGia;

            // 2. Fallback: nếu không match khoa, lấy theo khóa học
            // Ví dụ SV là K45, bảng đơn giá có CNTT - K45
            donGia = dsDonGia.FirstOrDefault(d =>
                ChuanHoaKhoaHoc(d.KhoaHoc) == khoaHocSinhVien);

            if (donGia != null)
                return donGia;

            // 3. Fallback cuối: lấy theo khoa
            donGia = dsDonGia.FirstOrDefault(d =>
                ChuanHoaMaKhoa(d.MaKhoa) == maKhoaSinhVien);

            return donGia;
        }

        private async Task<HocKy?> LayHocKyHienTaiEntityAsync(string? maHocKy = null)
        {
            if (!string.IsNullOrWhiteSpace(maHocKy))
            {
                return await _db.HocKies.FirstOrDefaultAsync(h => h.MaHocKy == maHocKy);
            }

            var hkDangDienRa = await _db.HocKies
                .Where(h => h.TrangThai == "Đang diễn ra")
                .OrderByDescending(h => h.NgayBatDau)
                .FirstOrDefaultAsync();

            if (hkDangDienRa != null)
            {
                return hkDangDienRa;
            }

            return await _db.HocKies
                .OrderByDescending(h => h.NgayBatDau)
                .FirstOrDefaultAsync();
        }

        private async Task<string> LayHocKyHienTaiAsync(string? maHocKy = null)
        {
            var hk = await LayHocKyHienTaiEntityAsync(maHocKy);
            return hk?.MaHocKy ?? "";
        }

        private async Task<decimal> TinhConNoHocPhiAsync(string mssv, string? maHocKy = null)
        {
            var sinhVien = await _db.SinhViens
                .Include(s => s.MaLopNavigation)
                .FirstOrDefaultAsync(s => s.Mssv == mssv);

            if (sinhVien == null)
            {
                return 0;
            }

            var donGia = await LayDonGiaChoSinhVienAsync(sinhVien);
            decimal giaTinChi = donGia?.SoTienMotTinChi ?? 0;

            string maHocKyCanTinh = await LayHocKyHienTaiAsync(maHocKy);

            if (string.IsNullOrEmpty(maHocKyCanTinh))
            {
                return 0;
            }

            var dsDangKy = await _db.DangKyHocs
                .Include(d => d.MaLhpNavigation)
                    .ThenInclude(l => l.MaMonNavigation)
                .Include(d => d.MaMonNavigation)
                .Where(d => d.Mssv == mssv && d.MaHocKy == maHocKyCanTinh)
                .ToListAsync();

            int tongTinChi = dsDangKy.Sum(d =>
                d.MaMonNavigation?.SoTinChi
                ?? d.MaLhpNavigation?.MaMonNavigation?.SoTinChi
                ?? 0);

            decimal tongPhaiDong = tongTinChi * giaTinChi;

            decimal tongDaDong = await _db.ThanhToanHocPhis
                .Where(t => t.Mssv == mssv && t.MaHocKy == maHocKyCanTinh)
                .SumAsync(t => t.SoTienDong);

            decimal conNo = tongPhaiDong - tongDaDong;

            return conNo > 0 ? conNo : 0;
        }

        [Authorize(Roles = "Admin,CanBo")]
        public async Task<IActionResult> QuanLyDonGia()
        {
            await LoadKhoaHocDropdown();

            var dsDonGia = await _db.DonGiaHocPhis
                .OrderByDescending(d => d.KhoaHoc)
                .ToListAsync();

            var configQR = await _db.CauHinhQRs.FirstOrDefaultAsync(c => c.Id == 1);

            ViewBag.DSKhoa = await _db.Khoas.ToListAsync();

            ViewBag.DSKhoaHoc = await _db.KhoaHocs
                .OrderByDescending(k => k.NamBatDau)
                .Select(k => k.MaKhoaHoc)
                .ToListAsync();

            ViewBag.DSNganHang = new List<dynamic>
            {
                new { Ma = "MB", Ten = "MB Bank" },
                new { Ma = "VCB", Ten = "Vietcombank" },
                new { Ma = "ICB", Ten = "VietinBank" },
                new { Ma = "BIDV", Ten = "BIDV" },
                new { Ma = "ACB", Ten = "ACB" }
            };

            ViewBag.ConfigQR = configQR;

            return View(dsDonGia);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,CanBo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuCaiDatQR(CauHinhQR config)
        {
            var existing = await _db.CauHinhQRs.FirstOrDefaultAsync(c => c.Id == 1);

            if (existing != null)
            {
                existing.TenNganHang = config.TenNganHang;
                existing.MaNganHang = config.MaNganHang;
                existing.SoTaiKhoan = config.SoTaiKhoan;
                existing.TenChuTaiKhoan = config.TenChuTaiKhoan;
                _db.Update(existing);
            }
            else
            {
                config.Id = 1;
                _db.CauHinhQRs.Add(config);
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã lưu cấu hình tài khoản nhận tiền!";
            return RedirectToAction(nameof(QuanLyDonGia));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,CanBo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemDonGia(DonGiaHocPhi donGia)
        {
            donGia.MaKhoa = ChuanHoaMaKhoa(donGia.MaKhoa);
            donGia.KhoaHoc = ChuanHoaKhoaHoc(donGia.KhoaHoc);

            if (!DonGiaHopLe(donGia, out string error))
            {
                TempData["Error"] = error;
                return RedirectToAction(nameof(QuanLyDonGia));
            }

            var dsDonGia = await _db.DonGiaHocPhis.ToListAsync();

            bool daTonTai = dsDonGia.Any(d =>
                ChuanHoaMaKhoa(d.MaKhoa) == donGia.MaKhoa &&
                ChuanHoaKhoaHoc(d.KhoaHoc) == donGia.KhoaHoc);

            if (daTonTai)
            {
                TempData["Error"] = "Đơn giá cho Khoa và Khóa này đã tồn tại!";
                return RedirectToAction(nameof(QuanLyDonGia));
            }

            _db.DonGiaHocPhis.Add(donGia);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Thêm đơn giá thành công!";
            return RedirectToAction(nameof(QuanLyDonGia));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,CanBo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuaDonGia(DonGiaHocPhi donGia)
        {
            donGia.MaKhoa = ChuanHoaMaKhoa(donGia.MaKhoa);
            donGia.KhoaHoc = ChuanHoaKhoaHoc(donGia.KhoaHoc);

            if (!DonGiaHopLe(donGia, out string error))
            {
                TempData["Error"] = error;
                return RedirectToAction(nameof(QuanLyDonGia));
            }

            var existing = await _db.DonGiaHocPhis.FindAsync(donGia.Id);

            if (existing == null)
            {
                TempData["Error"] = "Không tìm thấy đơn giá cần cập nhật.";
                return RedirectToAction(nameof(QuanLyDonGia));
            }

            var dsDonGia = await _db.DonGiaHocPhis
                .Where(d => d.Id != donGia.Id)
                .ToListAsync();

            bool biTrung = dsDonGia.Any(d =>
                ChuanHoaMaKhoa(d.MaKhoa) == donGia.MaKhoa &&
                ChuanHoaKhoaHoc(d.KhoaHoc) == donGia.KhoaHoc);

            if (biTrung)
            {
                TempData["Error"] = "Đơn giá cho Khoa và Khóa này đã tồn tại!";
                return RedirectToAction(nameof(QuanLyDonGia));
            }

            existing.MaKhoa = donGia.MaKhoa;
            existing.KhoaHoc = donGia.KhoaHoc;
            existing.SoTienMotTinChi = donGia.SoTienMotTinChi;
            existing.GhiChu = donGia.GhiChu;

            _db.Update(existing);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Cập nhật đơn giá thành công!";
            return RedirectToAction(nameof(QuanLyDonGia));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,CanBo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaDonGia(int id)
        {
            var donGia = await _db.DonGiaHocPhis.FindAsync(id);

            if (donGia != null)
            {
                _db.DonGiaHocPhis.Remove(donGia);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã xóa đơn giá!";
            }

            return RedirectToAction(nameof(QuanLyDonGia));
        }

        [Authorize(Roles = "SinhVien")]
        public async Task<IActionResult> ThongTinHocPhi(string? maHocKy)
        {
            string mssv = User.Identity?.Name ?? "";

            var sinhVien = await _db.SinhViens
                .Include(s => s.MaLopNavigation)
                .FirstOrDefaultAsync(s => s.Mssv == mssv);

            if (sinhVien == null)
            {
                return NotFound();
            }

            var dsHocKy = await _db.HocKies
                .OrderByDescending(h => h.NgayBatDau)
                .ToListAsync();

            var hocKyDangChon = await LayHocKyHienTaiEntityAsync(maHocKy);

            if (hocKyDangChon == null)
            {
                ViewBag.SinhVien = sinhVien;
                ViewBag.DanhSachHK = dsHocKy;
                ViewBag.MaHocKy = "";
                ViewBag.TenHocKy = "Chưa có học kỳ";
                ViewBag.GiaMotTinChi = 0m;
                ViewBag.TongTinChi = 0;
                ViewBag.TongPhaiDong = 0m;
                ViewBag.TongDaDong = 0m;
                ViewBag.ConNo = 0m;
                ViewBag.Mssv = mssv;
                ViewBag.MaKhoaSinhVien = ChuanHoaMaKhoa(sinhVien.MaLopNavigation?.MaKhoa);
                ViewBag.KhoaHocSinhVien = LayKhoaHocSinhVien(sinhVien);

                return View(new List<DangKyHoc>());
            }

            string maHocKyDangChon = hocKyDangChon.MaHocKy;

            var donGia = await LayDonGiaChoSinhVienAsync(sinhVien);
            decimal giaTinChi = donGia?.SoTienMotTinChi ?? 0;
            ViewBag.DebugMaKhoaSinhVien = ChuanHoaMaKhoa(sinhVien.MaLopNavigation?.MaKhoa);
            ViewBag.DebugKhoaHocSinhVien = LayKhoaHocSinhVien(sinhVien);
            ViewBag.DebugDonGiaTimDuoc = donGia == null
                ? "Không tìm thấy"
                : $"{donGia.MaKhoa} - {donGia.KhoaHoc} - {donGia.SoTienMotTinChi:N0}";

            ViewBag.DebugMaKhoaSinhVien = ChuanHoaMaKhoa(sinhVien.MaLopNavigation?.MaKhoa);
            ViewBag.DebugKhoaHocSinhVien = LayKhoaHocSinhVien(sinhVien);
            ViewBag.DebugDonGiaTimDuoc = donGia == null
                ? "Không tìm thấy"
                : $"{donGia.MaKhoa} - {donGia.KhoaHoc} - {donGia.SoTienMotTinChi:N0}";

            var dsDangKy = await _db.DangKyHocs
                .Include(d => d.MaLhpNavigation)
                    .ThenInclude(l => l.MaMonNavigation)
                .Include(d => d.MaMonNavigation)
                .Where(d => d.Mssv == mssv && d.MaHocKy == maHocKyDangChon)
                .OrderBy(d => d.MaMonNavigation != null ? d.MaMonNavigation.TenMon : "")
                .ThenBy(d => d.MaLhp)
                .AsNoTracking()
                .ToListAsync();

            decimal tongDaDong = await _db.ThanhToanHocPhis
                .Where(t => t.Mssv == mssv && t.MaHocKy == maHocKyDangChon)
                .SumAsync(t => t.SoTienDong);

            int tongTinChi = dsDangKy.Sum(d =>
                d.MaMonNavigation?.SoTinChi
                ?? d.MaLhpNavigation?.MaMonNavigation?.SoTinChi
                ?? 0);

            decimal tongPhaiDong = tongTinChi * giaTinChi;
            decimal conNo = tongPhaiDong - tongDaDong;

            ViewBag.SinhVien = sinhVien;
            ViewBag.DanhSachHK = dsHocKy;
            ViewBag.MaHocKy = maHocKyDangChon;
            ViewBag.TenHocKy = hocKyDangChon.TenHocKy;
            ViewBag.GiaMotTinChi = giaTinChi;
            ViewBag.TongTinChi = tongTinChi;
            ViewBag.TongPhaiDong = tongPhaiDong;
            ViewBag.TongDaDong = tongDaDong;
            ViewBag.ConNo = conNo > 0 ? conNo : 0;
            ViewBag.Mssv = mssv;
            ViewBag.MaKhoaSinhVien = ChuanHoaMaKhoa(sinhVien.MaLopNavigation?.MaKhoa);
            ViewBag.KhoaHocSinhVien = LayKhoaHocSinhVien(sinhVien);
            ViewBag.DaCoDonGia = donGia != null;

            return View(dsDangKy);
        }

        [HttpGet]
        [Authorize(Roles = "SinhVien")]
        public async Task<IActionResult> XacNhanThanhToan(decimal soTien, string? maHocKy)
        {
            string mssv = User.Identity?.Name ?? "";
            string maHocKyThanhToan = await LayHocKyHienTaiAsync(maHocKy);
            decimal conNo = await TinhConNoHocPhiAsync(mssv, maHocKyThanhToan);

            if (soTien <= 0)
            {
                TempData["Error"] = "Số tiền thanh toán phải lớn hơn 0.";
                return RedirectToAction(nameof(ThongTinHocPhi), new { maHocKy = maHocKyThanhToan });
            }

            if (soTien > conNo)
            {
                TempData["Error"] = $"Số tiền thanh toán không được vượt quá công nợ học kỳ hiện tại ({conNo:N0} đ).";
                return RedirectToAction(nameof(ThongTinHocPhi), new { maHocKy = maHocKyThanhToan });
            }

            var config = await _db.CauHinhQRs.FirstOrDefaultAsync(c => c.Id == 1);

            if (config == null)
            {
                return Content("Hệ thống chưa cấu hình tài khoản ngân hàng nhận tiền!");
            }

            ViewBag.SoTien = soTien;
            ViewBag.Mssv = mssv;
            ViewBag.MaHocKy = maHocKyThanhToan;

            return View(config);
        }

        [HttpPost]
        [Authorize(Roles = "SinhVien")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HoanTatThanhToan(decimal soTien, string? maHocKy)
        {
            string mssv = User.Identity?.Name ?? "";
            string maHocKyThanhToan = await LayHocKyHienTaiAsync(maHocKy);
            decimal conNo = await TinhConNoHocPhiAsync(mssv, maHocKyThanhToan);

            if (soTien <= 0)
            {
                TempData["Error"] = "Số tiền thanh toán phải lớn hơn 0.";
                return RedirectToAction(nameof(ThongTinHocPhi), new { maHocKy = maHocKyThanhToan });
            }

            if (soTien > conNo)
            {
                TempData["Error"] = $"Số tiền thanh toán không được vượt quá công nợ học kỳ hiện tại ({conNo:N0} đ).";
                return RedirectToAction(nameof(ThongTinHocPhi), new { maHocKy = maHocKyThanhToan });
            }

            _db.ThanhToanHocPhis.Add(new ThanhToanHocPhi
            {
                Mssv = mssv,
                MaHocKy = maHocKyThanhToan,
                SoTienDong = soTien,
                NgayDong = DateTime.Now,
                HinhThuc = "Chuyển khoản QR VietQR"
            });

            await _db.SaveChangesAsync();

            TempData["Success"] = "Giao dịch đã được ghi nhận!";
            return RedirectToAction(nameof(ThongTinHocPhi), new { maHocKy = maHocKyThanhToan });
        }
    }
}