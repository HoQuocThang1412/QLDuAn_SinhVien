using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Models;

namespace QuanLySinhVien.Services
{
    /// <summary>
    /// Định nghĩa các "tool" (function) mà Gemini có thể gọi để truy vấn DB.
    /// </summary>
    public class ChatBotTools
    {
        private readonly QuanLySinhVienContext _db;

        public ChatBotTools(QuanLySinhVienContext db) { _db = db; }

        // ─────────────────────────────────────────────
        //  TOOL DECLARATIONS (gửi cho Gemini)
        // ─────────────────────────────────────────────
        public static readonly IReadOnlyList<object> Declarations = new List<object>
        {
            new {
                name = "getCurrentUser",
                description = "Lấy thông tin user đang đăng nhập. Dùng khi user hỏi 'của tôi', 'của em', 'điểm tôi'...",
                parameters = new { type = "object", properties = new {} }
            },
            new {
                name = "searchStudents",
                description = "Tìm danh sách sinh viên theo MSSV, họ tên, mã lớp. Trả về danh sách rút gọn.",
                parameters = new {
                    type = "object",
                    properties = new {
                        mssv = new { type = "string", description = "Mã số sinh viên (chính xác hoặc một phần)" },
                        hoTen = new { type = "string", description = "Họ tên sinh viên (tìm gần đúng)" },
                        maLop = new { type = "string", description = "Mã lớp" },
                        top = new { type = "integer", description = "Số lượng tối đa trả về (mặc định 20)" }
                    }
                }
            },
            new {
                name = "getStudentDetail",
                description = "Lấy thông tin chi tiết của 1 sinh viên (lớp, khoa, ngày sinh, liên hệ).",
                parameters = new {
                    type = "object",
                    required = new[] { "mssv" },
                    properties = new {
                        mssv = new { type = "string", description = "MSSV chính xác" }
                    }
                }
            },
            new {
                name = "getStudentGrades",
                description = "Lấy bảng điểm chi tiết các môn của 1 sinh viên (điểm QT, thi, tổng kết, xếp loại).",
                parameters = new {
                    type = "object",
                    required = new[] { "mssv" },
                    properties = new {
                        mssv = new { type = "string", description = "MSSV chính xác" },
                        maHocKy = new { type = "string", description = "Lọc theo mã học kỳ (vd: HK1-2024). Bỏ qua để lấy tất cả" }
                    }
                }
            },
            new {
                name = "getStudentGPA",
                description = "Tính điểm trung bình tích lũy và tổng tín chỉ của 1 sinh viên.",
                parameters = new {
                    type = "object",
                    required = new[] { "mssv" },
                    properties = new {
                        mssv = new { type = "string" }
                    }
                }
            },
            new {
                name = "getStudentTuition",
                description = "Tính học phí dự kiến của sinh viên dựa trên số tín chỉ đã đăng ký và đơn giá.",
                parameters = new {
                    type = "object",
                    required = new[] { "mssv" },
                    properties = new {
                        mssv = new { type = "string" }
                    }
                }
            },
            new {
                name = "getStudentSchedule",
                description = "Lấy lịch học (thời khóa biểu) của sinh viên: thứ, tiết, phòng, môn, giảng viên.",
                parameters = new {
                    type = "object",
                    required = new[] { "mssv" },
                    properties = new {
                        mssv = new { type = "string" },
                        maHocKy = new { type = "string", description = "Lọc theo học kỳ" }
                    }
                }
            },
            new {
                name = "searchTeachers",
                description = "Tìm giảng viên theo mã, tên, hoặc mã khoa.",
                parameters = new {
                    type = "object",
                    properties = new {
                        maGv = new { type = "string" },
                        hoTen = new { type = "string" },
                        maKhoa = new { type = "string" },
                        top = new { type = "integer" }
                    }
                }
            },
            new {
                name = "getSubjects",
                description = "Lấy danh sách môn học, lọc theo khoa hoặc tên.",
                parameters = new {
                    type = "object",
                    properties = new {
                        maKhoa = new { type = "string" },
                        tenMon = new { type = "string", description = "Tìm gần đúng theo tên môn" },
                        top = new { type = "integer" }
                    }
                }
            },
            new {
                name = "getClasses",
                description = "Lấy danh sách lớp học, lọc theo khoa hoặc mã lớp.",
                parameters = new {
                    type = "object",
                    properties = new {
                        maKhoa = new { type = "string" },
                        maLop = new { type = "string" },
                        top = new { type = "integer" }
                    }
                }
            },
            new {
                name = "getFaculties",
                description = "Lấy toàn bộ danh sách khoa.",
                parameters = new { type = "object", properties = new {} }
            },
            new {
                name = "getSemesters",
                description = "Lấy danh sách học kỳ với ngày bắt đầu / kết thúc / trạng thái.",
                parameters = new {
                    type = "object",
                    properties = new {
                        trangThai = new { type = "string", description = "Lọc theo trạng thái (vd: 'DangMo', 'DaDong')" }
                    }
                }
            }
        };

