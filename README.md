# HealthCare Appointment System

A role-based healthcare appointment booking and management system built with **ASP.NET Core MVC (.NET 8)**, **Entity Framework Core**, and **ASP.NET Core Identity**.

Built as a portfolio project targeting the .NET/C#/ASP.NET stack commonly required by healthcare-tech and enterprise software companies in Pakistan.

## Tech Stack
- **Backend:** ASP.NET Core MVC (.NET 8), C#
- **ORM:** Entity Framework Core (Code-First, SQL Server)
- **Auth:** ASP.NET Core Identity (cookie-based, role-based authorization)
- **Frontend:** Razor Views, plain CSS (no framework - deliberately, to keep focus on backend/architecture)

## Features
- Three roles: **Admin**, **Doctor**, **Patient**, each with different permissions
- Patients can book, view, and cancel their own appointments
- Doctors can view their schedule and update appointment status
- Admins manage doctors, specializations, and have full visibility across the system
- Seeded roles, a default Admin account, and sample specializations on first run

## Documentation
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — system design, layers, and key decisions
- [`docs/DATABASE_SCHEMA.md`](docs/DATABASE_SCHEMA.md) — entity relationship diagram and table structure
- [`docs/SETUP_GUIDE.md`](docs/SETUP_GUIDE.md) — full setup instructions, including a note on Visual Studio vs. VS Code-based editors (Antigravity, Cursor, etc.)

## Quick Start

```bash
cd HealthCareAppointmentSystem
dotnet restore
dotnet tool install --global dotnet-ef   # first time only
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run
```

See [`docs/SETUP_GUIDE.md`](docs/SETUP_GUIDE.md) for full details, including default login credentials.

## Project Structure

```
HealthCareAppointmentSystem/
├── docs/                          # Architecture & setup documentation
├── HealthCareAppointmentSystem.sln
└── HealthCareAppointmentSystem/   # The actual ASP.NET Core project
    ├── Controllers/               # MVC Controllers (Home, Appointments, Doctors, Patients)
    ├── Models/                    # EF Core entities (ApplicationUser, Doctor, Patient, Appointment, Specialization)
    ├── ViewModels/                # Form-shaping view models
    ├── Views/                     # Razor views
    ├── Data/                      # DbContext + seeding logic
    ├── wwwroot/                   # Static files (CSS)
    ├── Program.cs                 # App startup & DI configuration
    └── appsettings.json           # Configuration (connection strings, etc.)
```

## Status
This project has been written to correct, standard ASP.NET Core MVC patterns but has **not been compiled/run in this sandboxed generation environment** (no .NET runtime available there). Please build and run it locally per the setup guide before relying on it for a demo - see the guide's Troubleshooting section if you hit anything unexpected.
