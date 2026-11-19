using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyERP.Application;
using MyERP.Domain;
using MyERP.Web.Models;

namespace MyERP.Web.Controllers
{
    [Authorize(Policy = "admin.manage")]
    public sealed class AdminController : Controller
    {
        private readonly IErpDbContext _db;
        private readonly IPasswordHasher<ErpUser> _passwordHasher;

        public AdminController(IErpDbContext db, IPasswordHasher<ErpUser> passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
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
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCompany(Company input)
        {
            if (!ModelState.IsValid) return await Index();
            var entity = input.Id == 0 ? new Company() : await _db.Companies.FindAsync(input.Id);
            if (entity == null) return NotFound();
            entity.Code = input.Code.Trim(); entity.Name = input.Name.Trim(); entity.Address = input.Address; entity.Phone = input.Phone; entity.IsActive = input.IsActive;
            if (input.Id == 0) _db.Companies.Add(entity);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveBranch(Branch input)
        {
            if (!ModelState.IsValid) return await Index();
            if (!await _db.Companies.AnyAsync(x => x.Id == input.CompanyId && x.IsActive))
            {
                ModelState.AddModelError(nameof(input.CompanyId), "Select an active company.");
                return await Index();
            }
            var entity = input.Id == 0 ? new Branch() : await _db.Branches.FindAsync(input.Id);
            if (entity == null) return NotFound();
            entity.CompanyId = input.CompanyId; entity.Code = input.Code.Trim(); entity.Name = input.Name.Trim(); entity.Address = input.Address; entity.IsActive = input.IsActive;
            if (input.Id == 0) _db.Branches.Add(entity);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveUser(string userName, string email, string password, int? branchId, int roleId)
        {
            if (string.IsNullOrWhiteSpace(userName) || userName.Length > 100 || !new EmailAddressAttribute().IsValid(email)
                || string.IsNullOrWhiteSpace(password) || password.Length < 12)
            {
                ModelState.AddModelError(string.Empty, "Enter a name and email and a password of at least 12 characters.");
                return await Index();
            }
            if (await _db.Users.AnyAsync(x => x.Email == email))
            {
                ModelState.AddModelError(string.Empty, "A user with this email already exists.");
                return await Index();
            }
            if (roleId <= 0 || !await _db.Roles.AnyAsync(x => x.Id == roleId))
            {
                ModelState.AddModelError(string.Empty, "Select a valid role.");
                return await Index();
            }
            var user = new ErpUser { UserName = userName.Trim(), Email = email.Trim(), BranchId = branchId, IsActive = true };
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            _db.UserRoles.Add(new ErpUserRole { UserId = user.Id, RoleId = roleId });
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveUserDetails(int id, string userName, string email, string password, int? branchId, bool isActive)
        {
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
            var current = await _db.RolePermissions.SingleOrDefaultAsync(x => x.RoleId == roleId && x.PermissionId == permissionId);
            if (enabled && current == null) _db.RolePermissions.Add(new ErpRolePermission { RoleId = roleId, PermissionId = permissionId });
            if (!enabled && current != null) _db.RolePermissions.Remove(current);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveMenu(string name, string url, string permissionCode, int sortOrder)
        {
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
            var current = await _db.UserRoles.SingleOrDefaultAsync(x => x.UserId == userId && x.RoleId == roleId);
            if (enabled && current == null) _db.UserRoles.Add(new ErpUserRole { UserId = userId, RoleId = roleId });
            if (!enabled && current != null) _db.UserRoles.Remove(current);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
