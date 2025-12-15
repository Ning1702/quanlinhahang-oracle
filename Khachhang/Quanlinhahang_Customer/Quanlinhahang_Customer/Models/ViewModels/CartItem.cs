namespace Quanlinhahang_Customer.Models.ViewModels
{
    public class CartItem
    {
        public int MonAnId { get; set; }
        public string TenMon { get; set; } = string.Empty;
        public decimal Gia { get; set; }
        public int SoLuong { get; set; }
    }
}
