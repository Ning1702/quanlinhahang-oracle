using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Quanlinhahang.Data.Models;

public partial class QuanLyNhaHangContext : DbContext
{
    public QuanLyNhaHangContext()
    {
    }

    public QuanLyNhaHangContext(DbContextOptions<QuanLyNhaHangContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BanPhong> BanPhongs { get; set; }

    public virtual DbSet<BanPhongTrangThai> BanPhongTrangThais { get; set; }

    public virtual DbSet<CauHinhHeThong> CauHinhHeThongs { get; set; }

    public virtual DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }

    public virtual DbSet<DanhMucMon> DanhMucMons { get; set; }

    public virtual DbSet<DatBan> DatBans { get; set; }

    public virtual DbSet<HangThanhVien> HangThanhViens { get; set; }

    public virtual DbSet<HoaDon> HoaDons { get; set; }

    public virtual DbSet<KhachHang> KhachHangs { get; set; }

    public virtual DbSet<KhungGio> KhungGios { get; set; }

    public virtual DbSet<LoaiBanPhong> LoaiBanPhongs { get; set; }

    public virtual DbSet<MonAn> MonAns { get; set; }

    public virtual DbSet<NhanVien> NhanViens { get; set; }

    public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }

    public virtual DbSet<TrangThaiDatBan> TrangThaiDatBans { get; set; }

    public virtual DbSet<TrangThaiHoaDon> TrangThaiHoaDons { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BanPhong>(entity =>
        {
            entity.HasKey(e => e.BanPhongId).HasName("PK__BanPhong__B2D0E957391905AE");

            entity.ToTable("BanPhong");

            entity.Property(e => e.BanPhongId).HasColumnName("BanPhongID");
            entity.Property(e => e.LoaiBanPhongId).HasColumnName("LoaiBanPhongID");
            entity.Property(e => e.TenBanPhong).HasMaxLength(50);
            entity.Property(e => e.TrangThaiId)
                .HasDefaultValue(0)
                .HasColumnName("TrangThaiID");

            entity.HasOne(d => d.LoaiBanPhong).WithMany(p => p.BanPhongs)
                .HasForeignKey(d => d.LoaiBanPhongId)
                .HasConstraintName("FK__BanPhong__LoaiBa__5070F446");

            entity.HasOne(d => d.TrangThai).WithMany(p => p.BanPhongs)
                .HasForeignKey(d => d.TrangThaiId)
                .HasConstraintName("FK__BanPhong__TrangT__5165187F");
        });

        modelBuilder.Entity<BanPhongTrangThai>(entity =>
        {
            entity.HasKey(e => e.TrangThaiId).HasName("PK__BanPhong__D5BF1E859AC44C28");

            entity.ToTable("BanPhongTrangThai");

            entity.Property(e => e.TrangThaiId)
                .ValueGeneratedNever()
                .HasColumnName("TrangThaiID");
            entity.Property(e => e.TenTrangThai).HasMaxLength(50);
        });

        modelBuilder.Entity<CauHinhHeThong>(entity =>
        {
            entity.HasKey(e => e.SettingKey).HasName("PK__CauHinhH__01E719ACB4968250");

            entity.ToTable("CauHinhHeThong");

            entity.Property(e => e.SettingKey).HasMaxLength(50);
            entity.Property(e => e.MoTa).HasMaxLength(255);
        });

        modelBuilder.Entity<ChiTietHoaDon>(entity =>
        {
            entity.HasKey(e => new { e.HoaDonId, e.MonAnId }).HasName("PK__ChiTietH__8B24EBF7E39AAE26");

            entity.ToTable("ChiTietHoaDon", tb => tb.HasTrigger("TRG_TinhTongTien"));

            entity.Property(e => e.HoaDonId).HasColumnName("HoaDonID");
            entity.Property(e => e.MonAnId).HasColumnName("MonAnID");
            entity.Property(e => e.DonGia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ThanhTien)
                .HasComputedColumnSql("([SoLuong]*[DonGia])", true)
                .HasColumnType("decimal(29, 2)");

            entity.HasOne(d => d.HoaDon).WithMany(p => p.ChiTietHoaDons)
                .HasForeignKey(d => d.HoaDonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChiTietHo__HoaDo__73BA3083");

            entity.HasOne(d => d.MonAn).WithMany(p => p.ChiTietHoaDons)
                .HasForeignKey(d => d.MonAnId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChiTietHo__MonAn__74AE54BC");
        });

        modelBuilder.Entity<DanhMucMon>(entity =>
        {
            entity.HasKey(e => e.DanhMucId).HasName("PK__DanhMucM__1C53BA7B114D40A1");

            entity.ToTable("DanhMucMon");

            entity.Property(e => e.DanhMucId).HasColumnName("DanhMucID");
            entity.Property(e => e.TenDanhMuc).HasMaxLength(100);
        });

        modelBuilder.Entity<DatBan>(entity =>
        {
            entity.HasKey(e => e.DatBanId).HasName("PK__DatBan__6A75F719719BD2AF");

            entity.ToTable("DatBan", tb => tb.HasTrigger("TRG_CHAN_SUA_DON"));

            entity.HasIndex(e => e.NgayDen, "IDX_DatBan_NgayDen");

            entity.HasIndex(e => new { e.BanPhongId, e.NgayDen, e.KhungGioId }, "UQ_DatBan").IsUnique();

            entity.Property(e => e.DatBanId).HasColumnName("DatBanID");
            entity.Property(e => e.BanPhongId).HasColumnName("BanPhongID");
            entity.Property(e => e.KhachHangId).HasColumnName("KhachHangID");
            entity.Property(e => e.KhungGioId).HasColumnName("KhungGioID");
            entity.Property(e => e.ThoiGianTaoDon).HasColumnType("datetime");
            entity.Property(e => e.TrangThaiId)
                .HasDefaultValue(1)
                .HasColumnName("TrangThaiID");
            entity.Property(e => e.YeuCauDacBiet).HasMaxLength(2000);

            entity.HasOne(d => d.BanPhong).WithMany(p => p.DatBans)
                .HasForeignKey(d => d.BanPhongId)
                .HasConstraintName("FK__DatBan__BanPhong__6383C8BA");

            entity.HasOne(d => d.KhachHang).WithMany(p => p.DatBans)
                .HasForeignKey(d => d.KhachHangId)
                .HasConstraintName("FK__DatBan__KhachHan__628FA481");

            entity.HasOne(d => d.KhungGio).WithMany(p => p.DatBans)
                .HasForeignKey(d => d.KhungGioId)
                .HasConstraintName("FK__DatBan__KhungGio__6477ECF3");

            entity.HasOne(d => d.TrangThai).WithMany(p => p.DatBans)
                .HasForeignKey(d => d.TrangThaiId)
                .HasConstraintName("FK__DatBan__TrangTha__656C112C");
        });

        modelBuilder.Entity<HangThanhVien>(entity =>
        {
            entity.HasKey(e => e.HangThanhVienId).HasName("PK__HangThan__16F81D7A44C50E10");

            entity.ToTable("HangThanhVien");

            entity.Property(e => e.HangThanhVienId).HasColumnName("HangThanhVienID");
            entity.Property(e => e.TenHang).HasMaxLength(50);
            entity.Property(e => e.TiLeGiamGia)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)");
        });

        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.HasKey(e => e.HoaDonId).HasName("PK__HoaDon__6956CE69DA2D05E8");

            entity.ToTable("HoaDon", tb => tb.HasTrigger("TRG_CongDiemKhiThanhToan"));

            entity.HasIndex(e => e.NgayLap, "IDX_HoaDon_NgayLap");

            entity.HasIndex(e => e.DatBanId, "IDX_Unique_DatBan_HoaDon")
                .IsUnique()
                .HasFilter("([DatBanID] IS NOT NULL)");

            entity.Property(e => e.HoaDonId).HasColumnName("HoaDonID");
            entity.Property(e => e.DatBanId).HasColumnName("DatBanID");
            entity.Property(e => e.NgayLap)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TaiKhoanId).HasColumnName("TaiKhoanID");
            entity.Property(e => e.TongTien)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrangThaiId)
                .HasDefaultValue(1)
                .HasColumnName("TrangThaiID");
            entity.Property(e => e.Vatpercent)
                .HasDefaultValue(10m)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("VATPercent");

            entity.HasOne(d => d.DatBan).WithOne(p => p.HoaDon)
                .HasForeignKey<HoaDon>(d => d.DatBanId)
                .HasConstraintName("FK__HoaDon__DatBanID__6E01572D");

            entity.HasOne(d => d.TaiKhoan).WithMany(p => p.HoaDons)
                .HasForeignKey(d => d.TaiKhoanId)
                .HasConstraintName("FK__HoaDon__TaiKhoan__6FE99F9F");

            entity.HasOne(d => d.TrangThai).WithMany(p => p.HoaDons)
                .HasForeignKey(d => d.TrangThaiId)
                .HasConstraintName("FK__HoaDon__TrangTha__6EF57B66");
        });

        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.HasKey(e => e.KhachHangId).HasName("PK__KhachHan__880F211BA731DD9D");

            entity.ToTable("KhachHang");

            entity.HasIndex(e => e.SoDienThoai, "IDX_KhachHang_SDT");

            entity.HasIndex(e => e.TaiKhoanId, "IDX_Unique_TaiKhoan_KhachHang")
                .IsUnique()
                .HasFilter("([TaiKhoanID] IS NOT NULL)");

            entity.HasIndex(e => e.SoDienThoai, "UQ__KhachHan__0389B7BDBEEE7DC8").IsUnique();

            entity.Property(e => e.KhachHangId).HasColumnName("KhachHangID");
            entity.Property(e => e.DiaChi).HasMaxLength(255);
            entity.Property(e => e.DiemTichLuy).HasDefaultValue(0);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.HangThanhVienId).HasColumnName("HangThanhVienID");
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SoDienThoai).HasMaxLength(20);
            entity.Property(e => e.TaiKhoanId).HasColumnName("TaiKhoanID");

            entity.HasOne(d => d.HangThanhVien).WithMany(p => p.KhachHangs)
                .HasForeignKey(d => d.HangThanhVienId)
                .HasConstraintName("FK__KhachHang__HangT__48CFD27E");

            entity.HasOne(d => d.TaiKhoan).WithOne(p => p.KhachHang)
                .HasForeignKey<KhachHang>(d => d.TaiKhoanId)
                .HasConstraintName("FK__KhachHang__TaiKh__49C3F6B7");
        });

        modelBuilder.Entity<KhungGio>(entity =>
        {
            entity.HasKey(e => e.KhungGioId).HasName("PK__KhungGio__CC9AB36A8F11D60B");

            entity.ToTable("KhungGio");

            entity.Property(e => e.KhungGioId).HasColumnName("KhungGioID");
            entity.Property(e => e.GioBatDau).HasPrecision(0);
            entity.Property(e => e.GioKetThuc).HasPrecision(0);
            entity.Property(e => e.TenKhungGio).HasMaxLength(50);
        });

        modelBuilder.Entity<LoaiBanPhong>(entity =>
        {
            entity.HasKey(e => e.LoaiBanPhongId).HasName("PK__LoaiBanP__BA742BBFB15A2947");

            entity.ToTable("LoaiBanPhong");

            entity.Property(e => e.LoaiBanPhongId).HasColumnName("LoaiBanPhongID");
            entity.Property(e => e.PhuThu)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TenLoai).HasMaxLength(100);
        });

        modelBuilder.Entity<MonAn>(entity =>
        {
            entity.HasKey(e => e.MonAnId).HasName("PK__MonAn__272259EF4032FEB4");

            entity.ToTable("MonAn");

            entity.HasIndex(e => e.TenMon, "IDX_MonAn_TenMon");

            entity.HasIndex(e => e.TenMon, "UQ__MonAn__332EF565E6D7A26F").IsUnique();

            entity.Property(e => e.MonAnId).HasColumnName("MonAnID");
            entity.Property(e => e.DanhMucId).HasColumnName("DanhMucID");
            entity.Property(e => e.DonGia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.HinhAnhUrl)
                .HasMaxLength(255)
                .HasColumnName("HinhAnhURL");
            entity.Property(e => e.LoaiMon).HasMaxLength(50);
            entity.Property(e => e.MoTa).HasMaxLength(2000);
            entity.Property(e => e.TenMon).HasMaxLength(150);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("Còn bán");

            entity.HasOne(d => d.DanhMuc).WithMany(p => p.MonAns)
                .HasForeignKey(d => d.DanhMucId)
                .HasConstraintName("FK__MonAn__DanhMucID__571DF1D5");
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.HasKey(e => e.NhanVienId).HasName("PK__NhanVien__E27FD7EAFF2F3869");

            entity.ToTable("NhanVien");

            entity.HasIndex(e => e.TaiKhoanId, "UQ__NhanVien__9A124B6496CF3612").IsUnique();

            entity.Property(e => e.NhanVienId).HasColumnName("NhanVienID");
            entity.Property(e => e.ChucVu).HasMaxLength(50);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.SoDienThoai).HasMaxLength(20);
            entity.Property(e => e.TaiKhoanId).HasColumnName("TaiKhoanID");
            entity.Property(e => e.TrangThai).HasMaxLength(50);

            entity.HasOne(d => d.TaiKhoan).WithOne(p => p.NhanVien)
                .HasForeignKey<NhanVien>(d => d.TaiKhoanId)
                .HasConstraintName("FK__NhanVien__TaiKho__5AEE82B9");
        });

        modelBuilder.Entity<TaiKhoan>(entity =>
        {
            entity.HasKey(e => e.TaiKhoanId).HasName("PK__TaiKhoan__9A124B65A3B3D93A");

            entity.ToTable("TaiKhoan");

            entity.HasIndex(e => e.TenDangNhap, "IDX_TaiKhoan_TenDangNhap");

            entity.HasIndex(e => e.TenDangNhap, "UQ__TaiKhoan__55F68FC05520F7F5").IsUnique();

            entity.Property(e => e.TaiKhoanId).HasColumnName("TaiKhoanID");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.MatKhauHash).HasMaxLength(255);
            entity.Property(e => e.TenDangNhap).HasMaxLength(50);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("Hoạt động");
            entity.Property(e => e.VaiTro).HasMaxLength(20);
        });

        modelBuilder.Entity<TrangThaiDatBan>(entity =>
        {
            entity.HasKey(e => e.TrangThaiId).HasName("PK__TrangTha__D5BF1E851562BD50");

            entity.ToTable("TrangThaiDatBan");

            entity.Property(e => e.TrangThaiId)
                .ValueGeneratedNever()
                .HasColumnName("TrangThaiID");
            entity.Property(e => e.TenTrangThai).HasMaxLength(50);
        });

        modelBuilder.Entity<TrangThaiHoaDon>(entity =>
        {
            entity.HasKey(e => e.TrangThaiId).HasName("PK__TrangTha__D5BF1E856404A220");

            entity.ToTable("TrangThaiHoaDon");

            entity.Property(e => e.TrangThaiId).HasColumnName("TrangThaiID");
            entity.Property(e => e.TenTrangThai).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}