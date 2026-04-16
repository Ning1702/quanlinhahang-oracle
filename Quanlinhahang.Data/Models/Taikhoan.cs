using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class TaiKhoan
{
    public int TaiKhoanId { get; set; }

    public string? TenDangNhap { get; set; }

    public string? MatKhauHash { get; set; }

    public string? Email { get; set; }

    public string? VaiTro { get; set; }

    public string? TrangThai { get; set; }

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    public virtual KhachHang? KhachHang { get; set; }

    public virtual NhanVien? NhanVien { get; set; }
}
