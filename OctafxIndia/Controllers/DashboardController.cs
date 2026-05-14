using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OctafxIndia.Data;
using OctafxIndia.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using OctafxIndia.Data;
using OctafxIndia.ViewModels;

namespace OctafxIndia.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> AfterLogin()
        {
            // Demo: pick latest account. Replace with user-specific filter in real app.
            var acc = await _db.TradingAccounts.OrderByDescending(a => a.LastUpdated).FirstOrDefaultAsync();
            if (acc == null)
            {
                return View(new BalanceViewModel());
            }

            var vm = new BalanceViewModel
            {
                Balance = acc.Balance,
                FreeMargin = acc.FreeMargin,
                Equity = acc.Equity,
                Leverage = acc.Leverage,
                Server = acc.Server,
                AccountNumber = acc.AccountNumber,
                AccountType = acc.AccountName,
                NoSwap = acc.NoSwap
            };

            return View(vm);
        }
    }
}
