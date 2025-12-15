using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class Taikhoan
{
    public int Taikhoanid { get; set; }

    public string Tendangnhap { get; set; } = null!;

    public string Matkhauhash { get; set; } = null!;

    public string? Email { get; set; }

    public VaiTroHeThong Vaitro { get; set; }

    public string Trangthai { get; set; } = null!;

    public virtual ICollection<Hoadon> Hoadons { get; set; } = new List<Hoadon>();

    public virtual ICollection<Khachhang> Khachhangs { get; set; } = new List<Khachhang>();

    public virtual ICollection<Nhanvien> Nhanviens { get; set; } = new List<Nhanvien>();
}
