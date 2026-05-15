using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OctafxIndia.Data;
using OctafxIndia.Models;
using OctafxIndia.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OctafxIndia.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, ILogger<HomeController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _logger = logger;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> AfterLoginAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var tradingAccount = await _dbContext.TradingAccounts
                                           .OrderByDescending(acc => acc.LastUpdated)
                                           .FirstOrDefaultAsync();

            var viewModel = new BalanceViewModel
            {
                Balance = tradingAccount?.Balance ?? 0m,
                FreeMargin = tradingAccount?.FreeMargin ?? 0m,
                Equity = tradingAccount?.Equity ?? 0m,
                Leverage = tradingAccount?.Leverage ?? "1:1000",
                Server = tradingAccount?.Server ?? "OctaFX-Real7",
                AccountNumber = tradingAccount?.AccountNumber ?? "N/A",
                AccountType = tradingAccount?.AccountName ?? "REAL",
                NoSwap = tradingAccount?.NoSwap ?? true,
                Email = user?.Email,
                PhoneNumber = user?.PhoneNumber,
                FullName = user?.FullName,
                BirthDate = user?.BirthDate,
                Country = user?.Country,
                VerificationStatus = user?.VerificationStatus,
                Nickname = user?.Nickname
            };

            viewModel.Accounts = await _dbContext.UserTradingAccounts
                .OrderBy(acc => acc.Id)
                .Select(acc => new TradingAccountItemViewModel
                {
                    AccountNumber = acc.AccountNumber,
                    AccountType = acc.AccountType ?? "REAL",
                    Server = acc.Server,
                    Balance = acc.Balance,
                    Equity = acc.Equity,
                    Status = acc.Status
                })
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfileAsync(
            string email,
            string phoneNumber,
            string fullName,
            DateTime? birthDate,
            string country,
            string nickname,
            string verificationStatus)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                if (!string.IsNullOrWhiteSpace(email) && !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    var emailResult = await _userManager.SetEmailAsync(user, email);
                    if (!emailResult.Succeeded) return RedirectToAfterLoginWithError(emailResult);

                    var userNameResult = await _userManager.SetUserNameAsync(user, email);
                    if (!userNameResult.Succeeded) return RedirectToAfterLoginWithError(userNameResult);
                }

                user.PhoneNumber = phoneNumber;
                user.FullName = fullName;
                user.Country = country;
                user.Nickname = nickname;

                if (birthDate.HasValue)
                {
                    // Normalize the BirthDate to ensure consistency across the application
                    user.BirthDate = DateTime.SpecifyKind(birthDate.Value.Date, DateTimeKind.Utc);
                }
                else
                {
                    user.BirthDate = null;
                }

                if (!string.IsNullOrWhiteSpace(verificationStatus))
                {
                    user.VerificationStatus = verificationStatus;
                }

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    return RedirectToAfterLoginWithError(updateResult);
                }

                TempData["ProfileMessage"] = "Your personal information has been successfully updated.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while updating the profile.");
                TempData["ProfileError"] = "An unexpected error occurred while saving your profile.";
            }

            return RedirectToAction(nameof(AfterLoginAsync));
        }

        private IActionResult RedirectToAfterLoginWithError(IdentityResult result)
        {
            TempData["ProfileError"] = string.Join("; ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(AfterLoginAsync));
        }
    }
}
