using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OctafxIndia.Data;
using OctafxIndia.Models;

namespace OctafxIndia.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUserAccountsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AdminUserAccountsController> _logger;

        public AdminUserAccountsController(ApplicationDbContext db, ILogger<AdminUserAccountsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET: /AdminUserAccounts
        public async Task<IActionResult> Index()
        {
            var list = await _db.UserTradingAccounts
                                .OrderBy(a => a.Id)
                                .ToListAsync();

            return View(list);
        }

        // GET: /AdminUserAccounts/Create
        public IActionResult Create()
        {
            var model = new UserTradingAccount
            {
                AccountType = "REAL",
                Server = "OctaFX-Real7",
                Status = "Active",
                Balance = 0m,
                Equity = 0m
            };

            return View("CreateEdit", model);
        }

        // POST: /AdminUserAccounts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserTradingAccount model)
        {
            if (!ModelState.IsValid)
                return View("CreateEdit", model);

            try
            {
                _db.UserTradingAccounts.Add(model);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Account added.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "DbUpdateException in Create: {Inner}", dbEx.InnerException?.Message);

                ModelState.AddModelError("", "Database error while creating account.");
                TempData["CreateErrors"] = dbEx.InnerException?.Message ?? dbEx.Message;

                return View("CreateEdit", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Create.");

                ModelState.AddModelError("", "Unexpected error.");
                TempData["CreateErrors"] = ex.Message;

                return View("CreateEdit", model);
            }
        }

        // GET: /AdminUserAccounts/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _db.UserTradingAccounts.FindAsync(id);
            if (entity == null) return NotFound();

            return View("CreateEdit", entity);
        }

        // POST: /AdminUserAccounts/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserTradingAccount model)
        {
            if (!ModelState.IsValid)
                return View("CreateEdit", model);

            if (model.Id == 0)
                return BadRequest();

            var entity = await _db.UserTradingAccounts.FindAsync(model.Id);
            if (entity == null) return NotFound();

            // Map editable fields
            entity.AccountNumber = model.AccountNumber;
            entity.AccountType = model.AccountType;
            entity.Server = model.Server;
            entity.Balance = model.Balance;
            entity.Equity = model.Equity;
            entity.Status = model.Status;

            try
            {
                _db.UserTradingAccounts.Update(entity);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Account updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "DbUpdateException in Edit: {Inner}", dbEx.InnerException?.Message);

                ModelState.AddModelError("", "Database error while updating.");
                TempData["EditErrors"] = dbEx.InnerException?.Message ?? dbEx.Message;

                return View("CreateEdit", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Edit.");

                ModelState.AddModelError("", "Unexpected error.");
                TempData["EditErrors"] = ex.Message;

                return View("CreateEdit", model);
            }
        }

        // POST: /AdminUserAccounts/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.UserTradingAccounts.FindAsync(id);
            if (entity == null) return NotFound();

            try
            {
                _db.UserTradingAccounts.Remove(entity);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Account deleted.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "DbUpdateException in Delete: {Inner}", dbEx.InnerException?.Message);
                TempData["EditErrors"] = dbEx.InnerException?.Message ?? dbEx.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Delete.");
                TempData["EditErrors"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
