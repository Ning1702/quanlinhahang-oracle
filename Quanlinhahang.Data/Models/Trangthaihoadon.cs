using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class TrangThaiHoaDon
{
    public int TrangThaiId { get; set; }

    public string? TenTrangThai { get; set; }

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();
}
