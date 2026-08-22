# Project Examination - Design System & Architecture Specification

## 1. High-Level System Architecture

The application is built on **.NET 9** utilizing a hybrid **Blazor Interactive Server & WebAssembly** architecture with **FastEndpoints** for HTTP API routes, **SignalR** for real-time presence telemetry, **EF Core** with **PostgreSQL** for persistence, and **Hangfire** for asynchronous background job execution.

```mermaid
graph TB
    subgraph ClientLayer["Frontend & Client Layer (Blazor WebAssembly / Server)"]
        UI["MudBlazor UI Components & Layouts"]
        HubClient["SignalR Hub Client"]
        ClientStorage["LocalStorage & ProtectedSessionStorage"]
    end

    subgraph AppLayer["Application Layer (.NET 9 Web / FastEndpoints)"]
        Program["Program.cs Pipeline"]
        Presence["PresenceHub & OnlineUserService"]
        FastAPI["FastEndpoints API (api/*)"]
        BlazorSSR["Blazor Interactive Server/Wasm Endpoints"]
    end

    subgraph ServiceLayer["Domain Services Layer"]
        ExamSvc["ExamService (IExam)"]
        DocxSvc["WordDocumentService (IDocx)"]
        RoomSvc["RoomService (IRoom)"]
        UserExamSvc["UserExamService (IUserExam)"]
        UserSvc["UserService (IUser)"]
        ReportSvc["ReportService (IReport)"]
        DashboardSvc["DashboardService (IDashboard)"]
        RefSvc["ReferenceService (IReferences)"]
        MailSvc["EmailService (IMailService)"]
    end

    subgraph BackgroundLayer["Background Jobs & Messaging"]
        HangfireSvc["Hangfire Server"]
        MailKit["MailKit SMTP Dispatcher"]
    end

    subgraph DataLayer["Data & Persistence Layer"]
        AppDb["AppDbContext (PostgreSQL via Npgsql)"]
        IdentityDb["ASP.NET Identity (RBAC)"]
        HangfireDb["PostgreSQL Hangfire Storage"]
        MemCache["IMemoryCache"]
    end

    UI --> BlazorSSR
    UI --> FastAPI
    HubClient --> Presence
    BlazorSSR --> ServiceLayer
    FastAPI --> ServiceLayer
    Presence --> ServiceLayer

    UserSvc --> IdentityDb
    ExamSvc --> AppDb
    RoomSvc --> AppDb
    UserExamSvc --> AppDb
    ReportSvc --> AppDb
    DashboardSvc --> AppDb
    RefSvc --> AppDb
    RefSvc --> MemCache
    RefSvc --> ClientStorage

    MailSvc --> HangfireSvc
    HangfireSvc --> HangfireDb
    HangfireSvc --> MailKit
```

---

## 2. Domain Data Model & Entity Relations

The persistence architecture is managed by `AppDbContext` with PostgreSQL:

```mermaid
erDiagram
    ApplicationUser ||--o{ UserExam : "takes"
    ApplicationUser }|--|| Kota : "resides in"
    Provinsi ||--o{ Kota : "contains"
    
    Exam ||--o{ Soal : "contains"
    Exam ||--o{ Room : "scheduled in"
    Soal ||--o{ SoalJawaban : "has options"
    
    Room ||--o{ UserExam : "sessions"
    UserExam ||--o{ UserAnswer : "submits"
    
    Soal ||--o{ UserAnswer : "referenced in"
    SoalJawaban ||--o{ UserAnswer : "selected answer"
    
    ExamReport }|--|| Room : "mapped view (vw_examreport)"
```

### Core Entities Summary
1. **`ApplicationUser`**: Extended ASP.NET Identity user storing full name, gender, phone number, photo, occupation, last login timestamp, and foreign key to `Kota`.
2. **`Exam`**: The test blueprint/exam collection containing a name, description, duration, and question collection.
3. **`Soal` & `SoalJawaban`**: Question bank items supporting single-choice or multiple-choice weighted point systems (`isMultipleJawaban`, points per option).
4. **`Room`**: Test room instance with a unique entry code (`Kode`), schedule window (`JadwalStart` to `JadwalEnd`), and assigned `Exam`.
5. **`UserExam`**: An individual candidate's exam session tracking start/end times, remaining time (`TimeLeft`), retry counts, normalized score calculation, and status flags (`IsOngoing`, `IsDone`).
6. **`UserAnswer`**: Candidate answers mapping `UserExam` to individual `Soal` and selected `SoalJawaban`.
7. **`ExamReport`**: Database view (`vw_examreport`) providing flattened query capabilities for reporting.

---

## 3. Service Layer Architecture & Responsibilities

