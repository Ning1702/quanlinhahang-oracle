using Microsoft.AspNetCore.Mvc;
using Quanlinhahang.Data.Models;    
using Quanlinhahang_Customer.Models;  

namespace Quanlinhahang_Customer.Models.ViewModels
{
    public class DatBanViewModel
    {
        public string CustomerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime BookingDate { get; set; }
        public string TimeSlot { get; set; } = string.Empty;
        public int GuestCount { get; set; }
        public string? TableType { get; set; }
        public string? Note { get; set; }

        public List<CartItem>? Items { get; set; }

        // [ORACLE FIX]: Sửa BanPhong -> Banphong
        public List<Banphong> DanhSachBan { get; set; } = new List<Banphong>();
    }
}