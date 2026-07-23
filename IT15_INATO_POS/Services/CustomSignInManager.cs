using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IT15_INATO_POS.Models;

namespace IT15_INATO_POS.Services
{
    public class CustomSignInManager : SignInManager<ApplicationUser>
    {
        public CustomSignInManager(
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
#pragma warning disable S6672
            ILogger<SignInManager<ApplicationUser>> logger,
#pragma warning restore S6672
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<ApplicationUser> confirmation)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
        }
    }
}