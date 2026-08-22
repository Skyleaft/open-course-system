# Comprehensive Implementation Task Breakdown: LMS & Online Examination Platform with Realtime Anti-Cheat Engine

**Source Document:** [`techdoc.md`](file:///e:/repo/cs/Project-Examination/techdoc.md)  
**Architecture:** Vertical Slice Architecture (VSA) + Domain-Driven Design (DDD) Modular Monolith  
**Tech Stack:** ASP.NET Core (.NET 10), SvelteKit 2, PostgreSQL (Latest), Redis (Latest: Streams & Cache), MinIO S3, SignalR, OpenTelemetry & Jaeger  

---

## 📋 Task Matrix & Phase Overview

| Phase | Category | Description | Status |
| :--- | :--- | :--- | :---: |
| **Phase 1** | **Infrastructure & Foundations** | PostgreSQL schemas, Redis, MinIO S3, OpenTelemetry, Docker Compose | `[x]` |
| **Phase 2** | **Core Framework & Shared Layer** | VSA pipeline, DDD base abstractions, Mediator, API wrappers, S3 clients | `[x]` |
| **Phase 3** | **Identity & Access Module** | JWT rotation, RBAC, Single-device/session token guard with Redis | `[x]` |
| **Phase 4** | **Payments Module** | Orders, AccessType verification, webhook HMAC, auto-enrollment events | `[x]` |
| **Phase 5** | **Courses Module** | Course lifecycle, curriculum builder, lesson storage, assignment workflow | `[x]` |
| **Phase 6** | **Exams Module (Core Engine)** | Dual-mode quiz, PRNG Fisher-Yates shuffle, one-time token, snapshot presign | `[ ]` |
| **Phase 7** | **Realtime Anti-Cheat & SignalR Engine** | ExamHub, Redis backplane, violation broadcasts, proctor live stream | `[ ]` |
| **Phase 8** | **Assessments & Certification Module** | Redis Stream grading consumer, retry/DLQ handling, SHA-256 cert generator | `[ ]` |
| **Phase 9** | **Communications Module** | Global/Course announcements, nested discussion threads | `[ ]` |
| **Phase 10** | **Frontend Client (SvelteKit 2)** | Student portal, exam runner + anti-cheat worker, instructor & proctor apps | `[ ]` |
| **Phase 11** | **Integration, Testing & Hardening** | Unit/Domain tests, WebApplicationFactory integration tests, load tests | `[ ]` |

---

## Phase 1: Infrastructure & Orchestration Setup

- [x] **1.1. Docker Compose Stack Orchestration**
  - [x] Configure `postgres:alpine` (Latest) container with healthchecks, persistent volume `postgres_data`, and initialization scripts.
  - [x] Configure `redis:alpine` (Latest) container with `--appendonly yes`, authentication password, and healthchecks.
  - [x] Configure `minio/minio` server with API on port `9000` and Console UI on port `9001`.
  - [x] Configure `minio/mc` initialization container (`minio-init`) to create buckets: `exam-snapshots`, `course-materials`, `assignment-submissions` with private access policies.
  - [x] Configure `jaegertracing/all-in-one` with OTLP receiver enabled on port `4317` / UI on `16686`.
  - [x] Configure `otel/opentelemetry-collector-contrib` pipeline config routing OTLP traces to Jaeger.
  - [x] Wire `.NET 10 Backend Host` and `SvelteKit 2 Frontend` containers in bridge network `lms-network`.

- [x] **1.2. PostgreSQL Multi-Schema Database Initialization**
  - [x] Create schema migration scripts for multi-schema architecture: `identity`, `payments`, `courses`, `exams`, `assessments`, `communications`.
  - [x] Enable PostgreSQL extensions: `uuid-ossp`, `pgcrypto`.
  - [x] Configure schema-level access grants and connection string multi-tenant/multi-schema separation.

---

## Phase 2: Core Framework & Shared Layer (`MonoSlice.Shared`)

- [x] **2.1. Shared Abstractions (`MonoSlice.Shared.Abstractions`)**
  - [x] Implement DDD base abstractions: `AggregateRoot<TId>`, `Entity<TId>`, `ValueObject`, `IDomainEvent`, `IIntegrationEvent`.
  - [x] Implement CQRS abstractions compatible with source generator: `ICommand<TResult>`, `IQuery<TResult>`, `ICommandHandler<TCommand, TResult>`, `IQueryHandler<TQuery, TResult>`.
  - [x] Define standardized API envelope `ApiResponse<T>` and `ApiErrorResponse` with standard error codes and validation error details.
  - [x] Define inter-module contract interfaces: `IIdentityModuleApi`, `IPaymentsModuleApi`, `ICoursesModuleApi`, `IExamsModuleApi`, `IAssessmentsModuleApi`.

- [x] **2.2. Shared Infrastructure (`MonoSlice.Shared.Infrastructure`)**
  - [x] Implement MinIO S3 Object Storage Service (`IObjectStorageService`):
    - [x] `GeneratePresignedUploadUrlAsync(bucket, objectKey, expiry, contentType)`
    - [x] `GeneratePresignedDownloadUrlAsync(bucket, objectKey, expiry)`
    - [x] `DeleteObjectAsync(bucket, objectKey)`
  - [x] Implement Redis Caching & Distributed Lock helper (`ICacheService`, `IDistributedLock`).
  - [x] Implement Redis Stream Publisher abstraction (`IEventStreamPublisher`) supporting `XADD` with `MAXLEN ~ 100000`.
  - [x] Implement OpenTelemetry tracing setup (W3C TraceContext propagator, EF Core / Npgsql span injector, Redis span enricher).
  - [x] Implement Mediator Source Generator pipeline behaviors (Validation behavior, Logging behavior, Metric & Trace behavior).

---

## Phase 3: Identity & Access Management Module (`identity` schema)

- [x] **3.1. Domain & Persistence**
  - [x] Implement `User` aggregate with `GuidV7` ID, `Email`, `PasswordHash`, `FullName`, `LastSeen`, `Roles` (`Student`, `Instructor`, `Admin`, `Proctor`), `IsActive`.
  - [x] Implement `RefreshToken` entity with rotation tracking, expiry, and revocation flags.
  - [x] Implement `IdentityDbContext` targeting schema `identity` with audit timestamp auto-filling.

- [x] **3.2. Authentication & Authorization Slices**
  - [x] `POST /api/v1/auth/register`: Register new user with default `Student` role.
  - [x] `POST /api/v1/auth/login`: Issue JWT token pair (Access Token + Refresh Token). Store active session fingerprint in Redis.
  - [x] `POST /api/v1/auth/google`: Authenticate or auto-register user using Google OAuth ID token with default `Student` role, Mapster mapping, and `Guid.CreateVersion7()`.
  - [x] `POST /api/v1/auth/refresh-token`: Validate and rotate refresh token, revoking prior token.
  - [x] `POST /api/v1/auth/logout`: Revoke active refresh token and invalidate Redis session guard.
  - [x] `GET /api/v1/auth/me`: Return authenticated user profile and permissions.
  - [x] `POST /api/v1/auth/assign-role`: Admin-only slice to grant/revoke roles (`Instructor`, `Proctor`, `Admin`).

- [x] **3.3. Inter-Module API & Token Guard**
  - [x] Implement `IIdentityModuleApi` (`GetUserByIdAsync`, `ValidateUserRoleAsync`, `GetUsersByIdsAsync`).
  - [x] Implement Redis-backed Single Active Device / Tab Guard middleware.

---

## Phase 4: Payments & Checkout Module (`payments` schema)

- [x] **4.1. Domain & Persistence**
  - [x] Implement `Order` aggregate:
    - [x] Invariants: Course must be `OpenPaid`, `Amount > 0`, state machine (`Pending` $\rightarrow$ `Paid` / `Expired` / `Failed`).
    - [x] Unique index on `external_payment_reference`.
  - [x] Implement `OrderPaidDomainEvent` raised on status transition to `Paid`.
  - [x] Implement `PaymentsDbContext` targeting schema `payments`.

- [x] **4.2. Feature Slices**
  - [x] `POST /api/v1/payments/checkout`: Create new course purchase order for `OpenPaid` course.
  - [x] `POST /api/v1/payments/webhook`: Webhook handler with HMAC-SHA256 signature validation and idempotency verification.
  - [x] `GET /api/v1/payments/orders/{id}`: Query order status and payment details.

- [x] **4.3. Event-Driven Auto-Enrollment Integration**
  - [x] Implement `OrderPaidDomainEventHandler`: Publishes `OrderPaidIntegrationEvent` and invokes `ICoursesModuleApi.EnrollStudentAsync`.
  - [x] Implement `IPaymentsModuleApi` (`GetOrderByIdAsync`, `IsOrderPaidAsync`, `HasUserPurchasedCourseAsync`).

---

## Phase 5: Courses & Curriculum Module (`courses` schema)

- [x] **5.1. Domain Models & Invariants**
  - [x] Implement `Course` aggregate with `AccessType` (`OpenFree`, `OpenPaid`, `PrivateWithKey`), `Price`, `EnrollmentKeyHash`, `IsPublished`.
  - [x] Implement `CourseSection` entity with `OrderIndex`.
  - [x] Implement `Lesson` entity with `LessonType` (`Video`, `PdfDocument`, `DownloadableFile`), `ContentUrl` (MinIO path), `DurationMinutes`, `OrderIndex`.
  - [x] Implement `Assignment` entity with `DeadlineUtc` and `MaxScore`.
  - [x] Implement `AssignmentSubmission` aggregate with unique constraint `(assignment_id, student_id)`, file URL, submission timestamp.
  - [x] Implement `CourseEnrollment` aggregate with unique constraint `(user_id, course_id)`.
  - [x] Implement `CoursesDbContext` targeting schema `courses`.

- [x] **5.2. Course Management Slices (Instructor / Admin)**
  - [x] `POST /api/v1/courses`: Create draft course.
  - [x] `PUT /api/v1/courses/{id}`: Update course metadata & access rules (SHA-256 hash for `PrivateWithKey`).
  - [x] `POST /api/v1/courses/{id}/publish`: Publish course validation.
  - [x] `POST /api/v1/courses/{id}/sections`: Create section.
  - [x] `POST /api/v1/courses/sections/{sectionId}/lessons`: Create lesson with MinIO material reference.
  - [x] `POST /api/v1/courses/{id}/assignments`: Create assignment with deadline and rubric.

- [x] **5.3. Student Learning & Enrollment Slices**
  - [x] `GET /api/v1/courses`: Public course catalog with caching in Redis.
  - [x] `GET /api/v1/courses/{id}`: Course overview and syllabus preview.
  - [x] `POST /api/v1/courses/{id}/enroll`: Self-enroll for `OpenFree`, verification for `OpenPaid` via `IPaymentsModuleApi`, or validation against `PrivateWithKey` enrollment key.
  - [x] `POST /api/v1/courses/assignments/{assignmentId}/submit`: Submit assignment solution file before deadline.
  - [x] Implement `ICoursesModuleApi` (`GetCourseByIdAsync`, `IsStudentEnrolledAsync`, `EnrollStudentAsync`).
  - [x] Implement `OrderPaidIntegrationEventHandler` consuming `OrderPaidIntegrationEvent`.

---

## Phase 6: Exams Module (Dual-Mode & Core Engine) (`exams` schema)

- [x] **6.1. Domain Model & Invariants**
  - [x] Implement `QuizExam` aggregate:
    - [x] `QuizMode`: `Simulation` vs `RealExam`.
    - [x] `duration_minutes`, `passing_score`, `max_allowed_violations`, `settings` JSONB, `is_published`, `xmin`.
  - [x] Implement `QuizQuestion` entity:
    - [x] `QuestionType`: `SingleChoice`, `MultipleChoice`, `Essay`, `TrueFalse`.
    - [x] `points`, `order_index`, `options` JSONB (`id`, `text`, `isCorrect`), `explanation`.
  - [x] Implement `QuizSubmission` aggregate:
    - [x] `StartedAtUtc`, `MaxAllowedEndTimeUtc = StartedAtUtc + Duration`.
    - [x] `Status`: `InProgress`, `Completed`, `Disqualified`, `TimedOut`.
    - [x] `RandomSeed` for deterministic PRNG Fisher-Yates question/option shuffle.
    - [x] `ActiveSessionToken` (UUID) validation against Redis single-session guard.
    - [x] `Violations` JSONB audit log.
  - [x] Implement `StudentAnswer` entity with `selected_option_ids UUID[]`, `essay_text`, `awarded_score`.
  - [x] Implement `ProctoringSnapshot` entity (`storage_object_key`, `captured_at_utc`).
  - [x] Implement `ExamsDbContext` targeting schema `exams`.

- [x] **6.2. Instructor Exam Authoring Slices**
  - [x] `POST /api/v1/exams`: Create quiz/exam with mode configuration.
  - [x] `PUT /api/v1/exams/{id}`: Update quiz parameters (cannot edit questions if published and active submissions exist).
  - [x] `POST /api/v1/exams/{id}/questions`: Add questions with options and answer keys.
  - [x] `POST /api/v1/exams/{id}/publish`: Publish exam.

- [x] **6.3. Student Exam Lifecycle Slices**
  - [x] `POST /api/v1/exams/{id}/start`: Initialize exam attempt:
    - [x] Validate enrollment and prerequisites.
    - [x] Generate `RandomSeed` (PRNG) and `ActiveSessionToken`.
    - [x] Set `MaxAllowedEndTimeUtc`.
    - [x] Save token to Redis with TTL.
  - [x] `GET /api/v1/exams/submissions/{submissionId}/questions`:
    - [x] Apply Fisher-Yates shuffle with seed to questions and options.
    - [x] Strip `isCorrect` and `explanation` from response payload.
  - [x] `POST /api/v1/exams/submissions/{submissionId}/answers`: Auto-save student answers per question.
  - [x] `POST /api/v1/exams/submissions/{submissionId}/snapshots/presign`:
    - [x] Validate active session token.
    - [x] Generate MinIO Presigned PUT URL for `exam-snapshots` bucket (`2-minute expiry`).
  - [x] `POST /api/v1/exams/submissions/{submissionId}/finish`:
    - [x] Finalize submission status (`Completed`).
    - [x] Invalidate session token in Redis.
    - [x] Publish message to Redis Stream `stream:exam-events` (`XADD`).
  - [x] `GET /api/v1/exams/submissions/{submissionId}/result`: Return graded results or simulation instant explanation.

---

## Phase 7: Realtime Anti-Cheat Engine & SignalR (`ExamHub` & Proctor Stream)

- [ ] **7.1. SignalR Infrastructure & Redis Backplane**
  - [ ] Configure SignalR with StackExchange.Redis backplane in host.
  - [ ] Implement `ExamHub` and `NotificationHub` endpoints with JWT authorization.

- [ ] **7.2. Hub Implementation (`ExamHub`)**
  - [ ] Implement Client-to-Server methods:
    - [ ] `JoinExamRoom(Guid submissionId, Guid sessionToken)`: Verify token against Redis, join SignalR group `exam_{submissionId}` and proctor group `proctor_exam_{quizId}`.
    - [ ] `Heartbeat(Guid submissionId, Guid sessionToken)`: Update student liveness in Redis with expiration window.
    - [ ] `ReportViolation(Guid submissionId, string violationType, string? details)`: Record violation to database JSONB and Redis. Check if violations $\ge \text{MaxAllowedViolations}$; if so, trigger auto-disqualification and broadcast `ForceDisconnectExam("Disqualified")`.
    - [ ] `ReportSnapshotUploaded(Guid submissionId, string objectKey)`: Save `ProctoringSnapshot` record and broadcast `ProctorSnapshotReceived` to proctor group.
  - [ ] Implement Server-to-Client broadcast events:
    - [ ] `SyncTimer(long remainingSeconds, DateTime serverTimeUtc)`
    - [ ] `ViolationWarning(int currentViolationCount, int maxAllowedViolations)`
    - [ ] `ForceDisconnectExam(string terminationReason)` (`Disqualified`, `SessionReplaced`, `Timeout`)
  - [ ] Implement Server-to-Proctor monitor events:
    - [ ] `ProctorViolationAlert(Guid studentId, Guid submissionId, string violationType, int count)`
    - [ ] `ProctorSnapshotReceived(Guid studentId, string snapshotPresignedViewUrl)`

- [ ] **7.3. Proctor Control API Slices**
  - [ ] `GET /api/v1/proctor/exams/{quizId}/live-candidates`: Get active candidates in exam room with violation tallies.
  - [ ] `POST /api/v1/proctor/submissions/{submissionId}/warn`: Proctor sends custom warning to candidate.
  - [ ] `POST /api/v1/proctor/submissions/{submissionId}/force-disconnect`: Proctor forcibly disqualifies candidate.

---

## Phase 8: Assessments & Certification Module (`assessments` schema)

- [ ] **8.1. Domain & Persistence**
  - [ ] Implement `GradeRecord` entity (`item_type`: `Quiz`/`Assignment`, `score`, `max_score`, `weight_percentage`, `evaluated_at_utc`).
  - [ ] Implement `Certificate` entity:
    - [ ] `CertificateNumber` (Unique formatted identifier).
    - [ ] `CertificateHash`: Cryptographic SHA-256 hash calculated as:
      $$\text{SHA256}(\text{CertNumber} \parallel \text{StudentId} \parallel \text{CourseId} \parallel \text{FinalScore} \parallel \text{IssuedAtUtc})$$
    - [ ] `Status`: `Issued`, `Revoked`.
  - [ ] Implement `GradingDeadLetter` entity (`stream_message_id`, `submission_id`, `error_message`, `stack_trace`, `failed_at_utc`, `is_resolved`).
  - [ ] Implement `AssessmentsDbContext` targeting schema `assessments`.

- [ ] **8.2. Redis Streams Background Consumer Worker**
  - [ ] Implement `GradingBackgroundWorker` (IHostedService):
    - [ ] Read from `stream:grading-queue` with consumer group (`XREADGROUP`).
    - [ ] Extract OTel trace context from stream message metadata and create linked span.
    - [ ] Execute automated question scoring: evaluate choice options against correct keys, tally `total_score`.
    - [ ] Check if `total_score >= passing_score`; if passed, generate digital `Certificate` with SHA-256 hash.
    - [ ] Acknowledge message with `XACK`.
  - [ ] Implement Retry & Dead Letter Handling:
    - [ ] Track retry count in message header or Redis Pending Entries List (PEL).
    - [ ] On $< 3$ failures: requeue with exponential backoff delay.
    - [ ] On $\ge 3$ failures: publish to `stream:grading-dlq`, persist to `assessments.grading_dead_letters`, and acknowledge (`XACK`) main stream.

- [ ] **8.3. Certificate & Dead Letter API Slices**
  - [ ] `GET /api/v1/certificates/verify/{certificateHash}`: Public verification endpoint returning certificate metadata and authenticity status.
  - [ ] `GET /api/v1/certificates/my-certificates`: List student earned certificates.
  - [ ] `GET /api/v1/admin/assessments/dlq`: Admin query for dead letter entries.
  - [ ] `POST /api/v1/admin/assessments/dlq/{id}/re-drive`: Replay and re-process dead letter submission.

---

## Phase 9: Communications Module (`communications` schema)

- [ ] **9.1. Domain & Persistence**
  - [ ] Implement `Announcement` aggregate (`course_id` nullable for platform vs course level, `is_pinned`).
  - [ ] Implement `DiscussionThread` aggregate (`course_id`, `lesson_id` nullable, `is_closed`).
  - [ ] Implement `ThreadComment` entity with hierarchical self-referencing `parent_comment_id`.
  - [ ] Implement `CommunicationsDbContext` targeting schema `communications`.

- [ ] **9.2. Feature Slices**
  - [ ] `POST /api/v1/communications/announcements`: Create platform or course announcement (Instructor/Admin).
  - [ ] `GET /api/v1/communications/announcements`: Query announcements with filter by course.
  - [ ] `POST /api/v1/communications/threads`: Start discussion thread on course or lesson.
  - [ ] `GET /api/v1/communications/threads`: List threads with pagination.
  - [ ] `POST /api/v1/communications/threads/{id}/comments`: Post comment / nested reply (rejected if `is_closed == true`).
  - [ ] `POST /api/v1/communications/threads/{id}/close`: Close thread.

---

## Phase 10: Frontend Client Application (SvelteKit 2)

- [ ] **10.1. Architecture & Design System**
  - [ ] Setup SvelteKit 2 project structure with TypeScript, TailwindCSS / Vanilla Design Tokens (Dark Mode, Glassmorphism, Micro-animations).
  - [ ] Implement API client with automatic JWT token refresh interceptor.
  - [ ] Implement SignalR client wrapper service with automatic reconnection and heartbeat management.

- [ ] **10.2. Student Portal**
  - [ ] Course Catalog, Category Filter, and Search UI.
  - [ ] Course Details & Checkout / Enrollment flow (Free / Key / Paid).
  - [ ] Course Learning Player:
    - [ ] Syllabus navigator (Sections & Lessons).
    - [ ] MinIO Video Stream / PDF Viewer / Downloadable File downloader.
    - [ ] Lesson discussion sidebar.
  - [ ] Assignment submission component with file upload progress.
  - [ ] My Grades & Certificate verification / download view.

- [ ] **10.3. Strict Realtime Exam Runner & Anti-Cheat Engine**
  - [ ] Pre-exam environment checker:
    - [ ] Webcam & microphone permission prompt (`navigator.mediaDevices.getUserMedia`).
    - [ ] Render local Picture-in-Picture (PiP) preview (video frame is not streamed to server).
    - [ ] Fullscreen activation request (`requestFullscreen`).
  - [ ] Client Security Interceptors:
    - [ ] Window blur listener (`window.onblur`) $\rightarrow$ SignalR `ReportViolation("WindowFocusLoss")`.
    - [ ] Tab visibility detector (`visibilitychange`) $\rightarrow$ SignalR `ReportViolation("TabSwitch")`.
    - [ ] Fullscreen change listener (`fullscreenchange`) $\rightarrow$ SignalR `ReportViolation("FullscreenExit")`.
    - [ ] Context menu and keyboard shortcut lock (`contextmenu`, `Ctrl+C`, `Ctrl+V`, `Alt+Tab`, `F12` prevention).
  - [ ] Web Worker Snapshot Capture Engine:
    - [ ] Background Web Worker triggering random interval timer (30–60s).
    - [ ] Draw video frame to offscreen `canvas` $\rightarrow$ Export WebP format.
    - [ ] Request presigned URL from API $\rightarrow$ Perform direct `HTTP PUT` to MinIO.
    - [ ] Notify SignalR `ReportSnapshotUploaded`.
  - [ ] Synchronized countdown timer with server time drift compensation.
  - [ ] Question navigation palette, answer selector, and auto-save indicators.
  - [ ] Instant simulation mode review screen with answer keys and explanations.

- [ ] **10.4. Instructor Dashboard**
  - [ ] Course & curriculum builder with drag-and-drop section/lesson reordering.
  - [ ] Question bank manager & exam configuration.
  - [ ] Assignment grading & rubric assessment panel.
  - [ ] Course announcement & discussion moderation tool.

- [ ] **10.5. Live Proctor & Examination Monitoring Console**
  - [ ] Live grid view of all active candidates per examination.
  - [ ] Real-time violation alert feed with violation counter badges.
  - [ ] Candidate webcam snapshot timeline modal with presigned view links.
  - [ ] Proctor actions: Send warning popup, Force Disconnect / Disqualify candidate.

---

## Phase 11: Testing, Verification & Quality Assurance

- [ ] **11.1. Unit & Domain Tests**
  - [ ] Order status state machine and access type invariants.
  - [ ] Fisher-Yates PRNG shuffle determinism with fixed seed.
  - [ ] RealExam auto-disqualification threshold check.
  - [ ] SHA-256 certificate cryptographic hash algorithm verification.
  - [ ] BCrypt key enrollment validation.

- [ ] **11.2. Integration & Infrastructure Tests**
  - [ ] `WebApplicationFactory` multi-schema PostgreSQL migration test.
  - [ ] Redis single active session token guard rejection test.
  - [ ] Redis Streams `stream:grading-queue` worker consumption and DLQ fallback on 3 errors.
  - [ ] MinIO S3 presigned upload and download end-to-end flow.
  - [ ] SignalR `ExamHub` connection, timer synchronization, and violation broadcast test.

- [ ] **11.3. Load & Security Testing**
  - [ ] Concurrent quiz submissions grading queue stress test under load.
  - [ ] SignalR connection density & heartbeat stability test.
  - [ ] Anti-cheat client interceptor bypass resilience validation.
