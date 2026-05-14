using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OctafxIndia.Data;
using OctafxIndia.ViewModels;

namespace OctafxIndia.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;

        // 🔥 HARD-CODE YOUR ADMIN EMAIL HERE 🔥
        private const string ADMIN_EMAIL = "chandrarout0@gmail.com";

        public AdminController(ApplicationDbContext db) => _db = db;

        private bool IsAllowedAdmin()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            return email != null && email.Equals(ADMIN_EMAIL, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<IActionResult> EditTradingAccount()
        {
            if (!IsAllowedAdmin())
                return Forbid();

            var acc = await _db.TradingAccounts
                .OrderByDescending(a => a.LastUpdated)
                .FirstOrDefaultAsync();

            var vm = acc != null
                ? new EditTradingAccountViewModel
                {
                    AccountNumber = acc.AccountNumber,
                    AccountName = acc.AccountName,
                    Balance = acc.Balance,
                    FreeMargin = acc.FreeMargin,
                    Equity = acc.Equity,
                    Leverage = acc.Leverage,
                    Server = acc.Server,
                    NoSwap = acc.NoSwap
                }
                : new EditTradingAccountViewModel();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTradingAccount(EditTradingAccountViewModel model)
        {
            if (!IsAllowedAdmin())
                return Forbid();

            if (!ModelState.IsValid)
                return View(nameof(EditTradingAccount), model);

            // UPSERT to enforce single-row table
            await _db.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO trading_accounts (
    id, account_number, account_name, balance,
    free_margin, equity, leverage, server, no_swap, last_updated
) VALUES (
    1, {model.AccountNumber}, {model.AccountName},
    {model.Balance}, {model.FreeMargin}, {model.Equity},
    {model.Leverage}, {model.Server}, {model.NoSwap}, NOW()
)
ON CONFLICT (id) DO UPDATE SET
    account_number = EXCLUDED.account_number,
    account_name   = EXCLUDED.account_name,
    balance        = EXCLUDED.balance,
    free_margin    = EXCLUDED.free_margin,
    equity         = EXCLUDED.equity,
    leverage       = EXCLUDED.leverage,
    server         = EXCLUDED.server,
    no_swap        = EXCLUDED.no_swap,
    last_updated   = NOW();
");

            return RedirectToAction(nameof(EditTradingAccount));
        }
    }
}