        // ─────────────────────────────────────────────
        //  TOOL DISPATCH
        // ─────────────────────────────────────────────
        public async Task<object> ExecuteAsync(string name, JsonElement args, string? currentUsername, string? currentRole)
        {
            try
            {
                return name switch
                {
                    "getCurrentUser" => await GetCurrentUser(currentUsername, currentRole),
                    "searchStudents" => await SearchStudents(GetStr(args, "mssv"), GetStr(args, "hoTen"), GetStr(args, "maLop"), GetInt(args, "top") ?? 20),
                    "getStudentDetail" => await GetStudentDetail(GetStr(args, "mssv") ?? ""),
                    "getStudentGrades" => await GetStudentGrades(GetStr(args, "mssv") ?? "", GetStr(args, "maHocKy")),
                    "getStudentGPA" => await GetStudentGPA(GetStr(args, "mssv") ?? ""),
                    "getStudentTuition" => await GetStudentTuition(GetStr(args, "mssv") ?? ""),
                    "getStudentSchedule" => await GetStudentSchedule(GetStr(args, "mssv") ?? "", GetStr(args, "maHocKy")),
                    "searchTeachers" => await SearchTeachers(GetStr(args, "maGv"), GetStr(args, "hoTen"), GetStr(args, "maKhoa"), GetInt(args, "top") ?? 20),
                    "getSubjects" => await GetSubjects(GetStr(args, "maKhoa"), GetStr(args, "tenMon"), GetInt(args, "top") ?? 50),
                    "getClasses" => await GetClasses(GetStr(args, "maKhoa"), GetStr(args, "maLop"), GetInt(args, "top") ?? 50),
                    "getFaculties" => await GetFaculties(),
                    "getSemesters" => await GetSemesters(GetStr(args, "trangThai")),
                    _ => new { error = $"Tool không tồn tại: {name}" }
                };
            }
            catch (Exception ex)
            {
                return new { error = $"Lỗi thực thi tool '{name}': {ex.Message}" };
            }
        }

        // ─────────────────────────────────────────────
        //  IMPLEMENTATIONS
        // ─────────────────────────────────────────────
        private async Task<object> GetCurrentUser(string? username, string? role)
        {
            if (string.IsNullOrEmpty(username))
                return new { notFound = true, message = "Chưa đăng nhập" };

            var tk = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.TenDangNhap == username);
            if (tk == null) return new { username, role, note = "Tài khoản đăng nhập thủ công (vd: admin), không có hồ sơ DB" };

            object? profile = null;
            if (role == "SinhVien")
            {
                var sv = await _db.SinhViens.Include(s => s.MaLopNavigation)
                    .ThenInclude(l => l!.MaKhoaNavigation)
                    .FirstOrDefaultAsync(s => s.MaTaiKhoan == tk.MaTaiKhoan);
                if (sv != null) profile = new {
                    mssv = sv.Mssv, hoTen = sv.HoTen, maLop = sv.MaLop,
                    tenLop = sv.MaLopNavigation?.TenLop,
                    maKhoa = sv.MaLopNavigation?.MaKhoa,
                    tenKhoa = sv.MaLopNavigation?.MaKhoaNavigation?.TenKhoa,
                    email = sv.Email, sdt = sv.SoDienThoai,
                    ngaySinh = sv.NgaySinh.ToString("dd/MM/yyyy")
                };
            }
            else if (role == "GiangVien")
            {
                var gv = await _db.GiangViens.Include(g => g.MaKhoaNavigation)
                    .FirstOrDefaultAsync(g => g.MaTaiKhoan == tk.MaTaiKhoan);
                if (gv != null) profile = new {
                    maGv = gv.MaGv, hoTen = gv.HoTen, hocVi = gv.HocVi,
                    maKhoa = gv.MaKhoa, tenKhoa = gv.MaKhoaNavigation?.TenKhoa,
                    email = gv.Email
                };
            }

