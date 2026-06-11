using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OctafxIndia.Data;
using OctafxIndia.Models;
using System;

var builder = WebApplication.CreateBuilder(args);

// --- Configure Services ---

// 1. Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DB_CONNECTION");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string is not configured.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = false;
    options.User.RequireUniqueEmail = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options => 
{ 
    // IMPORTANT
    options.LoginPath = "/"; 
    options.AccessDeniedPath = "/"; 
    options.Cookie.Name = "OctaAuth"; 
    options.Cookie.HttpOnly = true; 
    options.Cookie.SameSite = SameSiteMode.Lax; 
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; 
    options.ExpireTimeSpan = TimeSpan.FromDays(7); 
    options.SlidingExpiration = true; });

// 3. MVC and Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// 4. Kestrel Configuration
var portEnv = Environment.GetEnvironmentVariable("PORT") ?? "8080";
if (int.TryParse(portEnv, out var port))
{
    builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(port));
}

// --- Build Application ---

var app = builder.Build();

// --- Test and Log Database Connection ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        if (await dbContext.Database.CanConnectAsync())
        {
            logger.LogInformation("Database connection successful.");
        }
        else
        {
            logger.LogError("Database connection failed. Please check the connection string and database server status.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while trying to connect to the database.");
    }
}

// --- Configure Middleware ---

// Forwarded Headers Middleware for Cloud Environments
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Environment-specific configurations
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    //app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// --- Map Endpoints ---

app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// --- Database Migration and Seeding ---

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        logger.LogInformation("Applying database migrations...");
        dbContext.Database.Migrate();
        logger.LogInformation("Migrations applied successfully.");

        logger.LogInformation("Seeding database...");
        await SeedData.InitializeAsync(services, logger);
        logger.LogInformation("Seeding completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database migration or seeding. The application will continue to run.");
    }
}

// --- Run Application ---

app.Run();
