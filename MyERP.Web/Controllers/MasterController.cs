using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyERP.Application;
using MyERP.Domain;
using MyERP.Web.Models;

namespace MyERP.Web.Controllers
{
    [Authorize(Policy = "masters.manage")]
    public sealed class MasterController : Controller
    {
        private readonly IErpDbContext _db;
        public MasterController(IErpDbContext db) { _db = db; }

        public async Task<IActionResult> Index(string kind)
        {
            if (!TryKind(kind, out var normalized)) return NotFound();
            return View(await IndexModelAsync(normalized));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string kind, int id)
        {
            if (!TryKind(kind, out var normalized)) return NotFound();
            var model = await IndexModelAsync(normalized);
            if (normalized == "Suppliers")
            {
                var row = await _db.Suppliers.FindAsync(id);
                if (row == null) return NotFound();
                model.Input = new MasterInput { Code = row.Code, Name = row.Name, ContactName = row.ContactName, Email = row.Email, Phone = row.Phone, Address = row.Address };
            }
            else if (normalized == "Customers")
            {
                var row = await _db.Customers.FindAsync(id);
                if (row == null) return NotFound();
                model.Input = new MasterInput { Code = row.Code, Name = row.Name, ContactName = row.ContactName, Email = row.Email, Phone = row.Phone, Address = row.Address };
            }
            else
            {
                var row = await _db.Items.FindAsync(id);
                if (row == null) return NotFound();
                model.Input = new MasterInput { Code = row.Code, Name = row.Name, Unit = row.Unit, UnitPrice = row.UnitPrice, ReorderLevel = row.ReorderLevel };
            }
            ViewData["EditId"] = id;
            return View("Index", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(string kind, int id, MasterInput input)
        {
            if (!TryKind(kind, out var normalized)) return NotFound();
            if (!ModelState.IsValid)
            {
                var model = await IndexModelAsync(normalized);
                model.Input = input;
                ViewData["EditId"] = id == 0 ? (int?)null : id;
                return View("Index", model);
            }
            if (normalized == "Suppliers")
            {
                var entity = id == 0 ? new Supplier() : await _db.Suppliers.FindAsync(id);
                if (entity == null) return NotFound();
                entity.Code = input.Code.Trim(); entity.Name = input.Name.Trim(); entity.ContactName = input.ContactName; entity.Email = input.Email; entity.Phone = input.Phone; entity.Address = input.Address;
                if (id == 0) _db.Suppliers.Add(entity);
            }
            else if (normalized == "Customers")
            {
                var entity = id == 0 ? new Customer() : await _db.Customers.FindAsync(id);
                if (entity == null) return NotFound();
                entity.Code = input.Code.Trim(); entity.Name = input.Name.Trim(); entity.ContactName = input.ContactName; entity.Email = input.Email; entity.Phone = input.Phone; entity.Address = input.Address;
                if (id == 0) _db.Customers.Add(entity);
            }
            else
            {
                var entity = id == 0 ? new Item() : await _db.Items.FindAsync(id);
                if (entity == null) return NotFound();
                entity.Code = input.Code.Trim(); entity.Name = input.Name.Trim(); entity.Unit = input.Unit; entity.UnitPrice = input.UnitPrice; entity.ReorderLevel = input.ReorderLevel;
                if (id == 0) _db.Items.Add(entity);
            }
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { kind = normalized });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string kind, int id)
        {
            if (!TryKind(kind, out var normalized)) return NotFound();
            if (normalized == "Suppliers")
            {
                var entity = await _db.Suppliers.FindAsync(id); if (entity == null) return NotFound(); entity.IsActive = false;
            }
            else if (normalized == "Customers")
            {
                var entity = await _db.Customers.FindAsync(id); if (entity == null) return NotFound(); entity.IsActive = false;
            }
            else
            {
                var entity = await _db.Items.FindAsync(id); if (entity == null) return NotFound(); entity.IsActive = false;
            }
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { kind = normalized });
        }

        private async Task<MasterPageViewModel> IndexModelAsync(string normalized)
        {
            var model = new MasterPageViewModel { Kind = normalized, Title = normalized };
            if (normalized == "Suppliers")
                model.Rows = await _db.Suppliers.Select(x => new MasterRow { Id = x.Id, Code = x.Code, Name = x.Name, Contact = x.ContactName, Email = x.Email, Phone = x.Phone, Address = x.Address, IsActive = x.IsActive }).ToListAsync();
            else if (normalized == "Customers")
                model.Rows = await _db.Customers.Select(x => new MasterRow { Id = x.Id, Code = x.Code, Name = x.Name, Contact = x.ContactName, Email = x.Email, Phone = x.Phone, Address = x.Address, IsActive = x.IsActive }).ToListAsync();
            else
                model.Rows = await _db.Items.Select(x => new MasterRow { Id = x.Id, Code = x.Code, Name = x.Name, Unit = x.Unit, UnitPrice = x.UnitPrice, ReorderLevel = x.ReorderLevel, IsActive = x.IsActive }).ToListAsync();
            return model;
        }

        private static bool TryKind(string kind, out string normalized)
        {
            normalized = new[] { "Suppliers", "Customers", "Items" }.FirstOrDefault(x => string.Equals(x, kind, StringComparison.OrdinalIgnoreCase));
            return normalized != null;
        }
    }
}
