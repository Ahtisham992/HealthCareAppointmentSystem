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
└─────────────┬─────────────┘ 1         └─────────────┘
              │ 1
              │
              │ 1
        ┌─────▼──────┐
        │  Invoice   │
        └────────────┘
```
*(Note: AuditLogs and PlatformBills run in parallel to standard workflows to preserve history/earnings).*

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
| CNIC | nvarchar(20) | Required for verification |

### Appointments
| Column | Type | Notes |
|---|---|---|
| Id | int | PK, identity |
| DoctorId | int | FK → Doctors.Id |
| PatientId | int | FK → Patients.Id |
| AppointmentDateTime | datetime2 | Scheduled date/time |
| Status | int (enum) | 0=Pending, 1=Confirmed, 2=Completed, 3=Cancelled, 4=PatientCancelReq, 5=DoctorCancelReq |
| Notes | nvarchar(500) | Optional booking notes |
| CreatedAt | datetime2 | Auto-timestamp |

### Invoices (Cash & Refund Tracking)
| Column | Type | Notes |
|---|---|---|
| Id | int | PK, identity |
| AppointmentId | int | FK → Appointments.Id (1:1) |
| Amount | decimal(10,2) | Matches Doctor ConsultationFee |
| PaymentDateTime | datetime2 | |
| Status | int (enum) | 0=Pending, 1=Paid, 2=Refunded, 3=RefundPending |
| CollectedByReceptionistId | nvarchar | Links to Receptionist user |
| HandedOverToDoctor | bit | True if cash given to doctor |
| RefundScreenshotUrl | nvarchar | Path to uploaded receipt/proof |

### PlatformBills (Incremental Billing System)
| Column | Type | Notes |
|---|---|---|
| Id | int | PK, identity |
| DoctorId | int | FK → Doctors.Id |
| Month, Year | int | Billing period |
| EarningsAmount | decimal(10,2) | Total cash collected |
| CommissionAmount| decimal(10,2) | 10% platform fee |
| Status | int (enum) | 0=Pending, 1=PaymentSubmitted, 2=Paid, 3=Cancelled |
| CreatedAt, PaidAt | datetime2 | Timestamps |

### Reviews
| Column | Type | Notes |
|---|---|---|
| Id | int | PK, identity |
| AppointmentId | int | FK → Appointments.Id (1:1) |
| DoctorId | int | FK → Doctors.Id |
| PatientId | int | FK → Patients.Id |
| Rating | int | 1 to 5 scale |
| Comment | nvarchar(1000)| Optional feedback |

### AuditLogs
| Column | Type | Notes |
|---|---|---|
| Id | int | PK, identity |
| UserId | nvarchar(max) | The email or ID of the user performing the action |
| Action | nvarchar(200) | e.g., "Appointment Created", "Invoice Refunded" |
| Details | nvarchar(max) | Contextual details regarding the action |
| Timestamp | datetime2 | Auto-timestamp |

## Migration Strategy
The schema is managed via **EF Core Code-First Migrations**. 
To apply changes to the database:
```bash
dotnet ef database update
```
