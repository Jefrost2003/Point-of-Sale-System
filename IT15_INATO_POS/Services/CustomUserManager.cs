using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IT15_INATO_POS.Models;
using System.Text.Json;

namespace IT15_INATO_POS.Services
{
    public class CustomUserManager : UserManager<ApplicationUser>
    {
        private readonly ILogger<CustomUserManager> _logger;

        public CustomUserManager(
            IUserStore<ApplicationUser> store,
            IOptions<IdentityOptions> optionsAccessor,
            IPasswordHasher<ApplicationUser> passwordHasher,
            IEnumerable<IUserValidator<ApplicationUser>> userValidators,
            IEnumerable<IPasswordValidator<ApplicationUser>> passwordValidators,
            ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors,
            IServiceProvider services,
            ILogger<CustomUserManager> logger)
            : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
        {
            _logger = logger;
        }

        public override async Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword)
        {
            var result = await base.ChangePasswordAsync(user, currentPassword, newPassword);

            if (result.Succeeded)
            {
                user.PasswordLastChanged = DateTime.UtcNow;

                var history = string.IsNullOrEmpty(user.PasswordHistory)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(user.PasswordHistory) ?? new List<string>();

                history.Insert(0, user.PasswordHash ?? string.Empty);
                if (history.Count > 5)
                    history.RemoveAt(5);

                user.PasswordHistory = JsonSerializer.Serialize(history);

                await UpdateAsync(user);
#pragma warning disable S2629
                _logger.LogInformation($"Password changed for user: {user.Email}");
#pragma warning restore S2629
            }

            return result;
        }

        public override async Task<IdentityResult> CreateAsync(ApplicationUser user, string password)
        {
            user.PasswordLastChanged = DateTime.UtcNow;
            user.PasswordHistory = "[]";

            return await base.CreateAsync(user, password);
        }

        // ✅ Fixed: Removed async keyword, returns Task<bool> directly
        public Task<bool> IsPasswordInHistoryAsync(ApplicationUser user, string password)
        {
            if (string.IsNullOrEmpty(user.PasswordHistory))
                return Task.FromResult(false);

            var history = JsonSerializer.Deserialize<List<string>>(user.PasswordHistory) ?? new List<string>();

            foreach (var oldPasswordHash in history)
            {
                if (PasswordHasher.VerifyHashedPassword(user, oldPasswordHash, password) == PasswordVerificationResult.Success)
                    return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}