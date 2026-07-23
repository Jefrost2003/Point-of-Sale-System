using IT15_INATO_POS.Data;
using IT15_INATO_POS.Models;
using Microsoft.EntityFrameworkCore;

namespace IT15_INATO_POS.Services
{
    public interface IAuditLogService
    {
        Task LogActionAsync(string userId, string action, string module, string? details = null);
        Task<List<AuditLog>> GetRecentLogsAsync(int count = 50);
        Task<List<AuditLog>> GetLogsByUserAsync(string userId);
        Task<List<AuditLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<AuditLog>> GetLogsByModuleAsync(string module);
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogActionAsync(string userId, string action, string module, string? details = null)
        {
            try
            {
                var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

                var auditLog = new AuditLog
                {
                    UserID = userId,
                    Action = action,
                    Module = module,
                    Details = details,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.Now
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AuditLog Error: {ex.Message}");
            }
        }

        public async Task<List<AuditLog>> GetRecentLogsAsync(int count = 50)
        {
            return await _context.AuditLogs
                .Include(al => al.User)
                .OrderByDescending(al => al.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetLogsByUserAsync(string userId)
        {
            return await _context.AuditLogs
                .Include(al => al.User)
                .Where(al => al.UserID == userId)
                .OrderByDescending(al => al.Timestamp)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.AuditLogs
                .Include(al => al.User)
                .Where(al => al.Timestamp >= startDate && al.Timestamp <= endDate)
                .OrderByDescending(al => al.Timestamp)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetLogsByModuleAsync(string module)
        {
            return await _context.AuditLogs
                .Include(al => al.User)
                .Where(al => al.Module == module)
                .OrderByDescending(al => al.Timestamp)
                .ToListAsync();
        }
    }
}