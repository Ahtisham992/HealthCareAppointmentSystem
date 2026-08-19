# Architecture Document — HealthCare Appointment System

## 1. Overview

A comprehensive, enterprise-grade healthcare appointment booking and management system built with **ASP.NET Core MVC (.NET 8)**, using **Entity Framework Core** for data access and **ASP.NET Core Identity** for authentication and robust role-based authorization.

The system supports three distinct roles with isolated workflows:
- **Admin** — Manages doctors, specializations, system-wide audit logs, and has full visibility over the platform via a centralized dashboard with statistical charts.
- **Doctor** — Manages their daily schedule, processes appointment statuses, handles patient cancellation requests, and tracks their patient reviews.
- **Patient** — Books appointments, requests cancellations, tracks appointment history, and submits post-appointment reviews for doctors.

## 2. Architectural Pattern

The project follows the **ASP.NET Core MVC** pattern (Model-View-Controller) utilizing a direct-to-DbContext architecture, suitable for rapid iteration and clear data flow. 

```text
┌─────────────┐      ┌──────────────┐      ┌───────────────────┐      ┌─────────────┐
│   Browser   │ ───> │  Controllers │ ───> │ ApplicationDbContext│ ───> │ SQL Server  │
│  (Views/    │ <─── │   (MVC)      │ <─── │      (EF Core)     │ <─── │  (Docker)   │
│   Razor)    │      └──────────────┘      └───────────────────┘      └─────────────┘
└─────────────┘             │
                             ▼
                      ┌──────────────┐
                      │ ASP.NET Core │
                      │   Identity   │
                      │ (Auth/Roles) │
                      └──────────────┘
```

## 3. Core Subsystems

### 3.1 Two-Step Cancellation Workflow
To prevent accidental or unilateral cancellations that disrupt scheduling, the system implements a strict two-step cancellation workflow:
- If a Patient wants to cancel, they trigger a `PatientCancellationRequested` status. The Doctor is notified via their dashboard and must formally confirm it to `Cancelled`.
- If a Doctor initiates a cancellation, it triggers `DoctorCancellationRequested`, requiring Patient confirmation.

### 3.2 Audit Logging
All critical state changes (e.g., appointment bookings, status updates, doctor profile modifications) are intercepted and logged into an `AuditLogs` table. The Admin dashboard provides a real-time, scrollable view of these logs for system compliance and monitoring.

### 3.3 Patient Reviews & Ratings
Upon an appointment reaching the `Completed` status, the patient can submit a 1-5 star rating and review. These are aggregated to calculate a Doctor's overall rating, displayed on the platform.

## 4. Application Layers

### 4.1 Models (`/Models`)
Rich domain entities mapped via EF Core.
- `ApplicationUser`: Extends `IdentityUser` with a `FullName` property.
- `Doctor` & `Patient`: Profile entities linked 1:1 with `ApplicationUser`.
- `Specialization`: Lookup taxonomy for doctor categorization.
- `Appointment`: The core transactional entity linking Doctors, Patients, Time, and Status.
- `Review`: Post-appointment rating and feedback.
- `AuditLog`: System-generated tracking of user actions.

### 4.2 Data Access (`/Data`)
- `ApplicationDbContext`: Inherits `IdentityDbContext<ApplicationUser>`, configuring all database relationships via the Fluent API in `OnModelCreating`.
- `DbInitializer`: Ensures the database is seeded on startup with Roles (Admin, Doctor, Patient), a default Admin user, and base Specializations.

### 4.3 Presentation (`/Views` & `/Controllers`)
- **Controllers**: Granular controllers mapped to domain concerns (`AppointmentsController`, `DashboardController`, `ReviewsController`, etc.), secured via `[Authorize(Roles = "...")]`.
- **UI/UX**: The frontend is built using standard Razor syntax but heavily stylized using modern CSS principles. It features a bright, corporate "Health Tech" landing page, scrollable data tables, responsive CSS grids, and professional `FontAwesome` iconography instead of emojis.

## 5. Design Decisions

1. **EF Core Code-First:** Enables the database schema to be entirely defined by the C# codebase and version-controlled via Migrations. This pairs excellently with containerized SQL Server instances.
2. **Role-Based Authorization:** Utilizing fixed roles (`Admin`, `Doctor`, `Patient`) provides a clear, understandable security matrix without the overhead of complex claims policies.
3. **Decoupled Profiles:** Keeping `ApplicationUser` separate from `Doctor` and `Patient` adheres to the Single Responsibility Principle, isolating authentication concerns from domain-specific profile data.
4. **Modern UI without JavaScript Frameworks:** The system achieves a "Premium SaaS" look and feel purely through structured HTML, robust CSS (CSS variables, flexbox, grid, sticky headers), and minimal vanilla JS, proving that heavy SPA frameworks (React/Angular) aren't strictly necessary for a premium user experience.
