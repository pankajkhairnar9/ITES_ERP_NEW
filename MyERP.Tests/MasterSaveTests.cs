using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyERP.Domain;
using MyERP.Infrastructure;
using MyERP.Web.Controllers;
using MyERP.Web.Models;
using MyERP.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace MyERP.Tests;

public sealed class MasterSaveTests
{
    [Fact]
    public async Task Company_create_defaults_active_and_update_preserves_existing_status()
    {
        await using var db = CreateDatabase();
        var controller = CreateAdminController(db);

        await controller.SaveCompany(new CompanyInputViewModel { Code = "C1", Name = "Active Company" });
        var created = await db.Companies.SingleAsync();
        Assert.True(created.IsActive);

        created.IsActive = false;
        await db.SaveChangesAsync();
        await controller.SaveCompany(new CompanyInputViewModel { Id = created.Id, Code = "C1", Name = "Updated Company" });

        Assert.False(created.IsActive);
        Assert.Equal("Updated Company", created.Name);
        Assert.DoesNotContain(typeof(CompanyInputViewModel).GetProperties(), property => property.Name == nameof(Company.IsActive));
    }

    [Fact]
    public async Task Branch_create_defaults_active_and_update_preserves_existing_status()
    {
        await using var db = CreateDatabase();
        var company = new Company { Code = "C1", Name = "Company" };
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        var controller = CreateAdminController(db);

        await controller.SaveBranch(new BranchInputViewModel
        {
            CompanyId = company.Id, Code = "B1", Name = "Active Branch"
        });
        var created = await db.Branches.SingleAsync();
        Assert.True(created.IsActive);

        created.IsActive = false;
        await db.SaveChangesAsync();
        await controller.SaveBranch(new BranchInputViewModel
        {
            Id = created.Id, CompanyId = company.Id, Code = "B1", Name = "Updated Branch"
        });

        Assert.False(created.IsActive);
        Assert.Equal("Updated Branch", created.Name);
        Assert.DoesNotContain(typeof(BranchInputViewModel).GetProperties(), property => property.Name == nameof(Branch.IsActive));
    }

    [Fact]
    public async Task Supplier_customer_and_item_updates_preserve_inactive_status()
    {
        await using var db = CreateDatabase();
        var supplier = new Supplier { Code = "S1", Name = "Supplier", IsActive = false };
        var customer = new Customer { Code = "C1", Name = "Customer", IsActive = false };
        var item = new Item { Code = "I1", Name = "Item", Unit = "ea", IsActive = false };
        db.AddRange(supplier, customer, item);
        await db.SaveChangesAsync();
        var controller = new MasterController(db);

        await controller.Save("Suppliers", supplier.Id,
            new MasterInput { Code = "S1", Name = "Updated Supplier", Email = "supplier@example.test" });
        await controller.Save("Customers", customer.Id,
            new MasterInput { Code = "C1", Name = "Updated Customer", Email = "customer@example.test" });
        await controller.Save("Items", item.Id,
            new MasterInput { Code = "I1", Name = "Updated Item", Unit = "ea", UnitPrice = 1, ReorderLevel = 0 });

        Assert.False(supplier.IsActive);
        Assert.False(customer.IsActive);
        Assert.False(item.IsActive);
        Assert.Equal("Updated Supplier", supplier.Name);
        Assert.Equal("Updated Customer", customer.Name);
        Assert.Equal("Updated Item", item.Name);
    }

    private static ErpDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ErpDbContext(options);
    }

    private static AdminController CreateAdminController(ErpDbContext db)
    {
        return new AdminController(db, new PasswordHasher<ErpUser>(), NullLogger<AdminController>.Instance);
    }
}
