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

*(Note: AuditLogs, Wallets, Prescriptions run in parallel to standard workflows to preserve history/earnings).*

## Tables

### AspNetUsers (Identity-managed, extended)
| Column | Type | Notes |
|---|---|---|
| Id | nvarchar(450) | PK (GUID) |
| UserName / Email | nvarchar | Identity-managed |
| PasswordHash | nvarchar | Identity-managed |
| FullName | nvarchar(200) | Custom field added via `ApplicationUser` |

### Wallets & Transactions (Centralized Escrow)
| Table | Columns | Notes |
|---|---|---|
| **Wallets** | Id, ApplicationUserId, Balance | Centralized escrow. Null user = Platform Escrow. |
| **WalletTransactions** | Id, WalletId, Amount, Type, ReferenceId | Immutable ledger for Deposits, Payouts, Commissions. |
| **WithdrawalRequests** | Id, WalletId, Amount, Status, BankDetails | Track accountant wire transfer clearing. |

### Prescriptions & Pharmacy (Hybrid Payments)
| Table | Columns | Notes |
|---|---|---|
| **Pharmacists** | Id, ApplicationUserId, Qualifications | Profile linked to AspNetUsers. |
| **Prescriptions** | Id, AppointmentId, TotalAmount, Status | Generates bills for medicine dispensaries. |
| **PrescriptionItems**| Id, PrescriptionId, MedicineName, Price | Line items under a prescription. |

### Doctors & Specializations
| Table | Columns | Notes |
|---|---|---|
| **Specializations** | Id, Name | e.g. "Cardiology" |
| **Doctors** | Id, ApplicationUserId, SpecializationId, Fee | Profile entity linked to AspNetUsers (1:1). |

### Patients
| Column | Type | Notes |
|---|---|---|
| Id | int | PK, identity |
| ApplicationUserId | nvarchar(450) | FK → AspNetUsers.Id (1:1) |
| DateOfBirth | datetime2 | |
| PhoneNumber | nvarchar(20) | |
| Address | nvarchar(300) | |
| CNIC | nvarchar(20) | Required for verification |

### Appointments & Invoices
| Table | Columns | Notes |
|---|---|---|
| **Appointments** | Id, DoctorId, PatientId, Status | 0=Pending, 1=Confirmed, 3=Cancelled |
| **Invoices** | Id, AppointmentId, Amount, Status | Tracks the billing state of an appointment. |

### AuditLogs
| Column | Type | Notes |
|---|---|---|
| Id | int | PK, identity |
| UserId | nvarchar(max) | The email or ID of the user performing the action |
| Action | nvarchar(200) | e.g., "Appointment Created", "Withdrawal Approved" |
| Details | nvarchar(max) | Contextual details regarding the action |
| Timestamp | datetime2 | Auto-timestamp |

## Migration Strategy
The schema is managed via **EF Core Code-First Migrations**. 
To apply changes to the database:
```bash
dotnet ef database update
```
