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
            var model = new DashboardViewModel
            {
                SupplierCount = await _db.Suppliers.CountAsync(x => x.IsActive),
                CustomerCount = await _db.Customers.CountAsync(x => x.IsActive),
                ItemCount = await _db.Items.CountAsync(x => x.IsActive),
                PendingApprovals = await purchaseRequests.CountAsync(x => x.Status == WorkflowStatus.Submitted)
                    + await purchaseOrders.CountAsync(x => x.Status == WorkflowStatus.Submitted)
                    + await goodsReceipts.CountAsync(x => x.Status == WorkflowStatus.Submitted),
                OpenPurchaseOrders = await purchaseOrders.CountAsync(x => x.Status == WorkflowStatus.Approved || x.Status == WorkflowStatus.PartiallyReceived),
                OpenSalesOrders = await salesOrders.CountAsync(x => x.Status == WorkflowStatus.Approved),
                InventoryValue = inventory.GroupBy(x => new { x.BranchId, x.ItemId })
                    .Where(x => items.ContainsKey(x.Key.ItemId))
                    .Sum(x => x.Sum(transaction => transaction.QuantityDelta) * items[x.Key.ItemId].UnitPrice)
            };
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
