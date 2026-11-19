namespace MyERP.Web.Models
{
    public sealed class DashboardViewModel
    {
        public int SupplierCount { get; set; }
        public int CustomerCount { get; set; }
        public int ItemCount { get; set; }
        public int PendingApprovals { get; set; }
        public int OpenPurchaseOrders { get; set; }
        public int OpenSalesOrders { get; set; }
        public decimal InventoryValue { get; set; }
    }
}
