using HealthCareAppointmentSystem.Data;
using HealthCareAppointmentSystem.Models;
using HealthCareAppointmentSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---

// EF Core + SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ASP.NET Core Identity with roles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Reasonable defaults for a demo/portfolio project
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 6;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // required for Identity's default UI
builder.Services.AddHttpClient();

// Register background service for auto-canceling unpaid appointments
builder.Services.AddScoped<HealthCareAppointmentSystem.Services.IStripePaymentService, HealthCareAppointmentSystem.Services.StripePaymentService>();
builder.Services.AddHostedService<PaymentTimeoutService>();

var app = builder.Build();

// --- Middleware pipeline ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// --- Seed roles, admin user, and lookup data on startup ---
using (var scope = app.Services.CreateScope())
{
    await DbInitializer.SeedAsync(scope.ServiceProvider);
}

app.Run();
