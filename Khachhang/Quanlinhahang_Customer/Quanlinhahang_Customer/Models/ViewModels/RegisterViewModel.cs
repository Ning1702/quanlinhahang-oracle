using System.ComponentModel.DataAnnotations;

namespace Quanlinhahang_Customer.Models.ViewModels
{
    public class RegisterViewModel
{
    [Required] public string FullName { get; set; } = string.Empty;
    [Required] [EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
    [Required] public string Username { get; set; } = string.Empty;
    [Required] [DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    [DataType(DataType.Password)] [Compare("Password")] public string ConfirmPassword { get; set; } = string.Empty;
}

}
