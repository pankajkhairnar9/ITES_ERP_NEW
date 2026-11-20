using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyERP.Application;
using MyERP.Web.Models;

namespace MyERP.Web.Services
{
    public sealed class NavigationViewComponent : ViewComponent
    {
        private readonly IErpDbContext _db;

        public NavigationViewComponent(IErpDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var permissions = new HashSet<string>(
                UserClaimsPrincipal.Claims.Where(x => x.Type == "permission").Select(x => x.Value),
                StringComparer.OrdinalIgnoreCase);
            var path = HttpContext.Request.Path.Value ?? "/";
            var menu = await _db.MenuItems
                .Where(x => x.IsActive && x.PermissionCode != null)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();

            var items = menu
                .Where(x => permissions.Contains(x.PermissionCode) && IsLocalPath(x.Url))
                .Select(x => new NavigationLinkViewModel
                {
                    Name = x.Name,
                    Url = x.Url,
                    IsActive = IsActivePath(path, x.Url)
                })
                .ToList();

            var groups = items
                .GroupBy(x => GetGroup(x.Url))
                .Select(group => new NavigationGroupViewModel
                {
                    Name = group.Key.Name,
                    Icon = group.Key.Icon,
                    Items = group.ToList(),
                    IsActive = group.Any(x => x.IsActive)
                })
                .ToList();

            return View(groups);
        }

        private static bool IsLocalPath(string url)
        {
            return !string.IsNullOrWhiteSpace(url)
                && url.StartsWith("/", StringComparison.Ordinal)
                && !url.StartsWith("//", StringComparison.Ordinal)
                && !url.Contains('\\')
                && !url.Any(char.IsControl);
        }

        private static bool IsActivePath(string requestPath, string menuUrl)
        {
            var normalizedRequest = NormalizePath(requestPath);
            var normalizedMenu = NormalizePath(menuUrl);
            return normalizedRequest.Equals(normalizedMenu, StringComparison.OrdinalIgnoreCase)
                || (normalizedMenu != "/" && normalizedRequest.StartsWith(normalizedMenu + "/", StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizePath(string path)
        {
            var normalized = path.TrimEnd('/');
            return string.IsNullOrEmpty(normalized) ? "/" : normalized;
        }

        private static (string Name, string Icon) GetGroup(string url)
        {
            var controller = url.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            switch (controller?.ToLowerInvariant())
            {
                case null:
                    return ("Dashboard", "mdi-view-dashboard");
                case "admin":
                    return ("Administration", "mdi-settings");
                case "master":
                    return ("Master Data", "mdi-database");
                default:
                    return ("Operations", "mdi-cart");
            }
        }
    }
}
