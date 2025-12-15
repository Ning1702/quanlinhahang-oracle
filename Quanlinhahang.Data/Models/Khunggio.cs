using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class Khunggio
{
    public int Khunggioid { get; set; }

    public string Tenkhunggio { get; set; } = null!;

    public string Giobatdau { get; set; } = null!;

    public string Gioketthuc { get; set; } = null!;

    public virtual ICollection<Datban> Datbans { get; set; } = new List<Datban>();
}
