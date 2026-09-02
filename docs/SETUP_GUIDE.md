# Setup Guide — HealthCare Appointment System

This guide outlines the recommended workflow for setting up and running the HealthCare Appointment System locally, optimized for modern developer environments utilizing Docker and VS Code-based editors (like Antigravity IDE).

## Prerequisites

1. **.NET 8 SDK**
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Verify installation: `dotnet --version`
2. **Docker Desktop**
   - Required for running the localized SQL Server container seamlessly.
3. **Antigravity IDE / VS Code**
   - Ensure the standard C# extension is installed for syntax highlighting and basic IntelliSense.

## Step-by-Step Setup

### 1. Start the SQL Server Container
The most reliable way to run the database across any operating system is via a Docker container. Run the following command in your terminal to spin up an ephemeral SQL Server 2022 instance:
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong!Passw0rd" -p 1433:1433 --name hc_db -d mcr.microsoft.com/mssql/server:2022-latest
```

### 2. Configure the Connection String
Open `appsettings.json` in the root of the project and ensure your `DefaultConnection` points to your Docker instance:
```json
"DefaultConnection": "Server=localhost,1433;Database=HealthCareAppointmentDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True"
```

### 3. Restore Packages & Install EF Tools
Navigate to the project directory containing the `.csproj` file:
```bash
cd HealthCareAppointmentSystem
```

Restore the .NET packages:
```bash
dotnet restore
```

Install the Entity Framework Core CLI tools (if you haven't globally already):
```bash
dotnet tool install --global dotnet-ef
```

### 4. Apply Database Migrations
Initialize your Docker database with the necessary schema (Tables: Users, Wallets, Appointments, Prescriptions, Invoices, WithdrawalRequests, AuditLogs, etc.):
```bash
dotnet ef database update
```

### 5. Run the Application
Start the local Kestrel web server:
```bash
dotnet run
```
The application will boot up, and the terminal will output the local URL (usually `http://localhost:5000` or `https://localhost:5001`). Open this in your browser to view the premium corporate landing page.

## Extensive Seeding System

On the very first run, the `DbInitializer` automatically provisions the database with massive amounts of realistic dummy data to allow immediate testing of all features:
- **Roles & Admin:** `Admin`, `Accountant`, `Doctor`, `Receptionist`, `Pharmacist`, `LabTechnician`, `Patient` roles. Master admin account (`admin@healthcare.local` / `Admin@123`), Accountant (`accountant@healthcare.local` / `Accountant@123`), and Lab Technician (`labtech1@healthcare.local` / `LabTech1@123`).
- **Doctors:** 20 verified doctors across various specializations.
- **Patients:** 50 verified patients with CNIC and Medical Profile details.
- **Receptionists:** 2 receptionist accounts (`receptionist1@gmail.com` / `Receptionist1@123`).
- **Appointments & Finances:** Over 100 appointments in various states (Pending, Confirmed, Completed, Cancelled). Automatically generated `Invoices`, Wallet Escrow transactions, and Prescriptions attached to these appointments.

**Next Steps:** Log in with the accountant credentials, navigate to the Dashboard to view system finances and pending withdrawal requests. Log in as `receptionist1@gmail.com` to explore the Cash Drawer.

## Troubleshooting

- **"A network-related or instance-specific error occurred while establishing a connection to SQL Server"**
  - Ensure your Docker container is actually running: `docker ps`. If it stopped, start it with `docker start hc_db`.
- **"The database operation was expected to affect 1 row(s), but actually affected 0"**
  - Usually occurs during concurrent editing or stale data. Refresh the page or check the Audit Logs.
- **"Port already in use"**
  - If `dotnet run` complains about a used port, check your `Properties/launchSettings.json` and change the `applicationUrl` port, or kill the process using the current port.
