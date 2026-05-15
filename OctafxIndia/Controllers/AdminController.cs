using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OctafxIndia.Data;
using OctafxIndia.Models;
using OctafxIndia.ViewModels;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OctafxIndia.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public AdminController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: /Admin/EditTradingAccount
        [HttpGet]
        public async Task<IActionResult> EditTradingAccountAsync()
        {
            var viewModel = await _dbContext.TradingAccounts
                .OrderByDescending(acc => acc.LastUpdated)
                .Select(acc => new EditTradingAccountViewModel
                {
                    AccountNumber = acc.AccountNumber,
                    AccountName = acc.AccountName,
                    Balance = acc.Balance,
                    FreeMargin = acc.FreeMargin,
                    Equity = acc.Equity,
                    Leverage = acc.Leverage,
                    Server = acc.Server,
                    NoSwap = acc.NoSwap,
                })
                .FirstOrDefaultAsync();

            if (viewModel == null)
            {
                // Return a view with a new (empty) view model if no trading account exists
                return View(new EditTradingAccountViewModel());
            }

            return View(viewModel);
        }

        // POST: /Admin/SaveTradingAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTradingAccountAsync(EditTradingAccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(nameof(EditTradingAccount), model);
            }

            // Use a transaction to ensure atomicity
            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var tradingAccount = await _dbContext.TradingAccounts.FirstOrDefaultAsync();

                    if (tradingAccount != null)
                    {
                        // Update existing entity
                        tradingAccount.AccountNumber = model.AccountNumber;
                        tradingAccount.AccountName = model.AccountName;
                        tradingAccount.Balance = model.Balance;
                        tradingAccount.FreeMargin = model.FreeMargin;
                        tradingAccount.Equity = model.Equity;
                        tradingAccount.Leverage = model.Leverage;
                        tradingAccount.Server = model.Server;
                        tradingAccount.NoSwap = model.NoSwap;
                        tradingAccount.LastUpdated = DateTime.UtcNow;
                        _dbContext.TradingAccounts.Update(tradingAccount);
                    }
                    else
                    {
                        // Insert new entity
                        var newTradingAccount = new TradingAccount
                        {
                            AccountNumber = model.AccountNumber,
                            AccountName = model.AccountName,
                            Balance = model.Balance,
                            FreeMargin = model.FreeMargin,
                            Equity = model.Equity,
                            Leverage = model.Leverage,
                            Server = model.Server,
                            NoSwap = model.NoSwap,
                            LastUpdated = DateTime.UtcNow,
                        };
                        _dbContext.TradingAccounts.Add(newTradingAccount);
                    }

                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    // Optional: Log the exception
                    throw;
                }
            }

            return RedirectToAction(nameof(EditTradingAccount));
        }
    }
}
