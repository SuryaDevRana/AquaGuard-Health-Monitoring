using System.ComponentModel.DataAnnotations;

namespace AquaGuard.Web.ViewModels.Account;

public sealed class ForgotPasswordViewModel
{
    [Required, EmailAddress, Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;
}
