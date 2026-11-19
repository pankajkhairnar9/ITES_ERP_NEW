using System;
using System.ComponentModel.DataAnnotations;

namespace MyERP.Domain
{
    public abstract class Entity
    {
        public int Id { get; set; }
    }

    public enum WorkflowStatus
    {
        Draft,
        Submitted,
        Approved,
        Rejected,
        PartiallyReceived,
        Received,
        Dispatched
    }

    public sealed class Company : Entity
    {
        [Required, StringLength(40)]
        public string Code { get; set; }
        [Required, StringLength(160)]
        public string Name { get; set; }
        [StringLength(400)]
        public string Address { get; set; }
        [StringLength(40)]
        public string Phone { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public sealed class Branch : Entity
    {
        public int CompanyId { get; set; }
        public Company Company { get; set; }
        [Required, StringLength(40)]
        public string Code { get; set; }
        [Required, StringLength(160)]
        public string Name { get; set; }
        [StringLength(400)]
        public string Address { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public sealed class ErpUser : Entity
    {
        public int? BranchId { get; set; }
        public Branch Branch { get; set; }
        [Required, StringLength(100)]
        public string UserName { get; set; }
        [Required, EmailAddress, StringLength(160)]
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public sealed class ErpRole : Entity
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public sealed class ErpPermission : Entity
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Module { get; set; }
    }

    public sealed class ErpUserRole : Entity
    {
        public int UserId { get; set; }
        public ErpUser User { get; set; }
        public int RoleId { get; set; }
        public ErpRole Role { get; set; }
    }

    public sealed class ErpRolePermission : Entity
    {
        public int RoleId { get; set; }
        public ErpRole Role { get; set; }
        public int PermissionId { get; set; }
        public ErpPermission Permission { get; set; }
    }

    public sealed class MenuItem : Entity
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string PermissionCode { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public sealed class Supplier : Entity
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string ContactName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public sealed class Customer : Entity
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string ContactName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public sealed class Item : Entity
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal ReorderLevel { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public sealed class PurchaseRequest : Entity
    {
        public string Number { get; set; }
        public int BranchId { get; set; }
        public Branch Branch { get; set; }
        public int RequestedById { get; set; }
        public ErpUser RequestedBy { get; set; }
        public DateTime RequestedAt { get; set; }
        public WorkflowStatus Status { get; set; } = WorkflowStatus.Draft;
        public string Notes { get; set; }
    }

    public sealed class PurchaseRequestLine : Entity
    {
        public int PurchaseRequestId { get; set; }
        public PurchaseRequest PurchaseRequest { get; set; }
        public int ItemId { get; set; }
        public Item Item { get; set; }
        public decimal Quantity { get; set; }
        public decimal EstimatedUnitPrice { get; set; }
    }

    public sealed class PurchaseOrder : Entity
    {
        public string Number { get; set; }
        public int BranchId { get; set; }
        public Branch Branch { get; set; }
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }
        public int? PurchaseRequestId { get; set; }
        public PurchaseRequest PurchaseRequest { get; set; }
        public DateTime OrderedAt { get; set; }
        public WorkflowStatus Status { get; set; } = WorkflowStatus.Draft;
        public string Notes { get; set; }
    }

    public sealed class PurchaseOrderLine : Entity
    {
        public int PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; }
        public int ItemId { get; set; }
        public Item Item { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public sealed class GoodsReceipt : Entity
    {
        public string Number { get; set; }
        public int BranchId { get; set; }
        public Branch Branch { get; set; }
        public int PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; }
        public DateTime ReceivedAt { get; set; }
        public WorkflowStatus Status { get; set; } = WorkflowStatus.Submitted;
        public string Notes { get; set; }
    }

    public sealed class GoodsReceiptLine : Entity
    {
        public int GoodsReceiptId { get; set; }
        public GoodsReceipt GoodsReceipt { get; set; }
        public int PurchaseOrderLineId { get; set; }
        public PurchaseOrderLine PurchaseOrderLine { get; set; }
        public decimal Quantity { get; set; }
    }

    public sealed class SalesOrder : Entity
    {
        public string Number { get; set; }
        public int BranchId { get; set; }
        public Branch Branch { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }
        public DateTime OrderedAt { get; set; }
        public WorkflowStatus Status { get; set; } = WorkflowStatus.Approved;
        public string Notes { get; set; }
    }

    public sealed class SalesOrderLine : Entity
    {
        public int SalesOrderId { get; set; }
        public SalesOrder SalesOrder { get; set; }
        public int ItemId { get; set; }
        public Item Item { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public sealed class StockAdjustment : Entity
    {
        public string Reference { get; set; }
        public int BranchId { get; set; }
        public Branch Branch { get; set; }
        public int ItemId { get; set; }
        public Item Item { get; set; }
        public decimal QuantityDelta { get; set; }
        public DateTime AdjustedAt { get; set; }
        public string Reason { get; set; }
        public int AdjustedById { get; set; }
        public ErpUser AdjustedBy { get; set; }
    }

    public sealed class InventoryTransaction : Entity
    {
        public int BranchId { get; set; }
        public Branch Branch { get; set; }
        public int ItemId { get; set; }
        public Item Item { get; set; }
        public decimal QuantityDelta { get; set; }
        public string TransactionType { get; set; }
        public string Reference { get; set; }
        public DateTime OccurredAt { get; set; }
    }
}
