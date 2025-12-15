using System.Collections.Generic;

namespace Quanlinhahang_Customer.Models.ViewModels
{
    public class BookingItemUpdate
    {
        public int MonAnId { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
    }

    public class UpdateBookingViewModel
    {
        public string Username { get; set; } = null!;
        public int DatBanId { get; set; }

        public string BookingDate { get; set; } = null!;
        public string TimeSlot { get; set; } = null!;
        public int GuestCount { get; set; }
        public int? BanPhongId { get; set; } 

        public List<BookingItemUpdate> Items { get; set; } = null!;
    }
}