            return new { username, role, profile };
        }

        private async Task<object> SearchStudents(string? mssv, string? hoTen, string? maLop, int top)
        {
            var q = _db.SinhViens.Include(s => s.MaLopNavigation).AsQueryable();
            if (!string.IsNullOrWhiteSpace(mssv)) q = q.Where(s => EF.Functions.Like(s.Mssv, $"%{mssv}%"));
            if (!string.IsNullOrWhiteSpace(hoTen)) q = q.Where(s => EF.Functions.Like(s.HoTen, $"%{hoTen}%"));
            if (!string.IsNullOrWhiteSpace(maLop)) q = q.Where(s => s.MaLop == maLop);

            top = Math.Clamp(top, 1, 100);
            var list = await q.OrderBy(s => s.HoTen).Take(top).Select(s => new {
                mssv = s.Mssv, hoTen = s.HoTen, maLop = s.MaLop,
                tenLop = s.MaLopNavigation != null ? s.MaLopNavigation.TenLop : null,
                gioiTinh = s.GioiTinh, email = s.Email
            }).ToListAsync();

            return new { total = list.Count, students = list };
        }

        private async Task<object> GetStudentDetail(string mssv)
        {
            var sv = await _db.SinhViens
                .Include(s => s.MaLopNavigation).ThenInclude(l => l!.MaKhoaNavigation)
                .FirstOrDefaultAsync(s => s.Mssv == mssv);
            if (sv == null) return new { notFound = true, mssv };
            return new {
                mssv = sv.Mssv, hoTen = sv.HoTen,
                maLop = sv.MaLop, tenLop = sv.MaLopNavigation?.TenLop,
                khoaHoc = sv.MaLopNavigation?.KhoaHoc,
                maKhoa = sv.MaLopNavigation?.MaKhoa,
                tenKhoa = sv.MaLopNavigation?.MaKhoaNavigation?.TenKhoa,
                ngaySinh = sv.NgaySinh.ToString("dd/MM/yyyy"),
                gioiTinh = sv.GioiTinh, diaChi = sv.DiaChi,
                sdt = sv.SoDienThoai, email = sv.Email,
                trangThai = sv.TrangThai
            };
        }

        private async Task<object> GetStudentGrades(string mssv, string? maHocKy)
        {
            var q = _db.DangKyHocs
                .Where(d => d.Mssv == mssv)
                .Include(d => d.MaMonNavigation)
                .Include(d => d.MaHocKyNavigation)
                .Include(d => d.KetQuaHocTap)
                .AsQueryable();
            if (!string.IsNullOrEmpty(maHocKy)) q = q.Where(d => d.MaHocKy == maHocKy);

            var list = await q.OrderBy(d => d.MaHocKy).ThenBy(d => d.MaMon).ToListAsync();
            if (list.Count == 0) return new { notFound = true, mssv, message = "Sinh viên chưa đăng ký môn nào" };

            var data = list.Select(d => new {
                maHocKy = d.MaHocKy, tenHocKy = d.MaHocKyNavigation?.TenHocKy,
                maMon = d.MaMon, tenMon = d.MaMonNavigation?.TenMon,
                soTinChi = d.MaMonNavigation?.SoTinChi,
                diemQT = d.KetQuaHocTap?.DiemQt,
                diemThi = d.KetQuaHocTap?.DiemThi,
                diemTongKet = d.KetQuaHocTap?.DiemTongKet,
                xepLoai = d.KetQuaHocTap?.XepLoai,
                quaMon = d.KetQuaHocTap?.QuaMon,
                lanHoc = d.LanHoc
            });
            return new { mssv, total = list.Count, grades = data };
        }

