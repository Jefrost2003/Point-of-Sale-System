using IT15_INATO_POS.Data;
using IT15_INATO_POS.Models;
using IT15_INATO_POS.Services;
using IT15_INATO_POS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IT15_INATO_POS.Controllers
{
    [Authorize(Roles = "Super Admin")]
    public class UserManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
#pragma warning disable S4487
        private readonly IAuditLogService _auditLogService;
#pragma warning restore S4487
        private readonly ISecurityLogService _securityLogService;
        private readonly ISystemLogService _systemLogService;

        public UserManagementController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IAuditLogService auditLogService,
            ISecurityLogService securityLogService,
            ISystemLogService systemLogService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _auditLogService = auditLogService;
            _securityLogService = securityLogService;
            _systemLogService = systemLogService;
        }

        // GET: UserManagement
        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                var userViewModels = new List<UserManagementViewModel>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
#pragma warning disable S1192
                    userViewModels.Add(new UserManagementViewModel
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email ?? string.Empty,
                        RoleName = roles.FirstOrDefault() ?? "No Role",
                        IsActive = user.IsActive,
                        CreatedDate = user.CreatedDate,
                        LastLoginDate = user.LastLoginDate
                    });
#pragma warning restore S1192
                }

                return View(userViewModels);
            }
            catch (Exception ex)
            {
#pragma warning disable S1192
                TempData["ErrorMessage"] = $"Error loading users: {ex.Message}";
#pragma warning restore S1192
                return View(new List<UserManagementViewModel>());
            }
        }

        // GET: UserManagement/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = await _roleManager.Roles.ToListAsync();
            return View();
        }

        // POST: UserManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(model.RoleName))
                {
                    ModelState.AddModelError(nameof(model.RoleName), "Please select a role.");
                    ViewBag.Roles = await _roleManager.Roles.ToListAsync();
                    return View(model);
                }

#pragma warning disable S2068
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    PasswordLastChanged = DateTime.Now,
                    PasswordHistory = "[]"
                };
#pragma warning restore S2068

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.RoleName);

                    var currentUser = await _userManager.GetUserAsync(User);

                    // ✅ Security Log for user creation
                    await _securityLogService.LogUserCreationAsync(
                        user.Id,
                        user.Email,
                        currentUser!.FullName,
                        model.RoleName);

#pragma warning disable S1192
                    await _systemLogService.LogAsync(
                        currentUser!.Id,
                        "User Management",
                        "Create",
                        $"Created user: {user.FullName} - Email: {user.Email}, Role: {model.RoleName}"
                    );
#pragma warning restore S1192

#pragma warning disable S1192
                    TempData["SuccessMessage"] = "User created successfully!";
#pragma warning restore S1192
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewBag.Roles = await _roleManager.Roles.ToListAsync();
            return View(model);
        }

        // GET: UserManagement/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var model = new UserManagementViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                RoleName = roles.FirstOrDefault() ?? string.Empty,
                IsActive = user.IsActive
            };

            ViewBag.Roles = await _roleManager.Roles.ToListAsync();
            return View(model);
        }

        // POST: UserManagement/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserManagementViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null) return NotFound();

                var oldRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "No Role";
#pragma warning disable S1481
                var wasActive = user.IsActive;
