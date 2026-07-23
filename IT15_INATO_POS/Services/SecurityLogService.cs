using IT15_INATO_POS.Data;
using IT15_INATO_POS.Models;
using Microsoft.EntityFrameworkCore;

namespace IT15_INATO_POS.Services
{
    public interface ISecurityLogService
    {
        Task LogLoginAttemptAsync(string userId, string email, bool success, string? ipAddress = null);
        Task LogLogoutAsync(string userId, string email, string? ipAddress = null);
        Task LogPasswordChangeAsync(string userId, string email);
        Task LogLockoutAsync(string userId, string email, int failedCount);
        Task LogTwoFactorAsync(string userId, string email);
        Task LogEmailChangeAsync(string userId, string email, string oldEmail);
        Task LogRoleChangeAsync(string userId, string email, string oldRole, string newRole);
        Task LogUserCreationAsync(string userId, string email, string createdBy, string role);
        Task LogUserDeletionAsync(string userId, string email, string deletedBy);
        Task<List<SecurityLog>> GetRecentLogsAsync(int count = 100);
        Task<List<SecurityLog>> GetLogsByUserAsync(string userId);
    }

    public class SecurityLogService : ISecurityLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly ILogger<SecurityLogService>? _logger;

        public SecurityLogService(ApplicationDbContext context, IHttpContextAccessor? httpContextAccessor, ILogger<SecurityLogService>? logger = null)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogLoginAttemptAsync(string userId, string email, bool success, string? ipAddress = null)
        {
            try
            {
#pragma warning disable S1192
                var clientIp = ipAddress ?? _httpContextAccessor?.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
#pragma warning restore S1192

                var log = new SecurityLog
                {
                    UserId = userId,
                    EventType = success ? "LoginSuccess" : "LoginFailed",
                    Description = success
                        ? $"User {email} logged in successfully"
                        : $"Failed login attempt for {email}",
                    IpAddress = clientIp,
                    Timestamp = DateTime.UtcNow
                };

                _context.SecurityLogs.Add(log);
                await _context.SaveChangesAsync();

#pragma warning disable S2629
                _logger?.LogInformation($"Security log saved - User: {email}, Success: {success}, IP: {clientIp}");
#pragma warning restore S2629
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to write security log");
            }
        }

        public async Task LogLogoutAsync(string userId, string email, string? ipAddress = null)
        {
            try
            {
                var clientIp = ipAddress ?? _httpContextAccessor?.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                var log = new SecurityLog
                {
                    UserId = userId,
                    EventType = "Logout",
                    Description = $"User {email} logged out",
                    IpAddress = clientIp,
                    Timestamp = DateTime.UtcNow
                };

                _context.SecurityLogs.Add(log);
                await _context.SaveChangesAsync();

#pragma warning disable S2629
                _logger?.LogInformation($"Logout logged for {email} from IP: {clientIp}");
#pragma warning restore S2629
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to write logout log");
            }
        }

        public async Task LogPasswordChangeAsync(string userId, string email)
        {
            try
            {
                var clientIp = _httpContextAccessor?.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                var log = new SecurityLog
                {
                    UserId = userId,
                    EventType = "PasswordChanged",
                    Description = $"User {email} changed their password",
                    IpAddress = clientIp,
                    Timestamp = DateTime.UtcNow
                };

                _context.SecurityLogs.Add(log);
                await _context.SaveChangesAsync();
#pragma warning disable S2629
                _logger?.LogInformation($"Password change logged for {email}");
#pragma warning restore S2629
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to write password change log");
            }
        }

        public async Task LogLockoutAsync(string userId, string email, int failedCount)
        {
            try
            {
                var clientIp = _httpContextAccessor?.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                var log = new SecurityLog
                {
                    UserId = userId,
                    EventType = "AccountLocked",
                    Description = $"Account for {email} has been locked due to {failedCount} failed login attempts",
                    IpAddress = clientIp,
                    Timestamp = DateTime.UtcNow
                };

                _context.SecurityLogs.Add(log);
                await _context.SaveChangesAsync();
#pragma warning disable S2629
                _logger?.LogWarning($"Account locked for {email} after {failedCount} failed attempts");
#pragma warning restore S2629
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to write lockout log");
            }
        }

