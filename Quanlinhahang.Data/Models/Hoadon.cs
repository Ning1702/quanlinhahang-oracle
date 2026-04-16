using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class HoaDon
{
    public int HoaDonId { get; set; }

    public int? DatBanId { get; set; }

    public DateTime? NgayLap { get; set; }

    public decimal? TongTien { get; set; }

    public decimal? Vatpercent { get; set; }

    public int? TrangThaiId { get; set; }

    public int? TaiKhoanId { get; set; }

    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();

    public virtual DatBan? DatBan { get; set; }

    public virtual TaiKhoan? TaiKhoan { get; set; }

    public virtual TrangThaiHoaDon? TrangThai { get; set; }
}
