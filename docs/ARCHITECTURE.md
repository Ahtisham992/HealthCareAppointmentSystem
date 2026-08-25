# Architecture Document — HealthCare Appointment System

## 1. Overview

A comprehensive, enterprise-grade healthcare appointment booking and management system built with **ASP.NET Core MVC (.NET 8)**, using **Entity Framework Core** for data access and **ASP.NET Core Identity** for authentication and robust role-based authorization.

The system supports **four distinct roles** with isolated workflows:
- **Admin** — Manages accounts across all roles, system-wide audit logs, and controls the advanced incremental billing system. Has full visibility over the platform via a centralized dashboard with statistical charts.
- **Doctor** — Manages their daily schedule, tracks incoming cash earnings, reviews their transactional history, pays platform commissions, and tracks patient reviews.
- **Receptionist** — Handles on-site operations. Manages the "Cash Drawer", marks patients as arrived (collecting cash), issues refunds for cancelled appointments, hands over collected cash to doctors, and prints professional invoices.
- **Patient** — Books appointments, requests cancellations, tracks appointment history, and submits post-appointment reviews for doctors. Must complete profile (CNIC/Phone) before booking.

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

### 3.1 Advanced Incremental Billing System
The platform operates on a 10% commission model. Instead of a rigid monthly bill, the system supports *incremental unbilled earning calculation*:
- As doctors complete appointments and receive cash, the Admin dashboard calculates `Unbilled Earnings`.
- Admins can generate a `PlatformBill` for any unbilled amount at any time during the month.
- Doctors view their transaction logs in a dynamic ledger (`MyEarnings`), submit screenshots of platform fee payments, and Admins verify them.

### 3.2 Cash Drawer & Invoice Lifecycle
The Receptionist acts as the financial intermediary:
- When an appointment is confirmed, an `Invoice` is generated.
- The receptionist collects cash, marking the Invoice as Paid.
- If an appointment is cancelled later, the invoice enters `RefundPending` state. The receptionist issues a refund, uploading a screenshot as proof.
- Receptionists use the `MyDrawer` view to group all collected cash by Doctor, and perform a one-click "Hand Over Cash" action, clearing their drawer.
- Print-ready, styled PDF Invoices are available natively via browser printing.

### 3.3 Two-Step Cancellation Workflow
To prevent accidental or unilateral cancellations that disrupt scheduling, the system implements a strict two-step cancellation workflow:
- If a Patient wants to cancel, they trigger a `PatientCancellationRequested` status. The Doctor is notified and must formally confirm it to `Cancelled`.
- If a Doctor initiates a cancellation, it triggers `DoctorCancellationRequested`, requiring Patient confirmation.

### 3.4 Audit Logging
All critical state changes (appointment bookings, invoice payments, refunds, profile edits, admin actions) are intercepted and logged into an `AuditLogs` table. The Admin dashboard provides a real-time, scrollable view of these logs for system compliance and monitoring.

## 4. UI/UX Architecture

- **Premium CSS System:** Built without heavy frontend SPA frameworks (React/Angular), the UI achieves a modern SaaS look using pure CSS variables, flexbox, and grid.
- **Global Toast Notifications:** A centralized, animated notification system that intercepts `TempData` across the entire application and displays elegant, auto-dismissing toast alerts.
- **Global Custom Modals:** A JS DOM-mutation script intercepts all native `confirm()` dialogs across the platform and replaces them with a highly polished, blurred-background custom HTML modal, completely eliminating native browser alerts.
- **DataTables Integration:** All major entity lists (Audit Logs, Accounts, Patients, Doctors) are powered by jQuery DataTables with custom corporate styling applied on top.

## 5. Application Layers

### 5.1 Models (`/Models`)
Rich domain entities mapped via EF Core.
- `ApplicationUser`: Extends `IdentityUser`.
- `Doctor` & `Patient`: Profile entities linked 1:1 with `ApplicationUser`.
- `Appointment`, `Invoice`, `PlatformBill`, `Review`: Transactional entities.
- `AuditLog`: System-generated tracking of user actions.

### 5.2 Data Access (`/Data`)
- `ApplicationDbContext`: Inherits `IdentityDbContext<ApplicationUser>`, configuring all database relationships via the Fluent API.
- `DbInitializer`: Highly sophisticated seeder that injects 50+ patients, 20+ doctors, receptionists, 100+ realistic appointments, invoices, and platform bills for immediate testing.

## 6. Design Decisions

1. **EF Core Code-First:** Enables the database schema to be entirely defined by the C# codebase and version-controlled via Migrations. This pairs excellently with containerized SQL Server instances.
2. **Role-Based Authorization:** Utilizing fixed roles (`Admin`, `Doctor`, `Receptionist`, `Patient`) provides a clear, understandable security matrix.
3. **Decoupled Profiles:** Keeping `ApplicationUser` separate from `Doctor` and `Patient` adheres to the Single Responsibility Principle, isolating authentication concerns from domain-specific profile data.
