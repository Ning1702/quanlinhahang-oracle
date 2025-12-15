using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class Nhanvien
{
    public int Nhanvienid { get; set; }

    public int? Taikhoanid { get; set; }

    public string Hoten { get; set; } = null!;

    public string? Sodienthoai { get; set; }

    public string? Chucvu { get; set; }

    public DateTime? Ngayvaolam { get; set; }

    public string Trangthai { get; set; } = null!;

    public virtual Taikhoan? Taikhoan { get; set; }
}