| Service & Interface | Primary Responsibilities | Dependencies & Key Features |
| :--- | :--- | :--- |
| **`ExamService`** (`IExam`) | Manages question bank and exam sets. | - CRUD operations with eager loading of questions and answers.<br>- Differential synchronization (`Except`) for questions and options on update.<br>- Pagination with `WhereIf` filtering. |
| **`WordDocumentService`** (`IDocx`) | Ingests `.docx` files to parse questions and answer choices automatically. | - Uses `DocumentFormat.OpenXml`.<br>- Regex-based answer key & point parsing.<br>- Auto-detects decimal numbering (questions) and letter lists (options). |
| **`RoomService`** (`IRoom`) | Coordinates exam rooms, join tokens, and supervisor views. | - Generates and validates unique room codes (`Kode`).<br>- Scopes room management to current user/creator.<br>- Splits queries for efficient nested loads. |
| **`UserExamService`** (`IUserExam`) | Handles exam attempt lifecycle, submission, and grading. | - Prevents duplicate room entry.<br>- Calculates normalized scores and score histories.<br>- Synchronizes nested child collections (`UserAnswer`). |
| **`UserService`** (`IUser`) | Manages user registration, profiles, authentication, and RBAC. | - ASP.NET Identity integration.<br>- Roles: `Superuser`, `Operator`, `Dosen`, `User`.<br>- Password reset workflows and activation overrides. |
| **`ReportService`** (`IReport`) | Provides aggregated exam results and analytical outputs. | - Leverages database view `vw_examreport`.<br>- Multi-field filtering (Name, City, Room name). |
| **`DashboardService`** (`IDashboard`) | Computes live metrics for candidate and instructor dashboards. | - Candidate view: upcoming tests, ongoing tests, score average.<br>- Instructor (`Dosen`) view: participant count, averages, min/max metrics. |
| **`ReferenceService`** (`IReferences`) | Provides lookup datasets for Indonesian provinces (`Provinsi`) and cities (`Kota`). | - Two-level cache strategy: MemoryCache (server) + LocalStorage (client) with 1-day TTL. |
| **`EmailService`** (`IMailService`) | Sends transactional emails. | - MailKit SMTP provider.<br>- Hangfire queueing (`Enqueue`) for background execution. |
| **`PresenceHub` & `OnlineUserService`** | Real-time proctoring and user telemetry. | - SignalR Hub capturing connection ID, username, client IP (`X-Real-IP`/`X-Forwarded-For`), device string, and GPS coordinates.<br>- In-memory thread-safe state (`ConcurrentDictionary`). |

---

## 4. Key Workflows

### 4.1 Exam Room Lifecycle & Candidate Execution Flow
```mermaid
sequenceDiagram
    autonumber
    actor Candidate as Candidate (User)
    participant UI as Blazor Client
    participant RoomSvc as RoomService
    participant UserExamSvc as UserExamService
    participant Hub as PresenceHub
    participant Db as AppDbContext

    Candidate->>UI: Enter Room Code
    UI->>RoomSvc: Get(kode)
    RoomSvc->>Db: Query Room with Exam & Questions
    Db-->>RoomSvc: Room Details
    RoomSvc-->>UI: Room Details

    UI->>UserExamSvc: Create(CreateUserExamDTO)
    UserExamSvc->>Db: Insert UserExam
    Db-->>UserExamSvc: Created UserExam

    UI->>Hub: Connect (send IP, Device, Lat/Lng)
    Hub-->>UI: Broadcast "UserListUpdated"

    loop Answer Progression
        Candidate->>UI: Select Question Option
        UI->>UserExamSvc: Update(UserExam with UserAnswers)
        UserExamSvc->>Db: Sync UserAnswer records
    end

    Candidate->>UI: Finish Exam
    UI->>UserExamSvc: Update(IsDone = true, calculate score)
    UserExamSvc->>Db: Save Final Score & Normalize
    UserExamSvc-->>UI: Exam Completed
```

### 4.2 Automated Word Document Import Flow
```mermaid
flowchart TD
    A[Upload .docx Exam File] --> B[WordDocumentService.ProcessDocxAsync]
    B --> C[Read Paragraph Properties & NumberingDefinitions]
    C --> D{Format Type?}
    D -- Decimal 1, 2, 3... --> E[Create New Soal Question]
    D -- UpperLetter A, B, C... --> F[Create SoalJawaban Option]
    D -- Plain Text / Pattern --> G[Extract Answer Keys & Point Multipliers]
    E --> H[Aggregate into DocxResult]
    F --> H
    G --> H
    H --> I[Preview & Save via ExamService]
```

---

## 5. UI Theme & Visual Tokens Design System

The visual design system is defined in `Web.Client/Shared/Theme/Theme.cs` using MudBlazor theming tokens:

### Typography
* **Primary Font Families**: `Poppins`, `Roboto`, `Arial`, `sans-serif`, `Helvetica`
* **H1**: `4rem`, Weight: `700`
* **H2**: `2.5rem`, Weight: `600`
* **Body / Default**: `0.875rem`, `normal` letter spacing

### Color Tokens

#### Light Theme
* **Background / Drawer**: `#FFFFFF`
* **Surface**: `#FFFFFF`
* **Text / Appbar Text**: `#424242`
* **Appbar Background**: `rgba(255, 255, 255, 0.8)` (Glassmorphism)
* **Gray Light / Lighter**: `#E8E8E8` / `#F9F9F9`

#### Dark Theme
* **Primary**: `#7E6FFF`
* **Surface**: `#1E1E2D`
* **Background**: `#1A1A27`
* **Background Gray**: `#151521`
* **Appbar Background**: `rgba(26, 26, 39, 0.8)`
* **Text Primary**: `#B2B0BF`
* **Text Secondary / Drawer Icon**: `#92929F`
* **Success**: `#3DCB6C`
* **Warning**: `#FFB545`
* **Error**: `#FF3F5F`
* **Info**: `#4A86FF`
* **Divider / Lines**: `#292838` / `#33323E`

### Layout Properties
* **Default Border Radius**: `6px`
* **App Bar**: Translucent glassmorphism background with blur.

---

## 6. Security, Background Processing & Telemetry

1. **Role-Based Access Control (RBAC)**:
   * Roles: `Superuser`, `Operator`, `Dosen` (Lecturer/Teacher), `User` (Candidate).
   * Seeded on startup via `Program.cs`.
2. **Asynchronous Background Processing**:
   * Backed by **Hangfire** on PostgreSQL (`builder.Services.AddHangfireServer()`).
   * Handles non-blocking email notifications (`EmailService.SendMailBackground`) and background operations.
3. **Telemetry & Live Proctoring**:
   * Uses `PresenceHub` to stream real-time connected user locations (lat/long), remote IP, user agent device, and connection state to the proctoring dashboard.
