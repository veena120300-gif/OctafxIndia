using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OctafxIndia.Data;
using Microsoft.Extensions.Logging;
using OctafxIndia.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------------- Configuration (connection string) ----------------
// Priority: appsettings -> DB_CONNECTION env var -> local fallback
var conn = Environment.GetEnvironmentVariable("DB_CONNECTION");

// ---------------- Services ----------------
builder.Services.AddDbContext<ApplicationDbContext>(opts =>
    opts.UseNpgsql(conn));

//var conn = builder.Configuration.GetConnectionString("DefaultConnection")
//           ?? Environment.GetEnvironmentVariable("DB_CONNECTION")
//           ?? "Host=127.0.0.1;Port=5432;Database=octafxdb;Username=dbuser;Password=Str0ngDBPass!";

//builder.Services.AddDbContext<ApplicationDbContext>(opts =>
//{
//    opts.UseInMemoryDatabase("LocalTestDb");
//});

/*
 Use AddIdentity when you need roles. Do NOT call AddDefaultIdentity and AddIdentity together.
 */
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = false;
    options.User.RequireUniqueEmail = true;

    // lockout options
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(opts =>
{
    opts.Cookie.HttpOnly = true;
    opts.Cookie.SameSite = SameSiteMode.Lax;
    // Cloud Run provides HTTPS; require secure cookies in production
    opts.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    //opts.LoginPath = "/Identity/Account/Login";
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
// ---------------- Bind Kestrel to PORT from environment (Cloud Run sets PORT)
var portEnv = Environment.GetEnvironmentVariable("PORT") ?? "8080";
if (!int.TryParse(portEnv, out var portNumeric)) portNumeric = 8080;

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(portNumeric);
});
builder.WebHost.UseUrls($"http://0.0.0.0:{portNumeric}");

// ---------------- Build app ----------------
var app = builder.Build();

// forwarded headers for Cloud Run (X-Forwarded-For / X-Forwarded-Proto)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ---------------- Migrate & Seed (wrapped so startup won't crash) ----------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        // Apply any pending migrations
        logger.LogInformation("Attempting to apply migrations...");
        db.Database.Migrate();
        logger.LogInformation("Migrations applied.");

        // Run seeding (idempotent)
        await SeedData.InitializeAsync(services, logger);
        logger.LogInformation("Seeding complete.");
    }
    catch (Exception ex)
    {
        // IMPORTANT: we log the error and continue so container can bind the port.
        // This prevents Cloud Run health-check failures while you debug DB connectivity.
        var logger2 = services.GetRequiredService<ILogger<Program>>();
        logger2.LogError(ex, "Database migrate/seed failed during startup. Continuing so container can start for debugging.");
    }
}

app.Run();


// ---------------- SeedData helper ----------------
public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services, ILogger logger)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var db = services.GetRequiredService<ApplicationDbContext>();

        // Ensure Admin role exists
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            var r = await roleManager.CreateAsync(new IdentityRole("Admin"));
            if (!r.Succeeded) logger.LogWarning("Could not create Admin role: {Errors}", string.Join(',', r.Errors.Select(e => e.Description)));
        }

        // Four allowed users (change emails & passwords before production)
        var users = new[]
        {
            new { Email="user1@example.com", Password="UserPass123!" },
            new { Email="user2@example.com", Password="UserPass123!" },
            new { Email="user3@example.com", Password="UserPass123!" },
            new { Email="user4@example.com", Password="UserPass123!" }
        };

        foreach (var u in users)
        {
            var existing = await userManager.FindByEmailAsync(u.Email);
            if (existing == null)
            {
                var user = new ApplicationUser { UserName = u.Email, Email = u.Email, EmailConfirmed = true };
                var res = await userManager.CreateAsync(user, u.Password);
                if (!res.Succeeded) logger.LogWarning("Failed creating user {Email}: {Errors}", u.Email, string.Join(',', res.Errors.Select(e => e.Description)));
            }
        }

        // Admin account (read from env vars for production)
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@example.com";
        var adminPwd = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "AdminPass123!";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var res = await userManager.CreateAsync(adminUser, adminPwd);
            if (!res.Succeeded) logger.LogWarning("Failed creating admin {Email}: {Errors}", adminEmail, string.Join(',', res.Errors.Select(e => e.Description)));
        }

        // Ensure admin role is assigned
        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            var r = await userManager.AddToRoleAsync(adminUser, "Admin");
            if (!r.Succeeded) logger.LogWarning("Failed adding admin to role: {Errors}", string.Join(',', r.Errors.Select(e => e.Description)));
        }

        // Seed minimal site data table if exists in DbContext
        try
        {
            if (db.SiteData != null && !db.SiteData.Any())
            {
                db.SiteData.AddRange(
                    new SiteData { Key = "HeroTitle", Value = "Welcome - edit via admin" },
                    new SiteData { Key = "Footer", Value = "© OctafxIndia" }
                );
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SiteData seeding skipped or failed.");
        }
    }
}
