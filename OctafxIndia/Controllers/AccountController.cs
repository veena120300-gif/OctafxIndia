using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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

        private static readonly HashSet<string> AllowedEmails =
            new(StringComparer.OrdinalIgnoreCase)
        {
            "veenatarangini1226@gmail.com",
            "chandrarout0@gmail.com",
            "freedomoflife328@gmail.com",
            "sirishforexmatrix@gmail.com",
            "smartinvestments276@gmail.com",
            "jupiter3trader90@gmail.com"
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

        // =========================
        // GET LOGIN
        // =========================
        [HttpGet("Login")]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // =========================
        // POST LOGIN
        // =========================
        [HttpPost("Login")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> LoginFallback()
        {
            LoginStartModel? model = null;

            var contentType = Request.ContentType ?? string.Empty;

            var isJson =
                contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
                contentType.Contains("+json", StringComparison.OrdinalIgnoreCase);

            if (isJson)
            {
                try
                {
                    using var reader = new StreamReader(Request.Body);
                    var body = await reader.ReadToEndAsync();

                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        model = System.Text.Json.JsonSerializer.Deserialize<LoginStartModel>(
                            body,
                            new System.Text.Json.JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse login JSON body.");
                }
            }
            else if (Request.HasFormContentType)
            {
                try
                {
                    var email = Request.Form["Email"].FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        model = new LoginStartModel
                        {
                            Email = email
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse form login.");
                }
            }

            if (model == null || string.IsNullOrWhiteSpace(model.Email))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Email is required."
                });
            }

            return await LoginSimple(model);
        }

        // =========================
        // LOGIN LOGIC
        // =========================
        [HttpPost("LoginSimple")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> LoginSimple([FromBody] LoginStartModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Email))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Email is required."
                });
            }

            var email = model.Email.Trim();

            if (!AllowedEmails.Contains(email))
            {
                _logger.LogWarning("Unauthorized login attempt: {Email}", email);

                return Unauthorized(new
                {
                    success = false,
                    message = "User not allowed"
                });
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        FullName = email
                    };

                    var createResult = await _userManager.CreateAsync(user);

                    if (!createResult.Succeeded)
                    {
                        var errors = string.Join(
                            "; ",
                            createResult.Errors.Select(e => e.Description));

                        _logger.LogError(
                            "Failed creating user {Email}: {Errors}",
                            email,
                            errors);

                        return StatusCode(500, new
                        {
                            success = false,
                            message = "Failed to create user."
                        });
                    }
                }

                await _signInManager.SignInAsync(user, isPersistent: false);

                _logger.LogInformation("User logged in: {Email}", email);

                return Ok(new
                {
                    success = true,
                    redirectUrl = "/Home/AfterLogin"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for {Email}", email);

                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        // =========================
        // WHO AM I
        // =========================
        [HttpGet("WhoAmI")]
        public IActionResult WhoAmI()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                return Ok(new
                {
                    authenticated = true,
                    name = User.Identity.Name
                });
            }

            return Ok(new
            {
                authenticated = false
            });
        }

        // =========================
        // MODEL
        // =========================
        public class LoginStartModel
        {
            public string? Email { get; set; }
        }
    }
}
