using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class Monan
{
    public int Monanid { get; set; }

    public int Danhmucid { get; set; }

    public string Tenmon { get; set; } = null!;

    public string? Mota { get; set; }

    public long Dongia { get; set; }

    public string? Loaimon { get; set; }

    public string? Hinhanhurl { get; set; }

    public string Trangthai { get; set; } = null!;

    public virtual ICollection<Chitiethoadon> Chitiethoadons { get; set; } = new List<Chitiethoadon>();

    public virtual Danhmucmon Danhmuc { get; set; } = null!;
}
