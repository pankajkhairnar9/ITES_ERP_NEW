using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyERP.Application;
using MyERP.Domain;
using MyERP.Infrastructure;

namespace MyERP.Tests;

public sealed class WorkflowServiceTests
{
    [Fact]
    public async Task Dispatch_rejects_duplicate_lines_that_exceed_available_stock()
    {
        await using var db = CreateDatabase();
        var (branch, item, customer) = await SeedSalesReferencesAsync(db);
        var order = new SalesOrder { Number = "SO-1", BranchId = branch.Id, CustomerId = customer.Id, Status = WorkflowStatus.Approved };
        db.SalesOrders.Add(order);
        db.SalesOrderLines.Add(new SalesOrderLine { SalesOrder = order, ItemId = item.Id, Quantity = 3, UnitPrice = 2 });
        db.SalesOrderLines.Add(new SalesOrderLine { SalesOrder = order, ItemId = item.Id, Quantity = 3, UnitPrice = 2 });
        db.InventoryTransactions.Add(new InventoryTransaction { BranchId = branch.Id, ItemId = item.Id, QuantityDelta = 5, TransactionType = "GRN", Reference = "GRN-1", OccurredAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new WorkflowService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DispatchSalesOrderAsync(order.Id));

        Assert.Equal(WorkflowStatus.Approved, order.Status);
        Assert.Equal(1, await db.InventoryTransactions.CountAsync());
    }

    [Fact]
    public async Task Adjustment_cannot_make_branch_stock_negative()
    {
        await using var db = CreateDatabase();
        var (branch, item, _) = await SeedSalesReferencesAsync(db);
        var user = new ErpUser { UserName = "stock-user", Email = "stock@example.test", PasswordHash = "not-used" };
        db.Users.Add(user);
        db.InventoryTransactions.Add(new InventoryTransaction { BranchId = branch.Id, ItemId = item.Id, QuantityDelta = 2, TransactionType = "GRN", Reference = "GRN-2", OccurredAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new WorkflowService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AdjustStockAsync("ADJ-1", branch.Id, item.Id, -3, "Count correction", user.Id));

        Assert.Equal(1, await db.InventoryTransactions.CountAsync());
        Assert.Empty(await db.StockAdjustments.ToListAsync());
    }

    [Fact]
    public async Task Receipt_approval_posts_stock_once_and_rejects_over_receipt()
    {
        await using var db = CreateDatabase();
        var company = new Company { Code = "C1", Name = "Company" };
        var branch = new Branch { Code = "B1", Name = "Branch", Company = company };
        var supplier = new Supplier { Code = "S1", Name = "Supplier" };
        var item = new Item { Code = "I1", Name = "Item", Unit = "ea", UnitPrice = 1 };
        db.AddRange(company, branch, supplier, item);
        await db.SaveChangesAsync();
        var order = new PurchaseOrder { Number = "PO-1", BranchId = branch.Id, SupplierId = supplier.Id, Status = WorkflowStatus.Approved };
        db.PurchaseOrders.Add(order);
        var orderLine = new PurchaseOrderLine { PurchaseOrder = order, ItemId = item.Id, Quantity = 5, UnitPrice = 1 };
        db.PurchaseOrderLines.Add(orderLine);
        var unreceivedLine = new PurchaseOrderLine { PurchaseOrder = order, ItemId = item.Id, Quantity = 3, UnitPrice = 1 };
        db.PurchaseOrderLines.Add(unreceivedLine);
        await db.SaveChangesAsync();
        var service = new WorkflowService(db);

        var firstReceipt = await service.CreateGoodsReceiptAsync("GRN-1", order.Id, null, new[]
        {
            new DocumentLineInput { PurchaseOrderLineId = orderLine.Id, Quantity = 4 },
            new DocumentLineInput { PurchaseOrderLineId = unreceivedLine.Id, Quantity = 0 }
        });
        await service.ReviewGoodsReceiptAsync(firstReceipt.Id, true);
        Assert.Equal(WorkflowStatus.PartiallyReceived, order.Status);
        Assert.Single(await db.InventoryTransactions.ToListAsync());

        var secondReceipt = await service.CreateGoodsReceiptAsync("GRN-2", order.Id, null, new[]
        {
            new DocumentLineInput { PurchaseOrderLineId = orderLine.Id, Quantity = 2 },
            new DocumentLineInput { PurchaseOrderLineId = unreceivedLine.Id, Quantity = 0 }
        });
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewGoodsReceiptAsync(secondReceipt.Id, true));

        Assert.Equal(WorkflowStatus.Submitted, secondReceipt.Status);
        Assert.Equal(1, await db.InventoryTransactions.CountAsync());
    }

    private static ErpDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ErpDbContext(options);
    }

    private static async Task<(Branch Branch, Item Item, Customer Customer)> SeedSalesReferencesAsync(ErpDbContext db)
    {
        var company = new Company { Code = "C1", Name = "Company" };
        var branch = new Branch { Code = "B1", Name = "Branch", Company = company };
        var item = new Item { Code = "I1", Name = "Item", Unit = "ea", UnitPrice = 2 };
        var customer = new Customer { Code = "CU1", Name = "Customer" };
        db.AddRange(company, branch, item, customer);
        await db.SaveChangesAsync();
        return (branch, item, customer);
    }
}
