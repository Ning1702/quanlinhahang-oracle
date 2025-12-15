namespace Quanlinhahang_Staff.Models.ViewModels
{
    public class TaiKhoanStaffVM
    {
        // ===== Nhân viên =====
        public int NhanVienID { get; set; }
        public string? HoTen { get; set; }
        public string? SoDienThoai { get; set; }
        public string? ChucVu { get; set; }

        public DateTime? NgayVaoLam { get; set; }
        public string? TrangThaiNV { get; set; }

        // ===== Tài khoản =====
        public int TaiKhoanID { get; set; }

        public string? TenDangNhap { get; set; }
        public string? Email { get; set; }
        public string? VaiTro { get; set; }
        public string? TrangThaiTK { get; set; }

        // === MẬT KHẨU HIỂN THỊ ===
        public string MatKhauHash { get; set; } = "";

        // === MẬT KHẨU MỚI (user nhập khi chỉnh sửa) ===
        public string? MatKhau { get; set; }
    }
}