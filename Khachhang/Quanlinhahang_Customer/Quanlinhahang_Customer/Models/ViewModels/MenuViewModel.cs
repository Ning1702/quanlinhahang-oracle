using Quanlinhahang.Data.Models;
using System.Collections.Generic;

namespace Quanlinhahang_Customer.Models.ViewModels
{
    public class MenuViewModel
    {
        public List<MonAn> MonAnList { get; set; } = new List<MonAn>();
        public List<DanhMucMon> DanhMucList { get; set; } = new List<DanhMucMon>();
        public string? Search { get; set; }
        public int? DanhMucId { get; set; }
    }

}

