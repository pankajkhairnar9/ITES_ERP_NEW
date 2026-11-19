using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyERP.Application;
using MyERP.Domain;
using MyERP.Web.Models;

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
        public IActionResult Login(string returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _db.Users.SingleOrDefaultAsync(x => x.Email == model.Email && x.IsActive);
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
                var roles = await _db.Roles.Where(x => roleIds.Contains(x.Id)).Select(x => x.Name).ToListAsync();
                claims.AddRange(roles.Select(x => new Claim(ClaimTypes.Role, x)));
                var permissionCodes = await (from rolePermission in _db.RolePermissions
                                             join permission in _db.Permissions on rolePermission.PermissionId equals permission.Id
                                             where roleIds.Contains(rolePermission.RoleId)
                                             select permission.Code).Distinct().ToListAsync();
                claims.AddRange(permissionCodes.Select(x => new Claim("permission", x)));
            }

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);
            return RedirectToAction("Index", "Home");
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
