using System.ComponentModel.DataAnnotations;

namespace Quanlinhahang_Customer.Models.ViewModels
{
    public class UpdateInfoViewModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty; 

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        public string? Address { get; set; }
    }
}
