using System;
using System.Collections.Generic;

namespace MyERP.Web.Models
{
    public sealed class DashboardViewModel
    {
        public bool BranchAssignmentRequired { get; set; }
        public int SupplierCount { get; set; }
        public int CustomerCount { get; set; }
        public int ItemCount { get; set; }
        public int PendingApprovals { get; set; }
        public int OpenPurchaseOrders { get; set; }
        public int OpenSalesOrders { get; set; }
        public decimal InventoryValue { get; set; }
        public List<MonthlyOrderActivityViewModel> MonthlyOrderActivity { get; set; } = new List<MonthlyOrderActivityViewModel>();
        public List<RecentInventoryActivityViewModel> RecentInventoryActivity { get; set; } = new List<RecentInventoryActivityViewModel>();
    }

    public sealed class MonthlyOrderActivityViewModel
    {
        public string Month { get; set; }
        public int PurchaseOrders { get; set; }
        public int SalesOrders { get; set; }
    }

    public sealed class RecentInventoryActivityViewModel
    {
        public DateTime OccurredAt { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string TransactionType { get; set; }
        public string Reference { get; set; }
        public decimal QuantityDelta { get; set; }
    }
}
