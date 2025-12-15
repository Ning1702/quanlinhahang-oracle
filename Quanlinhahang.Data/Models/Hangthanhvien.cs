using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class Hangthanhvien
{
    public int Hangthanhvienid { get; set; }

    public string Tenhang { get; set; } = null!;

    public string? Mota { get; set; }

    public int Diemtoithieu { get; set; }

    public int? Diemtoida { get; set; }

    public virtual ICollection<Khachhang> Khachhangs { get; set; } = new List<Khachhang>();
}
