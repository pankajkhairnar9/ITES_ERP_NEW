using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MyERP.Domain;

namespace MyERP.Web.Models
{
    public sealed class LoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required, DataType(DataType.Password)]
        public string Password { get; set; }
        public string ReturnUrl { get; set; }
    }

    public sealed class UserRegistrationViewModel
    {
        [Required, StringLength(100)]
        public string UserName { get; set; }

        [Required, EmailAddress, StringLength(160)]
        public string Email { get; set; }

        [Required, DataType(DataType.Password), StringLength(256, MinimumLength = 12, ErrorMessage = "The password must be between 12 and 256 characters.")]
        public string Password { get; set; }

        [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public sealed class MasterInput
    {
        [Required, StringLength(40)]
        public string Code { get; set; }
        [Required, StringLength(160)]
        public string Name { get; set; }
        [StringLength(160)]
        public string ContactName { get; set; }
        [EmailAddress, StringLength(160)]
        public string Email { get; set; }
        [StringLength(40)]
        public string Phone { get; set; }
        [StringLength(400)]
        public string Address { get; set; }
        [StringLength(30)]
        public string Unit { get; set; }
        [Range(0, 999999999)]
        public decimal UnitPrice { get; set; }
        [Range(0, 999999999)]
        public decimal ReorderLevel { get; set; }
    }

    public sealed class MasterRow
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Contact { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Unit { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal ReorderLevel { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class MasterPageViewModel
    {
        public string Kind { get; set; }
        public string Title { get; set; }
        public MasterInput Input { get; set; } = new MasterInput();
        public IList<MasterRow> Rows { get; set; } = new List<MasterRow>();
    }

    public sealed class DocumentLineViewModel
    {
        public int ItemId { get; set; }
        public int PurchaseOrderLineId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public sealed class DocumentInputViewModel
    {
        [Required, StringLength(40)]
        public string Number { get; set; }
        [Range(1, int.MaxValue)]
        public int BranchId { get; set; }
        public int RelatedId { get; set; }
        public int PartyId { get; set; }
        public string Notes { get; set; }
        public IList<DocumentLineViewModel> Lines { get; set; } = new List<DocumentLineViewModel>();
    }

    public sealed class DocumentRowViewModel
    {
        public int Id { get; set; }
        public string Number { get; set; }
        public string Party { get; set; }
        public string Branch { get; set; }
        public DateTime Date { get; set; }
        public WorkflowStatus Status { get; set; }
    }

    public sealed class OperationPageViewModel
    {
        public string Title { get; set; }
        public DocumentInputViewModel Input { get; set; } = new DocumentInputViewModel();
        public IList<DocumentRowViewModel> Rows { get; set; } = new List<DocumentRowViewModel>();
        public IList<Branch> Branches { get; set; } = new List<Branch>();
        public IList<Supplier> Suppliers { get; set; } = new List<Supplier>();
        public IList<Customer> Customers { get; set; } = new List<Customer>();
        public IList<Item> Items { get; set; } = new List<Item>();
        public IList<PurchaseRequest> ApprovedRequests { get; set; } = new List<PurchaseRequest>();
        public IList<PurchaseOrder> ApprovedOrders { get; set; } = new List<PurchaseOrder>();
        public IList<PurchaseOrderLine> SelectedOrderLines { get; set; } = new List<PurchaseOrderLine>();
        public IList<StockRowViewModel> Stock { get; set; } = new List<StockRowViewModel>();
        public int? SelectedOrderId { get; set; }
    }

    public sealed class StockRowViewModel
    {
        public string Branch { get; set; }
        public string ItemCode { get; set; }
        public string Item { get; set; }
        public decimal OnHand { get; set; }
        public decimal ReorderLevel { get; set; }
    }

    public sealed class AdminPageViewModel
    {
        public Company EditingCompany { get; set; }
        public Branch EditingBranch { get; set; }
        public ErpUser EditingUser { get; set; }
        public string EditingUserRoleKeys { get; set; }
        public IList<Company> Companies { get; set; } = new List<Company>();
        public IList<Branch> Branches { get; set; } = new List<Branch>();
        public IList<ErpUser> Users { get; set; } = new List<ErpUser>();
        public IList<ErpRole> Roles { get; set; } = new List<ErpRole>();
        public IList<ErpPermission> Permissions { get; set; } = new List<ErpPermission>();
        public IList<MenuItem> Menus { get; set; } = new List<MenuItem>();
        public IList<string> RolePermissionKeys { get; set; } = new List<string>();
        public IList<string> UserRoleKeys { get; set; } = new List<string>();
    }
}
