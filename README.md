# HealthCare Appointment System

An enterprise-grade, role-based healthcare appointment booking and management system built with **ASP.NET Core MVC (.NET 8)**, **Entity Framework Core**, and **ASP.NET Core Identity**.

This project has been carefully architected and styled to serve as a comprehensive portfolio piece, showcasing backend .NET/C# proficiency alongside a premium, modern "SaaS-style" corporate frontend. It contains extensive workflows for appointments, financial tracking, invoicing, and auditing.

## Key Features

- **Six Distinct Roles:** 
  - **Admin**: Complete system visibility, global account management, access to a centralized dashboard featuring statistical charts and real-time audit logs.
  - **Accountant**: Operates the dual-escrow dashboard tracking "System Escrow Liabilities" (total funds held in all user wallets) vs "Platform Earnings", processes user wire withdrawal requests (with automated 2% fee deductions and 100% rejection refunds), and reconciles digital vs physical cash flows.
  - **Doctor**: Manages availability, handles patient appointments, issues prescriptions, and tracks personal digital wallet earnings.
  - **Pharmacist**: Manages digital prescriptions, sets medication pricing, and processes hybrid (cash + wallet) payments seamlessly routing 5% platform commission to escrow.
  - **Receptionist**: Operates the "Cash Drawer" to collect payments from arriving patients, handles appointment refunds, generates invoices, and deposits collected physical cash to the Accountant.
  - **Patient**: Books appointments, receives prescriptions, deposits digital funds via Stripe Checkout, and tracks transaction history.

- **Centralized Digital Wallet & Escrow Architecture:** 
  - **Stripe Hosted Checkout**: Users can instantly top up their digital wallets via secure Stripe integration.
  - **Automated Escrow Splitting**: Appointments and Pharmacy bills automatically deduct from patient wallets and instantaneously route 90-95% to the provider and 5-10% to the Platform Escrow.
  - **Hybrid Physical/Digital Clearing**: Receptionists and Pharmacists can accept physical cash. The system automatically debits their digital wallets, ensuring perfect system-wide reconciliation and preventing "cash leaks."

- **Two-Step Cancellation Workflow:** A robust state machine ensuring that cancellations initiated by either party (Patient or Doctor) must be confirmed by the other party to prevent scheduling conflicts.

- **Audit Logging System:** All critical database actions (creating appointments, updating statuses, profile changes, cash handovers) are intercepted and logged for compliance monitoring on the Admin Dashboard.

- **Premium UI/UX:** 
  - A custom-built, highly polished frontend using modern CSS, flexbox, and FontAwesome.
  - **Global Toast Notifications**: Beautiful, animated popups for success/error messages across the entire app.
  - **Custom Global Modals**: Complete elimination of ugly native browser `confirm()` boxes, replaced by a JS-intercepted animated DOM modal.
  - Integrated `DataTables` for powerful, search-enabled data grids.

## Tech Stack
- **Backend:** ASP.NET Core MVC (.NET 8), C#
- **Database & ORM:** Entity Framework Core (Code-First), SQL Server (Dockerized)
- **Auth:** ASP.NET Core Identity (Cookie-based, Role-based authorization)
- **Frontend:** Razor Views, custom vanilla CSS, FontAwesome, jQuery DataTables

## Documentation
For an in-depth understanding of the system, please refer to the dedicated documentation files:
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — System design, advanced financial workflows, and key technical decisions.
- [`docs/DATABASE_SCHEMA.md`](docs/DATABASE_SCHEMA.md) — Entity relationship diagram, table structures, and relationships.
- [`docs/SETUP_GUIDE.md`](docs/SETUP_GUIDE.md) — Full, step-by-step setup instructions explicitly tailored for Docker and modern IDEs (like Antigravity / VS Code).

## Quick Start

Ensure you have .NET 8 and Docker installed.

```bash
# 1. Start the ephemeral SQL Server container
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong!Passw0rd" -p 1433:1433 --name hc_db -d mcr.microsoft.com/mssql/server:2022-latest

docker start hc_db

# 2. Enter the project directory
cd HealthCareAppointmentSystem

# 3. Restore dependencies & apply schema
dotnet restore
dotnet tool install --global dotnet-ef
dotnet ef database update

# 4. Run the application
dotnet run
```

**Note on Dummy Data:** On the very first run, the database is automatically seeded with over **50 Patients, 20 Doctors, 100+ Appointments, and generated Bills/Invoices** so you can immediately test the advanced financial workflows!

See the [`docs/SETUP_GUIDE.md`](docs/SETUP_GUIDE.md) for full details, troubleshooting, and default login credentials.
