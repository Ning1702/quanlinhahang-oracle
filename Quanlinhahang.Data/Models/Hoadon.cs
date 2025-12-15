using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class Hoadon
{
    public int Hoadonid { get; set; }

    public int Datbanid { get; set; }

    public int? Banphongid { get; set; }

    public int? Taikhoanid { get; set; }

    public DateTime Ngaylap { get; set; }

    public long Tongtien { get; set; }

    public long Giamgia { get; set; }

    public int Diemcong { get; set; }

    public int Diemsudung { get; set; }

    public string? Hinhthucthanhtoan { get; set; }

    public int Trangthaiid { get; set; }

    public long? Vat { get; set; }

    public virtual Banphong? Banphong { get; set; }

    public virtual ICollection<Chitiethoadon> Chitiethoadons { get; set; } = new List<Chitiethoadon>();

    public virtual Datban Datban { get; set; } = null!;

    public virtual Taikhoan? Taikhoan { get; set; }

    public virtual Trangthaihoadon Trangthai { get; set; } = null!;
}
