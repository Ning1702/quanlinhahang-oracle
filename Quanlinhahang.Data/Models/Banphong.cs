using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class BanPhong
{
    public int BanPhongId { get; set; }

    public int? LoaiBanPhongId { get; set; }

    public string? TenBanPhong { get; set; }

    public int? SucChua { get; set; }

    public int? TrangThaiId { get; set; }

    public virtual ICollection<DatBan> DatBans { get; set; } = new List<DatBan>();

    public virtual LoaiBanPhong? LoaiBanPhong { get; set; }

    public virtual BanPhongTrangThai? TrangThai { get; set; }
}
