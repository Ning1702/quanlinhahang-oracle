using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class Datban
{
    public int Datbanid { get; set; }

    public int? Khachhangid { get; set; }

    public int? Banphongid { get; set; }

    public int Khunggioid { get; set; }

    public DateTime Ngayden { get; set; }

    public int Songuoi { get; set; }

    public long? Tongtiendukien { get; set; }

    public string? Yeucaudacbiet { get; set; }

    public string Trangthai { get; set; } = null!;

    public DateTime Ngaytao { get; set; }

    public virtual Banphong? Banphong { get; set; }

    public virtual ICollection<Hoadon> Hoadons { get; set; } = new List<Hoadon>();

    public virtual Khachhang? Khachhang { get; set; }

    public virtual Khunggio Khunggio { get; set; } = null!;
}
