# HealthCare Appointment System

An enterprise-grade, role-based healthcare appointment booking and management system built with **ASP.NET Core MVC (.NET 8)**, **Entity Framework Core**, and **ASP.NET Core Identity**.

This project has been carefully architected and styled to serve as a comprehensive portfolio piece, showcasing backend .NET/C# proficiency alongside a premium, modern "SaaS-style" corporate frontend.

## Key Features

- **Three Distinct Roles:** 
  - **Admin**: Complete system visibility, doctor/patient management, and access to a centralized dashboard featuring statistical charts and real-time audit logs.
  - **Doctor**: Manages availability, handles patient appointments, processes cancellations, and tracks post-visit reviews.
  - **Patient**: Books appointments, tracks history, requests cancellations, and leaves reviews for doctors.
- **Two-Step Cancellation Workflow:** A robust state machine ensuring that cancellations initiated by either party (Patient or Doctor) must be confirmed by the other party to prevent scheduling conflicts.
- **Audit Logging System:** All critical database actions (creating appointments, updating statuses, profile changes) are intercepted and logged for compliance monitoring on the Admin Dashboard.
- **Review & Rating Subsystem:** Patients can rate (1-5 stars) and review doctors after an appointment is completed.
- **Premium Corporate UI:** A completely custom-built, highly polished frontend using modern CSS grids, flexbox, and FontAwesome. Features a massive, detailed footer, legal pages, scrollable data tables, and an "AI Dashboard Mockup" hero section without the bloat of a JavaScript SPA framework.

## Tech Stack
- **Backend:** ASP.NET Core MVC (.NET 8), C#
- **Database & ORM:** Entity Framework Core (Code-First), SQL Server (Dockerized)
- **Auth:** ASP.NET Core Identity (Cookie-based, Role-based authorization)
- **Frontend:** Razor Views, custom vanilla CSS, FontAwesome

## Documentation
For an in-depth understanding of the system, please refer to the dedicated documentation files:
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — System design, layers, robust workflows, and key technical decisions.
- [`docs/DATABASE_SCHEMA.md`](docs/DATABASE_SCHEMA.md) — Entity relationship diagram, table structures, and relationships.
- [`docs/SETUP_GUIDE.md`](docs/SETUP_GUIDE.md) — Full, step-by-step setup instructions explicitly tailored for Docker and modern IDEs (like Antigravity / VS Code).

## Quick Start

Ensure you have .NET 8 and Docker installed.

```bash
# 1. Start the ephemeral SQL Server container
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong!Passw0rd" -p 1433:1433 --name hc_db -d mcr.microsoft.com/mssql/server:2022-latest

# 2. Enter the project directory
cd HealthCareAppointmentSystem

# 3. Restore dependencies
dotnet restore

# 4. Install EF Core tools & apply schema
dotnet tool install --global dotnet-ef
dotnet ef database update

# 5. Run the application
dotnet run
```

On first run, the database is automatically seeded with essential specializations, roles, and a default Admin account (`admin@healthcare.local` / `Admin@123`).

See the [`docs/SETUP_GUIDE.md`](docs/SETUP_GUIDE.md) for full details, troubleshooting, and recommended editor configurations.