        public async Task LogTwoFactorAsync(string userId, string email)
        {
            try
            {
                var clientIp = _httpContextAccessor?.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                var log = new SecurityLog
                {
                    UserId = userId,
                    EventType = "TwoFactorEnabled",
                    Description = $"User {email} enabled two-factor authentication",
                    IpAddress = clientIp,
                    Timestamp = DateTime.UtcNow
                };

                _context.SecurityLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to write 2FA log");
            }
        }

        public async Task LogEmailChangeAsync(string userId, string email, string oldEmail)
        {
            try
            {
                var clientIp = _httpContextAccessor?.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                var log = new SecurityLog
                {
                    UserId = userId,
                    EventType = "EmailChanged",
                    Description = $"User changed email from {oldEmail} to {email}",
                    IpAddress = clientIp,
                    Timestamp = DateTime.UtcNow
                };

                _context.SecurityLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to write email change log");
            }
        }

        public async Task LogRoleChangeAsync(string userId, string email, string oldRole, string newRole)
        {
            try
            {
                var clientIp = _httpContextAccessor?.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                var log = new SecurityLog
                {
                    UserId = userId,
                    EventType = "RoleChanged",
                    Description = $"User {email} role changed from {oldRole} to {newRole}",
                    IpAddress = clientIp,
                    Timestamp = DateTime.UtcNow
                };

                _context.SecurityLogs.Add(log);
                await _context.SaveChangesAsync();
#pragma warning disable S2629
                _logger?.LogInformation($"Role change logged for {email}: {oldRole} -> {newRole}");
#pragma warning restore S2629
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to write role change log");
            }
        }

        public async Task LogUserCreationAsync(string userId, string email, string createdBy, string role)
        {
            try
            {
                var clientIp = _httpContextAccessor?.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                var log = new SecurityLog
                {
                    UserId = userId,
                    EventType = "UserCreated",
                    Description = $"User {email} was created by {createdBy} with role: {role}",
                    IpAddress = clientIp,
                    Timestamp = DateTime.UtcNow
                };

                _context.SecurityLogs.Add(log);
                await _context.SaveChangesAsync();
#pragma warning disable S2629
                _logger?.LogInformation($"User creation logged: {email} created by {createdBy}");
#pragma warning restore S2629
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to write user creation log");
            }
        }

        public async Task LogUserDeletionAsync(string userId, string email, string deletedBy)
        {
            try
            {
                var clientIp = _httpContextAccessor?.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                var log = new SecurityLog
                {
                    UserId = userId,
                    EventType = "UserDeleted",
                    Description = $"User {email} was permanently deleted by {deletedBy}",
                    IpAddress = clientIp,
                    Timestamp = DateTime.UtcNow
                };

                _context.SecurityLogs.Add(log);
                await _context.SaveChangesAsync();
#pragma warning disable S2629
                _logger?.LogInformation($"User deletion logged: {email} deleted by {deletedBy}");
#pragma warning restore S2629
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to write user deletion log");
            }
        }

        public async Task<List<SecurityLog>> GetRecentLogsAsync(int count = 100)
        {
            try
            {
                var logs = await _context.SecurityLogs
                    .Include(x => x.User)
                    .OrderByDescending(x => x.Timestamp)
                    .Take(count)
                    .ToListAsync();
                return logs;
            }
            catch
            {
                return new List<SecurityLog>();
            }
        }

        public async Task<List<SecurityLog>> GetLogsByUserAsync(string userId)
        {
            try
            {
                var logs = await _context.SecurityLogs
                    .Include(x => x.User)
                    .Where(x => x.UserId == userId)
                    .OrderByDescending(x => x.Timestamp)
                    .ToListAsync();
                return logs;
            }
            catch
            {
                return new List<SecurityLog>();
            }
        }
    }
}