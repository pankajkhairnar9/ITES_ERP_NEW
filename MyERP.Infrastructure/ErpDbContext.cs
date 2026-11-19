using Microsoft.EntityFrameworkCore;
using MyERP.Application;
using MyERP.Domain;

namespace MyERP.Infrastructure
{
    public sealed class ErpDbContext : DbContext, IErpDbContext
    {
        public ErpDbContext(DbContextOptions<ErpDbContext> options) : base(options) { }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<ErpUser> Users { get; set; }
        public DbSet<ErpRole> Roles { get; set; }
        public DbSet<ErpPermission> Permissions { get; set; }
        public DbSet<ErpUserRole> UserRoles { get; set; }
        public DbSet<ErpRolePermission> RolePermissions { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<PurchaseRequest> PurchaseRequests { get; set; }
        public DbSet<PurchaseRequestLine> PurchaseRequestLines { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; }
        public DbSet<GoodsReceipt> GoodsReceipts { get; set; }
        public DbSet<GoodsReceiptLine> GoodsReceiptLines { get; set; }
        public DbSet<SalesOrder> SalesOrders { get; set; }
        public DbSet<SalesOrderLine> SalesOrderLines { get; set; }
        public DbSet<StockAdjustment> StockAdjustments { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Company>().HasIndex(x => x.Code).IsUnique();
            modelBuilder.Entity<Branch>().HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            modelBuilder.Entity<ErpUser>().HasIndex(x => x.Email).IsUnique();
            modelBuilder.Entity<ErpRole>().HasIndex(x => x.Name).IsUnique();
            modelBuilder.Entity<ErpPermission>().HasIndex(x => x.Code).IsUnique();
            modelBuilder.Entity<Supplier>().HasIndex(x => x.Code).IsUnique();
            modelBuilder.Entity<Customer>().HasIndex(x => x.Code).IsUnique();
            modelBuilder.Entity<Item>().HasIndex(x => x.Code).IsUnique();
            modelBuilder.Entity<PurchaseRequest>().HasIndex(x => x.Number).IsUnique();
            modelBuilder.Entity<PurchaseOrder>().HasIndex(x => x.Number).IsUnique();
            modelBuilder.Entity<GoodsReceipt>().HasIndex(x => x.Number).IsUnique();
            modelBuilder.Entity<SalesOrder>().HasIndex(x => x.Number).IsUnique();
            modelBuilder.Entity<StockAdjustment>().HasIndex(x => x.Reference).IsUnique();
            modelBuilder.Entity<ErpUserRole>().HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
            modelBuilder.Entity<ErpRolePermission>().HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();

            modelBuilder.Entity<Branch>().HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ErpUser>().HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ErpUserRole>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ErpUserRole>().HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ErpRolePermission>().HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ErpRolePermission>().HasOne(x => x.Permission).WithMany().HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PurchaseRequest>().HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PurchaseRequest>().HasOne(x => x.RequestedBy).WithMany().HasForeignKey(x => x.RequestedById).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PurchaseRequestLine>().HasOne(x => x.PurchaseRequest).WithMany().HasForeignKey(x => x.PurchaseRequestId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PurchaseRequestLine>().HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PurchaseOrder>().HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PurchaseOrder>().HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PurchaseOrder>().HasOne(x => x.PurchaseRequest).WithMany().HasForeignKey(x => x.PurchaseRequestId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PurchaseOrderLine>().HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PurchaseOrderLine>().HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<GoodsReceipt>().HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<GoodsReceipt>().HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<GoodsReceiptLine>().HasOne(x => x.GoodsReceipt).WithMany().HasForeignKey(x => x.GoodsReceiptId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<GoodsReceiptLine>().HasOne(x => x.PurchaseOrderLine).WithMany().HasForeignKey(x => x.PurchaseOrderLineId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<SalesOrder>().HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<SalesOrder>().HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<SalesOrderLine>().HasOne(x => x.SalesOrder).WithMany().HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<SalesOrderLine>().HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<StockAdjustment>().HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<StockAdjustment>().HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<StockAdjustment>().HasOne(x => x.AdjustedBy).WithMany().HasForeignKey(x => x.AdjustedById).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<InventoryTransaction>().HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<InventoryTransaction>().HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Item>().Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Item>().Property(x => x.ReorderLevel).HasColumnType("decimal(18,3)");
            modelBuilder.Entity<PurchaseRequestLine>().Property(x => x.Quantity).HasColumnType("decimal(18,3)");
            modelBuilder.Entity<PurchaseRequestLine>().Property(x => x.EstimatedUnitPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<PurchaseOrderLine>().Property(x => x.Quantity).HasColumnType("decimal(18,3)");
            modelBuilder.Entity<PurchaseOrderLine>().Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<GoodsReceiptLine>().Property(x => x.Quantity).HasColumnType("decimal(18,3)");
            modelBuilder.Entity<SalesOrderLine>().Property(x => x.Quantity).HasColumnType("decimal(18,3)");
            modelBuilder.Entity<SalesOrderLine>().Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<InventoryTransaction>().Property(x => x.QuantityDelta).HasColumnType("decimal(18,3)");
            modelBuilder.Entity<StockAdjustment>().Property(x => x.QuantityDelta).HasColumnType("decimal(18,3)");
        }
    }
}
