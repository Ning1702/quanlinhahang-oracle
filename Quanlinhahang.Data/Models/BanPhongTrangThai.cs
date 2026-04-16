using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class BanPhongTrangThai
{
    public int TrangThaiId { get; set; }

    public string? TenTrangThai { get; set; }

    public virtual ICollection<BanPhong> BanPhongs { get; set; } = new List<BanPhong>();
}
