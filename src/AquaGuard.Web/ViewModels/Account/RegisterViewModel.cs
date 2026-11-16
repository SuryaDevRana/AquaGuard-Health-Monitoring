using System.ComponentModel.DataAnnotations;

namespace AquaGuard.Web.ViewModels.Account;

public sealed class RegisterViewModel
{
    [Required, StringLength(160), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, Display(Name = "Work email")]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 10), DataType(DataType.Password), Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password)), Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
