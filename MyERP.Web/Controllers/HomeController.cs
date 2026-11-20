using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyERP.Application;
using MyERP.Domain;
using Microsoft.Extensions.Logging;
using MyERP.Web.Models;

namespace MyERP.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IErpDbContext _db;

        public HomeController(ILogger<HomeController> logger, IErpDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        [Authorize(Policy = "dashboard.view")]
        public async Task<IActionResult> Index()
        {
            if (User.HasClaim("branch_scope", "none"))
                return View(new DashboardViewModel { BranchAssignmentRequired = true });

            var items = await _db.Items.ToDictionaryAsync(x => x.Id);
            var transactions = _db.InventoryTransactions.AsQueryable();
            var assignedBranchId = User.FindFirst("branch_id");
            if (assignedBranchId != null && int.TryParse(assignedBranchId.Value, out var branchId))
                transactions = transactions.Where(x => x.BranchId == branchId);
            var inventory = await transactions.ToListAsync();
            var purchaseRequests = _db.PurchaseRequests.AsQueryable();
            var purchaseOrders = _db.PurchaseOrders.AsQueryable();
            var goodsReceipts = _db.GoodsReceipts.AsQueryable();
            var salesOrders = _db.SalesOrders.AsQueryable();
            if (assignedBranchId != null && int.TryParse(assignedBranchId.Value, out branchId))
            {
                purchaseRequests = purchaseRequests.Where(x => x.BranchId == branchId);
                purchaseOrders = purchaseOrders.Where(x => x.BranchId == branchId);
                goodsReceipts = goodsReceipts.Where(x => x.BranchId == branchId);
                salesOrders = salesOrders.Where(x => x.BranchId == branchId);
            }
            var canApproveRequests = User.HasClaim("permission", "purchase.approve");
            var canViewPurchaseOrders = User.HasClaim("permission", "purchase.order") || User.HasClaim("permission", "purchase.order.approve");
            var canViewSalesOrders = User.HasClaim("permission", "sales.manage");
            var canViewInventory = User.HasClaim("permission", "reports.view") || User.HasClaim("permission", "stock.adjust");
            var pendingApprovals = 0;
            if (canApproveRequests)
                pendingApprovals += await purchaseRequests.CountAsync(x => x.Status == WorkflowStatus.Submitted);
            if (User.HasClaim("permission", "purchase.order.approve"))
                pendingApprovals += await purchaseOrders.CountAsync(x => x.Status == WorkflowStatus.Submitted);
            if (User.HasClaim("permission", "grn.manage"))
                pendingApprovals += await goodsReceipts.CountAsync(x => x.Status == WorkflowStatus.Submitted);
            var model = new DashboardViewModel
            {
                SupplierCount = await _db.Suppliers.CountAsync(x => x.IsActive),
                CustomerCount = await _db.Customers.CountAsync(x => x.IsActive),
                ItemCount = await _db.Items.CountAsync(x => x.IsActive),
                PendingApprovals = pendingApprovals,
                OpenPurchaseOrders = canViewPurchaseOrders
                    ? await purchaseOrders.CountAsync(x => x.Status == WorkflowStatus.Approved || x.Status == WorkflowStatus.PartiallyReceived)
                    : 0,
                OpenSalesOrders = canViewSalesOrders ? await salesOrders.CountAsync(x => x.Status == WorkflowStatus.Approved) : 0,
                InventoryValue = inventory.GroupBy(x => new { x.BranchId, x.ItemId })
                    .Where(x => items.ContainsKey(x.Key.ItemId))
                    .Sum(x => x.Sum(transaction => transaction.QuantityDelta) * items[x.Key.ItemId].UnitPrice)
            };

            var firstMonth = new System.DateTime(System.DateTime.UtcNow.Year, System.DateTime.UtcNow.Month, 1).AddMonths(-5);
            var purchaseOrderDates = canViewPurchaseOrders
                ? await purchaseOrders.Where(x => x.OrderedAt >= firstMonth).Select(x => x.OrderedAt).ToListAsync()
                : new System.Collections.Generic.List<System.DateTime>();
            var salesOrderDates = canViewSalesOrders
                ? await salesOrders.Where(x => x.OrderedAt >= firstMonth).Select(x => x.OrderedAt).ToListAsync()
                : new System.Collections.Generic.List<System.DateTime>();
            model.MonthlyOrderActivity = Enumerable.Range(0, 6)
                .Select(offset => firstMonth.AddMonths(offset))
                .Select(month => new MonthlyOrderActivityViewModel
                {
                    Month = month.ToString("MMM"),
                    PurchaseOrders = purchaseOrderDates.Count(date => date.Year == month.Year && date.Month == month.Month),
                    SalesOrders = salesOrderDates.Count(date => date.Year == month.Year && date.Month == month.Month)
                })
                .ToList();

            if (canViewInventory)
            {
                model.RecentInventoryActivity = await transactions
                    .Include(x => x.Item)
                    .OrderByDescending(x => x.OccurredAt)
                    .Take(8)
                    .Select(x => new RecentInventoryActivityViewModel
                    {
                        OccurredAt = x.OccurredAt,
                        ItemCode = x.Item.Code,
                        ItemName = x.Item.Name,
                        TransactionType = x.TransactionType,
                        Reference = x.Reference,
                        QuantityDelta = x.QuantityDelta
                    })
                    .ToListAsync();
            }
            return View(model);
        }

        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
