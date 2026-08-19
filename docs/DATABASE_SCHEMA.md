# Database Schema — HealthCare Appointment System

## Entity Relationship Overview

```text
       AspNetUsers (Identity)                 Specializations
             │   1                                   │   1
             │                                       │
      ┌──────┴───────┐                               │ N
     1│             1│                        ┌──────▼──────┐
┌─────▼──────┐ ┌─────▼──────┐                 │   Doctor    │
│  Patient   │ │   Doctor   │◄────────────────┤ (FK: Spec)  │
└─────┬──────┘ └─────┬──────┘ 1               └─────────────┘
      │ 1            │ 1      
      │              │        
      │ N            │ N      
┌─────▼──────────────▼──────┐         1 ┌─────────────┐
│       Appointment         │◄──────────┤   Review    │
└───────────────────────────┘ 1         └─────────────┘
```
*(Note: AuditLogs are standalone records linked by UserId strings, not strict foreign keys, to preserve history if users are deleted).*

## Tables

### AspNetUsers (Identity-managed, extended)
| Column | Type | Notes |
|---|---|---|
| Id | nvarchar(450) | PK (GUID) |
| UserName / Email | nvarchar | Identity-managed |
| PasswordHash | nvarchar | Identity-managed |
| FullName | nvarchar(200) | Custom field added via `ApplicationUser` |

### Specializations
| Column | Type | Notes |
|---|---|---|
| Id | int | PK, identity |
| Name | nvarchar(100) | e.g., "Cardiology", "Neurology" |

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
| Status | int (enum) | 0=Pending, 1=Confirmed, 2=Completed, 3=Cancelled, 4=PatientCancellationRequested, 5=DoctorCancellationRequested |
| Notes | nvarchar(500) | Optional booking notes |
| CreatedAt | datetime2 | Auto-timestamp |

### Reviews
| Column | Type | Notes |
|---|---|---|
| Id | int | PK, identity |
| AppointmentId | int | FK → Appointments.Id (1:1) |
| DoctorId | int | FK → Doctors.Id |
| PatientId | int | FK → Patients.Id |
| Rating | int | 1 to 5 scale |
| Comment | nvarchar(1000)| Optional feedback |
| CreatedAt | datetime2 | Auto-timestamp |

### AuditLogs
| Column | Type | Notes |
|---|---|---|
| Id | int | PK, identity |
| UserId | nvarchar(max) | The email or ID of the user performing the action |
| Action | nvarchar(200) | e.g., "Appointment Created", "Status Updated" |
| Details | nvarchar(max) | Contextual details regarding the action |
| Timestamp | datetime2 | Auto-timestamp |

## Relationships Summary
- `ApplicationUser` 1 — 1 `Doctor`
- `ApplicationUser` 1 — 1 `Patient`
- `Specialization` 1 — N `Doctor`
- `Doctor` 1 — N `Appointment`
- `Patient` 1 — N `Appointment`
- `Appointment` 1 — 1 `Review`

## Migration Strategy
The schema is managed via **EF Core Code-First Migrations**. 
To apply changes to the database:
```bash
dotnet ef database update
```
