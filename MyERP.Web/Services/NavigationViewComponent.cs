using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyERP.Application;

namespace MyERP.Web.Services
{
    public sealed class NavigationViewComponent : ViewComponent
    {
        private readonly IErpDbContext _db;
        public NavigationViewComponent(IErpDbContext db) { _db = db; }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var permissions = UserClaimsPrincipal.Claims
                .Where(x => x.Type == "permission")
                .Select(x => x.Value)
                .ToList();
            var menu = await _db.MenuItems.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync();
            return View(menu.Where(x => permissions.Contains(x.PermissionCode)).ToList());
        }
    }
}
