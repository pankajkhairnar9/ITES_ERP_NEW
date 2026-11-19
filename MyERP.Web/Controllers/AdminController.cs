using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyERP.Application;
using MyERP.Domain;
using MyERP.Web.Models;
using MyERP.Web.Services;

namespace MyERP.Web.Controllers
{
    [Authorize(Policy = "admin.manage")]
    public sealed class AdminController : Controller
    {
        private readonly IErpDbContext _db;
        private readonly IPasswordHasher<ErpUser> _passwordHasher;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IErpDbContext db, IPasswordHasher<ErpUser> passwordHasher, ILogger<AdminController> logger)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? companyId = null, int? branchId = null, int? userId = null)
        {
            var model = new AdminPageViewModel
            {
                Companies = await _db.Companies.OrderBy(x => x.Name).ToListAsync(),
                Branches = await _db.Branches.Include(x => x.Company).OrderBy(x => x.Name).ToListAsync(),
                Users = await _db.Users.Include(x => x.Branch).OrderBy(x => x.Email).ToListAsync(),
                Roles = await _db.Roles.OrderBy(x => x.Name).ToListAsync(),
                Permissions = await _db.Permissions.OrderBy(x => x.Module).ThenBy(x => x.Name).ToListAsync(),
                Menus = await _db.MenuItems.OrderBy(x => x.SortOrder).ToListAsync()
            };
            model.RolePermissionKeys = await _db.RolePermissions.Select(x => x.RoleId + ":" + x.PermissionId).ToListAsync();
            model.UserRoleKeys = await _db.UserRoles.Select(x => x.UserId + ":" + x.RoleId).ToListAsync();
            if (companyId.HasValue)
                model.EditingCompany = await _db.Companies.FindAsync(companyId.Value);
            if (branchId.HasValue)
                model.EditingBranch = await _db.Branches.FindAsync(branchId.Value);
            if (userId.HasValue)
            {
                model.EditingUser = await _db.Users.FindAsync(userId.Value);
                model.EditingUserRoleKeys = userId.Value.ToString();
            }
            return View("Index", model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCompany(CompanyInputViewModel input)
        {
            if (!ModelState.IsValid) return await Index();
            var entity = input.Id == 0 ? new Company() : await _db.Companies.FindAsync(input.Id);
            if (entity == null) return NotFound();
            entity.Code = input.Code.Trim(); entity.Name = input.Name.Trim(); entity.Address = input.Address; entity.Phone = input.Phone;
            if (input.Id == 0) _db.Companies.Add(entity);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveBranch(BranchInputViewModel input)
        {
            if (!ModelState.IsValid) return await Index();
            if (!await _db.Companies.AnyAsync(x => x.Id == input.CompanyId && x.IsActive))
            {
                ModelState.AddModelError(nameof(input.CompanyId), "Select an active company.");
                return await Index();
            }
            var entity = input.Id == 0 ? new Branch() : await _db.Branches.FindAsync(input.Id);
            if (entity == null) return NotFound();
            entity.CompanyId = input.CompanyId; entity.Code = input.Code.Trim(); entity.Name = input.Name.Trim(); entity.Address = input.Address;
            if (input.Id == 0) _db.Branches.Add(entity);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveUserDetails(int id, string userName, string email, string password, int? branchId, bool isActive)
        {
            if (!ModelState.IsValid) return await Index(userId: id);
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            if (string.IsNullOrWhiteSpace(userName) || userName.Length > 100 || !new EmailAddressAttribute().IsValid(email)
                || (password != null && password.Length > 0 && password.Length < 12))
            {
                ModelState.AddModelError(string.Empty, "Enter a name and valid email. Any new password must be at least 12 characters.");
                return await Index(userId: id);
            }
            email = email.Trim();
            if (await _db.Users.AnyAsync(x => x.Id != id && x.Email == email))
            {
                ModelState.AddModelError(string.Empty, "Another user already uses this email.");
                return await Index(userId: id);
            }
            if (branchId.HasValue && !await _db.Branches.AnyAsync(x => x.Id == branchId.Value && x.IsActive))
            {
                ModelState.AddModelError(string.Empty, "Select an active branch.");
                return await Index(userId: id);
            }
            user.UserName = userName.Trim();
            user.Email = email;
            user.BranchId = branchId;
            user.IsActive = isActive;
            if (!string.IsNullOrWhiteSpace(password))
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRole(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name)) { ModelState.AddModelError(string.Empty, "Role name is required."); return await Index(); }
            if (string.Equals(name.Trim(), SelfRegistrationDefaults.RoleName, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "This role name is reserved for public registration.");
                return await Index();
            }
            _db.Roles.Add(new ErpRole { Name = name.Trim(), Description = description });
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePermission(string code, string name, string module)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(module))
            {
                ModelState.AddModelError(string.Empty, "Permission code, name and module are required.");
                return await Index();
            }
            _db.Permissions.Add(new ErpPermission { Code = code.Trim(), Name = name.Trim(), Module = module.Trim() });
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPermission(int roleId, int permissionId, bool enabled)
        {
            if (!ModelState.IsValid) return await Index();
            var role = await _db.Roles.FindAsync(roleId);
            var permission = await _db.Permissions.FindAsync(permissionId);
            if (role == null || permission == null) return NotFound();
            if (SelfRegistrationDefaults.IsSelfRegistrantRole(role.Name)
                && ((enabled && permission.Code != SelfRegistrationDefaults.PermissionCode)
                    || (!enabled && permission.Code == SelfRegistrationDefaults.PermissionCode)))
            {
                ModelState.AddModelError(string.Empty, "The public registration role can only have the dashboard.view permission.");
                return await Index();
            }
            var current = await _db.RolePermissions.SingleOrDefaultAsync(x => x.RoleId == roleId && x.PermissionId == permissionId);
            if (enabled && current == null) _db.RolePermissions.Add(new ErpRolePermission { RoleId = roleId, PermissionId = permissionId });
            if (!enabled && current != null) _db.RolePermissions.Remove(current);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveMenu(string name, string url, string permissionCode, int sortOrder)
        {
            if (!ModelState.IsValid) return await Index();
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(permissionCode))
            {
                ModelState.AddModelError(string.Empty, "Menu name, URL and required permission are required.");
                return await Index();
            }
            if (!url.StartsWith("/", StringComparison.Ordinal) || url.StartsWith("//", StringComparison.Ordinal))
            {
                ModelState.AddModelError(string.Empty, "Menu URL must be a local application path starting with one slash.");
                return await Index();
            }
            if (!await _db.Permissions.AnyAsync(x => x.Code == permissionCode))
            {
                ModelState.AddModelError(string.Empty, "Select an existing permission.");
                return await Index();
            }
            _db.MenuItems.Add(new MenuItem { Name = name.Trim(), Url = url, PermissionCode = permissionCode, SortOrder = sortOrder });
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetUserRole(int userId, int roleId, bool enabled)
        {
            if (!ModelState.IsValid) return await Index();
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var actorId))
                return Forbid();
            if (actorId == userId)
            {
                _logger.LogWarning("Administrator {AdministratorId} attempted to change their own role assignments.", actorId);
                ModelState.AddModelError(string.Empty, "You cannot change your own role assignments.");
                return await Index();
            }

            var user = await _db.Users.FindAsync(userId);
            var role = await _db.Roles.FindAsync(roleId);
            if (user == null || role == null)
                return NotFound();

            var current = await _db.UserRoles.SingleOrDefaultAsync(x => x.UserId == userId && x.RoleId == roleId);
            var changed = false;
            if (enabled && current == null)
            {
                _db.UserRoles.Add(new ErpUserRole { UserId = userId, RoleId = roleId });
                changed = true;
            }
            if (!enabled && current != null)
            {
                _db.UserRoles.Remove(current);
                changed = true;
            }
            await _db.SaveChangesAsync();
            if (changed)
                _logger.LogInformation("Administrator {AdministratorId} changed role {RoleId} for user {TargetUserId}; enabled={Enabled}.",
                    actorId, roleId, userId, enabled);
            return RedirectToAction(nameof(Index));
        }

    }
}
