using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyERP.Application;
using MyERP.Domain;
using MyERP.Web.Models;

namespace MyERP.Web.Controllers
{
    [Authorize]
    public sealed class OperationsController : Controller
    {
        private readonly IErpDbContext _db;
        private readonly WorkflowService _workflow;

        public OperationsController(IErpDbContext db, WorkflowService workflow)
        {
            _db = db;
            _workflow = workflow;
        }

        [Authorize(Policy = "purchase.request")]
        public async Task<IActionResult> Requests()
        {
            var model = new OperationPageViewModel { Title = "Purchase requests" };
            var requests = _db.PurchaseRequests.AsQueryable();
            var assignedBranchId = AssignedBranchId();
            if (assignedBranchId.HasValue)
                requests = requests.Where(x => x.BranchId == assignedBranchId.Value);
            model.Rows = await (from request in requests
                                join branch in _db.Branches on request.BranchId equals branch.Id
                                orderby request.RequestedAt descending
                                select new DocumentRowViewModel { Id = request.Id, Number = request.Number, Branch = branch.Name, Date = request.RequestedAt, Status = request.Status }).ToListAsync();
            return View(model);
        }

        [Authorize(Policy = "purchase.request")]
        [HttpGet]
        public async Task<IActionResult> NewRequest() => View(await PrepareInputPageAsync("New purchase request"));

