using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLySinhVien.Controllers
{
    [Authorize]
    public class DiemController : Controller
    {
        private readonly QuanLySinhVienContext _db;

        public DiemController(QuanLySinhVienContext db) => _db = db;

        private async Task<decimal> TinhConNoHocKyAsync(string mssv, string maHocKy)
        {
            var sinhVien = await _db.SinhViens
                .Include(s => s.MaLopNavigation)
                .FirstOrDefaultAsync(s => s.Mssv == mssv);

            if (sinhVien == null)
                return 0;

            string maKhoa = sinhVien.MaLopNavigation?.MaKhoa ?? "";
            string khoaHoc = sinhVien.MaLopNavigation?.KhoaHoc ?? "";

            var donGia = await _db.DonGiaHocPhis
                .FirstOrDefaultAsync(d => d.MaKhoa == maKhoa && d.KhoaHoc == khoaHoc);

            decimal giaTinChi = donGia?.SoTienMotTinChi ?? 0;

            var dsDangKy = await _db.DangKyHocs
                .Include(d => d.MaLhpNavigation)
                    .ThenInclude(l => l.MaMonNavigation)
                .Where(d => d.Mssv == mssv && d.MaHocKy == maHocKy)
                .ToListAsync();

            int tongTinChi = dsDangKy.Sum(d => d.MaLhpNavigation?.MaMonNavigation?.SoTinChi ?? 0);
            decimal tongPhaiDong = tongTinChi * giaTinChi;

            decimal tongDaDong = await _db.ThanhToanHocPhis
                .Where(t => t.Mssv == mssv && t.MaHocKy == maHocKy)
                .SumAsync(t => t.SoTienDong);

            decimal conNo = tongPhaiDong - tongDaDong;

            return conNo > 0 ? conNo : 0;
        }
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("SinhVien"))
            {
                return RedirectToAction("TraCuu");
            }

            var lhpQuery = _db.LopHocPhans
                .Include(l => l.MaMonNavigation)
                .AsQueryable();

            if (User.IsInRole("GiangVien"))
            {
                string? username = User.Identity?.Name;
                lhpQuery = lhpQuery.Where(l => l.MaGv == username);
            }

            var danhSachLop = await lhpQuery
                .OrderByDescending(l => l.MaHocKy)
                .ToListAsync();

            return View(danhSachLop);
        }

        [Authorize(Roles = "Admin,CanBo,GiangVien")]
        public async Task<IActionResult> NhapDiem(string? maHocKy, string? maLhp)
        {
            ViewBag.MaHocKy = maHocKy;
            ViewBag.MaLhp = maLhp;

            var dsHocKy = await _db.HocKies.ToListAsync();
            ViewBag.DanhSachHK = dsHocKy
                .OrderByDescending(h => h.TenHocKy != null && h.TenHocKy.Length >= 9
                    ? h.TenHocKy.Substring(h.TenHocKy.Length - 9)
                    : h.TenHocKy)
                .ThenByDescending(h => h.TenHocKy)
                .ToList();

            var lhpQuery = _db.LopHocPhans
                .Include(l => l.MaMonNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(maHocKy))
            {
                lhpQuery = lhpQuery.Where(l => l.MaHocKy == maHocKy);
            }

            if (User.IsInRole("GiangVien"))
            {
                string? username = User.Identity?.Name;
                lhpQuery = lhpQuery.Where(l => l.MaGv == username);
            }

            ViewBag.DanhSachLhp = await lhpQuery.Select(l => new SelectListItem
            {
                Value = l.MaLhp,
                Text = $"{l.MaMonNavigation.TenMon} (Mã: {l.MaLhp})"
            }).ToListAsync();

            if (string.IsNullOrEmpty(maLhp))
                return View(new List<DangKyHoc>());

            var lopHocPhanDangChon = await _db.LopHocPhans
                .Include(l => l.MaMonNavigation)
                .FirstOrDefaultAsync(l => l.MaLhp == maLhp);

            if (lopHocPhanDangChon != null)
            {
                ViewBag.TenMon = lopHocPhanDangChon.MaMonNavigation?.TenMon;
                ViewBag.HeSoQt = lopHocPhanDangChon.MaMonNavigation?.HeSoQt;
                ViewBag.HeSoCk = lopHocPhanDangChon.MaMonNavigation?.HeSoCk;

                if (string.IsNullOrEmpty(maHocKy))
                {
                    ViewBag.MaHocKy = lopHocPhanDangChon.MaHocKy;
                }
            }

            var dsSinhVien = await _db.DangKyHocs
                .Include(d => d.MssvNavigation)
                .Include(d => d.KetQuaHocTap)
                .Where(d => d.MaLhp == maLhp)
                .OrderBy(d => d.Mssv)
                .ToListAsync();

            return View(dsSinhVien);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,CanBo,GiangVien")]
        public async Task<IActionResult> LuuDiem(string maHocKy, string maLhp, List<KetQuaHocTap> points)
        {
            var lhp = await _db.LopHocPhans
                .Include(l => l.MaMonNavigation)
                .FirstOrDefaultAsync(l => l.MaLhp == maLhp);

            if (lhp == null) return NotFound();

            double hsQt = (double)(lhp.MaMonNavigation?.HeSoQt ?? 0.3m);
            double hsCk = (double)(lhp.MaMonNavigation?.HeSoCk ?? 0.7m);

            foreach (var item in points)
            {
                if (item.DiemQt == null && item.DiemThi == null)
                {
                    var existingNull = await _db.KetQuaHocTaps
                        .FirstOrDefaultAsync(k => k.MaDangKy == item.MaDangKy);

                    if (existingNull != null)
                    {
                        _db.KetQuaHocTaps.Remove(existingNull);
                    }

                    continue;
                }

                decimal? diemQtDaLamTron = item.DiemQt.HasValue
                    ? Math.Round(item.DiemQt.Value, 1, MidpointRounding.AwayFromZero)
                    : null;

                decimal? diemThiDaLamTron = item.DiemThi.HasValue
                    ? Math.Round(item.DiemThi.Value, 1, MidpointRounding.AwayFromZero)
                    : null;

                item.DiemQt = diemQtDaLamTron;
                item.DiemThi = diemThiDaLamTron;

                double qt = (double)(diemQtDaLamTron ?? 0);
                double thi = (double)(diemThiDaLamTron ?? 0);
                double tk = Math.Round((qt * hsQt) + (thi * hsCk), 1, MidpointRounding.AwayFromZero);

                if (thi < 1.0) tk = 0.0;

                item.DiemTongKet = (decimal)tk;

                if (tk >= 9.0) item.XepLoai = "Xuất sắc";
                else if (tk >= 8.0) item.XepLoai = "Giỏi";
                else if (tk >= 7.0) item.XepLoai = "Khá";
                else if (tk >= 5.0) item.XepLoai = "Trung bình";
                else if (tk >= 4.0) item.XepLoai = "Yếu";
                else item.XepLoai = "Kém";

                item.QuaMon = tk >= 4.0 && thi >= 1.0;

                var existing = await _db.KetQuaHocTaps
                    .FirstOrDefaultAsync(k => k.MaDangKy == item.MaDangKy);

                if (existing != null)
                {
                    existing.DiemQt = item.DiemQt;
                    existing.DiemThi = item.DiemThi;
                    existing.DiemTongKet = item.DiemTongKet;
                    existing.XepLoai = item.XepLoai;
                    existing.QuaMon = item.QuaMon;
                    _db.Update(existing);
                }
                else
                {
                    _db.KetQuaHocTaps.Add(item);
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã lưu điểm thành công!";

            return RedirectToAction("NhapDiem", new { maHocKy, maLhp });
        }

        [Authorize(Roles = "SinhVien")]
        public async Task<IActionResult> TraCuu(string? maHocKy)
        {
            string? mssv = User.Identity?.Name;

            var dsHocKy = await _db.HocKies.ToListAsync();

            ViewBag.DanhSachHK = dsHocKy
                .OrderByDescending(h => h.TenHocKy != null && h.TenHocKy.Length >= 9
                    ? h.TenHocKy.Substring(h.TenHocKy.Length - 9)
                    : h.TenHocKy)
                .ThenByDescending(h => h.TenHocKy)
                .ToList();

            ViewBag.MaHocKyDangChon = maHocKy;

            string? maHocKyCanCheck = maHocKy;

            if (string.IsNullOrEmpty(maHocKyCanCheck))
            {
                maHocKyCanCheck = dsHocKy
                    .FirstOrDefault(h => h.TrangThai == "Đang diễn ra")?.MaHocKy;
            }

            if (!string.IsNullOrEmpty(mssv) && !string.IsNullOrEmpty(maHocKyCanCheck))
            {
                decimal conNo = await TinhConNoHocKyAsync(mssv, maHocKyCanCheck);

                if (conNo > 0)
                {
                    ViewBag.KhoaBangDiem = true;
                    ViewBag.ConNoHocPhi = conNo;
                    return View(new List<DangKyHoc>());
                }
            }

            var tatCaMonDaHoc = await _db.DangKyHocs
                .Include(d => d.MaMonNavigation)
                .Include(d => d.MaLhpNavigation)
                    .ThenInclude(l => l.MaHocKyNavigation)
                .Include(d => d.KetQuaHocTap)
                .Where(d => d.Mssv == mssv)
                .AsNoTracking()
                .ToListAsync();

            return View(tatCaMonDaHoc);
        }

        [Authorize(Roles = "Admin,CanBo,GiangVien")]
        public async Task<IActionResult> DanhSach(string? maHocKy, string? maLhp)
        {
            ViewBag.MaHocKy = maHocKy;
            ViewBag.MaLhp = maLhp;

            var dsHocKy = await _db.HocKies.ToListAsync();
            ViewBag.DanhSachHK = dsHocKy
                .OrderByDescending(h => h.TenHocKy != null && h.TenHocKy.Length >= 9
                    ? h.TenHocKy.Substring(h.TenHocKy.Length - 9)
                    : h.TenHocKy)
                .ThenByDescending(h => h.TenHocKy)
                .ToList();

            var lhpQuery = _db.LopHocPhans
                .Include(l => l.MaMonNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(maHocKy))
            {
                lhpQuery = lhpQuery.Where(l => l.MaHocKy == maHocKy);
            }

            if (User.IsInRole("GiangVien"))
            {
                string? username = User.Identity?.Name;
                lhpQuery = lhpQuery.Where(l => l.MaGv == username);
            }

            ViewBag.DanhSachLhp = await lhpQuery.Select(l => new SelectListItem
            {
                Value = l.MaLhp,
                Text = $"{l.MaMonNavigation.TenMon} (Mã: {l.MaLhp})"
            }).ToListAsync();

            if (string.IsNullOrEmpty(maLhp))
                return View(new List<DangKyHoc>());

            var dsSinhVien = await _db.DangKyHocs
                .Include(d => d.MssvNavigation)
                .Include(d => d.KetQuaHocTap)
                .Where(d => d.MaLhp == maLhp)
                .OrderBy(d => d.Mssv)
                .ToListAsync();

            var lhpDangChon = await _db.LopHocPhans
                .Include(l => l.MaMonNavigation)
                .FirstOrDefaultAsync(l => l.MaLhp == maLhp);

            if (lhpDangChon != null)
            {
                ViewBag.TenMon = lhpDangChon.MaMonNavigation?.TenMon;
                ViewBag.HeSoQt = lhpDangChon.MaMonNavigation?.HeSoQt;
                ViewBag.HeSoCk = lhpDangChon.MaMonNavigation?.HeSoCk;

                if (string.IsNullOrEmpty(maHocKy))
                {
                    ViewBag.MaHocKy = lhpDangChon.MaHocKy;
                }
            }

            return View(dsSinhVien);
        }

        [Authorize(Roles = "Admin,CanBo,GiangVien")]
        public async Task<IActionResult> XuatExcel(string maLhp)
        {
            if (string.IsNullOrEmpty(maLhp))
                return RedirectToAction("DanhSach");

            var dsSinhVien = await _db.DangKyHocs
                .Include(d => d.MssvNavigation)
                .Include(d => d.KetQuaHocTap)
                .Where(d => d.MaLhp == maLhp)
                .OrderBy(d => d.Mssv)
                .ToListAsync();

            var lhp = await _db.LopHocPhans
                .Include(l => l.MaMonNavigation)
                .FirstOrDefaultAsync(l => l.MaLhp == maLhp);

            string tenMon = lhp?.MaMonNavigation?.TenMon ?? "KhongXacDinh";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("STT,MSSV,Họ và Tên,Lớp SH,Điểm QT,Điểm Thi,Tổng Kết,Xếp Loại,Trạng thái");

            for (int i = 0; i < dsSinhVien.Count; i++)
            {
                var sv = dsSinhVien[i];
                var kq = sv.KetQuaHocTap;

                string stt = (i + 1).ToString();
                string mssv = sv.Mssv;
                string hoTen = sv.MssvNavigation?.HoTen?.Replace(",", " ") ?? "";
                string lopSh = sv.MssvNavigation?.MaLop ?? "";
                string diemQt = kq?.DiemQt?.ToString("0.0") ?? "";
                string diemThi = kq?.DiemThi?.ToString("0.0") ?? "";
                string tongKet = kq?.DiemTongKet?.ToString("0.0") ?? "";
                string xepLoai = kq?.XepLoai ?? "";
                string trangThai = kq == null ? "Chưa nhập" : (kq.QuaMon == true ? "Đạt" : "Rớt");

                sb.AppendLine($"{stt},{mssv},{hoTen},{lopSh},{diemQt},{diemThi},{tongKet},{xepLoai},{trangThai}");
            }

            byte[] fileBytes = Encoding.UTF8.GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
                .ToArray();

            string fileName = $"BangDiem_{maLhp}_{DateTime.Now:ddMMyyyy}.csv";

            return File(fileBytes, "text/csv", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,CanBo,GiangVien")]
        public async Task<IActionResult> XoaDiem(int maDangKy, string maHocKy, string maLhp)
        {
            if (string.IsNullOrEmpty(maLhp))
            {
                return Forbid();
            }

            var dangKy = await _db.DangKyHocs
                .Include(d => d.MaLhpNavigation)
                .FirstOrDefaultAsync(d => d.MaDangKy == maDangKy);

            if (dangKy == null)
            {
                TempData["Error"] = "Không tìm thấy đăng ký học.";
                return RedirectToAction("NhapDiem", new { maHocKy, maLhp });
            }
            if (dangKy.MaLhp != maLhp)
            {
                return Forbid();
            }
            if (User.IsInRole("GiangVien"))
            {
                string? maGv = User.Identity?.Name;

                if (dangKy.MaLhpNavigation == null || dangKy.MaLhpNavigation.MaGv != maGv)
                {
                    return Forbid();
                }
            }

            var kq = await _db.KetQuaHocTaps
                .FirstOrDefaultAsync(k => k.MaDangKy == maDangKy);

            if (kq != null)
            {
                _db.KetQuaHocTaps.Remove(kq);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Đã xóa điểm thành công! Sinh viên giờ đã có thể hủy học phần.";
            }

            return RedirectToAction("NhapDiem", new { maHocKy, maLhp });
        }

        [Authorize(Roles = "Admin,CanBo,GiangVien")]
        public async Task<IActionResult> TaiTemplate(string maLhp)
        {
            if (string.IsNullOrEmpty(maLhp)) return NotFound();

            var dsSinhVien = await _db.DangKyHocs
                .Include(d => d.MssvNavigation)
                .Where(d => d.MaLhp == maLhp)
                .OrderBy(d => d.Mssv)
                .ToListAsync();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("MSSV;DiemQT;DiemThi;HoTen(Khong_Sua_Cot_Nay)");

            foreach (var sv in dsSinhVien)
            {
                string mssv = sv.Mssv;
                string hoTen = sv.MssvNavigation?.HoTen?
                    .Replace(";", " ")
                    .Replace(",", " ") ?? "";

                sb.AppendLine($"{mssv};;;{hoTen}");
            }

            byte[] fileBytes = Encoding.UTF8.GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
                .ToArray();

            return File(fileBytes, "text/csv", $"Template_NhapDiem_{maLhp}.csv");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,CanBo,GiangVien")]
        public async Task<IActionResult> ImportDiem(IFormFile fileImport, string maHocKy, string maLhp)
        {
            if (fileImport == null || fileImport.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file CSV!";
                return RedirectToAction("NhapDiem", new { maHocKy, maLhp });
            }

            var lhp = await _db.LopHocPhans
                .Include(l => l.MaMonNavigation)
                .FirstOrDefaultAsync(l => l.MaLhp == maLhp);

            if (lhp == null)
            {
                TempData["Error"] = "Không tìm thấy lớp học phần.";
                return RedirectToAction("NhapDiem", new { maHocKy, maLhp });
            }

            double hsQt = (double)(lhp.MaMonNavigation?.HeSoQt ?? 0.3m);
            double hsCk = (double)(lhp.MaMonNavigation?.HeSoCk ?? 0.7m);

            int countSuccess = 0;
            var errors = new List<string>();

            try
            {
                string content;

                using (var reader = new StreamReader(
                    fileImport.OpenReadStream(),
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: true))
                {
                    content = await reader.ReadToEndAsync();
                }

                var lines = content
                    .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToList();

                if (lines.Count < 2)
                {
                    TempData["Error"] = "File không có dữ liệu!";
                    return RedirectToAction("NhapDiem", new { maHocKy, maLhp });
                }

                char delimiter = DetectDelimiter(lines[0]);

                for (int i = 1; i < lines.Count; i++)
                {
                    string line = lines[i];

                    if (!TryReadImportLine(line, delimiter, out string mssv, out string diemQtRaw, out string diemThiRaw))
                    {
                        errors.Add($"Dòng {i + 1}: Không đọc được MSSV, Điểm QT, Điểm Thi.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(mssv))
                        continue;

                    if (string.IsNullOrWhiteSpace(diemQtRaw) && string.IsNullOrWhiteSpace(diemThiRaw))
                        continue;

                    decimal? diemQt = ParseDecimalFlexible(diemQtRaw);
                    decimal? diemThi = ParseDecimalFlexible(diemThiRaw);

                    if (!string.IsNullOrWhiteSpace(diemQtRaw) && diemQt == null)
                    {
                        errors.Add($"Dòng {i + 1}: Điểm QT không hợp lệ.");
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(diemThiRaw) && diemThi == null)
                    {
                        errors.Add($"Dòng {i + 1}: Điểm thi không hợp lệ.");
                        continue;
                    }

                    if (diemQt.HasValue && (diemQt < 0 || diemQt > 10))
                    {
                        errors.Add($"Dòng {i + 1}: Điểm QT ngoài khoảng 0-10.");
                        continue;
                    }

                    if (diemThi.HasValue && (diemThi < 0 || diemThi > 10))
                    {
                        errors.Add($"Dòng {i + 1}: Điểm thi ngoài khoảng 0-10.");
                        continue;
                    }

                    var dk = await _db.DangKyHocs
                        .FirstOrDefaultAsync(d => d.MaLhp == maLhp && d.Mssv == mssv);

                    if (dk == null)
                    {
                        errors.Add($"Dòng {i + 1}: Không tìm thấy sinh viên {mssv} trong lớp.");
                        continue;
                    }

                    var kq = await _db.KetQuaHocTaps
                        .FirstOrDefaultAsync(k => k.MaDangKy == dk.MaDangKy);

                    if (kq == null)
                    {
                        kq = new KetQuaHocTap
                        {
                            MaDangKy = dk.MaDangKy
                        };

                        _db.KetQuaHocTaps.Add(kq);
                    }

                    kq.DiemQt = diemQt;
                    kq.DiemThi = diemThi;

                    double qt = (double)(diemQt ?? 0);
                    double thi = (double)(diemThi ?? 0);

                    double tk = Math.Round(qt * hsQt + thi * hsCk, 1);

                    if (thi < 1.0)
                        tk = 0.0;

                    kq.DiemTongKet = (decimal)tk;
                    kq.QuaMon = tk >= 4.0 && thi >= 1.0;

                    kq.XepLoai = tk >= 9.0 ? "Xuất sắc"
                               : tk >= 8.0 ? "Giỏi"
                               : tk >= 7.0 ? "Khá"
                               : tk >= 5.0 ? "Trung bình"
                               : tk >= 4.0 ? "Yếu"
                               : "Kém";

                    countSuccess++;
                }

                await _db.SaveChangesAsync();

                if (countSuccess > 0)
                {
                    TempData["Success"] = $"Import thành công {countSuccess} sinh viên!";

                    if (errors.Any())
                    {
                        TempData["Error"] = string.Join(" | ", errors.Take(5));
                    }
                }
                else
                {
                    TempData["Error"] = "Không import được dòng nào! " + string.Join(" | ", errors.Take(5));
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi xử lý file: " + ex.Message;
            }

            return RedirectToAction("NhapDiem", new { maHocKy, maLhp });
        }

        private static char DetectDelimiter(string headerLine)
        {
            if (string.IsNullOrWhiteSpace(headerLine))
                return ',';

            headerLine = headerLine.Trim().TrimStart('\uFEFF');

            int semicolonCount = headerLine.Count(c => c == ';');
            int commaCount = headerLine.Count(c => c == ',');

            return semicolonCount > commaCount ? ';' : ',';
        }

        private static bool TryReadImportLine(
            string line,
            char delimiter,
            out string mssv,
            out string diemQtRaw,
            out string diemThiRaw)
        {
            mssv = "";
            diemQtRaw = "";
            diemThiRaw = "";

            if (string.IsNullOrWhiteSpace(line))
                return false;

            line = line.Trim().TrimStart('\uFEFF');

            if (delimiter == ';')
            {
                var cols = SplitCsvLine(line, ';');

                if (cols.Length < 3)
                    return false;

                mssv = cols[0];
                diemQtRaw = cols[1];
                diemThiRaw = cols[2];

                return true;
            }

            var commaCols = SplitCsvLine(line, ',');

            if (commaCols.Length < 3)
                return false;

            mssv = commaCols[0];

            if (commaCols.Length >= 5)
            {
                string combinedQt = commaCols[1] + "," + commaCols[2];
                string combinedThi = commaCols[3] + "," + commaCols[4];

                if (IsScoreInRange(combinedQt) && IsScoreInRange(combinedThi))
                {
                    diemQtRaw = combinedQt;
                    diemThiRaw = combinedThi;
                    return true;
                }
            }

            diemQtRaw = commaCols[1];
            diemThiRaw = commaCols[2];

            return true;
        }

        private static string[] SplitCsvLine(string line, char separator)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool insideQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    insideQuotes = !insideQuotes;
                    continue;
                }

                if (c == separator && !insideQuotes)
                {
                    result.Add(current.ToString().Trim().Trim('"').Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString().Trim().Trim('"').Trim());

            return result.ToArray();
        }

        private static bool IsScoreInRange(string raw)
        {
            decimal? value = ParseDecimalFlexible(raw);
            return value.HasValue && value >= 0 && value <= 10;
        }

        private static decimal? ParseDecimalFlexible(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            raw = raw.Trim().Trim('"').Trim();

            if (raw.Contains(';') && !raw.Contains(',') && !raw.Contains('.'))
            {
                raw = raw.Replace(';', ',');
            }

            if (raw.Contains(',') && !raw.Contains('.'))
            {
                if (decimal.TryParse(
                        raw,
                        NumberStyles.Number,
                        new CultureInfo("vi-VN"),
                        out decimal viResult))
                {
                    return viResult;
                }

                string normalizedComma = raw.Replace(",", ".");

                if (decimal.TryParse(
                        normalizedComma,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out decimal normalizedCommaResult))
                {
                    return normalizedCommaResult;
                }

                return null;
            }

            if (raw.Contains('.') && !raw.Contains(','))
            {
                if (decimal.TryParse(
                        raw,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out decimal invariantResult))
                {
                    return invariantResult;
                }

                return null;
            }

            if (decimal.TryParse(
                    raw,
                    NumberStyles.Number,
                    new CultureInfo("vi-VN"),
                    out decimal finalViResult))
            {
                return finalViResult;
            }

            if (decimal.TryParse(
                    raw,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out decimal finalInvariantResult))
            {
                return finalInvariantResult;
            }

            string normalized = raw
                .Replace(";", ".")
                .Replace(",", ".");

            if (decimal.TryParse(
                    normalized,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out decimal finalNormalizedResult))
            {
                return finalNormalizedResult;
            }

            return null;
        }
    }
}