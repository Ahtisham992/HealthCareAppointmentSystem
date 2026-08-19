using HealthCareAppointmentSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HealthCareAppointmentSystem.Data
{
    /// <summary>
    /// Seeds roles, a default Admin account, and a few Specializations
    /// so the app isn't empty on first run. Runs once at startup (idempotent).
    /// </summary>
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // 1. Seed roles
            string[] roles = { "Admin", "Doctor", "Patient" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed default Admin account
            const string adminEmail = "admin@healthcare.local";
            const string adminPassword = "Admin@123";

            if (await userManager.FindByEmailAsync(adminEmail) is null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // 3. Seed Specializations
            if (!await context.Specializations.AnyAsync())
            {
                context.Specializations.AddRange(
                    new Specialization { Name = "Cardiology" },
                    new Specialization { Name = "Dermatology" },
                    new Specialization { Name = "Pediatrics" },
                    new Specialization { Name = "General Medicine" },
                    new Specialization { Name = "Orthopedics" }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}