        private async Task<object> GetStudentGPA(string mssv)
        {
            var dk = await _db.DangKyHocs
                .Where(d => d.Mssv == mssv)
                .Include(d => d.MaMonNavigation)
                .Include(d => d.KetQuaHocTap)
                .ToListAsync();

            var co = dk.Where(d => d.KetQuaHocTap?.DiemTongKet != null && d.MaMonNavigation != null).ToList();
            if (co.Count == 0) return new { notFound = true, mssv, message = "Chưa có môn nào có điểm tổng kết" };

            decimal tongDiem = co.Sum(d => d.KetQuaHocTap!.DiemTongKet!.Value * d.MaMonNavigation!.SoTinChi);
            int tongTc = co.Sum(d => d.MaMonNavigation!.SoTinChi);
            decimal gpa = tongTc > 0 ? Math.Round(tongDiem / tongTc, 2) : 0;
            int qua = co.Count(d => d.KetQuaHocTap!.QuaMon == true);
            int truot = co.Count - qua;

            return new {
                mssv, gpaHe10 = gpa,
                tongTinChi = tongTc,
                soMonCoDiem = co.Count,
                soMonQua = qua, soMonTruot = truot
            };
        }

        private async Task<object> GetStudentTuition(string mssv)
        {
            var sv = await _db.SinhViens.Include(s => s.MaLopNavigation).FirstOrDefaultAsync(s => s.Mssv == mssv);
            if (sv == null) return new { notFound = true, mssv };

            var dk = await _db.DangKyHocs.Where(d => d.Mssv == mssv)
                .Include(d => d.MaMonNavigation).ToListAsync();
            var tongTc = dk.Sum(d => d.MaMonNavigation?.SoTinChi ?? 0);

            var maKhoa = sv.MaLopNavigation?.MaKhoa;
            var donGia = await _db.DonGiaHocPhis
                .Where(d => maKhoa == null || d.MaKhoa == maKhoa)
                .OrderByDescending(d => d.Id)
                .FirstOrDefaultAsync()
                ?? await _db.DonGiaHocPhis.OrderByDescending(d => d.Id).FirstOrDefaultAsync();

            decimal giaTC = donGia?.SoTienMotTinChi ?? 0;
            return new {
                mssv, hoTen = sv.HoTen,
                tongTinChi = tongTc,
                donGiaMotTinChi = giaTC,
                tongHocPhi = tongTc * giaTC,
                donVi = "VND"
            };
        }

        private async Task<object> GetStudentSchedule(string mssv, string? maHocKy)
        {
            // Lịch học của SV = các LopHocPhan mà SV đã đăng ký
            var q = _db.DangKyHocs
                .Where(d => d.Mssv == mssv && d.MaLhp != null)
                .Include(d => d.MaLhpNavigation).ThenInclude(l => l!.MaMonNavigation)
                .Include(d => d.MaLhpNavigation).ThenInclude(l => l!.MaGvNavigation)
                .Include(d => d.MaHocKyNavigation)
                .AsQueryable();
            if (!string.IsNullOrEmpty(maHocKy)) q = q.Where(d => d.MaHocKy == maHocKy);

            var list = await q.ToListAsync();
            if (list.Count == 0) return new { notFound = true, mssv, message = "Chưa có lịch học" };

            var data = list.Select(d => new {
                maHocKy = d.MaHocKy,
                maLhp = d.MaLhp,
                tenMon = d.MaLhpNavigation?.MaMonNavigation?.TenMon,
                giangVien = d.MaLhpNavigation?.MaGvNavigation?.HoTen,
                thu = d.MaLhpNavigation?.Thu,
                tietBatDau = d.MaLhpNavigation?.TietBatDau,
                soTiet = d.MaLhpNavigation?.SoTiet,
                phongHoc = d.MaLhpNavigation?.PhongHoc
            });
            return new { mssv, total = list.Count, schedule = data };
        }

        private async Task<object> SearchTeachers(string? maGv, string? hoTen, string? maKhoa, int top)
        {
            var q = _db.GiangViens.Include(g => g.MaKhoaNavigation).AsQueryable();
            if (!string.IsNullOrWhiteSpace(maGv)) q = q.Where(g => g.MaGv == maGv);
            if (!string.IsNullOrWhiteSpace(hoTen)) q = q.Where(g => EF.Functions.Like(g.HoTen, $"%{hoTen}%"));
            if (!string.IsNullOrWhiteSpace(maKhoa)) q = q.Where(g => g.MaKhoa == maKhoa);

            top = Math.Clamp(top, 1, 100);
            var list = await q.OrderBy(g => g.HoTen).Take(top).Select(g => new {
                maGv = g.MaGv, hoTen = g.HoTen, hocVi = g.HocVi,
                maKhoa = g.MaKhoa, tenKhoa = g.MaKhoaNavigation != null ? g.MaKhoaNavigation.TenKhoa : null,
                email = g.Email
            }).ToListAsync();
            return new { total = list.Count, teachers = list };
        }

