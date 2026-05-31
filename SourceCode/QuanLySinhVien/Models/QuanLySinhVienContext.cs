using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace QuanLySinhVien.Models;

public partial class QuanLySinhVienContext : DbContext
{
    public QuanLySinhVienContext() { }

    public QuanLySinhVienContext(DbContextOptions<QuanLySinhVienContext> options)
        : base(options) { }

    // ĐĂNG KÝ ĐỦ 12 BẢNG
    public virtual DbSet<DangKyHoc> DangKyHocs { get; set; }
    public virtual DbSet<GiangVien> GiangViens { get; set; }
    public virtual DbSet<HocKy> HocKies { get; set; }
    public virtual DbSet<KetQuaHocTap> KetQuaHocTaps { get; set; }
    public virtual DbSet<Khoa> Khoas { get; set; }
    public virtual DbSet<LichHoc> LichHocs { get; set; }
    public virtual DbSet<LopHoc> LopHocs { get; set; }
    public virtual DbSet<LopHocPhan> LopHocPhans { get; set; } = null!;
    public virtual DbSet<MonHoc> MonHocs { get; set; }
    public virtual DbSet<SinhVien> SinhViens { get; set; }
    public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }
    public virtual DbSet<ChiTietLichHoc> ChiTietLichHocs { get; set; }
    public virtual DbSet<DonGiaHocPhi> DonGiaHocPhis { get; set; }
    public virtual DbSet<ThanhToanHocPhi> ThanhToanHocPhis { get; set; }
    public virtual DbSet<CauHinhQR> CauHinhQRs { get; set; }
    public virtual DbSet<KhoaHoc> KhoaHocs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Server=HOQUTHANG;Database=QuanLySinhVien;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. KHOA
        modelBuilder.Entity<Khoa>(entity => {
            entity.HasKey(e => e.MaKhoa);
            entity.ToTable("Khoa");
            entity.Property(e => e.MaKhoa).HasMaxLength(10).IsUnicode(false);
            entity.Property(e => e.TenKhoa).HasMaxLength(100);
        });

        // 2. TAI KHOAN
        modelBuilder.Entity<TaiKhoan>(entity => {
            entity.HasKey(e => e.MaTaiKhoan);
            entity.ToTable("TaiKhoan");
            entity.Property(e => e.TenDangNhap).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.MatKhauHash).HasMaxLength(255).IsUnicode(false);
        });

        // 3. HOC KY
        modelBuilder.Entity<HocKy>(entity => {
            entity.HasKey(e => e.MaHocKy);
            entity.ToTable("HocKy");
            entity.Property(e => e.MaHocKy).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.TenHocKy).HasMaxLength(50);
        });

        // 4. MON HOC
        modelBuilder.Entity<MonHoc>(entity => {
            entity.HasKey(e => e.MaMon);
            entity.ToTable("MonHoc");
            entity.Property(e => e.MaMon).HasMaxLength(10).IsUnicode(false);
            entity.HasOne(d => d.MaKhoaNavigation).WithMany(p => p.MonHocs).HasForeignKey(d => d.MaKhoa);
        });

        // 5. LOP HOC
        modelBuilder.Entity<LopHoc>(entity => {
            entity.HasKey(e => e.MaLop);
            entity.ToTable("LopHoc");
            entity.Property(e => e.MaLop).HasMaxLength(20).IsUnicode(false);
            entity.HasOne(d => d.MaKhoaNavigation).WithMany(p => p.LopHocs).HasForeignKey(d => d.MaKhoa);
        });

        // 6. SINH VIEN (1-1 với TaiKhoan)
        modelBuilder.Entity<SinhVien>(entity => {
            entity.HasKey(e => e.Mssv);
            entity.ToTable("SinhVien");
            entity.Property(e => e.Mssv).HasColumnName("MSSV").HasMaxLength(10).IsUnicode(false);

            entity.HasOne(d => d.MaLopNavigation).WithMany(p => p.SinhViens).HasForeignKey(d => d.MaLop);
            entity.HasOne(d => d.MaTaiKhoanNavigation).WithOne(p => p.SinhVien)
                  .HasForeignKey<SinhVien>(d => d.MaTaiKhoan)
                  .HasConstraintName("FK_SinhVien_TaiKhoan");
        });

        // 7. GIANG VIEN (1-1 với TaiKhoan)
        modelBuilder.Entity<GiangVien>(entity => {
            entity.HasKey(e => e.MaGv);
            entity.ToTable("GiangVien");
            entity.Property(e => e.MaGv).HasColumnName("MaGV").HasMaxLength(10).IsUnicode(false);

            entity.HasOne(d => d.MaKhoaNavigation).WithMany(p => p.GiangViens).HasForeignKey(d => d.MaKhoa);
            entity.HasOne(d => d.MaTaiKhoanNavigation).WithOne(p => p.GiangVien)
                  .HasForeignKey<GiangVien>(d => d.MaTaiKhoan)
                  .HasConstraintName("FK_GiangVien_TaiKhoan");
        });

        // 8. DANG KY HOC (Đã fix cứng các cột khóa ngoại)
        modelBuilder.Entity<DangKyHoc>(entity => {
            entity.HasKey(e => e.MaDangKy);
            entity.ToTable("DangKyHoc");
            entity.Property(e => e.Mssv).HasColumnName("MSSV").HasMaxLength(10).IsUnicode(false);

            entity.HasOne(d => d.MssvNavigation).WithMany(p => p.DangKyHocs).HasForeignKey(d => d.Mssv);
            entity.HasOne(d => d.MaMonNavigation).WithMany(p => p.DangKyHocs).HasForeignKey(d => d.MaMon);
            entity.HasOne(d => d.MaLopNavigation).WithMany(p => p.DangKyHocs).HasForeignKey(d => d.MaLop);
            entity.HasOne(d => d.MaHocKyNavigation).WithMany(p => p.DangKyHocs).HasForeignKey(d => d.MaHocKy);
            entity.HasOne(d => d.MaLhpNavigation).WithMany(p => p.DangKyHocs).HasForeignKey(d => d.MaLhp);
        });

        // 9. KET QUA HOC TAP (1-1 với DangKyHoc)
        modelBuilder.Entity<KetQuaHocTap>(entity => {
            entity.HasKey(e => e.MaKetQua);
            entity.ToTable("KetQuaHocTap");
            entity.HasOne(d => d.MaDangKyNavigation).WithOne(p => p.KetQuaHocTap)
                  .HasForeignKey<KetQuaHocTap>(d => d.MaDangKy);
        });

        // 10. LOP HOC PHAN (Đã làm rỗng WithMany để tránh lỗi CS1061)
        modelBuilder.Entity<LopHocPhan>(entity => {
            entity.HasKey(e => e.MaLhp);
            entity.ToTable("LopHocPhan");

            // 3 ông này trong Model không có danh sách -> Để rỗng ruột
            entity.HasOne(d => d.MaMonNavigation).WithMany().HasForeignKey(d => d.MaMon);
            entity.HasOne(d => d.MaGvNavigation).WithMany().HasForeignKey(d => d.MaGv);
            entity.HasOne(d => d.MaLopNavigation).WithMany().HasForeignKey(d => d.MaLop);

            // 🛑 RIÊNG THẰNG HỌC KỲ THÌ PHẢI ĐIỀN VÀO ĐỂ EF CORE KHÔNG ĐOÁN MÒ NỮA
            entity.HasOne(d => d.MaHocKyNavigation).WithMany(p => p.LopHocPhans).HasForeignKey(d => d.MaHocKy);
        });

        // 12. LICH HOC
        modelBuilder.Entity<LichHoc>(entity => {
            entity.HasKey(e => e.MaLichHoc);
            entity.ToTable("LichHoc");
        });
        //13. Khoá Học
        modelBuilder.Entity<KhoaHoc>(entity => {
            entity.HasKey(e => e.Id);
            entity.ToTable("KhoaHoc");
            entity.Property(e => e.MaKhoaHoc).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.TenKhoaHoc).HasMaxLength(50);
            entity.HasIndex(e => e.MaKhoaHoc).IsUnique();
        });

        modelBuilder.Entity<DonGiaHocPhi>().ToTable("DonGiaHocPhi");
        modelBuilder.Entity<ThanhToanHocPhi>().ToTable("ThanhToanHocPhi");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}