using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class Loaibanphong
{
    public int Loaibanphongid { get; set; }

    public string Tenloai { get; set; } = null!;

    public string? Mota { get; set; }

    public long Phuthu { get; set; }

    public virtual ICollection<Banphong> Banphongs { get; set; } = new List<Banphong>();
}
