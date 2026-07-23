using System.ComponentModel.DataAnnotations;
using IT15_INATO_POS.Models;
using IT15_INATO_POS.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IT15_INATO_POS.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
#pragma warning disable S4487
        private readonly ILogger<IndexModel> _logger;
#pragma warning restore S4487
        private readonly ISecurityLogService _securityLogService;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<IndexModel> logger,
            ISecurityLogService securityLogService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _securityLogService = securityLogService;
        }

        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> UserRoles { get; set; } = new List<string>();

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty]
        public PasswordInputModel InputPassword { get; set; } = new();

        public class InputModel
        {
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; } = string.Empty;
        }

        public class PasswordInputModel
        {
            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Current password")]
            public string OldPassword { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 12)]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string NewPassword { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            var userRoles = await _userManager.GetRolesAsync(user);

            Username = userName ?? string.Empty;
            FirstName = user.FirstName ?? string.Empty;
            LastName = user.LastName ?? string.Empty;
            Email = user.Email ?? string.Empty;
            UserRoles = userRoles.ToList();

            Input = new InputModel
            {
                PhoneNumber = phoneNumber ?? string.Empty
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

#pragma warning disable S3776
        public async Task<IActionResult> OnPostAsync()
#pragma warning restore S3776
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

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            if (!string.IsNullOrEmpty(InputPassword.OldPassword) && !string.IsNullOrEmpty(InputPassword.NewPassword))
            {
                var changePasswordResult = await _userManager.ChangePasswordAsync(user, InputPassword.OldPassword, InputPassword.NewPassword);
                if (!changePasswordResult.Succeeded)
                {
                    foreach (var error in changePasswordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    await LoadAsync(user);
                    return Page();
                }

                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    await _securityLogService.LogPasswordChangeAsync(user.Id, user.Email);
                }
                StatusMessage = "Your profile has been updated and password has been changed.";
            }
            else
            {
                StatusMessage = "Your profile has been updated";
            }

            if (user != null)
            {
                await _signInManager.RefreshSignInAsync(user);
            }

            return RedirectToPage();
        }
    }
}