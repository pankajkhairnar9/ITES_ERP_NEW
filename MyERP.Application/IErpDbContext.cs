using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyERP.Domain;

namespace MyERP.Application
{
    public interface IErpDbContext
    {
        DbSet<Company> Companies { get; }
        DbSet<Branch> Branches { get; }
        DbSet<ErpUser> Users { get; }
        DbSet<ErpRole> Roles { get; }
        DbSet<ErpPermission> Permissions { get; }
        DbSet<ErpUserRole> UserRoles { get; }
        DbSet<ErpRolePermission> RolePermissions { get; }
        DbSet<MenuItem> MenuItems { get; }
        DbSet<Supplier> Suppliers { get; }
        DbSet<Customer> Customers { get; }
        DbSet<Item> Items { get; }
        DbSet<PurchaseRequest> PurchaseRequests { get; }
        DbSet<PurchaseRequestLine> PurchaseRequestLines { get; }
        DbSet<PurchaseOrder> PurchaseOrders { get; }
        DbSet<PurchaseOrderLine> PurchaseOrderLines { get; }
        DbSet<GoodsReceipt> GoodsReceipts { get; }
        DbSet<GoodsReceiptLine> GoodsReceiptLines { get; }
        DbSet<SalesOrder> SalesOrders { get; }
        DbSet<SalesOrderLine> SalesOrderLines { get; }
        DbSet<StockAdjustment> StockAdjustments { get; }
        DbSet<InventoryTransaction> InventoryTransactions { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
