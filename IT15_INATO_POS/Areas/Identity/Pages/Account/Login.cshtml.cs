// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using IT15_INATO_POS.Models;
using IT15_INATO_POS.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace IT15_INATO_POS.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly IReCaptchaService _recaptchaService;
        private readonly ISecurityLogService _securityLogService;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginModel> logger,
            IReCaptchaService recaptchaService,
            ISecurityLogService securityLogService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _recaptchaService = recaptchaService;
            _securityLogService = securityLogService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }

            public string ReCaptchaToken { get; set; } = string.Empty;
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;
        }

#pragma warning disable S3776
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
#pragma warning restore S3776
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            _logger.LogInformation("Login attempt for email: {Email}", Input.Email);

            if (string.IsNullOrEmpty(Input.ReCaptchaToken))
            {
                ModelState.AddModelError(string.Empty, "Security verification failed. Please refresh the page and try again.");
                return Page();
            }

            var isCaptchaValid = await _recaptchaService.VerifyTokenAsync(Input.ReCaptchaToken);
            if (!isCaptchaValid)
            {
                _logger.LogWarning("reCAPTCHA verification failed for user: {Email}", Input.Email);
                ModelState.AddModelError(string.Empty, "Security verification failed. Please try again.");
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            string userId = user?.Id ?? "unknown";

            // Check if user is locked out
            if (user != null && await _userManager.IsLockedOutAsync(user))
            {
                await _securityLogService.LogLockoutAsync(user.Id, Input.Email, 5);
                ModelState.AddModelError(string.Empty, "Account locked out due to multiple failed attempts. Please try again later.");
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(
                Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} logged in successfully.", Input.Email);
                await _securityLogService.LogLoginAttemptAsync(userId, Input.Email, true);

                if (user != null && await _userManager.GetAccessFailedCountAsync(user) > 0)
                {
                    await _userManager.ResetAccessFailedCountAsync(user);
                }
                return LocalRedirect(returnUrl);
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User {Email} account locked out.", Input.Email);
                int failedCount = user != null ? await _userManager.GetAccessFailedCountAsync(user) : 5;
                await _securityLogService.LogLockoutAsync(userId, Input.Email, failedCount);
                ModelState.AddModelError(string.Empty, "Account locked out due to multiple failed attempts. Please try again later.");
                return Page();
            }

            if (result.IsNotAllowed)
            {
                ModelState.AddModelError(string.Empty, "Your account is inactive. Please contact the Super Admin.");
                return Page();
            }

            // Failed login
            _logger.LogWarning("Invalid login attempt for {Email}", Input.Email);
            await _securityLogService.LogLoginAttemptAsync(userId, Input.Email, false);

            if (user != null)
            {
                int failedCount = await _userManager.GetAccessFailedCountAsync(user);
                int remainingAttempts = 5 - failedCount;
                if (remainingAttempts > 0)
                {
                    ModelState.AddModelError(string.Empty, $"Invalid login attempt. You have {remainingAttempts} attempt(s) remaining before your account is locked.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt. Your account has been locked due to multiple failed attempts.");
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return Page();
        }
    }
}