        private async Task<object> GetSubjects(string? maKhoa, string? tenMon, int top)
        {
            var q = _db.MonHocs.Include(m => m.MaKhoaNavigation).AsQueryable();
            if (!string.IsNullOrWhiteSpace(maKhoa)) q = q.Where(m => m.MaKhoa == maKhoa);
            if (!string.IsNullOrWhiteSpace(tenMon)) q = q.Where(m => EF.Functions.Like(m.TenMon, $"%{tenMon}%"));

            top = Math.Clamp(top, 1, 100);
            var list = await q.OrderBy(m => m.MaMon).Take(top).Select(m => new {
                maMon = m.MaMon, tenMon = m.TenMon, soTinChi = m.SoTinChi,
                maKhoa = m.MaKhoa, tenKhoa = m.MaKhoaNavigation != null ? m.MaKhoaNavigation.TenKhoa : null,
                heSoQT = m.HeSoQt, heSoCK = m.HeSoCk
            }).ToListAsync();
            return new { total = list.Count, subjects = list };
        }

        private async Task<object> GetClasses(string? maKhoa, string? maLop, int top)
        {
            var q = _db.LopHocs.Include(l => l.MaKhoaNavigation).AsQueryable();
            if (!string.IsNullOrWhiteSpace(maKhoa)) q = q.Where(l => l.MaKhoa == maKhoa);
            if (!string.IsNullOrWhiteSpace(maLop)) q = q.Where(l => l.MaLop == maLop);

            top = Math.Clamp(top, 1, 100);
            var list = await q.OrderBy(l => l.MaLop).Take(top).Select(l => new {
                maLop = l.MaLop, tenLop = l.TenLop, khoaHoc = l.KhoaHoc,
                maKhoa = l.MaKhoa, tenKhoa = l.MaKhoaNavigation != null ? l.MaKhoaNavigation.TenKhoa : null,
                siSo = l.SinhViens.Count
            }).ToListAsync();
            return new { total = list.Count, classes = list };
        }

        private async Task<object> GetFaculties()
        {
            var list = await _db.Khoas.OrderBy(k => k.MaKhoa).Select(k => new {
                maKhoa = k.MaKhoa, tenKhoa = k.TenKhoa
            }).ToListAsync();
            return new { total = list.Count, faculties = list };
        }

        private async Task<object> GetSemesters(string? trangThai)
        {
            var q = _db.HocKies.AsQueryable();
            if (!string.IsNullOrWhiteSpace(trangThai)) q = q.Where(h => h.TrangThai == trangThai);
            var list = await q.OrderBy(h => h.NgayBatDau).Select(h => new {
                maHocKy = h.MaHocKy, tenHocKy = h.TenHocKy,
                ngayBatDau = h.NgayBatDau.ToString("dd/MM/yyyy"),
                ngayKetThuc = h.NgayKetThuc.ToString("dd/MM/yyyy"),
                trangThai = h.TrangThai,
                gioiHanTinChi = h.GioiHanTinChi
            }).ToListAsync();
            return new { total = list.Count, semesters = list };
        }

        // ─────────────────────────────────────────────
        //  ARG HELPERS
        // ─────────────────────────────────────────────
        private static string? GetStr(JsonElement args, string key)
        {
            if (args.ValueKind != JsonValueKind.Object) return null;
            if (!args.TryGetProperty(key, out var v)) return null;
            if (v.ValueKind == JsonValueKind.Null) return null;
            if (v.ValueKind == JsonValueKind.String) return v.GetString();
            return v.ToString();
        }

        private static int? GetInt(JsonElement args, string key)
        {
            if (args.ValueKind != JsonValueKind.Object) return null;
            if (!args.TryGetProperty(key, out var v)) return null;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n)) return n;
            if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var p)) return p;
            return null;
        }
    }
}
