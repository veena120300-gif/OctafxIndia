
using Microsoft.AspNetCore.Identity;

using Microsoft.AspNetCore.Mvc;

using Microsoft.Extensions.Logging;

using OctafxIndia.Data;

using OctafxIndia.Models;

using Org.BouncyCastle.Crypto.Macs;

using System;

using System.IO;

using System.Linq;

using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;



namespace OctafxIndia.Controllers

{

    [Route("Account")]

    public class AccountController : Controller

    {

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly SignInManager<ApplicationUser> _signInManager;

        private readonly ILogger<AccountController> _logger;



        // Keep this list in sync with your seeded users in Program.cs

        private static readonly string[] AllowedEmails = new[]

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



        // GET: /Account/Register

        [HttpGet("Register")]

        [AllowAnonymous]

        public IActionResult Register()

        {

            return View();

        }



        // POST: /Account/Register

        [HttpPost("Register")]

        [AllowAnonymous]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Register(RegisterViewModel model)

        {

            if (ModelState.IsValid)

            {

                var user = new ApplicationUser { UserName = model.Email, Email = model.Email, FullName = model.FullName, PhoneNumber = model.PhoneNumber };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)

                {

                    _logger.LogInformation("User created a new account with password.");



                    await _signInManager.SignInAsync(user, isPersistent: false);

                    return RedirectToAction("Index", "Home");

                }

                foreach (var error in result.Errors)

                {

                    ModelState.AddModelError(string.Empty, error.Description);

                }

            }



            // If we got this far, something failed, redisplay form

            return View(model);

        }



        // POST /Account/Login  <-- compatibility fallback

        [HttpPost("Login")]

        [IgnoreAntiforgeryToken] // optional: remove if you prefer antiforgery

        public async Task<IActionResult> LoginFallback()

        {

            LoginStartModel model = null;



            // Detect JSON content-type robustly (supports application/json and +json vendor types)

            var contentType = Request.ContentType ?? string.Empty;

            var looksLikeJson = contentType.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) >= 0

                                || contentType.IndexOf("+json", StringComparison.OrdinalIgnoreCase) >= 0;



            if (looksLikeJson)

            {

                try

                {

                    // read body (rewind not needed since we only read here)

                    using var sr = new StreamReader(Request.Body);

                    var body = await sr.ReadToEndAsync();

                    if (!string.IsNullOrWhiteSpace(body))

                    {

                        model = System.Text.Json.JsonSerializer.Deserialize<LoginStartModel>(body,

                            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    }

                }

                catch (Exception ex)

                {

                    _logger.LogWarning(ex, "LoginFallback: failed to parse JSON body");

                    model = null;

                }

            }



            // If not JSON, try form post

            if (model == null && Request.HasFormContentType)

            {

                try

                {

                    var email = Request.Form["Email"].FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(email))

                        model = new LoginStartModel { Email = email };

                }

                catch (Exception ex)

                {

                    _logger.LogWarning(ex, "LoginFallback: failed to read form data");

                }

            }



            if (model == null)

                return BadRequest(new { success = false, message = "Missing email" });



            // forward to existing LoginSimple action

            return await LoginSimple(model);

        }



        // POST /Account/LoginSimple

        // Body: JSON { "Email": "user@example.com" }

        [HttpPost("LoginSimple")]

        [IgnoreAntiforgeryToken] // optional: remove if you want antiforgery validation

        public async Task<IActionResult> LoginSimple([FromBody] LoginStartModel model)

        {

            if (model == null || string.IsNullOrWhiteSpace(model.Email))

                return BadRequest(new { success = false, message = "Email is required." });



            var email = model.Email.Trim().ToLowerInvariant();



            // Check allowlist

            if (!AllowedEmails.Contains(email, StringComparer.OrdinalIgnoreCase))

            {

                _logger.LogWarning("LoginSimple: attempted login by non-allowed email {Email}", email);

                return NotFound(new { success = false, message = "User not allowed" });

            }



            try

            {

                // Find or create the Identity user

                var user = await _userManager.FindByEmailAsync(email);

                if (user == null)

                {

                    user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };

                    var createRes = await _userManager.CreateAsync(user);

                    if (!createRes.Succeeded)

                    {

                        _logger.LogError("LoginSimple: failed creating user {Email}: {Errors}", email, string.Join("; ", createRes.Errors.Select(e => e.Description)));

                        return StatusCode(500, new { success = false, message = "Failed creating user" });

                    }

                }



                // Sign in (cookie)

                await _signInManager.SignInAsync(user, isPersistent: false);



                _logger.LogInformation("LoginSimple: user {Email} signed in", email);

                return Ok(new { success = true, redirectUrl = "/Home/AfterLogin" });

            }

            catch (Exception ex)

            {

                _logger.LogError(ex, "LoginSimple: unexpected error for {Email}", email);

                return StatusCode(500, new { success = false, message = "Internal server error" });

            }

        }



        // GET /Account/WhoAmI - quick check if authenticated

        [HttpGet("WhoAmI")]

        public IActionResult WhoAmI()

        {

            if (User?.Identity?.IsAuthenticated == true)

                return Ok(new { authenticated = true, name = User.Identity.Name });

            return Ok(new { authenticated = false });

        }



        public class LoginStartModel { public string Email { get; set; } }

    }

}

