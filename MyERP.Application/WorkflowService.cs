using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyERP.Domain;

namespace MyERP.Application
{
    public sealed class DocumentLineInput
    {
        public int ItemId { get; set; }
        public int PurchaseOrderLineId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public sealed class WorkflowService
    {
        private readonly IErpDbContext _db;

        public WorkflowService(IErpDbContext db)
        {
            _db = db;
        }

        public async Task<PurchaseRequest> CreatePurchaseRequestAsync(string number, int branchId, int userId, string notes, IList<DocumentLineInput> lines)
        {
            ValidateLines(lines);
            if (!await _db.Branches.AnyAsync(x => x.Id == branchId && x.IsActive))
                throw new InvalidOperationException("The selected branch is not active.");
            if (!await _db.Users.AnyAsync(x => x.Id == userId && x.IsActive))
                throw new InvalidOperationException("The requesting user is not active.");
            await EnsureActiveItemsAsync(lines.Select(x => x.ItemId));
            var request = new PurchaseRequest
            {
                Number = Required(number, "Request number"),
                BranchId = branchId,
                RequestedById = userId,
                RequestedAt = DateTime.UtcNow,
                Notes = notes,
                Status = WorkflowStatus.Submitted
            };
            _db.PurchaseRequests.Add(request);
            foreach (var line in lines)
                _db.PurchaseRequestLines.Add(new PurchaseRequestLine { PurchaseRequest = request, ItemId = line.ItemId, Quantity = line.Quantity, EstimatedUnitPrice = line.UnitPrice });
            await _db.SaveChangesAsync();
            return request;
        }

        public async Task ReviewPurchaseRequestAsync(int id, bool approve)
        {
            var request = await _db.PurchaseRequests.FindAsync(id);
            RequireStatus(request == null ? (WorkflowStatus?)null : request.Status, WorkflowStatus.Submitted, "Purchase request");
            request.Status = approve ? WorkflowStatus.Approved : WorkflowStatus.Rejected;
            await _db.SaveChangesAsync();
        }

        public async Task<PurchaseOrder> CreatePurchaseOrderAsync(string number, int requestId, int supplierId, string notes)
        {
            var request = await _db.PurchaseRequests.FindAsync(requestId);
            RequireStatus(request == null ? (WorkflowStatus?)null : request.Status, WorkflowStatus.Approved, "Purchase request");
            if (!await _db.Suppliers.AnyAsync(x => x.Id == supplierId && x.IsActive))
                throw new InvalidOperationException("The selected supplier is not active.");
            if (await _db.PurchaseOrders.AnyAsync(x => x.PurchaseRequestId == requestId))
                throw new InvalidOperationException("A purchase order already exists for this request.");

            var order = new PurchaseOrder
            {
                Number = Required(number, "Order number"),
                BranchId = request.BranchId,
                SupplierId = supplierId,
                PurchaseRequestId = requestId,
                OrderedAt = DateTime.UtcNow,
                Status = WorkflowStatus.Submitted,
                Notes = notes
            };
            var requestLines = await _db.PurchaseRequestLines.Where(x => x.PurchaseRequestId == requestId).ToListAsync();
            if (!requestLines.Any())
                throw new InvalidOperationException("A purchase request must contain at least one line.");
            _db.PurchaseOrders.Add(order);
            foreach (var line in requestLines)
                _db.PurchaseOrderLines.Add(new PurchaseOrderLine { PurchaseOrder = order, ItemId = line.ItemId, Quantity = line.Quantity, UnitPrice = line.EstimatedUnitPrice });
            await _db.SaveChangesAsync();
            return order;
        }

        public async Task ReviewPurchaseOrderAsync(int id, bool approve)
        {
            var order = await _db.PurchaseOrders.FindAsync(id);
            RequireStatus(order == null ? (WorkflowStatus?)null : order.Status, WorkflowStatus.Submitted, "Purchase order");
            order.Status = approve ? WorkflowStatus.Approved : WorkflowStatus.Rejected;
            await _db.SaveChangesAsync();
        }

        public async Task<GoodsReceipt> CreateGoodsReceiptAsync(string number, int purchaseOrderId, string notes, IList<DocumentLineInput> lines)
        {
            var order = await _db.PurchaseOrders.FindAsync(purchaseOrderId);
            if (order == null)
                throw new InvalidOperationException("Purchase order was not found.");
            if (order.Status != WorkflowStatus.Approved && order.Status != WorkflowStatus.PartiallyReceived)
                throw new InvalidOperationException("Purchase order must be Approved or PartiallyReceived before a receipt can be created.");
            if (lines == null || lines.Any(x => x == null || x.Quantity < 0))
                throw new InvalidOperationException("Received quantities cannot be negative.");
            var receivedLines = lines.Where(x => x.Quantity > 0).ToList();
            if (receivedLines.Count == 0 || receivedLines.Any(x => x.PurchaseOrderLineId <= 0))
                throw new InvalidOperationException("Enter a positive received quantity for at least one purchase order line.");
            if (receivedLines.GroupBy(x => x.PurchaseOrderLineId).Any(group => group.Count() > 1))
                throw new InvalidOperationException("A purchase order line can only appear once on a receipt.");
            var orderLines = await _db.PurchaseOrderLines.Where(x => x.PurchaseOrderId == purchaseOrderId).ToListAsync();
            if (receivedLines.Any(x => !orderLines.Any(orderLine => orderLine.Id == x.PurchaseOrderLineId)))
                throw new InvalidOperationException("A receipt line does not belong to the selected purchase order.");

            var receipt = new GoodsReceipt
            {
                Number = Required(number, "Receipt number"),
                BranchId = order.BranchId,
                PurchaseOrderId = order.Id,
                ReceivedAt = DateTime.UtcNow,
                Status = WorkflowStatus.Submitted,
                Notes = notes
            };
            _db.GoodsReceipts.Add(receipt);
            foreach (var line in receivedLines)
                _db.GoodsReceiptLines.Add(new GoodsReceiptLine { GoodsReceipt = receipt, PurchaseOrderLineId = line.PurchaseOrderLineId, Quantity = line.Quantity });
            await _db.SaveChangesAsync();
            return receipt;
        }

        public async Task ReviewGoodsReceiptAsync(int id, bool approve)
        {
            var receipt = await _db.GoodsReceipts.FindAsync(id);
            RequireStatus(receipt == null ? (WorkflowStatus?)null : receipt.Status, WorkflowStatus.Submitted, "Goods receipt");
            if (!approve)
            {
                receipt.Status = WorkflowStatus.Rejected;
                await _db.SaveChangesAsync();
                return;
            }

            var lines = await _db.GoodsReceiptLines.Where(x => x.GoodsReceiptId == id).ToListAsync();
            foreach (var line in lines)
            {
                var orderLine = await _db.PurchaseOrderLines.FindAsync(line.PurchaseOrderLineId);
                var alreadyReceived = await (from receiptLine in _db.GoodsReceiptLines
                                             join otherReceipt in _db.GoodsReceipts on receiptLine.GoodsReceiptId equals otherReceipt.Id
                                             where receiptLine.PurchaseOrderLineId == line.PurchaseOrderLineId
                                                 && otherReceipt.Status == WorkflowStatus.Approved
                                                 && otherReceipt.Id != receipt.Id
                                             select (decimal?)receiptLine.Quantity).SumAsync() ?? 0m;
                var thisReceiptQuantity = lines.Where(x => x.PurchaseOrderLineId == line.PurchaseOrderLineId).Sum(x => x.Quantity);
                if (orderLine == null || alreadyReceived + thisReceiptQuantity > orderLine.Quantity)
                    throw new InvalidOperationException("The received quantity exceeds the remaining purchase order quantity.");
                var itemId = orderLine.ItemId;
                _db.InventoryTransactions.Add(new InventoryTransaction
                {
                    BranchId = receipt.BranchId,
                    ItemId = itemId,
                    QuantityDelta = line.Quantity,
                    TransactionType = "GRN",
                    Reference = receipt.Number,
                    OccurredAt = DateTime.UtcNow
                });
            }
            receipt.Status = WorkflowStatus.Approved;
            var order = await _db.PurchaseOrders.FindAsync(receipt.PurchaseOrderId);
            var orderLines = await _db.PurchaseOrderLines.Where(x => x.PurchaseOrderId == order.Id).ToListAsync();
            var remaining = false;
            foreach (var orderLine in orderLines)
            {
                var received = await (from receiptLine in _db.GoodsReceiptLines
                                      join otherReceipt in _db.GoodsReceipts on receiptLine.GoodsReceiptId equals otherReceipt.Id
                                      where receiptLine.PurchaseOrderLineId == orderLine.Id
                                          && otherReceipt.Status == WorkflowStatus.Approved && otherReceipt.Id != receipt.Id
                                      select (decimal?)receiptLine.Quantity).SumAsync() ?? 0m;
                var currentReceiptQuantity = lines.Where(x => x.PurchaseOrderLineId == orderLine.Id).Sum(x => x.Quantity);
                remaining |= received + currentReceiptQuantity < orderLine.Quantity;
            }
            order.Status = remaining ? WorkflowStatus.PartiallyReceived : WorkflowStatus.Received;
            await _db.SaveChangesAsync();
        }

        public async Task<SalesOrder> CreateSalesOrderAsync(string number, int branchId, int customerId, string notes, IList<DocumentLineInput> lines)
        {
            ValidateLines(lines);
            if (!await _db.Branches.AnyAsync(x => x.Id == branchId && x.IsActive))
                throw new InvalidOperationException("The selected branch is not active.");
            if (!await _db.Customers.AnyAsync(x => x.Id == customerId && x.IsActive))
                throw new InvalidOperationException("The selected customer is not active.");
            await EnsureActiveItemsAsync(lines.Select(x => x.ItemId));
            var order = new SalesOrder
            {
                Number = Required(number, "Sales order number"),
                BranchId = branchId,
                CustomerId = customerId,
                OrderedAt = DateTime.UtcNow,
                Status = WorkflowStatus.Approved,
                Notes = notes
            };
            _db.SalesOrders.Add(order);
            foreach (var line in lines)
                _db.SalesOrderLines.Add(new SalesOrderLine { SalesOrder = order, ItemId = line.ItemId, Quantity = line.Quantity, UnitPrice = line.UnitPrice });
            await _db.SaveChangesAsync();
            return order;
        }

        public async Task DispatchSalesOrderAsync(int id)
        {
            var order = await _db.SalesOrders.FindAsync(id);
            RequireStatus(order == null ? (WorkflowStatus?)null : order.Status, WorkflowStatus.Approved, "Sales order");
            var lines = await _db.SalesOrderLines.Where(x => x.SalesOrderId == id).ToListAsync();
            foreach (var itemLines in lines.GroupBy(x => x.ItemId))
            {
                var available = await _db.InventoryTransactions.Where(x => x.BranchId == order.BranchId && x.ItemId == itemLines.Key)
                    .Select(x => (decimal?)x.QuantityDelta).SumAsync() ?? 0m;
                if (itemLines.Sum(x => x.Quantity) > available)
                    throw new InvalidOperationException("Insufficient stock to dispatch item " + itemLines.Key + ".");
            }
            foreach (var line in lines)
            {
                _db.InventoryTransactions.Add(new InventoryTransaction
                {
                    BranchId = order.BranchId,
                    ItemId = line.ItemId,
                    QuantityDelta = -line.Quantity,
                    TransactionType = "Dispatch",
                    Reference = order.Number,
                    OccurredAt = DateTime.UtcNow
                });
            }
            order.Status = WorkflowStatus.Dispatched;
            await _db.SaveChangesAsync();
        }

        public async Task AdjustStockAsync(string reference, int branchId, int itemId, decimal quantityDelta, string reason, int userId)
        {
            if (quantityDelta == 0)
                throw new InvalidOperationException("Adjustment quantity cannot be zero.");
            if (!await _db.Branches.AnyAsync(x => x.Id == branchId && x.IsActive))
                throw new InvalidOperationException("The selected branch is not active.");
            await EnsureActiveItemsAsync(new[] { itemId });
            if (!await _db.Users.AnyAsync(x => x.Id == userId && x.IsActive))
                throw new InvalidOperationException("The adjusting user is not active.");
            var available = await _db.InventoryTransactions.Where(x => x.BranchId == branchId && x.ItemId == itemId)
                .Select(x => (decimal?)x.QuantityDelta).SumAsync() ?? 0m;
            if (available + quantityDelta < 0)
                throw new InvalidOperationException("The adjustment would make stock negative.");
            var adjustment = new StockAdjustment
            {
                Reference = Required(reference, "Adjustment reference"),
                BranchId = branchId,
                ItemId = itemId,
                QuantityDelta = quantityDelta,
                AdjustedAt = DateTime.UtcNow,
                Reason = Required(reason, "Reason"),
                AdjustedById = userId
            };
            _db.StockAdjustments.Add(adjustment);
            _db.InventoryTransactions.Add(new InventoryTransaction
            {
                BranchId = branchId,
                ItemId = itemId,
                QuantityDelta = quantityDelta,
                TransactionType = "Adjustment",
                Reference = reference,
                OccurredAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        private static void ValidateLines(IList<DocumentLineInput> lines)
        {
            if (lines == null || lines.Count == 0 || lines.Any(x => x == null || x.ItemId <= 0 || x.Quantity <= 0 || x.UnitPrice < 0))
                throw new InvalidOperationException("Add at least one item with a positive quantity and non-negative price.");
        }

        private async Task EnsureActiveItemsAsync(IEnumerable<int> itemIds)
        {
            var ids = itemIds.Distinct().ToList();
            if (ids.Any(id => id <= 0) || await _db.Items.CountAsync(x => ids.Contains(x.Id) && x.IsActive) != ids.Count)
                throw new InvalidOperationException("One or more selected items are not active.");
        }

        private static string Required(string value, string label)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(label + " is required.");
            return value.Trim();
        }

        private static void RequireStatus(WorkflowStatus? current, WorkflowStatus expected, string label)
        {
            if (current == null)
                throw new InvalidOperationException(label + " was not found.");
            if (current != expected)
                throw new InvalidOperationException(label + " must be " + expected + " before this action.");
        }
    }
}
