using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLySinhVien.Controllers
{
    [Authorize(Roles = "Admin,CanBo,SinhVien")]
    public class DangKyHocController : Controller
    {
        private readonly QuanLySinhVienContext _db;
        public DangKyHocController(QuanLySinhVienContext db) => _db = db;

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

        private async Task<decimal> TinhConNoCacHocKyTruocAsync(string mssv, string maHocKyDangDangKy)
        {
            var hkDangKy = await _db.HocKies.FirstOrDefaultAsync(h => h.MaHocKy == maHocKyDangDangKy);

            if (hkDangKy == null)
                return 0;

            var dsHocKyTruoc = await _db.HocKies
                .Where(h => h.NgayKetThuc < hkDangKy.NgayBatDau)
                .Select(h => h.MaHocKy)
                .ToListAsync();

            decimal tongNo = 0;

            foreach (var maHocKy in dsHocKyTruoc)
            {
                tongNo += await TinhConNoHocKyAsync(mssv, maHocKy);
            }

            return tongNo;
        }

        // ==========================================
        // 1. TRANG CHỦ ĐĂNG KÝ
        // ==========================================
        public async Task<IActionResult> Index(string? mssv, string? maHocKy, string? maKhoa)
        {
            if (User.IsInRole("SinhVien")) mssv = User.Identity?.Name;
            if (User.IsInRole("SinhVien"))
            {
                decimal noHocKyTruoc = await TinhConNoCacHocKyTruocAsync(mssv, maHocKy);

                if (noHocKyTruoc > 0)
                {
                    TempData["Error"] = $"Tài khoản bị khóa chức năng đăng ký học phần do còn nợ học phí kỳ trước ({noHocKyTruoc:N0} đ).";
                    return RedirectToAction("Index", new { mssv, maHocKy });
                }
            }

            var dsHocKy = await _db.HocKies.ToListAsync();
            ViewBag.DanhSachHK = dsHocKy
                .Where(h => h.TrangThai == "Đang diễn ra" || h.TrangThai == "Sắp diễn ra")
                .OrderByDescending(h => h.TenHocKy != null && h.TenHocKy.Length >= 9 ? h.TenHocKy.Substring(h.TenHocKy.Length - 9) : h.TenHocKy)
                .ThenByDescending(h => h.TenHocKy)
                .ToList();

            ViewBag.DanhSachKhoa = await _db.Khoas.OrderBy(k => k.TenKhoa).ToListAsync();

            if (string.IsNullOrEmpty(mssv) || string.IsNullOrEmpty(maHocKy))
            {
                ViewBag.Mssv = mssv; ViewBag.MaHocKy = maHocKy; ViewBag.MaKhoa = maKhoa;
                ViewBag.MaxTinChi = 25;
                return View();
            }

            var sv = await _db.SinhViens.Include(s => s.MaLopNavigation).FirstOrDefaultAsync(s => s.Mssv == mssv);
            if (sv == null) { TempData["Error"] = "Không tìm thấy sinh viên."; return View(); }

            string khoaCuaSinhVien = sv.MaLopNavigation?.MaKhoa;
            maKhoa = khoaCuaSinhVien;

            ViewBag.Mssv = mssv; ViewBag.MaHocKy = maHocKy; ViewBag.MaKhoa = maKhoa;

            var hkDangChon = dsHocKy.FirstOrDefault(h => h.MaHocKy == maHocKy);
            ViewBag.MaxTinChi = hkDangChon?.GioiHanTinChi ?? 25;

            var daDangKy = await _db.DangKyHocs
                .Include(d => d.MaMonNavigation)
                .Include(d => d.MaLhpNavigation)
                .Where(d => d.Mssv == mssv && d.MaHocKy == maHocKy).ToListAsync();

            var listMaLhp = daDangKy.Select(d => d.MaLhp).ToList();
            var listChiTiet = await _db.ChiTietLichHocs.Where(c => listMaLhp.Contains(c.MaLhp)).ToListAsync();

            var listMaLhpTrung = new List<string>();
            foreach (var item in daDangKy)
            {
                var lhp = item.MaLhpNavigation;
                if (lhp == null) continue;

                var cacBuoiHocLopNay = listChiTiet.Where(c => c.MaLhp == lhp.MaLhp).ToList();
                if (!cacBuoiHocLopNay.Any() && lhp.Thu >= 2 && lhp.TietBatDau >= 1)
                    cacBuoiHocLopNay.Add(new ChiTietLichHoc { Thu = lhp.Thu, TietBatDau = lhp.TietBatDau, SoTiet = lhp.SoTiet });

                bool biTrung = false;
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

                            if (buoiNay.Thu == buoiKhac.Thu &&
                                buoiNay.TietBatDau <= (buoiKhac.TietBatDau + buoiKhac.SoTiet - 1) &&
                                buoiKhac.TietBatDau <= (buoiNay.TietBatDau + buoiNay.SoTiet - 1))
                            {
                                biTrung = true; break;
                            }
                        }
                        if (biTrung) break;
                    }
                    if (biTrung) break;
                }
                if (biTrung) listMaLhpTrung.Add(item.MaLhp);
            }
            ViewBag.ListMaLhpTrung = listMaLhpTrung;

            var listMaMonDaDk = daDangKy.Select(d => d.MaMon).ToList();

            var queryLopMo = _db.LopHocPhans
                .Include(p => p.MaMonNavigation)
                .Include(p => p.MaGvNavigation)
                .Where(p => p.MaHocKy == maHocKy && !listMaMonDaDk.Contains(p.MaMon))
                .AsQueryable();

            if (!string.IsNullOrEmpty(maKhoa))
            {
                queryLopMo = queryLopMo.Where(p => p.MaMonNavigation.MaKhoa == maKhoa);
            }

            var lopMo = await queryLopMo.ToListAsync();

            // 🛑 TÍNH SĨ SỐ HIỆN TẠI TỐI ƯU CHO TOÀN BỘ CÁC LỚP (TRUYỀN RA VIEW)
            var listSiSo = await _db.DangKyHocs
                .Where(d => d.MaHocKy == maHocKy)
                .GroupBy(d => d.MaLhp)
                .Select(g => new { MaLhp = g.Key, Count = g.Count() })
                .ToListAsync();

            var dictSiSo = new Dictionary<string, int>();
            foreach (var item in listSiSo)
            {
                if (item.MaLhp != null) dictSiSo[item.MaLhp] = item.Count;
            }
            ViewBag.DictSiSo = dictSiSo;

            ViewBag.LopMoKhacTatCa = await _db.LopHocPhans.Where(p => p.MaHocKy == maHocKy).ToListAsync();
            ViewBag.SinhVien = sv;
            ViewBag.DaDangKy = daDangKy;
            ViewBag.LopMo = lopMo;

            return View();
        }

        // ==========================================
        // 2. XỬ LÝ ĐĂNG KÝ
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangKy(string mssv, string maLhp, string maHocKy)
        {
            if (User.IsInRole("SinhVien") && mssv != User.Identity?.Name) return Forbid();

            var lopMoi = await _db.LopHocPhans.Include(l => l.MaMonNavigation).FirstOrDefaultAsync(l => l.MaLhp == maLhp);
            var sv = await _db.SinhViens.FirstOrDefaultAsync(s => s.Mssv == mssv);

            if (lopMoi == null || sv == null) return NotFound();

            var dsDaDk = await _db.DangKyHocs
                .Include(d => d.MaLhpNavigation)
                .Include(d => d.MaMonNavigation)
                .Where(d => d.Mssv == mssv && d.MaHocKy == maHocKy)
                .ToListAsync();

            // 🛑 A1. KIỂM TRA GIỚI HẠN TÍN CHỈ (ĐÃ ĐỒNG BỘ VỚI DATABASE)
            var hkHienTai = await _db.HocKies.FindAsync(maHocKy);
            int MAX_TIN_CHI = hkHienTai?.GioiHanTinChi ?? 25; // Lấy đúng con số 15 của sếp ra!

            int tongTinChiHienTai = dsDaDk.Sum(d => d.MaMonNavigation?.SoTinChi ?? 0);
            int tinChiMonMoi = lopMoi.MaMonNavigation?.SoTinChi ?? 0;

            if (tongTinChiHienTai + tinChiMonMoi > MAX_TIN_CHI)
            {
                TempData["Error"] = $"Không thể đăng ký! Vượt quá giới hạn tín chỉ học kỳ này. Tối đa cho phép: {MAX_TIN_CHI} TC (Bạn đang chọn tổng cộng {tongTinChiHienTai + tinChiMonMoi} TC).";
                return RedirectToAction("Index", new { mssv, maHocKy });
            }

            // 🛑 A2. KIỂM TRA MÔN TIÊN QUYẾT
            string maMonTienQuyet = null;
            var propTienQuyet = typeof(MonHoc).GetProperty("MaMonTienQuyet");
            if (propTienQuyet != null && lopMoi.MaMonNavigation != null)
            {
                maMonTienQuyet = propTienQuyet.GetValue(lopMoi.MaMonNavigation) as string;
            }

            if (!string.IsNullOrEmpty(maMonTienQuyet))
            {
                bool daQuaMonTienQuyet = await _db.KetQuaHocTaps
                    .Include(k => k.MaDangKyNavigation)
                    .AnyAsync(k => k.MaDangKyNavigation != null
                                && k.MaDangKyNavigation.Mssv == mssv
                                && k.MaDangKyNavigation.MaMon == maMonTienQuyet
                                && k.QuaMon == true);

                if (!daQuaMonTienQuyet)
                {
                    var monTQ = await _db.MonHocs.FindAsync(maMonTienQuyet);
                    string tenMonTQ = monTQ?.TenMon ?? maMonTienQuyet;

                    TempData["Error"] = $"Không thể đăng ký! Môn này yêu cầu phải hoàn thành môn tiên quyết: [{maMonTienQuyet}] {tenMonTQ}.";
                    return RedirectToAction("Index", new { mssv, maHocKy });
                }
            }

            // B. Kiểm tra trùng môn
            bool trungMon = dsDaDk.Any(d => d.MaMon == lopMoi.MaMon);
            if (trungMon)
            {
                TempData["Error"] = "Bạn đã đăng ký một lớp khác của môn học này rồi!";
                return RedirectToAction("Index", new { mssv, maHocKy });
            }

            // C. CHECK TRÙNG LỊCH ĐA BUỔI
            var listLhpIds = dsDaDk.Select(d => d.MaLhp).ToList();
            listLhpIds.Add(lopMoi.MaLhp);
            var listChiTiet = await _db.ChiTietLichHocs.Where(c => listLhpIds.Contains(c.MaLhp)).ToListAsync();

            bool isTrungLich = false;
            var cacBuoiHocLopMoi = listChiTiet.Where(c => c.MaLhp == lopMoi.MaLhp).ToList();
            if (!cacBuoiHocLopMoi.Any() && lopMoi.Thu >= 2 && lopMoi.TietBatDau >= 1)
                cacBuoiHocLopMoi.Add(new ChiTietLichHoc { Thu = lopMoi.Thu, TietBatDau = lopMoi.TietBatDau, SoTiet = lopMoi.SoTiet });

            foreach (var item in dsDaDk)
            {
                var lhpCu = item.MaLhpNavigation;
                if (lhpCu == null) continue;

                var cacBuoiHocLopCu = listChiTiet.Where(c => c.MaLhp == lhpCu.MaLhp).ToList();
                if (!cacBuoiHocLopCu.Any() && lhpCu.Thu >= 2 && lhpCu.TietBatDau >= 1)
                    cacBuoiHocLopCu.Add(new ChiTietLichHoc { Thu = lhpCu.Thu, TietBatDau = lhpCu.TietBatDau, SoTiet = lhpCu.SoTiet });

                foreach (var buoiMoi in cacBuoiHocLopMoi)
                {
                    if (buoiMoi.Thu < 2 || buoiMoi.TietBatDau < 1) continue;

                    foreach (var buoiCu in cacBuoiHocLopCu)
                    {
                        if (buoiCu.Thu < 2 || buoiCu.TietBatDau < 1) continue;

                        if (buoiMoi.Thu == buoiCu.Thu &&
                            buoiMoi.TietBatDau <= (buoiCu.TietBatDau + buoiCu.SoTiet - 1) &&
                            buoiCu.TietBatDau <= (buoiMoi.TietBatDau + buoiMoi.SoTiet - 1))
                        {
                            isTrungLich = true; break;
                        }
                    }
                    if (isTrungLich) break;
                }
                if (isTrungLich) break;
            }

            // D. CHECK SĨ SỐ 
            int siSoHienTai = await _db.DangKyHocs.CountAsync(d => d.MaLhp == maLhp);
            int siSoMax = lopMoi.SiSoToiDa;

            if (siSoMax <= 0)
            {
                TempData["Error"] = $"Lớp {maLhp} chưa được cài đặt Sĩ số tối đa. Báo Giáo vụ!";
                return RedirectToAction("Index", new { mssv, maHocKy });
            }

            if (siSoHienTai >= siSoMax)
            {
                TempData["Error"] = $"Lớp học phần này đã đầy sĩ số! ({siSoHienTai}/{siSoMax})";
                return RedirectToAction("Index", new { mssv, maHocKy });
            }

            // E. LƯU VÀO DB
            var dk = new DangKyHoc
            {
                Mssv = mssv,
                MaLhp = maLhp,
                MaMon = lopMoi.MaMon,
                MaLop = sv.MaLop,
                MaHocKy = maHocKy,
                LanHoc = (byte)(await _db.DangKyHocs.CountAsync(d => d.Mssv == mssv && d.MaMon == lopMoi.MaMon) + 1)
            };

            _db.DangKyHocs.Add(dk);
            await _db.SaveChangesAsync();

            if (isTrungLich) TempData["Warning"] = "Đăng ký thành công, nhưng CẢNH BÁO: Môn này bị TRÙNG LỊCH với môn khác!";
            else TempData["Success"] = "Đăng ký thành công!";

            return RedirectToAction("Index", new { mssv, maHocKy });
        }

        // ==========================================
        // 3. HỦY ĐĂNG KÝ
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyDangKy(string mssv, string maLhp, string maHocKy)
        {
            if (User.IsInRole("SinhVien") && mssv != User.Identity?.Name) return Forbid();

            var dk = await _db.DangKyHocs.FirstOrDefaultAsync(d => d.Mssv == mssv && d.MaLhp == maLhp && d.MaHocKy == maHocKy);
            if (dk != null)
            {
                bool daCoDiem = await _db.KetQuaHocTaps.AnyAsync(k => k.MaDangKy == dk.MaDangKy);
                if (daCoDiem)
                {
                    TempData["Error"] = "Không thể hủy đăng ký vì học phần này ĐÃ CÓ ĐIỂM!";
                    return RedirectToAction("Index", new { mssv, maHocKy });
                }

                _db.DangKyHocs.Remove(dk);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã hủy môn học thành công.";
            }

            return RedirectToAction("Index", new { mssv, maHocKy });
        }

        // ==========================================
        // 4. ĐỔI LỚP (SWAP)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiLop(string mssv, string maMon, string maLhpMoi, string maHocKy)
        {
            if (User.IsInRole("SinhVien") && mssv != User.Identity?.Name) return Forbid();

            var dkCu = await _db.DangKyHocs.FirstOrDefaultAsync(d => d.Mssv == mssv && d.MaMon == maMon && d.MaHocKy == maHocKy);
            if (dkCu == null) return NotFound();

            var lopMoi = await _db.LopHocPhans.Include(l => l.MaMonNavigation).FirstOrDefaultAsync(l => l.MaLhp == maLhpMoi);
            if (lopMoi != null)
            {
                // 🛑 A. KIỂM TRA LẠI GIỚI HẠN TÍN CHỈ KHI ĐỔI LỚP (Tránh đổi môn ít tín sang môn nhiều tín)
                var dsDaDk = await _db.DangKyHocs.Include(d => d.MaMonNavigation).Where(d => d.Mssv == mssv && d.MaHocKy == maHocKy && d.MaMon != maMon).ToListAsync();
                var hkHienTai = await _db.HocKies.FindAsync(maHocKy);
                int MAX_TIN_CHI = hkHienTai?.GioiHanTinChi ?? 25;

                int tongTinChiHienTai = dsDaDk.Sum(d => d.MaMonNavigation?.SoTinChi ?? 0);
                int tinChiMonMoi = lopMoi.MaMonNavigation?.SoTinChi ?? 0;

                if (tongTinChiHienTai + tinChiMonMoi > MAX_TIN_CHI)
                {
                    TempData["Error"] = $"Không thể đổi lớp! Lớp mới có số tín chỉ cao hơn làm vượt quá giới hạn tối đa ({MAX_TIN_CHI} TC).";
                    return RedirectToAction("Index", new { mssv, maHocKy });
                }

                // CHECK SĨ SỐ
                int siSoHienTai = await _db.DangKyHocs.CountAsync(d => d.MaLhp == maLhpMoi);
                int siSoMax = lopMoi.SiSoToiDa;

                if (siSoMax <= 0) { TempData["Error"] = $"Lớp mới chưa cài Sĩ số tối đa. Báo Giáo vụ!"; return RedirectToAction("Index", new { mssv, maHocKy }); }
                if (siSoHienTai >= siSoMax) { TempData["Error"] = $"Lớp mới đã đầy sĩ số ({siSoHienTai}/{siSoMax}), không thể đổi!"; return RedirectToAction("Index", new { mssv, maHocKy }); }

                dkCu.MaLhp = maLhpMoi;
                _db.DangKyHocs.Update(dkCu);
                await _db.SaveChangesAsync();

                // Lấy lại danh sách sau khi đã update để dò trùng lịch
                var dsDaDkMoi = await _db.DangKyHocs.Include(d => d.MaLhpNavigation).Where(d => d.Mssv == mssv && d.MaHocKy == maHocKy && d.MaLhp != maLhpMoi).ToListAsync();
                var listLhpIds = dsDaDkMoi.Select(d => d.MaLhp).ToList();
                listLhpIds.Add(lopMoi.MaLhp);
                var listChiTiet = await _db.ChiTietLichHocs.Where(c => listLhpIds.Contains(c.MaLhp)).ToListAsync();

                bool isTrungLich = false;
                var cacBuoiHocLopMoi = listChiTiet.Where(c => c.MaLhp == lopMoi.MaLhp).ToList();
                if (!cacBuoiHocLopMoi.Any() && lopMoi.Thu >= 2 && lopMoi.TietBatDau >= 1)
                    cacBuoiHocLopMoi.Add(new ChiTietLichHoc { Thu = lopMoi.Thu, TietBatDau = lopMoi.TietBatDau, SoTiet = lopMoi.SoTiet });

                foreach (var item in dsDaDkMoi)
                {
                    var lhpCu = item.MaLhpNavigation;
                    if (lhpCu == null) continue;

                    var cacBuoiHocLopCu = listChiTiet.Where(c => c.MaLhp == lhpCu.MaLhp).ToList();
                    if (!cacBuoiHocLopCu.Any() && lhpCu.Thu >= 2 && lhpCu.TietBatDau >= 1)
                        cacBuoiHocLopCu.Add(new ChiTietLichHoc { Thu = lhpCu.Thu, TietBatDau = lhpCu.TietBatDau, SoTiet = lhpCu.SoTiet });

                    foreach (var buoiMoi in cacBuoiHocLopMoi)
                    {
                        if (buoiMoi.Thu < 2 || buoiMoi.TietBatDau < 1) continue;

                        foreach (var buoiCu in cacBuoiHocLopCu)
                        {
                            if (buoiCu.Thu < 2 || buoiCu.TietBatDau < 1) continue;

                            if (buoiMoi.Thu == buoiCu.Thu &&
                                buoiMoi.TietBatDau <= (buoiCu.TietBatDau + buoiCu.SoTiet - 1) &&
                                buoiCu.TietBatDau <= (buoiMoi.TietBatDau + buoiMoi.SoTiet - 1))
                            {
                                isTrungLich = true; break;
                            }
                        }
                        if (isTrungLich) break;
                    }
                    if (isTrungLich) break;
                }

                if (isTrungLich) TempData["Warning"] = "Đổi lớp thành công, nhưng CẢNH BÁO: Lớp mới bị TRÙNG LỊCH với môn khác!";
                else TempData["Success"] = "Đã đổi lớp học phần thành công!";
            }

            return RedirectToAction("Index", new { mssv, maHocKy });
        }
    }
}