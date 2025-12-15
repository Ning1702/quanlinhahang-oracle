using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class Danhmucmon
{
    public int Danhmucid { get; set; }

    public string Tendanhmuc { get; set; } = null!;

    public string? Mota { get; set; }

    public virtual ICollection<Monan> Monans { get; set; } = new List<Monan>();
}
