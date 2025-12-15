using System.ComponentModel.DataAnnotations;

namespace Quanlinhahang_Customer.Models.ViewModels
{
    public class ResetPasswordModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
