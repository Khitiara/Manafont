using System.Threading.Tasks;
using Manafont.Db.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manafont.Web.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel : PageModel
    {
        private readonly UserManager<ManafontUser> _userManager;
        private readonly SignInManager<ManafontUser> _signInManager;

        public IndexModel(
            UserManager<ManafontUser> userManager,
            SignInManager<ManafontUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Username { get; set; } = null!;

        [TempData]
        public string StatusMessage { get; set; } = null!;

        private async Task LoadAsync(ManafontUser user)
        {
            string? userName = await _userManager.GetUserNameAsync(user);

            Username = userName;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            ManafontUser? user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}
