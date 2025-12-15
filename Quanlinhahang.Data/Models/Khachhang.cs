using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class Khachhang
{
    public int Khachhangid { get; set; }

    public string Hoten { get; set; } = null!;

    public string? Email { get; set; }

    public string Sodienthoai { get; set; } = null!;

    public string? Diachi { get; set; }

    public int Diemtichluy { get; set; }

    public int? Hangthanhvienid { get; set; }

    public int? Taikhoanid { get; set; }

    public DateTime Ngaytao { get; set; }

    public string Trangthai { get; set; } = null!;

    public virtual ICollection<Datban> Datbans { get; set; } = new List<Datban>();

    public virtual Hangthanhvien? Hangthanhvien { get; set; }

    public virtual Taikhoan? Taikhoan { get; set; }
}
