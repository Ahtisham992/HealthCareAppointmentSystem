# Architecture Document — HealthCare Appointment System

## 1. Overview

A comprehensive, enterprise-grade healthcare appointment booking and management system built with **ASP.NET Core MVC (.NET 8)**, using **Entity Framework Core** for data access and **ASP.NET Core Identity** for authentication and robust role-based authorization.

The system supports **six distinct roles** with isolated workflows:
- **Admin** — Manages accounts across all roles, system-wide audit logs. Has full visibility over the platform via a centralized dashboard.
- **Accountant** — Manages the Platform Escrow Wallet, processes user wire withdrawal requests, and reconciles digital vs physical cash flows.
- **Doctor** — Manages their daily schedule, tracks incoming digital wallet earnings, and reviews patient feedback.
- **Pharmacist** — Manages digital prescriptions, sets medication pricing, and processes hybrid (cash + wallet) payments routing 5% platform commission to escrow.
- **Receptionist** — Handles on-site operations. Manages the "Cash Drawer", issues refunds for cancelled appointments, and deposits physical cash to the Accountant.
- **Patient** — Books appointments, tracks history, requests cancellations, deposits digital funds via Stripe Checkout, and purchases medicine via Wallet Escrow.

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

### 3.1 Centralized Wallet Escrow System
The platform handles all financial transactions via a centralized digital wallet architecture to prevent double spending and ensure immediate payout routing.
- **Stripe Checkout:** Patients deposit funds securely via Stripe.
- **Immediate Escrow Splitting:** When an appointment or prescription is paid, the patient's wallet is debited, the provider (Doctor/Pharmacist) is instantly credited their cut (90-95%), and the Platform Escrow Wallet receives the commission (5-10%).
- **Withdrawal Clearances:** Providers request bank withdrawals. Their digital wallet is instantly locked/debited, and an Accountant formally wires the cash and approves the `WithdrawalRequest`.

### 3.2 Hybrid Physical/Digital Clearing
Receptionists and Pharmacists can accept physical cash for low-balance patients.
- The physical cash received is **instantly debited** from the receiver's digital wallet, balancing the equation.
- The receptionist runs a routine to "Hand over to Escrow," passing the physical cash to the Accountant to reset their balance.

### 3.3 Two-Step Cancellation Workflow
To prevent accidental or unilateral cancellations that disrupt scheduling, the system implements a strict two-step cancellation workflow:
- If a Patient wants to cancel, they trigger a `PatientCancellationRequested` status. The Doctor is notified and must formally confirm it to `Cancelled`.
- If a Doctor initiates a cancellation, it triggers `DoctorCancellationRequested`, requiring Patient confirmation.

### 3.4 Audit Logging
All critical state changes (appointment bookings, invoice payments, refunds, withdrawals, admin actions) are intercepted and logged into an `AuditLogs` table. The Admin dashboard provides a real-time, scrollable view of these logs for system compliance and monitoring.

## 4. UI/UX Architecture

- **Premium CSS System:** Built without heavy frontend SPA frameworks (React/Angular), the UI achieves a modern SaaS look using pure CSS variables, flexbox, and grid.
- **Global Toast Notifications:** A centralized, animated notification system that intercepts `TempData` across the entire application and displays elegant, auto-dismissing toast alerts.
- **Global Custom Modals:** A JS DOM-mutation script intercepts all native `confirm()` dialogs across the platform and replaces them with a highly polished, blurred-background custom HTML modal.
- **DataTables Integration:** All major entity lists (Audit Logs, Accounts, Patients, Doctors) are powered by jQuery DataTables.

## 5. Application Layers

### 5.1 Models (`/Models`)
Rich domain entities mapped via EF Core.
- `ApplicationUser`: Extends `IdentityUser`.
- `Doctor`, `Patient`, `Pharmacist`: Profile entities linked 1:1 with `ApplicationUser`.
- `Wallet`, `WalletTransaction`, `WithdrawalRequest`: Financial system.
- `Prescription`, `Invoice`: Billing systems.

### 5.2 Data Access (`/Data`)
- `ApplicationDbContext`: Inherits `IdentityDbContext<ApplicationUser>`, configuring all database relationships via the Fluent API.
- `DbInitializer`: Highly sophisticated seeder that injects patients, doctors, and realistic appointments for immediate testing.
