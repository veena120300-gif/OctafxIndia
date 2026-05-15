using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OctafxIndia.Data;
using OctafxIndia.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace OctafxIndia.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public DashboardController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: /Dashboard/AfterLogin
        [HttpGet]
        public async Task<IActionResult> AfterLoginAsync()
        {
            var viewModel = await _dbContext.TradingAccounts
                .OrderByDescending(acc => acc.LastUpdated)
                .Select(acc => new BalanceViewModel
                {
                    Balance = acc.Balance,
                    FreeMargin = acc.FreeMargin,
                    Equity = acc.Equity,
                    Leverage = acc.Leverage,
                    Server = acc.Server,
                    AccountNumber = acc.AccountNumber,
                    AccountType = acc.AccountName,
                    NoSwap = acc.NoSwap
                })
                .FirstOrDefaultAsync();

            if (viewModel == null)
            {
                // Return a view with a new (empty) view model if no trading account exists
                return View(new BalanceViewModel());
            }

            return View(viewModel);
        }
    }
}
