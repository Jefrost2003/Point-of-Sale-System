// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using IT15_INATO_POS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace IT15_INATO_POS.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);

            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                TempData["SuccessMessage"] = "If an account exists with this email, a password reset link has been sent.";
                return Page();
            }

            try
            {
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                var emailBody = BuildEmailBody(user, callbackUrl);

                await _emailSender.SendEmailAsync(Input.Email, "Reset Your OOTD System Password", emailBody);

                TempData["SuccessMessage"] = "A password reset link has been sent to your email address. Please check your inbox.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to: {Email}", Input.Email);
                TempData["ErrorMessage"] = "Unable to send reset email at this time. Please try again later.";
            }

            return Page();
        }

        private static string BuildEmailBody(ApplicationUser user, string callbackUrl)
        {
            string friendlyName = GetFriendlyName(user);
            string encodedUrl = HtmlEncoder.Default.Encode(callbackUrl);

            return @$"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Password Reset - OOTD System</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f7fc; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 12px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; padding: 20px 0; border-bottom: 3px solid #4F46E5; }}
        .logo {{ max-width: 120px; height: auto; margin-bottom: 15px; }}
        .header h1 {{ color: #1E293B; font-size: 24px; margin: 0; font-weight: 600; }}
        .content {{ padding: 30px 20px; }}
        .greeting {{ font-size: 18px; font-weight: 600; color: #1E293B; margin-bottom: 20px; }}
        .button {{ display: inline-block; background: linear-gradient(135deg, #4F46E5 0%, #4338CA 100%); color: white !important; text-decoration: none; padding: 14px 32px; border-radius: 8px; font-weight: 600; font-size: 16px; margin: 20px 0; text-align: center; }}
        .link-container {{ margin: 20px 0; padding: 15px; background-color: #F1F5F9; border-radius: 8px; word-break: break-all; }}
        .footer {{ text-align: center; padding: 20px; border-top: 1px solid #E2E8F0; font-size: 12px; color: #94A3B8; }}
        .warning {{ background-color: #FEF3C7; border-left: 4px solid #F59E0B; padding: 12px; margin: 20px 0; font-size: 13px; color: #92400E; border-radius: 6px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <img src=""https://ootd.runasp.net/images/ootd.png"" alt=""OOTD Logo"" class=""logo"">
            <h1>OOTD System</h1>
            <p>Point of Sale & Inventory Management</p>
        </div>
        <div class=""content"">
            <div class=""greeting"">Hello <strong>{friendlyName}</strong>,</div>
            <p>We received a request to reset your password for your OOTD System account.</p>
            <div style=""text-align: center;"">
                <a href='{encodedUrl}' class=""button"">Reset My Password</a>
            </div>
            <div class=""link-container"">
                <strong>If the button doesn't work, copy and paste this link:</strong><br>
                <a href='{encodedUrl}'>{encodedUrl}</a>
            </div>
            <div class=""warning"">
                <strong>Security Notice:</strong> This link will expire in 24 hours. If you did not request a password reset, please ignore this email.
            </div>
        </div>
        <div class=""footer"">
            <p>Thank you for using OOTD System</p>
            <p>© 2026 OOTD System - All Rights Reserved</p>
        </div>
    </div>
</body>
</html>";
        }

        private static string GetFriendlyName(ApplicationUser user)
        {
            string emailPrefix = user?.Email?.Split('@')[0]?.ToLowerInvariant() ?? "";

            if (emailPrefix.Contains("admin")) return "Admin";
            if (emailPrefix.Contains("cashier")) return "Cashier";
            if (emailPrefix.Contains("manager")) return "Store Manager";
            if (emailPrefix.Contains("inventory")) return "Inventory Clerk";
            if (emailPrefix.Contains("inatojepoy")) return "Super Admin";
            if (!string.IsNullOrEmpty(emailPrefix))
                return char.ToUpper(emailPrefix[0]) + (emailPrefix.Length > 1 ? emailPrefix.Substring(1) : "");

            return "User";
        }
    }
}