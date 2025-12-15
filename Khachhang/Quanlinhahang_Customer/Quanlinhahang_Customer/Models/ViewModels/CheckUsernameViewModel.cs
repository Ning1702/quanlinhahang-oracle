using System.ComponentModel.DataAnnotations;

namespace Quanlinhahang_Customer.Models.ViewModels
{
    public class CheckUsernameViewModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;
    }
}
