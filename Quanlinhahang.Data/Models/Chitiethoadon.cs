using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class ChiTietHoaDon
{
    public int HoaDonId { get; set; }

    public int MonAnId { get; set; }

    public int? SoLuong { get; set; }

    public decimal? DonGia { get; set; }

    public decimal? ThanhTien { get; set; }

    public virtual HoaDon HoaDon { get; set; } = null!;

    public virtual MonAn MonAn { get; set; } = null!;
}
