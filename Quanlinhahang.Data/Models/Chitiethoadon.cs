using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class Chitiethoadon
{
    public int Hoadonid { get; set; }

    public int Monanid { get; set; }

    public int Soluong { get; set; }

    public long Dongia { get; set; }

    public long Thanhtien { get; set; }

    public virtual Hoadon Hoadon { get; set; } = null!;

    public virtual Monan Monan { get; set; } = null!;
}
