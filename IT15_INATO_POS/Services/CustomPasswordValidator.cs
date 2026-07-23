using IT15_INATO_POS.Models;
using Microsoft.AspNetCore.Identity;

namespace IT15_INATO_POS.Services
{
    public class CustomPasswordValidator : IPasswordValidator<ApplicationUser>
    {
        private static readonly HashSet<string> _compromisedPasswords = new HashSet<string>
        {
            "123456", "password", "123456789", "12345678", "12345", "qwerty", "abc123",
            "111111", "admin", "letmein", "welcome", "monkey", "dragon", "master",
            "sunshine", "iloveyou", "654321", "123321"
        };

        public CustomPasswordValidator(ILogger<CustomPasswordValidator> logger)
        {
            // Logger kept for future use but not stored as field
            logger.LogInformation("CustomPasswordValidator initialized");
        }

        public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
        {
            var errors = new List<IdentityError>();
            var pwd = password ?? string.Empty;

            if (pwd.Length < 12)
                errors.Add(new IdentityError { Code = "PasswordTooShort", Description = "Password must be at least 12 characters." });

            if (!pwd.Any(char.IsUpper))
                errors.Add(new IdentityError { Code = "PasswordNoUpper", Description = "Password must contain an uppercase letter." });

            if (!pwd.Any(char.IsLower))
                errors.Add(new IdentityError { Code = "PasswordNoLower", Description = "Password must contain a lowercase letter." });

            if (!pwd.Any(char.IsDigit))
                errors.Add(new IdentityError { Code = "PasswordNoDigit", Description = "Password must contain a number." });

            if (!pwd.Any(c => !char.IsLetterOrDigit(c)))
                errors.Add(new IdentityError { Code = "PasswordNoSpecial", Description = "Password must contain a special character." });

            if (_compromisedPasswords.Contains(pwd.ToLowerInvariant()))
                errors.Add(new IdentityError { Code = "PasswordCompromised", Description = "This password is too common or has been compromised." });

            if (HasSequentialPattern(pwd))
                errors.Add(new IdentityError { Code = "PasswordSequential", Description = "Password cannot contain sequential characters (abcd, 1234)." });

            if (HasRepeatingPattern(pwd))
                errors.Add(new IdentityError { Code = "PasswordRepeating", Description = "Password cannot contain repeating characters (aaaa, 1111)." });

            if (ContainsPersonalInfo(user, pwd))
                errors.Add(new IdentityError { Code = "PasswordPersonalInfo", Description = "Password cannot contain your name or email." });

            return Task.FromResult(errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed(errors.ToArray()));
        }

        private static bool HasSequentialPattern(string password)
        {
            var lower = password.ToLowerInvariant();
            for (int i = 0; i <= lower.Length - 4; i++)
            {
                var seq = true;
                for (int j = 0; j < 3; j++)
                {
                    if (lower[i + j + 1] != lower[i + j] + 1)
                    {
                        seq = false;
                        break;
                    }
                }
                if (seq) return true;
            }
            return false;
        }

        private static bool HasRepeatingPattern(string password)
        {
            for (int i = 0; i <= password.Length - 4; i++)
            {
                if (password[i] == password[i + 1] &&
                    password[i] == password[i + 2] &&
                    password[i] == password[i + 3])
                    return true;
            }
            return false;
        }

        private static bool ContainsPersonalInfo(ApplicationUser user, string password)
        {
            var lowerPwd = password.ToLowerInvariant();

            if (!string.IsNullOrEmpty(user.Email) && lowerPwd.Contains(user.Email.Split('@')[0].ToLowerInvariant()))
                return true;

            if (!string.IsNullOrEmpty(user.FirstName) && lowerPwd.Contains(user.FirstName.ToLowerInvariant()))
                return true;

            if (!string.IsNullOrEmpty(user.LastName) && lowerPwd.Contains(user.LastName.ToLowerInvariant()))
                return true;

            return false;
        }
    }
}