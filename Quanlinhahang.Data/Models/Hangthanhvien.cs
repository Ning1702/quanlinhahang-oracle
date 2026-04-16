using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class HangThanhVien
{
    public int HangThanhVienId { get; set; }

    public string TenHang { get; set; } = null!;

    public int DiemToiThieu { get; set; }

    public int? DiemToiDa { get; set; }

    public decimal? TiLeGiamGia { get; set; }

    public virtual ICollection<KhachHang> KhachHangs { get; set; } = new List<KhachHang>();
}
