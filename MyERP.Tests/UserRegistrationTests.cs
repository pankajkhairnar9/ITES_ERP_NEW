using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Routing;
using MyERP.Domain;
using MyERP.Infrastructure;
using MyERP.Web.Controllers;
using MyERP.Web.Models;
using MyERP.Web.Services;

namespace MyERP.Tests;

public sealed class UserRegistrationTests
{
    [Fact]
    public async Task Public_registration_creates_restricted_account_that_can_sign_in()
    {
        await using var db = CreateDatabase();
        var hasher = new PasswordHasher<ErpUser>();
        var (controller, _) = CreateAccountController(db, hasher);
        var model = ValidInput();

        var result = await controller.Register(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AccountController.Login), redirect.ActionName);
        var user = await db.Users.SingleAsync();
        Assert.Equal("new.user@example.test", user.Email);
        Assert.Null(user.BranchId);
        Assert.True(user.IsActive);
        Assert.NotEqual(model.Password, user.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password));

        var userRole = await db.UserRoles.Include(x => x.Role).SingleAsync();
        Assert.Equal(SelfRegistrationDefaults.RoleName, userRole.Role.Name);
        var rolePermissions = await db.RolePermissions
            .Where(x => x.RoleId == userRole.RoleId)
            .Select(x => x.Permission.Code)
            .ToListAsync();
        Assert.Equal(new[] { SelfRegistrationDefaults.PermissionCode }, rolePermissions);
        Assert.Equal("admin.manage", typeof(AdminController).GetCustomAttribute<AuthorizeAttribute>()?.Policy);

        var (loginController, authentication) = CreateAccountController(db, hasher);
        var loginResult = await loginController.Login(new LoginViewModel
        {
            Email = "NEW.USER@EXAMPLE.TEST",
            Password = model.Password
        });

        Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(loginResult).ActionName);
        Assert.NotNull(authentication.SignedInPrincipal);
        Assert.Contains(authentication.SignedInPrincipal.Claims,
            claim => claim.Type == "permission" && claim.Value == SelfRegistrationDefaults.PermissionCode);
        Assert.Contains(authentication.SignedInPrincipal.Claims,
            claim => claim.Type == "branch_scope" && claim.Value == "none");
        Assert.DoesNotContain(authentication.SignedInPrincipal.Claims,
            claim => claim.Type == "permission" && claim.Value == "admin.manage");
    }

    [Fact]
    public async Task Registration_rejects_duplicate_email_case_insensitively_without_saving_role()
    {
        await using var db = CreateDatabase();
        db.Users.Add(new ErpUser
        {
            UserName = "Existing User",
            Email = "NEW.USER@EXAMPLE.TEST",
            PasswordHash = "existing-hash",
            IsActive = true
        });
        await db.SaveChangesAsync();
        var (controller, _) = CreateAccountController(db, new PasswordHasher<ErpUser>());

        var result = await controller.Register(ValidInput());

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Register", view.ViewName);
        Assert.Contains(controller.ModelState[nameof(UserRegistrationViewModel.Email)]!.Errors,
            error => error.ErrorMessage.Contains("already exists", StringComparison.OrdinalIgnoreCase));
        Assert.Null(Assert.IsType<UserRegistrationViewModel>(view.Model).Password);
        Assert.Equal(1, await db.Users.CountAsync());
        Assert.Empty(await db.Roles.ToListAsync());
    }

    [Fact]
    public async Task Registration_refuses_existing_builtin_role_with_extra_permissions()
    {
        await using var db = CreateDatabase();
        var role = new ErpRole { Name = SelfRegistrationDefaults.RoleName };
        var permission = new ErpPermission { Code = "admin.manage", Name = "Manage access", Module = "Administration" };
        db.AddRange(role, permission);
        await db.SaveChangesAsync();
        db.RolePermissions.Add(new ErpRolePermission { RoleId = role.Id, PermissionId = permission.Id });
        await db.SaveChangesAsync();
        var (controller, _) = CreateAccountController(db, new PasswordHasher<ErpUser>());

        var result = await controller.Register(ValidInput());

        Assert.IsType<ViewResult>(result);
        Assert.Contains(controller.ModelState[string.Empty]!.Errors,
            error => error.ErrorMessage.Contains("administrator review", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(await db.Users.ToListAsync());
        Assert.Empty(await db.UserRoles.ToListAsync());
    }

    [Fact]
    public async Task Login_filters_extra_permissions_from_builtin_role()
    {
        await using var db = CreateDatabase();
        var role = new ErpRole { Name = SelfRegistrationDefaults.RoleName };
        var dashboard = new ErpPermission { Code = SelfRegistrationDefaults.PermissionCode, Name = "Dashboard", Module = "Dashboard" };
        var administration = new ErpPermission { Code = "admin.manage", Name = "Administration", Module = "Administration" };
        db.AddRange(role, dashboard, administration);
        await db.SaveChangesAsync();
        var user = new ErpUser { UserName = "New User", Email = "new.user@example.test", IsActive = true };
        user.PasswordHash = new PasswordHasher<ErpUser>().HashPassword(user, ValidInput().Password);
        db.Users.Add(user);
        db.UserRoles.Add(new ErpUserRole { User = user, Role = role });
        db.RolePermissions.AddRange(
            new ErpRolePermission { Role = role, Permission = dashboard },
            new ErpRolePermission { Role = role, Permission = administration });
        await db.SaveChangesAsync();
        var (controller, authentication) = CreateAccountController(db, new PasswordHasher<ErpUser>());

        await controller.Login(new LoginViewModel { Email = user.Email, Password = ValidInput().Password });

        Assert.NotNull(authentication.SignedInPrincipal);
        Assert.Contains(authentication.SignedInPrincipal.Claims,
            claim => claim.Type == "permission" && claim.Value == SelfRegistrationDefaults.PermissionCode);
        Assert.DoesNotContain(authentication.SignedInPrincipal.Claims,
            claim => claim.Type == "permission" && claim.Value == "admin.manage");
    }

    [Fact]
    public async Task Admin_cannot_grant_extra_permissions_to_builtin_registration_role()
    {
        await using var db = CreateDatabase();
        var role = new ErpRole { Name = SelfRegistrationDefaults.RoleName };
        var permission = new ErpPermission { Code = "purchase.order", Name = "Orders", Module = "Purchasing" };
        db.AddRange(role, permission);
        await db.SaveChangesAsync();
        var controller = new AdminController(db, new PasswordHasher<ErpUser>(), NullLogger<AdminController>.Instance);

        var result = await controller.AssignPermission(role.Id, permission.Id, enabled: true);

        Assert.IsType<ViewResult>(result);
        Assert.Contains(controller.ModelState[string.Empty]!.Errors,
            error => error.ErrorMessage.Contains("dashboard.view", StringComparison.Ordinal));
        Assert.Empty(await db.RolePermissions.ToListAsync());
    }

    [Fact]
    public async Task Admin_can_promote_another_user_to_administrator()
    {
        await using var db = CreateDatabase();
        var actor = new ErpUser { UserName = "Administrator", Email = "admin@example.test", PasswordHash = "hash", IsActive = true };
        var target = new ErpUser { UserName = "New User", Email = "new.user@example.test", PasswordHash = "hash", IsActive = true };
        var selfRegistrant = new ErpRole { Name = SelfRegistrationDefaults.RoleName };
        var administrator = new ErpRole { Name = "Administrator" };
        db.AddRange(actor, target, selfRegistrant, administrator);
        await db.SaveChangesAsync();
        db.UserRoles.Add(new ErpUserRole { UserId = target.Id, RoleId = selfRegistrant.Id });
        await db.SaveChangesAsync();
        var controller = CreateAdminController(db, actor.Id);

        var result = await controller.SetUserRole(target.Id, administrator.Id, enabled: true);

        Assert.Equal(nameof(AdminController.Index), Assert.IsType<RedirectToActionResult>(result).ActionName);
        Assert.True(await db.UserRoles.AnyAsync(x => x.UserId == target.Id && x.RoleId == administrator.Id));
    }

    [Fact]
    public async Task Admin_cannot_promote_themselves_to_administrator()
    {
        await using var db = CreateDatabase();
        var actor = new ErpUser { UserName = "Existing user", Email = "existing@example.test", PasswordHash = "hash", IsActive = true };
        var selfRegistrant = new ErpRole { Name = SelfRegistrationDefaults.RoleName };
        var administrator = new ErpRole { Name = "Administrator" };
        db.AddRange(actor, selfRegistrant, administrator);
        await db.SaveChangesAsync();
        db.UserRoles.Add(new ErpUserRole { UserId = actor.Id, RoleId = selfRegistrant.Id });
        await db.SaveChangesAsync();
        var controller = CreateAdminController(db, actor.Id);

        var result = await controller.SetUserRole(actor.Id, administrator.Id, enabled: true);

        Assert.IsType<ViewResult>(result);
        Assert.Contains(controller.ModelState[string.Empty]!.Errors,
            error => error.ErrorMessage.Contains("own role", StringComparison.OrdinalIgnoreCase));
        Assert.False(await db.UserRoles.AnyAsync(x => x.UserId == actor.Id && x.RoleId == administrator.Id));
    }

    [Fact]
    public async Task Invalid_company_submission_renders_admin_index_and_preserves_validation_errors()
    {
        await using var db = CreateDatabase();
        var controller = CreateAdminController(db, actorId: 1);
        controller.ModelState.AddModelError(nameof(CompanyInputViewModel.Code), "Company code is required.");

        var result = await controller.SaveCompany(new CompanyInputViewModel());

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
        Assert.Contains(controller.ModelState[nameof(CompanyInputViewModel.Code)]!.Errors,
            error => error.ErrorMessage == "Company code is required.");
        Assert.Empty(await db.Companies.ToListAsync());
    }

    [Fact]
    public async Task Successful_company_submission_redirects_to_admin_index()
    {
        await using var db = CreateDatabase();
        var controller = CreateAdminController(db, actorId: 1);

        var result = await controller.SaveCompany(new CompanyInputViewModel { Code = "C1", Name = "Company" });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminController.Index), redirect.ActionName);
        Assert.Single(await db.Companies.ToListAsync());
    }

    [Fact]
    public async Task User_status_checkbox_can_set_active_and_inactive_values()
    {
        await using var db = CreateDatabase();
        var user = new ErpUser
        {
            UserName = "Status User",
            Email = "status@example.test",
            PasswordHash = "hash",
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var controller = CreateAdminController(db, actorId: 1);

        var deactivate = await controller.SaveUserDetails(user.Id, user.UserName, user.Email, null, null, isActive: false);
        Assert.Equal(nameof(AdminController.Index), Assert.IsType<RedirectToActionResult>(deactivate).ActionName);
        Assert.False(user.IsActive);

        var activate = await controller.SaveUserDetails(user.Id, user.UserName, user.Email, null, null, isActive: true);
        Assert.Equal(nameof(AdminController.Index), Assert.IsType<RedirectToActionResult>(activate).ActionName);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task Invalid_user_status_value_does_not_deactivate_user()
    {
        await using var db = CreateDatabase();
        var user = new ErpUser
        {
            UserName = "Status User",
            Email = "status@example.test",
            PasswordHash = "hash",
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var controller = CreateAdminController(db, actorId: 1);
        controller.ModelState.AddModelError("isActive", "The value 'value' is not valid for IsActive.");

        var result = await controller.SaveUserDetails(user.Id, user.UserName, user.Email, null, null, isActive: false);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task Unassigned_self_registrant_dashboard_does_not_expose_cross_branch_metrics()
    {
        await using var db = CreateDatabase();
        db.Suppliers.Add(new Supplier { Code = "S1", Name = "Private supplier" });
        await db.SaveChangesAsync();
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("branch_scope", "none") }, "test"));
        var controller = new HomeController(NullLogger<HomeController>.Instance, db)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = Assert.IsType<ViewResult>(await controller.Index());
        var model = Assert.IsType<DashboardViewModel>(result.Model);

        Assert.True(model.BranchAssignmentRequired);
        Assert.Equal(0, model.SupplierCount);
    }

    [Fact]
    public void Registration_requires_valid_name_email_and_matching_12_character_password()
    {
        var model = ValidInput();
        model.UserName = " ";
        model.Email = "invalid";
        model.Password = new string('x', 11);
        model.ConfirmPassword = new string('y', 11);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.UserName)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.Email)));
        Assert.Contains(results, result => result.ErrorMessage!.Contains("12 and 256", StringComparison.Ordinal));
        Assert.Contains(results, result => result.ErrorMessage!.Contains("do not match", StringComparison.Ordinal));
        Assert.DoesNotContain(typeof(UserRegistrationViewModel).GetProperties(),
            property => new[] { "RoleId", "BranchId", "CompanyId", "PermissionId" }.Contains(property.Name));
    }

    [Fact]
    public void Registration_route_is_anonymous_and_post_requires_antiforgery()
    {
        var controllerType = typeof(AccountController);
        var registerPost = controllerType.GetMethod(nameof(AccountController.Register), new[] { typeof(UserRegistrationViewModel) })!;

        Assert.NotNull(registerPost.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.NotNull(registerPost.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());

        var setUserRole = typeof(AdminController).GetMethod(nameof(AdminController.SetUserRole),
            new[] { typeof(int), typeof(int), typeof(bool) })!;
        Assert.NotNull(setUserRole.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(setUserRole.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }

    private static ErpDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ErpDbContext(options);
    }

    private static (AccountController Controller, CapturingAuthenticationService Authentication)
        CreateAccountController(ErpDbContext db, IPasswordHasher<ErpUser> passwordHasher)
    {
        var authentication = new CapturingAuthenticationService();
        var context = new DefaultHttpContext();
        var controller = new AccountController(db, passwordHasher)
        {
            ControllerContext = new ControllerContext { HttpContext = context },
            Url = new TestUrlHelper(new ActionContext(context, new RouteData(), new ActionDescriptor())),
            TempData = new TempDataDictionary(context, new NoOpTempDataProvider())
        };
        context.RequestServices = new ServiceCollection()
            .AddSingleton<IAuthenticationService>(authentication)
            .BuildServiceProvider();
        return (controller, authentication);
    }

    private static UserRegistrationViewModel ValidInput()
    {
        return new UserRegistrationViewModel
        {
            UserName = "New User",
            Email = "New.User@Example.Test",
            Password = new string('x', 16),
            ConfirmPassword = new string('x', 16)
        };
    }

    private static AdminController CreateAdminController(ErpDbContext db, int actorId)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, actorId.ToString()) }, "test"));
        return new AdminController(db, new PasswordHasher<ErpUser>(), NullLogger<AdminController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private sealed class NoOpTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private sealed class CapturingAuthenticationService : IAuthenticationService
    {
        public ClaimsPrincipal? SignedInPrincipal { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
        {
            SignedInPrincipal = principal;
            return Task.CompletedTask;
        }

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;
    }

    private sealed class TestUrlHelper : IUrlHelper
    {
        public TestUrlHelper(ActionContext actionContext) => ActionContext = actionContext;

        public ActionContext ActionContext { get; }
        public string? Action(UrlActionContext actionContext) => "/";
        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url) => true;
        public string? Link(string? routeName, object? values) => "/";
        public string? RouteUrl(UrlRouteContext routeContext) => "/";
    }
}
