using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class Banphong
{
    public int Banphongid { get; set; }

    public int Loaibanphongid { get; set; }

    public string Tenbanphong { get; set; } = null!;

    public int Succhua { get; set; }

    public bool Trangthai { get; set; }

    public virtual ICollection<Datban> Datbans { get; set; } = new List<Datban>();

    public virtual ICollection<Hoadon> Hoadons { get; set; } = new List<Hoadon>();

    public virtual Loaibanphong Loaibanphong { get; set; } = null!;
}
