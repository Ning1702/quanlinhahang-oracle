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

    public virtual DbSet<Banphong> Banphongs { get; set; }

    public virtual DbSet<Chitiethoadon> Chitiethoadons { get; set; }

    public virtual DbSet<Danhmucmon> Danhmucmons { get; set; }

    public virtual DbSet<Datban> Datbans { get; set; }

    public virtual DbSet<Hangthanhvien> Hangthanhviens { get; set; }

    public virtual DbSet<Hoadon> Hoadons { get; set; }

    public virtual DbSet<Khachhang> Khachhangs { get; set; }

    public virtual DbSet<Khunggio> Khunggios { get; set; }

    public virtual DbSet<Loaibanphong> Loaibanphongs { get; set; }

    public virtual DbSet<Monan> Monans { get; set; }

    public virtual DbSet<Nhanvien> Nhanviens { get; set; }

    public virtual DbSet<Taikhoan> Taikhoans { get; set; }

    public virtual DbSet<Trangthaihoadon> Trangthaihoadons { get; set; }

    // Quanlinhahang.Data/Models/QuanLyNhaHangContext.cs



    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseOracle("User Id=system;Password=abc123;Data Source=192.168.56.111:1521/orcl;", b =>
            {
                b.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19);
            });
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("USING_NLS_COMP");

        modelBuilder.Entity<Banphong>(entity =>
        {
            entity.HasKey(e => e.Banphongid).HasName("SYS_C008021");

            entity.ToTable("BANPHONG");

            entity.Property(e => e.Banphongid)
                .ValueGeneratedOnAdd()
                .HasColumnType("NUMBER")
                .HasColumnName("BANPHONGID");
            entity.Property(e => e.Loaibanphongid)
                .HasColumnType("NUMBER")
                .HasColumnName("LOAIBANPHONGID");
            entity.Property(e => e.Succhua)
                .HasColumnType("NUMBER")
                .HasColumnName("SUCCHUA");
            entity.Property(e => e.Tenbanphong)
                .HasMaxLength(50)
                .HasColumnName("TENBANPHONG");
            entity.Property(e => e.Trangthai)
                .HasColumnName("TRANGTHAI")
                .HasConversion<int>();

            entity.HasOne(d => d.Loaibanphong).WithMany(p => p.Banphongs)
                .HasForeignKey(d => d.Loaibanphongid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BAN_LOAIBAN");
        });

        modelBuilder.Entity<Chitiethoadon>(entity =>
        {
            entity.HasKey(e => new { e.Hoadonid, e.Monanid });

            entity.ToTable("CHITIETHOADON");

            entity.Property(e => e.Hoadonid)
                .HasColumnType("NUMBER")
                .HasColumnName("HOADONID");
            entity.Property(e => e.Monanid)
                .HasColumnType("NUMBER")
                .HasColumnName("MONANID");
            entity.Property(e => e.Dongia)
                .HasColumnType("NUMBER(18,2)")
                .HasColumnName("DONGIA");
            entity.Property(e => e.Soluong)
                .HasDefaultValueSql("1 ")
                .HasColumnType("NUMBER")
                .HasColumnName("SOLUONG");
            entity.Property(e => e.Thanhtien)
                .HasColumnType("NUMBER(18,2)")
                .HasColumnName("THANHTIEN");

            entity.HasOne(d => d.Hoadon).WithMany(p => p.Chitiethoadons)
                .HasForeignKey(d => d.Hoadonid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTHD_HOADON");

            entity.HasOne(d => d.Monan).WithMany(p => p.Chitiethoadons)
                .HasForeignKey(d => d.Monanid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTHD_MONAN");
        });

        modelBuilder.Entity<Danhmucmon>(entity =>
        {
            entity.HasKey(e => e.Danhmucid).HasName("SYS_C007999");

            entity.ToTable("DANHMUCMON");

            entity.Property(e => e.Danhmucid)
                .ValueGeneratedOnAdd()
                .HasColumnType("NUMBER")
                .HasColumnName("DANHMUCID");
            entity.Property(e => e.Mota)
                .HasMaxLength(255)
                .HasColumnName("MOTA");
            entity.Property(e => e.Tendanhmuc)
                .HasMaxLength(100)
                .HasColumnName("TENDANHMUC");
        });

        modelBuilder.Entity<Datban>(entity =>
        {
            entity.HasKey(e => e.Datbanid).HasName("SYS_C008036");

            entity.ToTable("DATBAN");

            entity.Property(e => e.Datbanid)
                .ValueGeneratedOnAdd()
                .HasColumnType("NUMBER")
                .HasColumnName("DATBANID");
            entity.Property(e => e.Banphongid)
                .HasColumnType("NUMBER")
                .HasColumnName("BANPHONGID");
            entity.Property(e => e.Khachhangid)
                .HasColumnType("NUMBER")
                .HasColumnName("KHACHHANGID");
            entity.Property(e => e.Khunggioid)
                .HasColumnType("NUMBER")
                .HasColumnName("KHUNGGIOID");
            entity.Property(e => e.Ngayden)
                .HasColumnType("DATE")
                .HasColumnName("NGAYDEN");
            entity.Property(e => e.Ngaytao)
                .HasPrecision(6)
                .HasDefaultValueSql("CURRENT_TIMESTAMP ")
                .HasColumnName("NGAYTAO");
            entity.Property(e => e.Songuoi)
                .HasColumnType("NUMBER")
                .HasColumnName("SONGUOI");
            entity.Property(e => e.Tongtiendukien)
                .HasColumnType("NUMBER(18,2)")
                .HasColumnName("TONGTIENDUKIEN");
            entity.Property(e => e.Trangthai)
                .HasMaxLength(30)
                .HasDefaultValueSql("u'Ch\\1edd x\\00e1c nh\\1eadn' ")
                .HasColumnName("TRANGTHAI");
            entity.Property(e => e.Yeucaudacbiet)
                .HasColumnType("NCLOB")
                .HasColumnName("YEUCAUDACBIET");

            entity.HasOne(d => d.Banphong).WithMany(p => p.Datbans)
                .HasForeignKey(d => d.Banphongid)
                .HasConstraintName("FK_DB_BAN");

            entity.HasOne(d => d.Khachhang).WithMany(p => p.Datbans)
                .HasForeignKey(d => d.Khachhangid)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_DB_KHACHHANG");

            entity.HasOne(d => d.Khunggio).WithMany(p => p.Datbans)
                .HasForeignKey(d => d.Khunggioid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DB_KHUNGGIO");
        });

        modelBuilder.Entity<Hangthanhvien>(entity =>
        {
            entity.HasKey(e => e.Hangthanhvienid).HasName("SYS_C007987");

            entity.ToTable("HANGTHANHVIEN");

            entity.Property(e => e.Hangthanhvienid)
                .ValueGeneratedOnAdd()
                .HasColumnType("NUMBER")
                .HasColumnName("HANGTHANHVIENID");
            entity.Property(e => e.Diemtoida)
                .HasColumnType("NUMBER")
                .HasColumnName("DIEMTOIDA");
            entity.Property(e => e.Diemtoithieu)
                .HasColumnType("NUMBER")
                .HasColumnName("DIEMTOITHIEU");
            entity.Property(e => e.Mota)
                .HasMaxLength(255)
                .HasColumnName("MOTA");
            entity.Property(e => e.Tenhang)
                .HasMaxLength(50)
                .HasColumnName("TENHANG");
        });

        modelBuilder.Entity<Hoadon>(entity =>
        {
            entity.HasKey(e => e.Hoadonid).HasName("SYS_C008052");

            entity.ToTable("HOADON");

            entity.Property(e => e.Hoadonid)
                .ValueGeneratedOnAdd()
                .HasColumnType("NUMBER")
                .HasColumnName("HOADONID");
            entity.Property(e => e.Banphongid)
                .HasColumnType("NUMBER")
                .HasColumnName("BANPHONGID");
            entity.Property(e => e.Datbanid)
                .HasColumnType("NUMBER")
                .HasColumnName("DATBANID");
            entity.Property(e => e.Diemcong)
                .HasDefaultValueSql("0 ")
                .HasColumnType("NUMBER")
                .HasColumnName("DIEMCONG");
            entity.Property(e => e.Diemsudung)
                .HasDefaultValueSql("0 ")
                .HasColumnType("NUMBER")
                .HasColumnName("DIEMSUDUNG");
            entity.Property(e => e.Giamgia)
                .HasDefaultValueSql("0 ")
                .HasColumnType("NUMBER(18,2)")
                .HasColumnName("GIAMGIA");
            entity.Property(e => e.Hinhthucthanhtoan)
                .HasMaxLength(50)
                .HasColumnName("HINHTHUCTHANHTOAN");
            entity.Property(e => e.Ngaylap)
                .HasPrecision(6)
                .HasDefaultValueSql("CURRENT_TIMESTAMP ")
                .HasColumnName("NGAYLAP");
            entity.Property(e => e.Taikhoanid)
                .HasColumnType("NUMBER")
                .HasColumnName("TAIKHOANID");
            entity.Property(e => e.Tongtien)
                .HasColumnType("NUMBER(18,2)")
                .HasColumnName("TONGTIEN");
            entity.Property(e => e.Trangthaiid)
                .HasDefaultValueSql("1 ")
                .HasColumnType("NUMBER")
                .HasColumnName("TRANGTHAIID");
            entity.Property(e => e.Vat)
                .HasDefaultValueSql("0.10")
                .HasColumnType("NUMBER(5,2)")
                .HasColumnName("VAT");

            entity.HasOne(d => d.Banphong).WithMany(p => p.Hoadons)
                .HasForeignKey(d => d.Banphongid)
                .HasConstraintName("FK_HD_BAN");

            entity.HasOne(d => d.Datban).WithMany(p => p.Hoadons)
                .HasForeignKey(d => d.Datbanid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HD_DATBAN");

            entity.HasOne(d => d.Taikhoan).WithMany(p => p.Hoadons)
                .HasForeignKey(d => d.Taikhoanid)
                .HasConstraintName("FK_HD_TAIKHOAN");

            entity.HasOne(d => d.Trangthai).WithMany(p => p.Hoadons)
                .HasForeignKey(d => d.Trangthaiid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HD_TRANGTHAI");
        });

        modelBuilder.Entity<Khachhang>(entity =>
        {
            entity.HasKey(e => e.Khachhangid).HasName("SYS_C008013");

            entity.ToTable("KHACHHANG");

            entity.Property(e => e.Khachhangid)
                .ValueGeneratedOnAdd()
                .HasColumnType("NUMBER")
                .HasColumnName("KHACHHANGID");
            entity.Property(e => e.Diachi)
                .HasMaxLength(255)
                .HasColumnName("DIACHI");
            entity.Property(e => e.Diemtichluy)
                .HasDefaultValueSql("0 ")
                .HasColumnType("NUMBER")
                .HasColumnName("DIEMTICHLUY");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("EMAIL");
            entity.Property(e => e.Hangthanhvienid)
                .HasColumnType("NUMBER")
                .HasColumnName("HANGTHANHVIENID");
            entity.Property(e => e.Hoten)
                .HasMaxLength(100)
                .HasColumnName("HOTEN");
            entity.Property(e => e.Ngaytao)
                .HasPrecision(6)
                .HasDefaultValueSql("CURRENT_TIMESTAMP ")
                .HasColumnName("NGAYTAO");
            entity.Property(e => e.Sodienthoai)
                .HasMaxLength(20)
                .HasColumnName("SODIENTHOAI");
            entity.Property(e => e.Taikhoanid)
                .HasColumnType("NUMBER")
                .HasColumnName("TAIKHOANID");
            entity.Property(e => e.Trangthai)
                .HasMaxLength(20)
                .HasDefaultValueSql("u'Ho\\1ea1t \\0111\\1ed9ng' ")
                .HasColumnName("TRANGTHAI");

            entity.HasOne(d => d.Hangthanhvien).WithMany(p => p.Khachhangs)
                .HasForeignKey(d => d.Hangthanhvienid)
                .HasConstraintName("FK_KH_HANGTV");

            entity.HasOne(d => d.Taikhoan)
                .WithMany(p => p.Khachhangs)
                .HasForeignKey(d => d.Taikhoanid)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Khunggio>(entity =>
        {
            entity.HasKey(e => e.Khunggioid).HasName("SYS_C007992");

            entity.ToTable("KHUNGGIO");

            entity.Property(e => e.Khunggioid)
                .ValueGeneratedOnAdd()
                .HasColumnType("NUMBER")
                .HasColumnName("KHUNGGIOID");
            entity.Property(e => e.Giobatdau)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("GIOBATDAU");
            entity.Property(e => e.Gioketthuc)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("GIOKETTHUC");
            entity.Property(e => e.Tenkhunggio)
                .HasMaxLength(50)
                .HasColumnName("TENKHUNGGIO");
        });

        modelBuilder.Entity<Loaibanphong>(entity =>
        {
            entity.HasKey(e => e.Loaibanphongid).HasName("SYS_C007996");

            entity.ToTable("LOAIBANPHONG");

            entity.Property(e => e.Loaibanphongid)
                .ValueGeneratedOnAdd()
                .HasColumnType("NUMBER")
                .HasColumnName("LOAIBANPHONGID");
            entity.Property(e => e.Mota)
                .HasMaxLength(255)
                .HasColumnName("MOTA");
            entity.Property(e => e.Phuthu)
                .HasDefaultValueSql("0 ")
                .HasColumnType("NUMBER(18,2)")
                .HasColumnName("PHUTHU");
            entity.Property(e => e.Tenloai)
                .HasMaxLength(100)
                .HasColumnName("TENLOAI");
        });

        modelBuilder.Entity<Monan>(entity =>
        {
            entity.HasKey(e => e.Monanid).HasName("SYS_C008028");

            entity.ToTable("MONAN");

            entity.Property(e => e.Monanid)
                .ValueGeneratedOnAdd()
                .HasColumnType("NUMBER")
                .HasColumnName("MONANID");
            entity.Property(e => e.Danhmucid)
                .HasColumnType("NUMBER")
                .HasColumnName("DANHMUCID");
            entity.Property(e => e.Dongia)
                .HasColumnType("NUMBER(18,2)")
                .HasColumnName("DONGIA");
            entity.Property(e => e.Hinhanhurl)
                .HasMaxLength(255)
                .HasColumnName("HINHANHURL");
            entity.Property(e => e.Loaimon)
                .HasMaxLength(50)
                .HasColumnName("LOAIMON");
            entity.Property(e => e.Mota)
                .HasColumnType("NCLOB")
                .HasColumnName("MOTA");
            entity.Property(e => e.Tenmon)
                .HasMaxLength(150)
                .HasColumnName("TENMON");
            entity.Property(e => e.Trangthai)
                .HasMaxLength(20)
                .HasDefaultValueSql("u'C\\00f2n b\\00e1n' ")
                .HasColumnName("TRANGTHAI");

            entity.HasOne(d => d.Danhmuc).WithMany(p => p.Monans)
                .HasForeignKey(d => d.Danhmucid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MON_DANHMUC");
        });

        modelBuilder.Entity<Nhanvien>(entity =>
        {
            entity.HasKey(e => e.Nhanvienid).HasName("SYS_C008068");

            entity.ToTable("NHANVIEN");

            entity.Property(e => e.Nhanvienid)
                .ValueGeneratedOnAdd()
                .HasColumnType("NUMBER")
                .HasColumnName("NHANVIENID");
            entity.Property(e => e.Chucvu)
                .HasMaxLength(50)
                .HasColumnName("CHUCVU");
            entity.Property(e => e.Hoten)
                .HasMaxLength(100)
                .HasColumnName("HOTEN");
            entity.Property(e => e.Ngayvaolam)
                .HasColumnType("DATE")
                .HasColumnName("NGAYVAOLAM");
            entity.Property(e => e.Sodienthoai)
                .HasMaxLength(20)
                .HasColumnName("SODIENTHOAI");
            entity.Property(e => e.Taikhoanid)
                .HasColumnType("NUMBER")
                .HasColumnName("TAIKHOANID");
            entity.Property(e => e.Trangthai)
                .HasMaxLength(20)
                .HasDefaultValueSql("u'\\0110ang l\\00e0m' ")
                .HasColumnName("TRANGTHAI");

            entity.HasOne(d => d.Taikhoan).WithMany(p => p.Nhanviens)
                .HasForeignKey(d => d.Taikhoanid)
                .HasConstraintName("FK_NV_TAIKHOAN");
        });

        modelBuilder.Entity<Taikhoan>(entity =>
        {
            entity.HasKey(e => e.Taikhoanid).HasName("SYS_C008005");

            entity.ToTable("TAIKHOAN");

            entity.HasIndex(e => e.Tendangnhap, "SYS_C008006").IsUnique();

            entity.Property(e => e.Taikhoanid)
                .ValueGeneratedOnAdd()
                .HasColumnType("NUMBER")
                .HasColumnName("TAIKHOANID");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("EMAIL");
            entity.Property(e => e.Matkhauhash)
                .HasMaxLength(255)
                .HasColumnName("MATKHAUHASH");
            entity.Property(e => e.Tendangnhap)
                .HasMaxLength(50)
                .HasColumnName("TENDANGNHAP");
            entity.Property(e => e.Trangthai)
                .HasMaxLength(20)
                .HasDefaultValueSql("u'Ho\\1ea1t \\0111\\1ed9ng' ")
                .HasColumnName("TRANGTHAI");
            entity.Property(e => e.Vaitro)
            .HasColumnName("VAITRO")
            .HasMaxLength(20)
            .HasConversion(
                v => v.ToString(),
                v => (VaiTroHeThong)Enum.Parse(typeof(VaiTroHeThong), v)
            );
        });

        modelBuilder.Entity<Trangthaihoadon>(entity =>
        {
            entity.HasKey(e => e.Trangthaiid).HasName("SYS_C008042");

            entity.ToTable("TRANGTHAIHOADON");

            entity.HasIndex(e => e.Tentrangthai, "SYS_C008043").IsUnique();

            entity.Property(e => e.Trangthaiid)
                .ValueGeneratedOnAdd()
                .HasColumnType("NUMBER")
                .HasColumnName("TRANGTHAIID");
            entity.Property(e => e.Tentrangthai)
                .HasMaxLength(50)
                .HasColumnName("TENTRANGTHAI");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
