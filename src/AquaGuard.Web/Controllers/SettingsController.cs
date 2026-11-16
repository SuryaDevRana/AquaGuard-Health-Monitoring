using AquaGuard.Application.EarlyWarnings;
using AquaGuard.Infrastructure.Email;
using AquaGuard.Infrastructure.Identity;
using AquaGuard.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AquaGuard.Web.Controllers;

[Authorize]
public sealed class SettingsController(
    IOptions<EarlyWarningSettings> warningSettings,
    IOptions<SmtpOptions> smtpOptions,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var smtp = smtpOptions.Value;
        var user = await userManager.GetUserAsync(User);
        var model = new SettingsPageViewModel(
            user?.FullName ?? User.Identity?.Name ?? "Signed-in user",
            user?.Email ?? string.Empty,
            warningSettings.Value,
            !string.IsNullOrWhiteSpace(smtp.Host) && !string.IsNullOrWhiteSpace(smtp.FromAddress));
        return View(model);
    }
}
