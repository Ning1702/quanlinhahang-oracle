using Quanlinhahang.Data.Models;
using System.Collections.Generic;

namespace Quanlinhahang_Customer.Models.ViewModels
{
    public class MenuViewModel
    {
        public List<Monan> MonAnList { get; set; } = new List<Monan>();
        public List<Danhmucmon> DanhMucList { get; set; } = new List<Danhmucmon>();
        public string? Search { get; set; }
        public int? DanhMucId { get; set; }
    }

}

