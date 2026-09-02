# Future Roadmap & Feature Expansion

This document outlines the step-by-step roadmap for upgrading the HealthCare Appointment System from an administrative booking platform into a fully fledged **Enterprise Clinical & HealthTech SaaS**.

The roadmap is divided into logical phases. Each phase introduces new roles, database entities, and workflows that seamlessly integrate with the existing architecture.

---

## Phase 1: The Clinical Pipeline (E-Prescriptions & Pharmacy) [COMPLETED ✅]
**Goal:** Digitize the post-consultation workflow. Instead of doctors just leaving "Notes", they will write structured digital prescriptions that flow directly to a Pharmacy system.

### New Roles
*   **Pharmacist**: Can view pending prescriptions, mark them as fulfilled/dispensed, and track pharmacy billing.

### Database Architecture Updates
*   `Pharmacist`: Links 1:1 to `ApplicationUser`.
*   `Prescription`: Links 1:1 to `Appointment`.
*   `PrescriptionItem`: Links N:1 to `Prescription`. Stores Medicine Name, Dosage (e.g., "500mg"), Frequency (e.g., "1x a day"), and Duration ("5 days").

### Core Features
*   **Doctor E-Prescription Builder:** A dynamic UI where doctors can dynamically add multiple medicine rows to an appointment.
*   **PDF Generation:** The system automatically formats the `Prescription` and its items into a beautifully styled, print-ready PDF containing the Doctor's details and a digital signature.
*   **Pharmacy Dashboard:** A Kanban-style board for Pharmacists to see `Pending`, `Processing`, and `Dispensed` prescriptions.

---

## Phase 2: Diagnostics & EHR (Electronic Health Records) [NEXT TARGET 🎯]
**Goal:** Give doctors comprehensive context before a consultation, and allow them to order external tests.

### New Roles
*   **Lab Technician**: Operates a diagnostic center. Receives test orders from doctors, conducts tests, and uploads the results.

### Database Architecture Updates
*   `LabTechnician`: Links 1:1 to `ApplicationUser`.
*   `LabOrder`: Links N:1 to `Appointment`. Stores the type of test (e.g., "Complete Blood Count", "Chest X-Ray").
*   `LabResult`: Links 1:1 to `LabOrder`. Contains a `FileUrl` pointing to the uploaded PDF/Image report.
*   `MedicalProfile`: Links 1:1 to `Patient`. Stores Blood Group, Known Allergies, and Chronic Conditions.

### Core Features
*   **Patient Medical Timeline:** When a doctor opens a patient's appointment, they see a full EHR sidebar showing allergies, past diagnoses, and previous prescriptions.
*   **Diagnostic Workflow:** Doctors can order tests. Lab Techs log in, see pending orders, upload the PDF results, and the Doctor gets a notification that the results are ready to review.

---

## Phase 3: Digital Modernization [PARTIALLY COMPLETED ✅]
**Goal:** Modernize the patient experience to match top-tier platforms like Zocdoc or Practo.

### Database Architecture Updates
*   Add `AppointmentType` (Enum: InPerson, VideoCall) to `Appointment`.
*   Add `MeetingLink` (string) to `Appointment`.

### Core Features
*   **Telehealth / Video Consultations:** 
    *   Patients can choose "Video Call" during booking.
    *   The system generates a unique meeting link.
    *   A "Join Call" button appears on both the Doctor and Patient dashboards 10 minutes before the appointment time.
*   **Automated Background Reminders:** 
    *   Implementation of a `.NET HostedService` (Background Worker) that runs every hour.
    *   It scans the database for appointments happening in exactly 24 hours and logs an "Email/SMS Reminder Sent" audit log.
*   **Online Payments (Stripe):** 
    *   Bypass the physical Receptionist Cash Drawer for patients who want to pay online.
    *   Integrate Stripe Checkout so patients can pay their Consultation Fee securely via credit card at the time of booking. 

---

## Implementation Strategy
To maintain system stability, we will implement these features strictly one phase at a time. 

**Recommended Starting Point:** We have successfully completed Phase 1 (Pharmacy Pipeline) and the financial/Wallet components of Phase 3 (Stripe integration, Background Automation). Our next major milestone is **Phase 2: Diagnostics & EHR (Electronic Health Records)**. This will introduce the `LabTechnician` role and allow doctors to order external tests seamlessly.
