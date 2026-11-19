using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MyERP.Application;
using MyERP.Domain;
using MyERP.Web.Models;
using MyERP.Web.Services;

namespace MyERP.Web.Controllers
{
    public sealed class AccountController : Controller
    {
        private readonly IErpDbContext _db;
        private readonly IPasswordHasher<ErpUser> _passwordHasher;

        public AccountController(IErpDbContext db, IPasswordHasher<ErpUser> passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register() => View(new UserRegistrationViewModel());

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(UserRegistrationViewModel model)
        {
            if (!ModelState.IsValid)
                return RegistrationError(model);

            var userName = model.UserName.Trim();
            var email = model.Email.Trim().ToLowerInvariant();
            if (userName.Length == 0)
            {
                ModelState.AddModelError(nameof(model.UserName), "Enter your name.");
                return RegistrationError(model);
            }

            if (await _db.Users.AnyAsync(x => x.Email.ToLower() == email))
            {
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
                return RegistrationError(model);
            }

            var user = new ErpUser
            {
                UserName = userName,
                Email = email,
                IsActive = true
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

            var registrationRole = await PrepareSelfRegistrantRoleAsync();
            if (registrationRole == null)
            {
                ModelState.AddModelError(string.Empty,
                    "Registration is unavailable because the default access role needs administrator review.");
                return RegistrationError(model);
            }

            _db.Users.Add(user);
            _db.UserRoles.Add(new ErpUserRole { User = user, Role = registrationRole });
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                ModelState.AddModelError(string.Empty,
                    "Registration setup changed while you were submitting. Please try again.");
                return RegistrationError(model);
            }

            TempData["RegistrationSuccess"] = "Your account has been created. Sign in to continue.";
            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim().ToLowerInvariant();
            var user = await _db.Users.SingleOrDefaultAsync(x => x.Email.ToLower() == email && x.IsActive);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash)
                || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password) == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Email or password is incorrect.");
                return View(model);
            }

            var roleIds = await _db.UserRoles.Where(x => x.UserId == user.Id).Select(x => x.RoleId).ToListAsync();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email)
            };
            if (user.BranchId.HasValue)
                claims.Add(new Claim("branch_id", user.BranchId.Value.ToString()));
            if (roleIds.Count > 0)
            {
                var roles = await _db.Roles.Where(x => roleIds.Contains(x.Id))
                    .Select(x => new { x.Id, x.Name }).ToListAsync();
                claims.AddRange(roles.Select(x => new Claim(ClaimTypes.Role, x.Name)));
                var selfRegistrantRoleIds = roles
                    .Where(x => SelfRegistrationDefaults.IsSelfRegistrantRole(x.Name))
                    .Select(x => x.Id)
                    .ToHashSet();
                var permissionAssignments = await (from rolePermission in _db.RolePermissions
                                                   join permission in _db.Permissions on rolePermission.PermissionId equals permission.Id
                                                   where roleIds.Contains(rolePermission.RoleId)
                                                   select new { rolePermission.RoleId, permission.Code }).ToListAsync();
                var permissionCodes = permissionAssignments
                    .Where(x => !selfRegistrantRoleIds.Contains(x.RoleId)
                        || x.Code == SelfRegistrationDefaults.PermissionCode)
                    .Select(x => x.Code)
                    .Distinct()
                    .ToList();
                claims.AddRange(permissionCodes.Select(x => new Claim("permission", x)));
                if (!user.BranchId.HasValue && selfRegistrantRoleIds.Count > 0)
                    claims.Add(new Claim("branch_scope", "none"));
            }

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);
            return RedirectToAction("Index", "Home");
        }

        private async Task<ErpRole> PrepareSelfRegistrantRoleAsync()
        {
            var matchingRoles = await _db.Roles
                .Where(x => x.Name != null && x.Name.ToUpper() == SelfRegistrationDefaults.RoleName.ToUpper())
                .Take(2)
                .ToListAsync();
            if (matchingRoles.Count > 1)
                return null;

            var role = matchingRoles.SingleOrDefault();
            if (role != null && role.Id > 0)
            {
                var existingPermissionCodes = await (from rolePermission in _db.RolePermissions
                                                     join assignedPermission in _db.Permissions on rolePermission.PermissionId equals assignedPermission.Id
                                                     where rolePermission.RoleId == role.Id
                                                     select assignedPermission.Code).ToListAsync();
                if (existingPermissionCodes.Any(code => code != SelfRegistrationDefaults.PermissionCode))
                    return null;
            }

            var permission = await _db.Permissions
                .SingleOrDefaultAsync(x => x.Code == SelfRegistrationDefaults.PermissionCode);
            if (role == null)
            {
                role = new ErpRole
                {
                    Name = SelfRegistrationDefaults.RoleName,
                    Description = SelfRegistrationDefaults.RoleDescription
                };
                _db.Roles.Add(role);
            }
            if (permission == null)
            {
                permission = new ErpPermission
                {
                    Code = SelfRegistrationDefaults.PermissionCode,
                    Name = "View dashboard",
                    Module = "Dashboard"
                };
                _db.Permissions.Add(permission);
            }

            var hasDashboardPermission = role.Id > 0 && permission.Id > 0
                && await _db.RolePermissions.AnyAsync(x => x.RoleId == role.Id && x.PermissionId == permission.Id);
            if (!hasDashboardPermission)
                _db.RolePermissions.Add(new ErpRolePermission { Role = role, Permission = permission });

            return role;
        }

        private IActionResult RegistrationError(UserRegistrationViewModel model)
        {
            model.Password = null;
            model.ConfirmPassword = null;
            return View("Register", model);
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        {
            return exception.GetBaseException() is SqlException sqlException
                && (sqlException.Number == 2601 || sqlException.Number == 2627);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        public IActionResult AccessDenied() => View();
    }
}
