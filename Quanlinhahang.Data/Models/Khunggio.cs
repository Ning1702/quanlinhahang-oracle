using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class KhungGio
{
    public int KhungGioId { get; set; }

    public string? TenKhungGio { get; set; }

    public TimeOnly GioBatDau { get; set; }

    public TimeOnly GioKetThuc { get; set; }

    public virtual ICollection<DatBan> DatBans { get; set; } = new List<DatBan>();
}
