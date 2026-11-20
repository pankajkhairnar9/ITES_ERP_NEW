using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyERP.Application;
using MyERP.Domain;
using MyERP.Web.Models;

namespace MyERP.Web.Services
{
    public sealed class ApprovalNoticesViewComponent : ViewComponent
    {
        private readonly IErpDbContext _db;

        public ApprovalNoticesViewComponent(IErpDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var permissions = new HashSet<string>(
                UserClaimsPrincipal.Claims.Where(x => x.Type == "permission").Select(x => x.Value));
            var model = new ApprovalNoticesViewModel();
            if (UserClaimsPrincipal.HasClaim("branch_scope", "none"))
                return View(model);

            var branchClaim = UserClaimsPrincipal.FindFirst("branch_id");
            int? branchId = int.TryParse(branchClaim?.Value, out var parsedBranchId) ? parsedBranchId : (int?)null;

            if (permissions.Contains("purchase.approve"))
            {
                var requests = _db.PurchaseRequests.Where(x => x.Status == WorkflowStatus.Submitted);
                if (branchId.HasValue) requests = requests.Where(x => x.BranchId == branchId.Value);
                model.Items.Add(new ApprovalNoticeViewModel { Name = "Purchase requests", Count = await requests.CountAsync() });
            }
            if (permissions.Contains("purchase.order.approve"))
            {
                var orders = _db.PurchaseOrders.Where(x => x.Status == WorkflowStatus.Submitted);
                if (branchId.HasValue) orders = orders.Where(x => x.BranchId == branchId.Value);
                model.Items.Add(new ApprovalNoticeViewModel { Name = "Purchase orders", Count = await orders.CountAsync() });
            }
            if (permissions.Contains("grn.manage"))
            {
                var receipts = _db.GoodsReceipts.Where(x => x.Status == WorkflowStatus.Submitted);
                if (branchId.HasValue) receipts = receipts.Where(x => x.BranchId == branchId.Value);
                model.Items.Add(new ApprovalNoticeViewModel { Name = "Goods receipts", Count = await receipts.CountAsync() });
            }

            return View(model);
        }
    }
}
