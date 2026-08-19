# Architecture Document — HealthCare Appointment System

## 1. Overview

A healthcare appointment booking and management system built with **ASP.NET Core MVC (.NET 8)**, using **Entity Framework Core** for data access and **ASP.NET Core Identity** for authentication and role-based authorization.

The system supports three roles:
- **Admin** — manages doctors, specializations, and has full visibility over all appointments.
- **Doctor** — views their own schedule and manages appointment status (confirm/complete/cancel).
- **Patient** — books appointments, views their own appointment history.

## 2. Architectural Pattern

The project follows the standard **ASP.NET Core MVC** pattern (Model-View-Controller), with a light **Service/Repository-free** approach for simplicity (Controllers talk to `ApplicationDbContext` directly via EF Core). This is intentional for a learning/portfolio project — in a larger production system, you would typically introduce a **Repository + Unit of Work pattern** or a **Service layer** to decouple controllers from EF Core directly. That's a natural "next step" to mention in an interview if asked how you'd scale this.

```
┌─────────────┐      ┌──────────────┐      ┌───────────────────┐      ┌─────────────┐
│   Browser   │ ───> │  Controllers │ ───> │ ApplicationDbContext│ ───> │ SQL Server  │
│  (Views/    │ <─── │   (MVC)      │ <─── │      (EF Core)     │ <─── │  Database   │
│   Razor)    │      └──────────────┘      └───────────────────┘      └─────────────┘
└─────────────┘             │
                             ▼
                      ┌──────────────┐
                      │ ASP.NET Core │
                      │   Identity   │
                      │ (Auth/Roles) │
                      └──────────────┘
```

## 3. Layers

### 3.1 Models (`/Models`)
Plain C# classes representing the domain entities, decorated with Data Annotations for validation (`[Required]`, `[StringLength]`, etc.) and EF Core relationship attributes.

- `ApplicationUser` — extends `IdentityUser`, adds `FullName`. This is the base identity account for every logged-in user (Admin, Doctor, or Patient).
- `Doctor` — profile data for a doctor (linked 1:1 to an `ApplicationUser`), includes `SpecializationId`.
- `Patient` — profile data for a patient (linked 1:1 to an `ApplicationUser`).
- `Specialization` — lookup table (Cardiology, Dermatology, etc.)
- `Appointment` — the core booking entity: links a `Doctor`, a `Patient`, a date/time, a status enum, and notes.

### 3.2 Data (`/Data`)
- `ApplicationDbContext` — extends `IdentityDbContext<ApplicationUser>`, defines `DbSet<>` properties for each entity, and configures relationships (Fluent API) in `OnModelCreating`.
- `DbInitializer` — seeds the database on startup: creates the Admin/Doctor/Patient roles, creates one default Admin user, and seeds a few Specializations so the app isn't empty on first run.

### 3.3 Controllers (`/Controllers`)
Each controller is annotated with `[Authorize]` at the appropriate level:

- `HomeController` — public landing page.
- `AppointmentsController` — full CRUD for appointments, with role-aware logic (a Patient only sees their own appointments; a Doctor only sees theirs; Admin sees all).
- `DoctorsController` — Admin-only CRUD for managing doctor profiles.
- `PatientsController` — Admin-only view of patient list; patients manage their own profile separately (not included in this scope, noted as a future extension).

### 3.4 Views (`/Views`)
Razor views using the standard MVC scaffolded structure — Index (list), Create, Edit, Details, Delete per controller, sharing a common `_Layout.cshtml`.

### 3.5 Authentication & Authorization
Uses **ASP.NET Core Identity** with cookie-based authentication. Role-based authorization (`[Authorize(Roles = "Admin")]`) gates access to sensitive actions. Identity's default scaffolded UI (`/Identity/Account/Login`, `/Identity/Account/Register`) handles login/registration.

## 4. Key Design Decisions (talking points for interviews)

1. **Why EF Core Code-First?** Lets the database schema live in source control as C# code (migrations), making it easy to evolve and review changes — the same instinct behind using Prisma migrations in your Node.js projects.
2. **Why role-based authorization instead of claims-based?** For a project this size, three fixed roles (Admin/Doctor/Patient) are simpler to reason about than a full claims/policy system. In a larger system with more granular permissions, ASP.NET Core's **Policy-based authorization** would be the next step.
3. **Why no separate Service layer yet?** Kept the project scoped to be genuinely finishable and understandable end-to-end. The natural evolution (and a great "what would you improve" interview answer) is introducing an `IAppointmentService` interface between controllers and the DbContext, to make business logic testable independent of EF Core.
4. **1:1 relationship between ApplicationUser and Doctor/Patient**: Keeps authentication concerns (handled by Identity) separate from domain profile data (Doctor/Patient specific fields), following the Single Responsibility Principle.

## 5. Possible Future Extensions
- Email notifications on appointment booking/cancellation (using `IEmailSender`, already an Identity interface).
- A Web API layer (ASP.NET Core Web API) alongside the MVC app, so a future mobile/React frontend could consume the same backend.
- Appointment conflict validation (prevent double-booking the same doctor at the same time).
- Audit logging for appointment status changes.
