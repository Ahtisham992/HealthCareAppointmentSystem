# Setup Guide — HealthCare Appointment System

## Prerequisites

1. **.NET 8 SDK** — download from https://dotnet.microsoft.com/download/dotnet/8.0
   Verify with: `dotnet --version` (should show 8.x)
2. **SQL Server** — any of these work:
   - SQL Server Express / Developer Edition (Windows)
   - SQL Server via Docker (`docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong!Passw0rd" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest`)
   - **Or**, for the fastest local setup, swap the connection string to use **LocalDB** (comes bundled with Visual Studio on Windows) — already configured as the default in `appsettings.json`.
3. **An IDE**: Visual Studio 2022 (recommended, see note at bottom) or VS Code / Antigravity with the C# extension.

## Step-by-Step Setup

### 1. Open the project
- **Visual Studio**: Open `HealthCareAppointmentSystem.sln`
- **VS Code / Antigravity / CLI**: `cd HealthCareAppointmentSystem` (the inner project folder)

### 2. Restore packages
```bash
dotnet restore
```

### 3. Update the connection string (if needed)
Open `appsettings.json` and confirm/update the `DefaultConnection` string to match your SQL Server setup. The default uses LocalDB:
```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=HealthCareAppointmentDb;Trusted_Connection=True;MultipleActiveResultSets=true"
```
If you're using Docker/SQL auth instead, it'll look more like:
```json
"DefaultConnection": "Server=localhost,1433;Database=HealthCareAppointmentDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True"
```

### 4. Install the EF Core CLI tool (one-time, machine-wide)
```bash
dotnet tool install --global dotnet-ef
```

### 5. Create and apply the database migration
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```
This creates the database and all tables based on the models.

### 6. Run the project
```bash
dotnet run
```
Or press **F5** in Visual Studio.

The app will be available at `https://localhost:5001` (or the port shown in your terminal).

### 7. Default seeded login
On first run, `DbInitializer` seeds one Admin account:
- **Email:** admin@healthcare.local
- **Password:** Admin@123

Log in as Admin to add Specializations and Doctors, then register additional Patient/Doctor accounts through the normal registration page to explore all three roles.

## Running on Visual Studio vs. VS Code-based editors (Antigravity, Cursor, etc.)

**Visual Studio (Windows) — recommended for this project.**
Full native support: IntelliSense, integrated debugger, EF Core migration tooling built into the Package Manager Console, NuGet management UI, and it's genuinely what most Pakistani job postings asking for ".NET experience" mean when they say "Visual Studio" as a required tool. If your goal is a resume-ready, fully working project you can demo confidently, build and run it in Visual Studio at least once.

**Google Antigravity / Cursor / Windsurf (VS Code forks).**
You *can* open and edit this project's code in Antigravity, and its AI agent can run `dotnet build` / `dotnet run` via the integrated terminal. However, Microsoft's official **C# Dev Kit** extension (which provides IntelliSense, solution explorer, and the integrated debugger for C#) is **not licensed for use in third-party VS Code forks** like Antigravity, Cursor, or Windsurf — Microsoft restricts it to genuine VS Code and Visual Studio. You'd be limited to the older/basic C# extension or unofficial community alternatives, and debugging support has been reported as inconsistent. Good for quick edits or letting an AI agent scaffold something, not ideal as your primary environment for a project you want to deeply understand and demo reliably.

**Recommendation:** Use Antigravity/AI tools to help you understand or extend the code (exactly like we're doing now), but do your actual build/run/debug cycle in real Visual Studio, or plain VS Code with the standard (non-Dev Kit) C# extension if you're not on Windows.

## Troubleshooting
- **"A network-related or instance-specific error..."** — SQL Server isn't running, or the connection string is wrong. Double check step 3.
- **"No migrations configuration type was found"** — you're not in the project folder containing the `.csproj` file when running `dotnet ef` commands.
- **Port already in use** — check `Properties/launchSettings.json` and change the port, or stop whatever else is using it.
