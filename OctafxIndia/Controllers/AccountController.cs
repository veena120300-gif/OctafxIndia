using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OctafxIndia.Data;
using OctafxIndia.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OctafxIndia.Controllers
{
    [Route("Account")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        // Using a HashSet for efficient, case-insensitive email lookups.
        private static readonly HashSet<string> AllowedEmails = new(StringComparer.OrdinalIgnoreCase)
        {
            "veenatarangini1226@gmail.com",
            "chandrarout0@gmail.com",
            "freedomoflife328@gmail.com",
            "sirishforexmatrix@gmail.com",
            "smartinvestments276@gmail.com",
            "jupiter3trader90@gmail.com",
        };

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpGet("Register")]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost("Register")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.Email != null && model.Password != null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        FullName = model.FullName,
                        PhoneNumber = model.PhoneNumber
                    };

                    try
                    {
                        var result = await _userManager.CreateAsync(user, model.Password);

                        if (result.Succeeded)
                        {
                            _logger.LogInformation("User created a new account with password.");
                            await _signInManager.SignInAsync(user, isPersistent: false);
                            TempData["SuccessMessage"] = "Registration successful!";
                            return RedirectToAction("Index", "Home");
                        }

                        foreach (var error in result.Errors)
                        {
                            _logger.LogWarning("Registration failed for user {Email}. Error: {Error}", model.Email, error.Description);
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An unexpected error occurred during registration for user {Email}.", model.Email);
                        ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Email and password are required.");
                }
            }

            return View(model);
        }

        // POST /Account/Login - Compatibility fallback for different content types
        [HttpPost("Login")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> LoginFallback()
        {
            LoginStartModel? model = null;
            var contentType = Request.ContentType ?? string.Empty;

            // Robustly detect JSON content-type
            var isJson = contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
                         contentType.Contains("+json", StringComparison.OrdinalIgnoreCase);

            if (isJson)
            {
                try
                {
                    using var reader = new StreamReader(Request.Body);
                    var body = await reader.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        model = System.Text.Json.JsonSerializer.Deserialize<LoginStartModel>(body,
                            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "LoginFallback: Failed to parse JSON body.");
                }
            }
            else if (Request.HasFormContentType)
            {
                try
                {
                    var email = Request.Form["Email"].FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        model = new LoginStartModel { Email = email };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "LoginFallback: Failed to read form data.");
                }
            }

            if (model == null)
            {
                return BadRequest(new { success = false, message = "Missing email" });
            }

            return await LoginSimple(model);
        }

        // POST /Account/LoginSimple
        [HttpPost("LoginSimple")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> LoginSimple([FromBody] LoginStartModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Email))
            {
                return BadRequest(new { success = false, message = "Email is required." });
            }

            var email = model.Email.Trim();

            if (!AllowedEmails.Contains(email))
            {
                _logger.LogWarning("LoginSimple: Attempted login by non-allowed email {Email}", email);
                return NotFound(new { success = false, message = "User not allowed" });
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                        _logger.LogError("LoginSimple: Failed creating user {Email}: {Errors}", email, errors);
                        return StatusCode(500, new { success = false, message = "Failed to create user." });
                    }
                }

                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation("LoginSimple: User {Email} signed in.", email);
                return Ok(new { success = true, redirectUrl = "/Home/AfterLogin" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoginSimple: Unexpected error for {Email}", email);
                return StatusCode(500, new { success = false, message = "An internal server error occurred." });
            }
        }

        // GET /Account/WhoAmI - Quick check for authentication status
        [HttpGet("WhoAmI")]
        public IActionResult WhoAmI()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                return Ok(new { authenticated = true, name = User.Identity.Name });
            }
            return Ok(new { authenticated = false });
        }

        public class LoginStartModel
        {
            public string? Email { get; set; }
        }
    }
}
