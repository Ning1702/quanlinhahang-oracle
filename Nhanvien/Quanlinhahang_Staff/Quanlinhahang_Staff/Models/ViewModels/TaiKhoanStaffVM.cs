namespace Quanlinhahang_Staff.Models.ViewModels
{
    public class TaiKhoanStaffVM
    {
        public int NhanVienID { get; set; }
        public string? HoTen { get; set; }
        public string? SoDienThoai { get; set; }
        public string? ChucVu { get; set; }

        public DateTime? NgayVaoLam { get; set; }
        public string? TrangThaiNV { get; set; }

        public int TaiKhoanID { get; set; }

        public string? TenDangNhap { get; set; }
        public string? Email { get; set; }
        public string? VaiTro { get; set; }
        public string? TrangThaiTK { get; set; }

        public string MatKhauHash { get; set; } = "";
        public string? MatKhau { get; set; }
    }
}