        [Authorize(Policy = "purchase.request")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> NewRequest(OperationPageViewModel page)
        {
            var input = page.Input;
            try
            {
                if (!await CanUseBranchAsync(input.BranchId))
                    throw new InvalidOperationException("Select a branch assigned to your account.");
                await _workflow.CreatePurchaseRequestAsync(input.Number, input.BranchId, CurrentUserId(), input.Notes, ConvertLines(input));
                return RedirectToAction(nameof(Requests));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                var model = await PrepareInputPageAsync("New purchase request");
                model.Input = input;
                return View(model);
            }
        }

        [Authorize(Policy = "purchase.approve")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewRequest(int id, bool approve)
        {
            var request = await _db.PurchaseRequests.FindAsync(id);
            if (request == null || !await CanUseBranchAsync(request.BranchId)) return Forbid();
            try { await _workflow.ReviewPurchaseRequestAsync(id, approve); }
            catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction(nameof(Requests));
        }

        [Authorize(Policy = "purchase.order")]
        public async Task<IActionResult> Orders()
        {
            var model = new OperationPageViewModel { Title = "Purchase orders" };
            var orders = _db.PurchaseOrders.AsQueryable();
            var assignedBranchId = AssignedBranchId();
            if (assignedBranchId.HasValue)
                orders = orders.Where(x => x.BranchId == assignedBranchId.Value);
            model.Rows = await (from order in orders
                                join branch in _db.Branches on order.BranchId equals branch.Id
                                join supplier in _db.Suppliers on order.SupplierId equals supplier.Id
                                orderby order.OrderedAt descending
                                select new DocumentRowViewModel { Id = order.Id, Number = order.Number, Branch = branch.Name, Party = supplier.Name, Date = order.OrderedAt, Status = order.Status }).ToListAsync();
            var requests = _db.PurchaseRequests.Where(x => x.Status == WorkflowStatus.Approved);
            if (assignedBranchId.HasValue)
                requests = requests.Where(x => x.BranchId == assignedBranchId.Value);
            model.ApprovedRequests = await requests.OrderByDescending(x => x.RequestedAt).ToListAsync();
            model.Suppliers = await _db.Suppliers.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync();
            return View(model);
        }

        [Authorize(Policy = "purchase.order")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder(string number, int requestId, int supplierId, string notes)
        {
            var request = await _db.PurchaseRequests.FindAsync(requestId);
            if (request == null || !await CanUseBranchAsync(request.BranchId)) return Forbid();
            try { await _workflow.CreatePurchaseOrderAsync(number, requestId, supplierId, notes); }
            catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction(nameof(Orders));
        }

        [Authorize(Policy = "purchase.order.approve")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewOrder(int id, bool approve)
        {
            var order = await _db.PurchaseOrders.FindAsync(id);
            if (order == null || !await CanUseBranchAsync(order.BranchId)) return Forbid();
            try { await _workflow.ReviewPurchaseOrderAsync(id, approve); }
            catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction(nameof(Orders));
        }

        [Authorize(Policy = "grn.manage")]
        public async Task<IActionResult> Receipts()
        {
            var model = new OperationPageViewModel { Title = "Goods receipts" };
            var receipts = _db.GoodsReceipts.AsQueryable();
            var assignedBranchId = AssignedBranchId();
            if (assignedBranchId.HasValue)
                receipts = receipts.Where(x => x.BranchId == assignedBranchId.Value);
            model.Rows = await (from receipt in receipts
                                join order in _db.PurchaseOrders on receipt.PurchaseOrderId equals order.Id
                                join branch in _db.Branches on receipt.BranchId equals branch.Id
                                orderby receipt.ReceivedAt descending
                                select new DocumentRowViewModel { Id = receipt.Id, Number = receipt.Number, Party = order.Number, Branch = branch.Name, Date = receipt.ReceivedAt, Status = receipt.Status }).ToListAsync();
            var orders = _db.PurchaseOrders.Where(x => x.Status == WorkflowStatus.Approved || x.Status == WorkflowStatus.PartiallyReceived);
            if (assignedBranchId.HasValue) orders = orders.Where(x => x.BranchId == assignedBranchId.Value);
            model.ApprovedOrders = await orders.OrderByDescending(x => x.OrderedAt).ToListAsync();
            return View(model);
        }

        [Authorize(Policy = "grn.manage")]
        [HttpGet]
        public async Task<IActionResult> NewReceipt(int? orderId)
        {
            var orderQuery = _db.PurchaseOrders.Where(x => x.Status == WorkflowStatus.Approved || x.Status == WorkflowStatus.PartiallyReceived);
            var assignedBranchId = AssignedBranchId();
            if (assignedBranchId.HasValue) orderQuery = orderQuery.Where(x => x.BranchId == assignedBranchId.Value);
            var model = new OperationPageViewModel { Title = "Receive goods", ApprovedOrders = await orderQuery.OrderBy(x => x.Number).ToListAsync(), SelectedOrderId = orderId };
            if (orderId.HasValue)
            {
                if (!model.ApprovedOrders.Any(x => x.Id == orderId.Value)) return Forbid();
                model.SelectedOrderLines = await _db.PurchaseOrderLines.Where(x => x.PurchaseOrderId == orderId.Value).ToListAsync();
                model.Items = await _db.Items.OrderBy(x => x.Name).ToListAsync();
            }
            return View(model);
        }

        [Authorize(Policy = "grn.manage")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> NewReceipt(OperationPageViewModel page)
        {
            var input = page.Input;
            try
            {
                var order = await _db.PurchaseOrders.FindAsync(input.RelatedId);
                if (order == null || !await CanUseBranchAsync(order.BranchId)) return Forbid();
                await _workflow.CreateGoodsReceiptAsync(input.Number, input.RelatedId, input.Notes, ConvertLines(input));
                return RedirectToAction(nameof(Receipts));
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(NewReceipt), new { orderId = input.RelatedId });
            }
        }

        [Authorize(Policy = "grn.manage")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewReceipt(int id, bool approve)
        {
            var receipt = await _db.GoodsReceipts.FindAsync(id);
            if (receipt == null || !await CanUseBranchAsync(receipt.BranchId)) return Forbid();
            try { await _workflow.ReviewGoodsReceiptAsync(id, approve); }
            catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction(nameof(Receipts));
        }

        [Authorize(Policy = "sales.manage")]
        public async Task<IActionResult> Sales()
        {
            var model = await PrepareInputPageAsync("Sales orders");
            model.Customers = await _db.Customers.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync();
            var salesOrders = _db.SalesOrders.AsQueryable();
            var assignedBranchId = AssignedBranchId();
            if (assignedBranchId.HasValue)
                salesOrders = salesOrders.Where(x => x.BranchId == assignedBranchId.Value);
            model.Rows = await (from order in salesOrders
                                join customer in _db.Customers on order.CustomerId equals customer.Id
                                join branch in _db.Branches on order.BranchId equals branch.Id
                                orderby order.OrderedAt descending
                                select new DocumentRowViewModel { Id = order.Id, Number = order.Number, Party = customer.Name, Branch = branch.Name, Date = order.OrderedAt, Status = order.Status }).ToListAsync();
            return View(model);
        }

        [Authorize(Policy = "sales.manage")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Sales(OperationPageViewModel page)
        {
            var input = page.Input;
            try
            {
                if (!await CanUseBranchAsync(input.BranchId))
                    throw new InvalidOperationException("Select a branch assigned to your account.");
                await _workflow.CreateSalesOrderAsync(input.Number, input.BranchId, input.PartyId, input.Notes, ConvertLines(input));
                return RedirectToAction(nameof(Sales));
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Sales));
            }
        }

        [Authorize(Policy = "sales.manage")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Dispatch(int id)
        {
            var order = await _db.SalesOrders.FindAsync(id);
            if (order == null || !await CanUseBranchAsync(order.BranchId)) return Forbid();
            try { await _workflow.DispatchSalesOrderAsync(id); }
            catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction(nameof(Sales));
        }

        [Authorize(Policy = "stock.adjust")]
        public async Task<IActionResult> Adjustments()
        {
            var model = await PrepareInputPageAsync("Stock adjustment");
            return View(model);
        }

        [Authorize(Policy = "stock.adjust")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjustments(string reference, int branchId, int itemId, decimal quantityDelta, string reason)
        {
            try
            {
                if (!await CanUseBranchAsync(branchId))
                    throw new InvalidOperationException("Select a branch assigned to your account.");
                await _workflow.AdjustStockAsync(reference, branchId, itemId, quantityDelta, reason, CurrentUserId());
                TempData["Message"] = "Stock adjustment posted.";
            }
            catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction(nameof(Adjustments));
        }

        [Authorize(Policy = "reports.view")]
        public async Task<IActionResult> Reports()
        {
            var branches = await _db.Branches.ToDictionaryAsync(x => x.Id, x => x.Name);
            var items = await _db.Items.ToDictionaryAsync(x => x.Id);
            var transactionsQuery = _db.InventoryTransactions.AsQueryable();
            var assignedBranchId = AssignedBranchId();
            if (assignedBranchId.HasValue)
                transactionsQuery = transactionsQuery.Where(x => x.BranchId == assignedBranchId.Value);
            var transactions = await transactionsQuery.ToListAsync();
            var model = transactions.GroupBy(x => new { x.BranchId, x.ItemId })
                .Select(group => new StockRowViewModel
                {
                    Branch = branches.ContainsKey(group.Key.BranchId) ? branches[group.Key.BranchId] : "Unknown",
                    ItemCode = items.ContainsKey(group.Key.ItemId) ? items[group.Key.ItemId].Code : "",
                    Item = items.ContainsKey(group.Key.ItemId) ? items[group.Key.ItemId].Name : "Unknown item",
                    OnHand = group.Sum(x => x.QuantityDelta),
                    ReorderLevel = items.ContainsKey(group.Key.ItemId) ? items[group.Key.ItemId].ReorderLevel : 0
                })
                .OrderBy(x => x.Branch).ThenBy(x => x.Item).ToList();
            return View(new OperationPageViewModel { Title = "Stock report", Stock = model });
        }

        private async Task<OperationPageViewModel> PrepareInputPageAsync(string title)
        {
            var model = new OperationPageViewModel
            {
                Title = title,
                Branches = await AccessibleBranchesAsync(),
                Items = await _db.Items.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync()
            };
            return model;
        }

        private async Task<List<Branch>> AccessibleBranchesAsync()
        {
            var branchClaim = User.FindFirst("branch_id");
            var query = _db.Branches.Where(x => x.IsActive);
            if (branchClaim != null && int.TryParse(branchClaim.Value, out var branchId))
                query = query.Where(x => x.Id == branchId);
            return await query.OrderBy(x => x.Name).ToListAsync();
        }

        private async Task<bool> CanUseBranchAsync(int branchId)
        {
            return (await AccessibleBranchesAsync()).Any(x => x.Id == branchId);
        }

        private int? AssignedBranchId()
        {
            var branchClaim = User.FindFirst("branch_id");
            return branchClaim != null && int.TryParse(branchClaim.Value, out var branchId) ? branchId : (int?)null;
        }

        private int CurrentUserId()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
                throw new InvalidOperationException("Your session is not associated with an ERP user.");
            return id;
        }

        private static IList<DocumentLineInput> ConvertLines(DocumentInputViewModel input)
        {
            return (input.Lines ?? new List<DocumentLineViewModel>()).Select(x => new DocumentLineInput
            {
                ItemId = x.ItemId,
                PurchaseOrderLineId = x.PurchaseOrderLineId,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice
            }).Where(x => x.ItemId > 0 || x.PurchaseOrderLineId > 0).ToList();
        }
    }
}
