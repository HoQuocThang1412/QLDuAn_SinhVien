USE QuanLySinhVien;
GO

-- ══════════════════════════════════════════
-- XÓA SẠCH THEO THỨ TỰ FK
-- ══════════════════════════════════════════
DELETE FROM KetQuaHocTap;
DELETE FROM LichHoc;
DELETE FROM DangKyHoc;
DELETE FROM PhanCong;
DELETE FROM SinhVien;
DELETE FROM GiangVien;
DELETE FROM MonHoc;
DELETE FROM LopHoc;
DELETE FROM HocKy;
DELETE FROM TaiKhoan;
DELETE FROM Khoa;
GO

DBCC CHECKIDENT ('TaiKhoan', RESEED, 0);
GO

-- ══════════════════════════════════════════
-- 1. KHOA
-- ══════════════════════════════════════════
INSERT INTO Khoa (MaKhoa, TenKhoa) VALUES
('CNTT',  N'Công nghệ Thông tin'),
('KT',    N'Kế toán'),
('QTKD',  N'Quản trị Kinh doanh'),
('SP',    N'Sư phạm'),
('LUAT',  N'Luật'),
('NN',    N'Ngoại ngữ'),
('KH',    N'Khoa học Tự nhiên'),
('XH',    N'Khoa học Xã hội');
GO

-- ══════════════════════════════════════════
-- 2. HỌC KỲ
-- ══════════════════════════════════════════
INSERT INTO HocKy (MaHocKy, TenHocKy, NgayBatDau, NgayKetThuc, TrangThai) VALUES
('HK1-2022-2023', N'Học kỳ 1 năm học 2022-2023', '2022-09-01', '2023-01-15', N'Đã kết thúc'),
('HK2-2022-2023', N'Học kỳ 2 năm học 2022-2023', '2023-02-01', '2023-06-15', N'Đã kết thúc'),
('HK1-2023-2024', N'Học kỳ 1 năm học 2023-2024', '2023-09-01', '2024-01-15', N'Đã kết thúc'),
('HK2-2023-2024', N'Học kỳ 2 năm học 2023-2024', '2024-02-01', '2024-06-15', N'Đã kết thúc'),
('HK1-2024-2025', N'Học kỳ 1 năm học 2024-2025', '2024-09-01', '2025-01-15', N'Đã kết thúc'),
('HK2-2024-2025', N'Học kỳ 2 năm học 2024-2025', '2025-02-01', '2025-06-15', N'Đang diễn ra'),
('HK1-2025-2026', N'Học kỳ 1 năm học 2025-2026', '2025-09-01', '2026-01-15', N'Sắp diễn ra');
GO

-- ══════════════════════════════════════════
-- 3. LỚP HỌC
-- ══════════════════════════════════════════
INSERT INTO LopHoc (MaLop, TenLop, MaKhoa, KhoaHoc) VALUES
-- CNTT K44 (2021)
('CNTTK44A', N'CNTTK44A - Khóa 44', 'CNTT', '2021'),
('CNTTK44B', N'CNTTK44B - Khóa 44', 'CNTT', '2021'),
('CNTTK44C', N'CNTTK44C - Khóa 44', 'CNTT', '2021'),
-- CNTT K45 (2022)
('CNTTK45A', N'CNTTK45A - Khóa 45', 'CNTT', '2022'),
('CNTTK45B', N'CNTTK45B - Khóa 45', 'CNTT', '2022'),
('CNTTK45C', N'CNTTK45C - Khóa 45', 'CNTT', '2022'),
('CNTTK45D', N'CNTTK45D - Khóa 45', 'CNTT', '2022'),
-- CNTT K46 (2023)
('CNTTK46A', N'CNTTK46A - Khóa 46', 'CNTT', '2023'),
('CNTTK46B', N'CNTTK46B - Khóa 46', 'CNTT', '2023'),
('CNTTK46C', N'CNTTK46C - Khóa 46', 'CNTT', '2023'),
-- Kế toán
('KTK44A',   N'KTK44A - Khóa 44',   'KT', '2021'),
('KTK44B',   N'KTK44B - Khóa 44',   'KT', '2021'),
('KTK45A',   N'KTK45A - Khóa 45',   'KT', '2022'),
('KTK45B',   N'KTK45B - Khóa 45',   'KT', '2022'),
('KTK46A',   N'KTK46A - Khóa 46',   'KT', '2023'),
-- QTKD
('QTKDK44A', N'QTKDK44A - Khóa 44', 'QTKD', '2021'),
('QTKDK45A', N'QTKDK45A - Khóa 45', 'QTKD', '2022'),
('QTKDK45B', N'QTKDK45B - Khóa 45', 'QTKD', '2022'),
('QTKDK46A', N'QTKDK46A - Khóa 46', 'QTKD', '2023'),
-- Sư phạm
('SPToanK44', N'SP Toán K44',        'SP', '2021'),
('SPToanK45', N'SP Toán K45',        'SP', '2022'),
('SPVanK45',  N'SP Ngữ Văn K45',    'SP', '2022'),
('SPAnhK45',  N'SP Tiếng Anh K45',  'SP', '2022'),
('SPToanK46', N'SP Toán K46',        'SP', '2023'),
('SPVanK46',  N'SP Ngữ Văn K46',    'SP', '2023'),
-- Luật
('LuatK44A',  N'Luật K44A - Khóa 44', 'LUAT', '2021'),
('LuatK45A',  N'Luật K45A - Khóa 45', 'LUAT', '2022'),
('LuatK46A',  N'Luật K46A - Khóa 46', 'LUAT', '2023'),
-- Ngoại ngữ
('NNAnhK44',  N'NN Tiếng Anh K44',  'NN', '2021'),
('NNAnhK45',  N'NN Tiếng Anh K45',  'NN', '2022'),
('NNAnhK46',  N'NN Tiếng Anh K46',  'NN', '2023'),
-- Khoa học Tự nhiên
('KHTNK44',   N'KHTN K44 - Khóa 44', 'KH', '2021'),
('KHTNK45',   N'KHTN K45 - Khóa 45', 'KH', '2022'),
-- Khoa học Xã hội
('KHXHK44',   N'KHXH K44 - Khóa 44', 'XH', '2021'),
('KHXHK45',   N'KHXH K45 - Khóa 45', 'XH', '2022');
GO

