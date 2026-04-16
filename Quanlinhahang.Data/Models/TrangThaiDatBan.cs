using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class TrangThaiDatBan
{
    public int TrangThaiId { get; set; }

    public string? TenTrangThai { get; set; }

    public virtual ICollection<DatBan> DatBans { get; set; } = new List<DatBan>();
}
