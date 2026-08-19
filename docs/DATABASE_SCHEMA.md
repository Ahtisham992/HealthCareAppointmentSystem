# Database Schema — HealthCare Appointment System

## Entity Relationship Overview

```
AspNetUsers (Identity)          Specializations
      │  1                             │  1
      │                                │
      │ 1                              │ N
┌─────▼──────┐                  ┌──────▼──────┐
│   Doctor   │ ─────────────────│   Doctor    │
│            │  N              1│ (FK: Spec)  │
└─────┬──────┘                  └─────────────┘
      │ 1
      │
      │ N
┌─────▼──────┐         N      1 ┌─────────────┐
│ Appointment│◄─────────────────│   Patient   │
│            │                  │             │
└────────────┘                  └──────┬──────┘
                                        │ 1
                                        │
                                        │ 1
                                 AspNetUsers (Identity)
```

## Tables

### AspNetUsers (Identity-managed, extended)
| Column | Type | Notes |
|---|---|---|
| Id | nvarchar(450) | PK (string, GUID by default) |
| UserName / Email | nvarchar | Identity-managed |
| PasswordHash | nvarchar | Identity-managed, hashed |
| FullName | nvarchar(200) | Custom field added via `ApplicationUser` |

### AspNetRoles / AspNetUserRoles (Identity-managed)
Standard Identity role tables. Seeded roles: `Admin`, `Doctor`, `Patient`.

### Specializations
| Column | Type | Notes |
|---|---|---|
| Id | int | PK, identity |
| Name | nvarchar(100) | e.g. "Cardiology", "Dermatology" |

### Doctors
| Column | Type | Notes |
|---|---|---|
| Id | int | PK, identity |
| ApplicationUserId | nvarchar(450) | FK → AspNetUsers.Id (1:1) |
| SpecializationId | int | FK → Specializations.Id |
| LicenseNumber | nvarchar(50) | |
| YearsOfExperience | int | |
| ConsultationFee | decimal(10,2) | |

### Patients
| Column | Type | Notes |
|---|---|---|
| Id | int | PK, identity |
| ApplicationUserId | nvarchar(450) | FK → AspNetUsers.Id (1:1) |
| DateOfBirth | datetime2 | |
| PhoneNumber | nvarchar(20) | |
| Address | nvarchar(300) | |

### Appointments
| Column | Type | Notes |
|---|---|---|
| Id | int | PK, identity |
| DoctorId | int | FK → Doctors.Id |
| PatientId | int | FK → Patients.Id |
| AppointmentDateTime | datetime2 | Scheduled date/time |
| Status | int (enum) | Pending = 0, Confirmed = 1, Completed = 2, Cancelled = 3 |
| Notes | nvarchar(500) | Optional notes from patient at booking |
| CreatedAt | datetime2 | Set automatically on creation |

## Relationships Summary
- `ApplicationUser` 1 — 1 `Doctor` (a doctor's login account)
- `ApplicationUser` 1 — 1 `Patient` (a patient's login account)
- `Specialization` 1 — N `Doctor`
- `Doctor` 1 — N `Appointment`
- `Patient` 1 — N `Appointment`

## Migration Strategy
This project uses **EF Core Code-First Migrations**. After cloning, run:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

This generates the schema above from the C# model classes in `/Models` and the Fluent API configuration in `ApplicationDbContext.OnModelCreating`.
