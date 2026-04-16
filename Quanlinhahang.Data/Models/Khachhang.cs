using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class KhachHang
{
    public int KhachHangId { get; set; }

    public string? HoTen { get; set; }

    public string? Email { get; set; }

    public string SoDienThoai { get; set; } = null!;

    public string? DiaChi { get; set; }

    public DateTime? NgayTao { get; set; }

    public int? DiemTichLuy { get; set; }

    public int? HangThanhVienId { get; set; }

    public int? TaiKhoanId { get; set; }

    public virtual ICollection<DatBan> DatBans { get; set; } = new List<DatBan>();

    public virtual HangThanhVien? HangThanhVien { get; set; }

    public virtual TaiKhoan? TaiKhoan { get; set; }
}
