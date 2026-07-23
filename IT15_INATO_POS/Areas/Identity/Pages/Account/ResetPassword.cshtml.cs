// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using IT15_INATO_POS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace IT15_INATO_POS.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ResetPasswordModel> _logger;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager, ILogger<ResetPasswordModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 12)]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("Password", ErrorMessage = "The new password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Required]
            public string Code { get; set; }
        }

        public IActionResult OnGet(string code = null)
        {
            if (code == null)
            {
                TempData["ErrorMessage"] = "Invalid password reset link. Please request a new one.";
                return RedirectToPage("./ForgotPassword");
            }

            Input = new InputModel
            {
                Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code))
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                TempData["SuccessMessage"] = "Your password has been reset successfully. Please login with your new password.";
                return RedirectToPage("./Login");
            }

            var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("Password reset successfully for user: {Email}", Input.Email);

                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.ResetAccessFailedCountAsync(user);

                TempData["SuccessMessage"] = "Your password has been reset successfully. Please login with your new password.";
                return RedirectToPage("./Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }
    }
}