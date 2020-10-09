using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Manafont.Db.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Manafont.Web.Areas.Identity.Pages.Account.Manage
{
    public class GenerateRecoveryCodesModel : PageModel
    {
        private readonly UserManager<ManafontUser>           _userManager;
        private readonly ILogger<GenerateRecoveryCodesModel> _logger;

        public GenerateRecoveryCodesModel(
            UserManager<ManafontUser> userManager,
            ILogger<GenerateRecoveryCodesModel> logger) {
            _userManager = userManager;
            _logger = logger;
        }

        [TempData]
        public string[] RecoveryCodes { get; set; } = null!;

        [TempData]
        public string StatusMessage { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync() {
            ManafontUser? user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (await _userManager.GetTwoFactorEnabledAsync(user)) return Page();
            string? userId = await _userManager.GetUserIdAsync(user);
            throw new InvalidOperationException($"Cannot generate recovery codes for user with ID " +
                $"'{userId}' because they do not have 2FA enabled.");
        }

        public async Task<IActionResult> OnPostAsync() {
            ManafontUser? user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            bool isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            string? userId = await _userManager.GetUserIdAsync(user);
            if (!isTwoFactorEnabled) {
                throw new InvalidOperationException(
                    $"Cannot generate recovery codes for user with ID '{userId}' as they do not have 2FA " +
                    $"enabled.");
            }

            IEnumerable<string>? recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            RecoveryCodes = recoveryCodes.ToArray();

            _logger.LogInformation("User with ID '{UserId}' has generated new 2FA recovery codes.", userId);
            StatusMessage = "You have generated new recovery codes.";
            return RedirectToPage("./ShowRecoveryCodes");
        }
    }
}