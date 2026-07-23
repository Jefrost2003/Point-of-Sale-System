// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using IT15_INATO_POS.Models;
using IT15_INATO_POS.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace IT15_INATO_POS.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;
        private readonly ISecurityLogService _securityLogService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LogoutModel(
            SignInManager<ApplicationUser> signInManager,
            ILogger<LogoutModel> logger,
            ISecurityLogService securityLogService,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _logger = logger;
            _securityLogService = securityLogService;
            _userManager = userManager;
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            // Get user info before logout for logging
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                // ✅ Log logout event to Security Logs
                await _securityLogService.LogLogoutAsync(user.Id, user.Email);
            }

            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");

            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                return RedirectToPage();
            }
        }
    }
}