using System;
using System.Collections.Generic;

namespace Quanlinhahang.Data.Models;

public partial class DatBan
{
    public int DatBanId { get; set; }

    public int? KhachHangId { get; set; }

    public int? BanPhongId { get; set; }

    public int? KhungGioId { get; set; }

    public DateOnly? NgayDen { get; set; }

    public int? SoNguoi { get; set; }

    public string? YeuCauDacBiet { get; set; }

    public int? TrangThaiId { get; set; }

    public DateTime? ThoiGianTaoDon { get; set; }

    public virtual BanPhong? BanPhong { get; set; }

    public virtual HoaDon? HoaDon { get; set; }

    public virtual KhachHang? KhachHang { get; set; }

    public virtual KhungGio? KhungGio { get; set; }

    public virtual TrangThaiDatBan? TrangThai { get; set; }
}
