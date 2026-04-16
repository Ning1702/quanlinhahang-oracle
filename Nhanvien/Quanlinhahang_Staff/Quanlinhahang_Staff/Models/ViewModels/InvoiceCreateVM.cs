using System;
using System.ComponentModel.DataAnnotations;

namespace Quanlinhahang_Staff.Models.ViewModels
{
    public class InvoiceCreateVM
    {
        public int? KhachHangID { get; set; } // Khách hàng (có thể null nếu là khách vãng lai)

        public int? BanPhongID { get; set; } // Bàn phòng

        [Required(ErrorMessage = "Vui lòng chọn ngày đến")]
        public DateTime NgayDen { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Vui lòng chọn khung giờ")]
        public int KhungGioID { get; set; }
    }
}