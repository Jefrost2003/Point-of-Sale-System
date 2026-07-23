using System.ComponentModel.DataAnnotations;

namespace IT15_INATO_POS.ViewModels
{
    public class UserManagementViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Display(Name = "Role")]
        public string RoleName { get; set; } = string.Empty;

        [Display(Name = "Active")]
#pragma warning disable S6964
        public bool IsActive { get; set; }
#pragma warning restore S6964

        [Display(Name = "Created Date")]
#pragma warning disable S6964
        public DateTime CreatedDate { get; set; }
#pragma warning restore S6964

        [Display(Name = "Last Login")]
        public DateTime? LastLoginDate { get; set; }

        // New: indicates if user has any transactions or audit logs
#pragma warning disable S6964
        public bool HasActivity { get; set; }
#pragma warning restore S6964
    }

    public class CreateUserViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role")]
        public string RoleName { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}