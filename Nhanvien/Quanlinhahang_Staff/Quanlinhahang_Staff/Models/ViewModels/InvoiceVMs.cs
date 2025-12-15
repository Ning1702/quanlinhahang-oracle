using System.ComponentModel.DataAnnotations;

namespace Quanlinhahang_Staff.Models.ViewModels
{
    // 🔍 Lọc hóa đơn theo trạng thái, ngày, từ khóa
    public class InvoiceFilterVM
    {
        public string? Search { get; set; }

        [DataType(DataType.Date)]
        public DateTime? From { get; set; }

        [DataType(DataType.Date)]
        public DateTime? To { get; set; }
    }

    // 📋 Hàng hiển thị trong danh sách hóa đơn
    public class InvoiceRowVM
    {
        public int HoaDonID { get; set; }
        public DateTime NgayLap { get; set; }

        public DateTime NgayDen { get; set; }
        public string? KhungGio { get; set; }
        public string KhachHang { get; set; } = "";
        public string? SoDienThoai { get; set; }
        public decimal ThanhTien { get; set; }
        public string BanPhong { get; set; } = "";
        public string LoaiBanPhong { get; set; } = "";
        public int TrangThaiID { get; set; }
        public string TrangThaiTen { get; set; } = "";
    }


    // ✏️ ViewModel chỉnh sửa / tạo hóa đơn
    public class InvoiceEditVM
    {
        public int HoaDonID { get; set; }
        public int DatBanID { get; set; }


        // ⭐ BỔ SUNG QUAN TRỌNG: LƯU BÀN / PHÒNG
        [Display(Name = "Bàn / Phòng phục vụ")]
        public int? BanPhongID { get; set; }

        // Các thông tin thanh toán
        [Display(Name = "Giảm giá")]
        public decimal GiamGia { get; set; }

        [Display(Name = "Điểm sử dụng")]
        public int DiemSuDung { get; set; }

        [Display(Name = "Hình thức thanh toán")]
        public string? HinhThucThanhToan { get; set; }

        // Trạng thái hóa đơn
        public string TrangThai { get; set; } = "";

        // Danh sách món trong hóa đơn
        public List<ItemLine> Items { get; set; } = new List<ItemLine>();

        // Lớp con ItemLine
        public class ItemLine
        {
            public int MonAnID { get; set; }
            public string TenMon { get; set; } = "";
            public int SoLuong { get; set; }
            public decimal DonGia { get; set; }

            // Tính tiền tự động
            public decimal ThanhTien => SoLuong * DonGia;
        }
    }
}
