using IT15_INATO_POS.Data;
using IT15_INATO_POS.Models;
using Microsoft.EntityFrameworkCore;

namespace IT15_INATO_POS.Services
{
    public interface ISystemLogService
    {
        Task LogAsync(string userId, string module, string action, string? details = null);
        Task LogErrorAsync(string userId, string module, string error, Exception? ex = null);
        Task<List<SystemLog>> GetRecentLogsAsync(int count = 100);
        Task<List<SystemLog>> GetLogsByModuleAsync(string module);
    }

    public class SystemLogService : ISystemLogService
    {
        private readonly ApplicationDbContext _context;
#pragma warning disable S4487
        private readonly IHttpContextAccessor? _httpContextAccessor;
#pragma warning restore S4487
        private readonly ILogger<SystemLogService>? _logger;

        public SystemLogService(ApplicationDbContext context, IHttpContextAccessor? httpContextAccessor = null, ILogger<SystemLogService>? logger = null)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogAsync(string userId, string module, string action, string? details = null)
        {
            try
            {
                var log = new SystemLog
                {
                    UserId = userId,
                    Module = module,
                    Action = action,
                    Details = details,
                    Timestamp = DateTime.UtcNow
                };

                _context.SystemLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to write system log");
            }
        }

        public async Task LogErrorAsync(string userId, string module, string error, Exception? ex = null)
        {
            try
            {
                var details = error;
                if (ex != null)
                {
                    details += $"\nException: {ex.Message}\nStack Trace: {ex.StackTrace}";
                }

                var log = new SystemLog
                {
                    UserId = userId,
                    Module = module,
                    Action = "Error",
                    Details = details,
                    Timestamp = DateTime.UtcNow
                };

                _context.SystemLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception logEx)
            {
                _logger?.LogWarning(logEx, "Failed to write error log");
            }
        }

        public async Task<List<SystemLog>> GetRecentLogsAsync(int count = 100)
        {
            try
            {
                var logs = await _context.SystemLogs
                    .Include(x => x.User)
                    .OrderByDescending(x => x.Timestamp)
                    .Take(count)
                    .ToListAsync();
                return logs;
            }
            catch
            {
                return new List<SystemLog>();
            }
        }

        public async Task<List<SystemLog>> GetLogsByModuleAsync(string module)
        {
            try
            {
                var logs = await _context.SystemLogs
                    .Include(x => x.User)
                    .Where(x => x.Module == module)
                    .OrderByDescending(x => x.Timestamp)
                    .ToListAsync();
                return logs;
            }
            catch
            {
                return new List<SystemLog>();
            }
        }
    }
}