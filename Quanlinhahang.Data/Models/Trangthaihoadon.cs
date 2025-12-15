using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class Trangthaihoadon
{
    public int Trangthaiid { get; set; }

    public string Tentrangthai { get; set; } = null!;

    public virtual ICollection<Hoadon> Hoadons { get; set; } = new List<Hoadon>();
}