#pragma warning restore S1481

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Email = model.Email;
                user.UserName = model.Email;
                user.IsActive = model.IsActive;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!string.IsNullOrWhiteSpace(model.RoleName))
                        await _userManager.AddToRoleAsync(user, model.RoleName);

                    var currentUser = await _userManager.GetUserAsync(User);

                    // ✅ Security Log for role change
                    if (oldRole != model.RoleName)
                    {
                        await _securityLogService.LogRoleChangeAsync(user.Id, user.Email, oldRole, model.RoleName);
                    }

                    await _systemLogService.LogAsync(
                        currentUser!.Id,
                        "User Management",
                        "Update",
                        $"Updated user: {user.FullName} - Email: {user.Email}, Role: {model.RoleName}, Active: {user.IsActive}"
                    );

                    TempData["SuccessMessage"] = "User updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewBag.Roles = await _roleManager.Roles.ToListAsync();
            return View(model);
        }

        // POST: UserManagement/ArchiveUser/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "User ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction(nameof(Index));
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null && currentUser.Id == id)
                {
                    TempData["ErrorMessage"] = "You cannot archive your own account.";
                    return RedirectToAction(nameof(Index));
                }

                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Super Admin"))
                {
                    var superAdmins = await _userManager.GetUsersInRoleAsync("Super Admin");
                    if (superAdmins.Count() <= 1)
                    {
                        TempData["ErrorMessage"] = "Cannot archive the only Super Admin account.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                var alreadyArchived = await _context.ArchivedUsers.AnyAsync(a => a.OriginalUserId == user.Id);
                if (!alreadyArchived)
                {
                    var roleName = roles.FirstOrDefault() ?? "No Role";

                    var archivedUser = new ArchivedUser
                    {
                        OriginalUserId = user.Id,
                        UserName = user.UserName ?? string.Empty,
                        Email = user.Email ?? string.Empty,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        RoleName = roleName,
                        WasActive = user.IsActive,
                        CreatedDate = user.CreatedDate,
                        ArchivedDate = DateTime.Now,
                        ArchivedBy = currentUser?.Id ?? "system"
                    };
                    _context.ArchivedUsers.Add(archivedUser);
                }

                user.IsActive = false;
                var updateResult = await _userManager.UpdateAsync(user);

                if (updateResult.Succeeded)
                {
                    await _context.SaveChangesAsync();

                    await _securityLogService.LogLockoutAsync(user.Id, user.Email ?? "unknown", 1);
                    await _systemLogService.LogAsync(
                        currentUser?.Id ?? "system",
                        "User Management",
                        "Archive",
                        $"Archived user: {user.FullName} - Email: {user.Email}"
                    );

                    TempData["SuccessMessage"] = $"User '{user.FullName}' has been archived successfully.";
                }
                else
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = $"Failed to archive user: {errors}";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error archiving user: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: UserManagement/RestoreUser/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "User ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction(nameof(Index));
                }

                var archivedUser = await _context.ArchivedUsers.FirstOrDefaultAsync(a => a.OriginalUserId == user.Id);
                if (archivedUser != null) _context.ArchivedUsers.Remove(archivedUser);

                user.IsActive = true;
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    await _context.SaveChangesAsync();

                    var currentUser = await _userManager.GetUserAsync(User);

                    await _systemLogService.LogAsync(
                        currentUser!.Id,
                        "User Management",
                        "Restore",
                        $"Restored user: {user.FullName} - Email: {user.Email}"
                    );

                    TempData["SuccessMessage"] = $"User '{user.FullName}' has been restored successfully!";
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = $"Failed to restore user: {errors}";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error restoring user: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: UserManagement/PermanentlyDeleteUser/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PermanentlyDeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "User ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction(nameof(Index));
                }

                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains("Super Admin"))
                {
                    TempData["ErrorMessage"] = "Cannot delete Super Admin account.";
                    return RedirectToAction(nameof(Index));
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null && currentUser.Id == id)
                {
                    TempData["ErrorMessage"] = "You cannot delete your own account.";
                    return RedirectToAction(nameof(Index));
                }

                var userName = $"{user.FirstName} {user.LastName}";
                var userEmail = user.Email;

                // Delete related data
                _context.SecurityLogs.RemoveRange(_context.SecurityLogs.Where(sl => sl.UserId == id));
                _context.AuditLogs.RemoveRange(_context.AuditLogs.Where(al => al.UserID == id));
                _context.UserRoles.RemoveRange(_context.UserRoles.Where(ur => ur.UserId == id));
                _context.UserClaims.RemoveRange(_context.UserClaims.Where(uc => uc.UserId == id));
                _context.UserLogins.RemoveRange(_context.UserLogins.Where(ul => ul.UserId == id));
                _context.UserTokens.RemoveRange(_context.UserTokens.Where(ut => ut.UserId == id));

                var archivedUser = await _context.ArchivedUsers.FirstOrDefaultAsync(a => a.OriginalUserId == id);
                if (archivedUser != null) _context.ArchivedUsers.Remove(archivedUser);

                await _context.SaveChangesAsync();

                // ✅ Security Log for user deletion

#pragma warning disable S125
                //await _securityLogService.LogUserDeletionAsync(id, userEmail, currentUser?.FullName ?? "System");

                var deleteResult = await _userManager.DeleteAsync(user);
#pragma warning restore S125

                if (deleteResult.Succeeded)
                {
                    await _systemLogService.LogAsync(
                        currentUser!.Id,
                        "User Management",
                        "PermanentDelete",
                        $"Permanently deleted user: {userName} - Email: {userEmail}"
                    );

                    TempData["SuccessMessage"] = $"User '{userName}' has been permanently deleted from the system.";
                }
                else
                {
                    var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = $"Failed to delete user: {errors}";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting user: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: UserManagement/Delete/{id}
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var model = new UserManagementViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                RoleName = roles.FirstOrDefault() ?? "No Role",
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate
            };

            return View(model);
        }

        // GET: UserManagement/AuditLogs
#pragma warning disable S6967
        public async Task<IActionResult> AuditLogs(DateTime? startDate, DateTime? endDate, string? userId)
#pragma warning restore S6967
        {
            var query = _context.AuditLogs
                .Include(a => a.User)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(a => a.Timestamp >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(a => a.Timestamp <= endDate.Value.AddDays(1));
            if (!string.IsNullOrEmpty(userId))
                query = query.Where(a => a.UserID == userId);

            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Take(500)
                .ToListAsync();

            ViewBag.Users = await _userManager.Users.ToListAsync();
#pragma warning disable S1192
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
#pragma warning restore S1192
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedUserId = userId;

            return View(logs);
        }

        // GET: UserManagement/SecurityLogs
#pragma warning disable S6967
        public async Task<IActionResult> SecurityLogs(DateTime? startDate, DateTime? endDate, string? userId)
#pragma warning restore S6967
        {
            var query = _context.SecurityLogs
                .Include(x => x.User)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(a => a.Timestamp >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(a => a.Timestamp <= endDate.Value.AddDays(1));
            if (!string.IsNullOrEmpty(userId))
                query = query.Where(a => a.UserId == userId);

            var logs = await query
                .OrderByDescending(x => x.Timestamp)
                .Take(500)
                .ToListAsync();

            ViewBag.Users = await _userManager.Users.ToListAsync();
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedUserId = userId;

            return View(logs);
        }

        // GET: UserManagement/SystemLogs
#pragma warning disable S6967
        public async Task<IActionResult> SystemLogs(DateTime? startDate, DateTime? endDate, string? module)
#pragma warning restore S6967
        {
            var query = _context.SystemLogs
                .Include(x => x.User)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(a => a.Timestamp >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(a => a.Timestamp <= endDate.Value.AddDays(1));
            if (!string.IsNullOrEmpty(module))
                query = query.Where(a => a.Module == module);

            var logs = await query
                .OrderByDescending(x => x.Timestamp)
                .Take(500)
                .ToListAsync();

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedModule = module;

            return View(logs);
        }
    }
}