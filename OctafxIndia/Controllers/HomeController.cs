using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OctafxIndia.Data;
// 👇 adjust this if your ApplicationUser is in a different namespace
using OctafxIndia.Models;
using OctafxIndia.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OctafxIndia.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        // SINGLE constructor: inject both DbContext and UserManager
        public HomeController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> AfterLogin()
        {
            var user = await _userManager.GetUserAsync(User);
            // 1) Load the balance row (latest) for the balance box
            var acc = await _db.TradingAccounts
                               .OrderByDescending(a => a.LastUpdated)
                               .FirstOrDefaultAsync();

            // Build viewmodel with safe defaults if balance row is missing
            var vm = new BalanceViewModel
            {
                Balance = acc?.Balance ?? 0m,
                FreeMargin = acc?.FreeMargin ?? 0m,
                Equity = acc?.Equity ?? 0m,
                Leverage = acc?.Leverage ?? "1:1000",
                Server = acc?.Server ?? "OctaFX-Real7",
                AccountNumber = acc?.AccountNumber ?? "N/A",
                AccountType = acc?.AccountName ?? "REAL",
                NoSwap = acc?.NoSwap ?? true,
                // 👇 map user info (null-safe)
                Email = user?.Email,
                PhoneNumber = user?.PhoneNumber,
                FullName = user?.FullName,
                BirthDate = user?.BirthDate,
                Country = user?.Country,
                VerificationStatus = user?.VerificationStatus,
                Nickname = user?.Nickname
            };

            // 2) Load ALL trading accounts (global list) and map to the viewmodel.
            //    NOTE: this intentionally does NOT filter by user - everyone sees these rows.
            var allAccounts = await _db.UserTradingAccounts
                                       .OrderBy(a => a.Id)
                                       .ToListAsync();

            vm.Accounts = allAccounts.Select(a => new TradingAccountItemViewModel
            {
                AccountNumber = a.AccountNumber,
                AccountType = a.AccountType ?? "REAL",
                Server = a.Server,
                Balance = a.Balance,
                Equity = a.Equity,
                Status = a.Status
            }).ToList();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
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
                    return Challenge();

                // --- Email / username ---
                if (!string.IsNullOrWhiteSpace(email) &&
                    !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    var emailResult = await _userManager.SetEmailAsync(user, email);
                    if (!emailResult.Succeeded)
                    {
                        TempData["ProfileError"] =
                            string.Join("; ", emailResult.Errors.Select(e => e.Description));
                        return RedirectToAction("AfterLogin", "Home");
                    }

                    var userNameResult = await _userManager.SetUserNameAsync(user, email);
                    if (!userNameResult.Succeeded)
                    {
                        TempData["ProfileError"] =
                            string.Join("; ", userNameResult.Errors.Select(e => e.Description));
                        return RedirectToAction("AfterLogin", "Home");
                    }
                }

                // --- Other fields ---
                user.PhoneNumber = phoneNumber;
                user.FullName = fullName;

                // 🔥 IMPORTANT: normalize BirthDate to UTC (or null)
                if (birthDate.HasValue)
                {
                    // If this is just a date (no time), keep date part and mark as UTC
                    var d = birthDate.Value.Date;
                    user.BirthDate = DateTime.SpecifyKind(d, DateTimeKind.Utc);
                }
                else
                {
                    user.BirthDate = null;
                }

                user.Country = country;
                user.Nickname = nickname;

                if (!string.IsNullOrWhiteSpace(verificationStatus))
                    user.VerificationStatus = verificationStatus;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    TempData["ProfileError"] =
                        string.Join("; ", result.Errors.Select(e => e.Description));
                    return RedirectToAction("AfterLogin", "Home");
                }

                TempData["ProfileMessage"] = "Your personal information has been updated.";
                return RedirectToAction("AfterLogin", "Home");
            }
            catch (Exception ex)
            {
                // optional: log ex
                TempData["ProfileError"] = "Unexpected error while saving profile.";
                return RedirectToAction("AfterLogin", "Home");
            }
        }

    }
}
