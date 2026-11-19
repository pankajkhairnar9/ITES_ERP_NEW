using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyERP.Application;
using MyERP.Domain;
using MyERP.Infrastructure;

namespace MyERP.Web.Services
{
    public interface IErpDatabaseInitializer
    {
        Task InitializeAsync();
    }

    public sealed class ErpDatabaseInitializer : IErpDatabaseInitializer
    {
        private readonly ErpDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly IPasswordHasher<ErpUser> _passwordHasher;

        public ErpDatabaseInitializer(ErpDbContext db, IConfiguration configuration, IPasswordHasher<ErpUser> passwordHasher)
        {
            _db = db;
            _configuration = configuration;
            _passwordHasher = passwordHasher;
        }

        public async Task InitializeAsync()
        {
            if (await _db.Database.CanConnectAsync())
            {
                var appliedMigrations = await _db.Database.GetAppliedMigrationsAsync();
                if (!appliedMigrations.Any() && await HasApplicationTablesAsync())
                    throw new InvalidOperationException("The database already contains tables but is not tracked by EF Core migrations. Review and baseline the existing schema before enabling automatic migration.");
            }
            await _db.Database.MigrateAsync();

            var permissions = new[]
            {
                new ErpPermission { Code = "dashboard.view", Name = "View dashboard", Module = "Dashboard" },
                new ErpPermission { Code = "masters.manage", Name = "Manage master data", Module = "Masters" },
                new ErpPermission { Code = "admin.manage", Name = "Manage company, branch, users and roles", Module = "Administration" },
                new ErpPermission { Code = "purchase.request", Name = "Create purchase requests", Module = "Purchasing" },
                new ErpPermission { Code = "purchase.approve", Name = "Approve purchase requests", Module = "Purchasing" },
                new ErpPermission { Code = "purchase.order", Name = "Manage purchase orders", Module = "Purchasing" },
                new ErpPermission { Code = "purchase.order.approve", Name = "Approve purchase orders", Module = "Purchasing" },
                new ErpPermission { Code = "grn.manage", Name = "Manage and approve receipts", Module = "Inventory" },
                new ErpPermission { Code = "sales.manage", Name = "Manage and dispatch sales orders", Module = "Sales" },
                new ErpPermission { Code = "stock.adjust", Name = "Adjust stock", Module = "Inventory" },
                new ErpPermission { Code = "reports.view", Name = "View reports", Module = "Reports" }
            };
            foreach (var permission in permissions)
                if (!await _db.Permissions.AnyAsync(x => x.Code == permission.Code))
                    _db.Permissions.Add(permission);

            var administrator = await _db.Roles.SingleOrDefaultAsync(x => x.Name == "Administrator");
            if (administrator == null)
            {
                administrator = new ErpRole { Name = "Administrator", Description = "Full ERP access" };
                _db.Roles.Add(administrator);
                await _db.SaveChangesAsync();
            }
            await _db.SaveChangesAsync();
            var allPermissions = await _db.Permissions.ToListAsync();
            foreach (var permission in allPermissions)
                if (!await _db.RolePermissions.AnyAsync(x => x.RoleId == administrator.Id && x.PermissionId == permission.Id))
                    _db.RolePermissions.Add(new ErpRolePermission { RoleId = administrator.Id, PermissionId = permission.Id });

            var initialEmail = _configuration["ERP_ADMIN_EMAIL"];
            var initialPassword = _configuration["ERP_ADMIN_PASSWORD"];
            if (!string.IsNullOrWhiteSpace(initialEmail) && !string.IsNullOrWhiteSpace(initialPassword))
            {
                var user = await _db.Users.SingleOrDefaultAsync(x => x.Email == initialEmail);
                if (user == null)
                {
                    user = new ErpUser
                    {
                        UserName = initialEmail.Trim(),
                        Email = initialEmail.Trim(),
                        IsActive = true
                    };
                    user.PasswordHash = _passwordHasher.HashPassword(user, initialPassword);
                    _db.Users.Add(user);
                    await _db.SaveChangesAsync();
                }
                if (!await _db.UserRoles.AnyAsync(x => x.UserId == user.Id && x.RoleId == administrator.Id))
                    _db.UserRoles.Add(new ErpUserRole { UserId = user.Id, RoleId = administrator.Id });
            }

            var menu = new[]
            {
                new MenuItem { Name = "Dashboard", Url = "/", PermissionCode = "dashboard.view", SortOrder = 1 },
                new MenuItem { Name = "Company & access", Url = "/Admin", PermissionCode = "admin.manage", SortOrder = 2 },
                new MenuItem { Name = "Suppliers", Url = "/Master/Suppliers", PermissionCode = "masters.manage", SortOrder = 3 },
                new MenuItem { Name = "Customers", Url = "/Master/Customers", PermissionCode = "masters.manage", SortOrder = 4 },
                new MenuItem { Name = "Items", Url = "/Master/Items", PermissionCode = "masters.manage", SortOrder = 5 },
                new MenuItem { Name = "Purchase requests", Url = "/Operations/Requests", PermissionCode = "purchase.request", SortOrder = 6 },
                new MenuItem { Name = "Purchase orders", Url = "/Operations/Orders", PermissionCode = "purchase.order", SortOrder = 7 },
                new MenuItem { Name = "Goods receipts", Url = "/Operations/Receipts", PermissionCode = "grn.manage", SortOrder = 8 },
                new MenuItem { Name = "Sales orders", Url = "/Operations/Sales", PermissionCode = "sales.manage", SortOrder = 9 },
                new MenuItem { Name = "Stock adjustment", Url = "/Operations/Adjustments", PermissionCode = "stock.adjust", SortOrder = 10 },
                new MenuItem { Name = "Stock report", Url = "/Operations/Reports", PermissionCode = "reports.view", SortOrder = 11 }
            };
            foreach (var item in menu)
                if (!await _db.MenuItems.AnyAsync(x => x.Url == item.Url))
                    _db.MenuItems.Add(item);

            await _db.SaveChangesAsync();
        }

        private async Task<bool> HasApplicationTablesAsync()
        {
            var connection = _db.Database.GetDbConnection();
            var closeWhenDone = connection.State != ConnectionState.Open;
            if (closeWhenDone)
                await connection.OpenAsync();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME <> '__EFMigrationsHistory'";
                    return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
                }
            }
            finally
            {
                if (closeWhenDone)
                    await connection.CloseAsync();
            }
        }
    }
}
