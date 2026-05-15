using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OctafxIndia.Data;
using OctafxIndia.Models;
using System.Linq;
using System.Threading.Tasks;

namespace OctafxIndia.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUserAccountsController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<AdminUserAccountsController> _logger;

        public AdminUserAccountsController(ApplicationDbContext dbContext, ILogger<AdminUserAccountsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // GET: /AdminUserAccounts
        [HttpGet]
        public async Task<IActionResult> IndexAsync()
        {
            var userTradingAccounts = await _dbContext.UserTradingAccounts
                                                 .OrderBy(acc => acc.Id)
                                                 .ToListAsync();
            return View(userTradingAccounts);
        }

        // GET: /AdminUserAccounts/Create
        [HttpGet]
        public IActionResult Create()
        {
            var viewModel = new UserTradingAccount
            {
                AccountType = "REAL",
                Server = "OctaFX-Real7",
                Status = "Active",
                Balance = 0m,
                Equity = 0m
            };
            return View("CreateEdit", viewModel);
        }

        // GET: /AdminUserAccounts/Edit/5
        [HttpGet]
        public async Task<IActionResult> EditAsync(int id)
        {
            var userTradingAccount = await _dbContext.UserTradingAccounts.FindAsync(id);
            if (userTradingAccount == null)
            {
                return NotFound();
            }
            return View("CreateEdit", userTradingAccount);
        }

        // POST: /AdminUserAccounts/CreateEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEditAsync(UserTradingAccount model)
        {
            if (!ModelState.IsValid)
            {
                return View("CreateEdit", model);
            }

            if (model.Id == 0)
            {
                // Create new entity
                _dbContext.UserTradingAccounts.Add(model);
                TempData["Success"] = "Account added successfully.";
            }
            else
            {
                // Update existing entity
                var userTradingAccount = await _dbContext.UserTradingAccounts.FindAsync(model.Id);
                if (userTradingAccount == null)
                {
                    return NotFound();
                }

                userTradingAccount.AccountNumber = model.AccountNumber;
                userTradingAccount.AccountType = model.AccountType;
                userTradingAccount.Server = model.Server;
                userTradingAccount.Balance = model.Balance;
                userTradingAccount.Equity = model.Equity;
                userTradingAccount.Status = model.Status;

                _dbContext.UserTradingAccounts.Update(userTradingAccount);
                TempData["Success"] = "Account updated successfully.";
            }

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while saving UserTradingAccount.");
                ModelState.AddModelError(string.Empty, "A database error occurred. Please try again.");
                return View("CreateEdit", model);
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /AdminUserAccounts/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            var userTradingAccount = await _dbContext.UserTradingAccounts.FindAsync(id);
            if (userTradingAccount == null)
            {
                return NotFound();
            }

            _dbContext.UserTradingAccounts.Remove(userTradingAccount);

            try
            {
                await _dbContext.SaveChangesAsync();
                TempData["Success"] = "Account deleted successfully.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while deleting UserTradingAccount.");
                TempData["Error"] = "A database error occurred while deleting the account.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
