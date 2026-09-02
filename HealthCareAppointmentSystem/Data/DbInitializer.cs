using HealthCareAppointmentSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HealthCareAppointmentSystem.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // 1. Seed roles
            string[] roleNames = { "Admin", "Doctor", "Patient", "Receptionist", "Pharmacist", "Accountant", "LabTechnician" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // --- 2. Create Default Admin ---
            var adminEmail = "admin@healthcare.local";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                var newAdmin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };

                var createPowerUser = await userManager.CreateAsync(newAdmin, "Admin@123");
                if (createPowerUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
            
            // --- 3. Create Default Accountant ---
            var accountantEmail = "accountant@healthcare.local";
            var accountantUser = await userManager.FindByEmailAsync(accountantEmail);
            if (accountantUser == null)
            {
                var newAccountant = new ApplicationUser
                {
                    UserName = accountantEmail,
                    Email = accountantEmail,
                    FullName = "Platform Accountant",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(newAccountant, "Accountant@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAccountant, "Accountant");
                }
            }

            // Helper for users
            async Task<ApplicationUser?> CreateUser(string email, string name, string role, string password)
            {
                if (await userManager.FindByEmailAsync(email) == null)
                {
                    var u = new ApplicationUser { UserName = email, Email = email, FullName = name, EmailConfirmed = true };
                    var res = await userManager.CreateAsync(u, password);
                    if (res.Succeeded)
                    {
                        await userManager.AddToRoleAsync(u, role);
                        return u;
                    }
                }
                return await userManager.FindByEmailAsync(email);
            }

            // 2. Admins and Receptionists
            await CreateUser("admin@healthcare.local", "System Administrator", "Admin", "Admin@123");
            await CreateUser("admin2@healthcare.local", "System Administrator 2", "Admin", "Admin2@123");
            
            await CreateUser("receptionist1@healthcare.local", "Receptionist 1", "Receptionist", "Receptionist1@123");
            await CreateUser("receptionist2@healthcare.local", "Receptionist 2", "Receptionist", "Receptionist2@123");
            await CreateUser("receptionist3@healthcare.local", "Receptionist 3", "Receptionist", "Receptionist3@123");

            var pharmUser = await CreateUser("pharmacist1@healthcare.local", "Pharmacist 1", "Pharmacist", "Pharmacist1@123");
            if (pharmUser != null && !await context.Pharmacists.AnyAsync(p => p.ApplicationUserId == pharmUser.Id))
            {
                context.Pharmacists.Add(new Pharmacist
                {
                    ApplicationUserId = pharmUser.Id,
                    IsActive = true
                });
                await context.SaveChangesAsync();
            }

            var labTechUser = await CreateUser("labtech1@healthcare.local", "Lab Technician 1", "LabTechnician", "LabTech1@123");
            if (labTechUser != null && !await context.LabTechnicians.AnyAsync(p => p.ApplicationUserId == labTechUser.Id))
            {
                context.LabTechnicians.Add(new LabTechnician
                {
                    ApplicationUserId = labTechUser.Id,
                    CertificationNumber = "LT-998877"
                });
                await context.SaveChangesAsync();
            }
            // 3. Specializations
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

            var specs = await context.Specializations.ToListAsync();

            // 4. Doctors
            for (int i = 1; i <= 20; i++)
            {
                var docUser = await CreateUser($"doctor{i}@healthcare.local", $"Doctor {i}", "Doctor", $"Doctor{i}@123");
                if (docUser != null && !await context.Doctors.AnyAsync(d => d.ApplicationUserId == docUser.Id))
                {
                    context.Doctors.Add(new Doctor
                    {
                        ApplicationUserId = docUser.Id,
                        SpecializationId = specs[i % specs.Count].Id,
                        LicenseNumber = $"LIC-2026-00{i}",
                        YearsOfExperience = 5 + i,
                        ConsultationFee = 1000 + (i * 200),
                        SlotDurationMinutes = 20,
                        IsApproved = true
                    });
                }
            }
            await context.SaveChangesAsync();

            // 5. Patients
            for (int i = 1; i <= 50; i++)
            {
                var patUser = await CreateUser($"patient{i}@gmail.com", $"Patient {i}", "Patient", $"Patient{i}@123");
                if (patUser != null && !await context.Patients.AnyAsync(p => p.ApplicationUserId == patUser.Id))
                {
                    context.Patients.Add(new Patient
                    {
                        ApplicationUserId = patUser.Id,
                        CNIC = $"35202-12345{i:D2}-1",
                        DateOfBirth = new DateTime(1990, 1, 1).AddDays(i * 100)
                    });
                }
            }
            await context.SaveChangesAsync();

            // 6. Appointments & Invoices
            var doctors = await context.Doctors.ToListAsync();
            var patients = await context.Patients.ToListAsync();

            if (await context.Appointments.CountAsync() < 100 && doctors.Count > 0 && patients.Count > 0)
            {
                for (int i = 0; i < 100; i++)
                {
                    var doc = doctors[i % doctors.Count];
                    var pat = patients[i % patients.Count];
                    var aptTime = DateTime.Today.AddDays(i % 5).AddHours(9 + (i % 6));

                    var apt = new Appointment
                    {
                        DoctorId = doc.Id,
                        PatientId = pat.Id,
                        AppointmentDateTime = aptTime,
                        Status = aptTime < DateTime.Now ? AppointmentStatus.Completed : AppointmentStatus.Confirmed,
                        CreatedAt = DateTime.Now.AddDays(-5),
                        Notes = $"Routine checkup #{i}"
                    };
                    context.Appointments.Add(apt);
                    await context.SaveChangesAsync();

                    // Invoice
                    if (apt.Status == AppointmentStatus.Completed || apt.Status == AppointmentStatus.Confirmed)
                    {
                        var inv = new Invoice
                        {
                            AppointmentId = apt.Id,
                            Amount = doc.ConsultationFee,
                            Status = aptTime < DateTime.Now ? PaymentStatus.Paid : PaymentStatus.Pending,
                            IssuedAt = DateTime.Now.AddDays(-5)
                        };
                        if (inv.Status == PaymentStatus.Paid)
                        {
                            inv.PaidAt = DateTime.Now.AddDays(-2);
                            inv.PaymentMethod = "Credit Card";
                        }
                        context.Invoices.Add(inv);
                    }
                }
                await context.SaveChangesAsync();
            }

            // 7. Wallets
            // Create Platform Escrow Wallet
            if (!await context.Wallets.AnyAsync(w => w.ApplicationUserId == null))
            {
                context.Wallets.Add(new Wallet { Balance = 0, Currency = "PKR" });
            }
            
            // Create Wallet for every user
            var allUsers = await userManager.Users.ToListAsync();
            foreach (var u in allUsers)
            {
                if (!await context.Wallets.AnyAsync(w => w.ApplicationUserId == u.Id))
                {
                    // Give patients a starting balance of 5000 for dummy testing
                    decimal startingBalance = await userManager.IsInRoleAsync(u, "Patient") ? 5000m : 0m;
                    context.Wallets.Add(new Wallet
                    {
                        ApplicationUserId = u.Id,
                        Balance = startingBalance,
                        Currency = "PKR"
                    });
                }
            }
            await context.SaveChangesAsync();
        }
    }
}