-- ══════════════════════════════════════════
-- 4. MÔN HỌC
-- ══════════════════════════════════════════
INSERT INTO MonHoc (MaMon, TenMon, MaKhoa, SoTinChi, HeSoQT, HeSoCK) VALUES
('CNTT101', N'Lập trình căn bản',       'CNTT', 3, 0.30, 0.70),
('CNTT102', N'Cấu trúc dữ liệu',        'CNTT', 3, 0.30, 0.70),
('CNTT201', N'Lập trình Web',           'CNTT', 3, 0.40, 0.60),
('CNTT202', N'Cơ sở dữ liệu',           'CNTT', 3, 0.30, 0.70),
('CNTT301', N'Lập trình ASP.NET Core',  'CNTT', 3, 0.40, 0.60),
('CNTT302', N'An toàn thông tin',       'CNTT', 2, 0.30, 0.70),
('KT101',   N'Kế toán đại cương',       'KT',   3, 0.30, 0.70),
('KT102',   N'Nguyên lý kế toán',       'KT',   3, 0.30, 0.70),
('KT201',   N'Kế toán tài chính',       'KT',   3, 0.40, 0.60),
('KT202',   N'Kế toán quản trị',        'KT',   3, 0.40, 0.60),
('QT101',   N'Quản trị học',            'QTKD', 3, 0.30, 0.70),
('QT102',   N'Marketing căn bản',       'QTKD', 3, 0.30, 0.70),
('QT201',   N'Quản trị nhân lực',       'QTKD', 3, 0.40, 0.60),
('SP101',   N'Tâm lý giáo dục',         'SP',   2, 0.30, 0.70),
('SP102',   N'Phương pháp dạy học',     'SP',   3, 0.40, 0.60),
('SP201',   N'Giáo dục học',            'SP',   3, 0.30, 0.70),
('LU101',   N'Pháp luật đại cương',     'LUAT', 3, 0.30, 0.70),
('LU102',   N'Luật dân sự',             'LUAT', 3, 0.30, 0.70),
('LU201',   N'Luật kinh tế',            'LUAT', 3, 0.40, 0.60),
('NN101',   N'Tiếng Anh cơ bản',        'NN',   3, 0.30, 0.70),
('NN102',   N'Tiếng Anh giao tiếp',     'NN',   3, 0.40, 0.60),
('NN201',   N'Tiếng Anh chuyên ngành',  'NN',   3, 0.40, 0.60),
('DC101',   N'Triết học Mác-Lênin',     'KH',   3, 0.30, 0.70),
('DC102',   N'Toán cao cấp',            'KH',   3, 0.30, 0.70),
('DC103',   N'Vật lý đại cương',        'KH',   3, 0.30, 0.70),
('DC201',   N'Lịch sử Đảng CSVN',      'XH',   2, 0.30, 0.70),
('DC202',   N'Tư tưởng Hồ Chí Minh',   'XH',   2, 0.30, 0.70);
GO

