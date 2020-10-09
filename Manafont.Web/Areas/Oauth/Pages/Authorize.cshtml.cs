using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Manafont.Db.Model;
using Manafont.Web.OpenIddict.RazorPages.Helpers;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using OpenIddict.Server.AspNetCore;

namespace Manafont.Web.Areas.Oauth.Pages
{
    [Authorize]
    public class AuthorizeModel : PageModel
    {
        private readonly OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication> _applicationManager;
        private readonly SignInManager<ManafontUser>                                            _signInManager;
        private readonly UserManager<ManafontUser>                                              _userManager;


        public AuthorizeModel(OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication> applicationManager,
            SignInManager<ManafontUser> signInManager, UserManager<ManafontUser> userManager) {
            _applicationManager = applicationManager;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync() {
            OpenIddictRequest? request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            // Retrieve the profile of the logged in user.
            ManafontUser user = await _userManager.GetUserAsync(User) ??
                throw new InvalidOperationException("The user details cannot be retrieved.");

            ClaimsPrincipal? principal = await _signInManager.CreateUserPrincipalAsync(user);

            //Note: in this sample, the granted scopes match the requested scopes
            // but you may want to allow the user to check or uncheck specific scopes
            // Simply restrict the list of scopes but calling SetScopes
            principal.SetScopes(request.GetScopes());
            principal.SetResources("resource_server");

            foreach (var claim in principal.Claims) {
                claim.SetDestinations(ClaimsHelpers.GetDestinations(claim, principal));
            }

            //Returning a signin result will ask OpenIddict to issue the appropriate access and/or identity tokens.
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
    }
}