-- ══════════════════════════════════════════
-- 5. TÀI KHOẢN
-- ══════════════════════════════════════════
INSERT INTO TaiKhoan (TenDangNhap, MatKhauHash, VaiTro, TrangThai, LanDangNhapSai) VALUES
('admin',     '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.', 'Admin',     1, 0),
('canbo',     '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.', 'CanBo',     1, 0),
('giangvien', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.', 'GiangVien', 1, 0),
('sinhvien',  '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.', 'SinhVien',  1, 0);
GO

-- ══════════════════════════════════════════
-- 6. GIẢNG VIÊN
-- ══════════════════════════════════════════
INSERT INTO GiangVien (MaGv, MaKhoa, HoTen, Email, HocVi) VALUES
('GV001', 'CNTT', N'Nguyễn Văn An',    'nguyenvanan@qnu.edu.vn',   N'Tiến sĩ'),
('GV002', 'CNTT', N'Trần Thị Bình',    'tranthihinh@qnu.edu.vn',   N'Thạc sĩ'),
('GV003', 'CNTT', N'Lê Văn Cường',     'levancuong@qnu.edu.vn',    N'Tiến sĩ'),
('GV004', 'KT',   N'Phạm Thị Dung',    'phamthidung@qnu.edu.vn',   N'Thạc sĩ'),
('GV005', 'KT',   N'Hoàng Văn Em',     'hoangvanem@qnu.edu.vn',    N'Tiến sĩ'),
('GV006', 'QTKD', N'Vũ Thị Phương',    'vuthiphuong@qnu.edu.vn',   N'Thạc sĩ'),
('GV007', 'QTKD', N'Đặng Văn Giang',   'dangvangiang@qnu.edu.vn',  N'Tiến sĩ'),
('GV008', 'SP',   N'Bùi Thị Hoa',      'buithihoa@qnu.edu.vn',     N'Thạc sĩ'),
('GV009', 'SP',   N'Ngô Văn Inh',      'ngovaninh@qnu.edu.vn',     N'Tiến sĩ'),
('GV010', 'LUAT', N'Đinh Thị Kim',     'dinhthikim@qnu.edu.vn',    N'Tiến sĩ'),
('GV011', 'NN',   N'Lý Văn Long',      'lyvanlong@qnu.edu.vn',     N'Thạc sĩ'),
('GV012', 'KH',   N'Mai Thị Mận',      'maithiman@qnu.edu.vn',     N'Phó Giáo sư'),
('GV013', 'XH',   N'Phan Văn Nam',     'phanvannam@qnu.edu.vn',    N'Tiến sĩ');
GO

-- ══════════════════════════════════════════
-- 7. SINH VIÊN (30 sinh viên mẫu)
-- ══════════════════════════════════════════
INSERT INTO SinhVien (MSSV, MaLop, HoTen, NgaySinh, GioiTinh, SoDienThoai, Email, TrangThai) VALUES
-- CNTT K45
('SV001', 'CNTTK45A', N'Nguyễn Văn An',     '2004-01-15', N'Nam', '0901234561', 'sv001@gmail.com', N'Đang học'),
('SV002', 'CNTTK45A', N'Trần Thị Bình',     '2004-03-20', N'Nữ',  '0901234562', 'sv002@gmail.com', N'Đang học'),
('SV003', 'CNTTK45A', N'Lê Văn Cường',      '2004-05-10', N'Nam', '0901234563', 'sv003@gmail.com', N'Đang học'),
('SV004', 'CNTTK45B', N'Phạm Thị Dung',     '2004-07-22', N'Nữ',  '0901234564', 'sv004@gmail.com', N'Đang học'),
('SV005', 'CNTTK45B', N'Hoàng Văn Em',      '2004-09-05', N'Nam', '0901234565', 'sv005@gmail.com', N'Đang học'),
('SV006', 'CNTTK45C', N'Vũ Thị Phương',     '2004-11-18', N'Nữ',  '0901234566', 'sv006@gmail.com', N'Đang học'),
('SV007', 'CNTTK45C', N'Đặng Văn Giang',    '2004-02-28', N'Nam', '0901234567', 'sv007@gmail.com', N'Bảo lưu'),
('SV008', 'CNTTK45D', N'Bùi Thị Hoa',       '2004-04-12', N'Nữ',  '0901234568', 'sv008@gmail.com', N'Đang học'),
('SV009', 'CNTTK45D', N'Ngô Văn Inh',       '2004-06-30', N'Nam', '0901234569', 'sv009@gmail.com', N'Đang học'),
('SV010', 'CNTTK46A', N'Đinh Thị Kim',      '2005-08-14', N'Nữ',  '0901234570', 'sv010@gmail.com', N'Đang học'),
-- Kế toán
('SV011', 'KTK45A',   N'Lý Văn Long',       '2004-10-25', N'Nam', '0901234571', 'sv011@gmail.com', N'Đang học'),
('SV012', 'KTK45A',   N'Mai Thị Mận',       '2004-12-08', N'Nữ',  '0901234572', 'sv012@gmail.com', N'Đang học'),
('SV013', 'KTK45B',   N'Phan Văn Nam',      '2004-03-15', N'Nam', '0901234573', 'sv013@gmail.com', N'Đang học'),
('SV014', 'KTK45B',   N'Trịnh Thị Oanh',   '2004-05-20', N'Nữ',  '0901234574', 'sv014@gmail.com', N'Đang học'),
('SV015', 'KTK46A',   N'Võ Văn Phong',      '2005-01-10', N'Nam', '0901234575', 'sv015@gmail.com', N'Đang học'),
-- QTKD
('SV016', 'QTKDK45A', N'Đỗ Thị Quỳnh',     '2004-07-18', N'Nữ',  '0901234576', 'sv016@gmail.com', N'Đang học'),
('SV017', 'QTKDK45A', N'Hà Văn Rạng',      '2004-09-22', N'Nam', '0901234577', 'sv017@gmail.com', N'Đang học'),
('SV018', 'QTKDK45B', N'Kiều Thị Sen',      '2004-11-05', N'Nữ',  '0901234578', 'sv018@gmail.com', N'Thôi học'),
('SV019', 'QTKDK46A', N'Lâm Văn Tùng',     '2005-02-14', N'Nam', '0901234579', 'sv019@gmail.com', N'Đang học'),
-- Sư phạm
('SV020', 'SPToanK45', N'Mạc Thị Uyên',    '2004-04-28', N'Nữ',  '0901234580', 'sv020@gmail.com', N'Đang học'),
('SV021', 'SPToanK45', N'Nghiêm Văn Vũ',   '2004-06-15', N'Nam', '0901234581', 'sv021@gmail.com', N'Đang học'),
('SV022', 'SPVanK45',  N'Nhữ Thị Xuân',    '2004-08-30', N'Nữ',  '0901234582', 'sv022@gmail.com', N'Đang học'),
('SV023', 'SPAnhK45',  N'Ông Văn Yên',     '2004-10-12', N'Nam', '0901234583', 'sv023@gmail.com', N'Đang học'),
-- Luật
('SV024', 'LuatK45A',  N'Pháp Thị Zung',   '2004-12-25', N'Nữ',  '0901234584', 'sv024@gmail.com', N'Đang học'),
('SV025', 'LuatK45A',  N'Quách Văn Anh',   '2004-01-08', N'Nam', '0901234585', 'sv025@gmail.com', N'Bảo lưu'),
-- Ngoại ngữ
('SV026', 'NNAnhK45',  N'Rạch Thị Bảo',    '2004-03-18', N'Nữ',  '0901234586', 'sv026@gmail.com', N'Đang học'),
('SV027', 'NNAnhK45',  N'Sơn Văn Cao',     '2004-05-25', N'Nam', '0901234587', 'sv027@gmail.com', N'Đang học'),
-- KHTN
('SV028', 'KHTNK45',   N'Tạ Thị Duyên',    '2004-07-10', N'Nữ',  '0901234588', 'sv028@gmail.com', N'Đang học'),
-- KHXH
('SV029', 'KHXHK45',   N'Thái Văn Hào',    '2004-09-20', N'Nam', '0901234589', 'sv029@gmail.com', N'Đang học'),
('SV030', 'KHXHK45',   N'Ung Thị Liên',    '2004-11-30', N'Nữ',  '0901234590', 'sv030@gmail.com', N'Đang học');
GO

-- ══════════════════════════════════════════
-- KIỂM TRA KẾT QUẢ
-- ══════════════════════════════════════════
SELECT 'Khoa'      AS Bang, COUNT(*) AS SoLuong FROM Khoa      UNION ALL
SELECT 'HocKy',             COUNT(*)            FROM HocKy     UNION ALL
SELECT 'LopHoc',            COUNT(*)            FROM LopHoc    UNION ALL
SELECT 'MonHoc',            COUNT(*)            FROM MonHoc    UNION ALL
SELECT 'TaiKhoan',          COUNT(*)            FROM TaiKhoan  UNION ALL
SELECT 'GiangVien',         COUNT(*)            FROM GiangVien UNION ALL
SELECT 'SinhVien',          COUNT(*)            FROM SinhVien;
GO