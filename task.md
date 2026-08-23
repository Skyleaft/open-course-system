# Comprehensive Implementation Task Breakdown: LMS & Online Examination Platform with Realtime Anti-Cheat Engine

**Source Document:** [`techdoc.md`](file:///e:/repo/cs/Project-Examination/techdoc.md)  
**Architecture:** Vertical Slice Architecture (VSA) + Domain-Driven Design (DDD) Modular Monolith  
**Tech Stack:** ASP.NET Core (.NET 10), SvelteKit V3 RC, daisyUI 5, Edra, PostgreSQL, Redis, MinIO S3, SignalR, OpenTelemetry & Jaeger  

---

## 📋 Task Matrix & Phase Overview

| Phase | Category | Description | Status |
| :--- | :--- | :--- | :--- |
| **Phase 1** | **Infrastructure & Foundations** | PostgreSQL schemas, Redis, MinIO S3, OpenTelemetry, Docker Compose | `[x]` |
| **Phase 2** | **Core Framework & Shared Layer** | VSA pipeline, DDD base abstractions, Mediator, API wrappers, S3 clients | `[x]` |
| **Phase 3** | **Identity & Access Module** | JWT rotation, RBAC, Single-device/session token guard with Redis | `[x]` |
| **Phase 4** | **Payments Module** | Orders, AccessType verification, webhook HMAC, auto-enrollment events | `[x]` |
| **Phase 5** | **Courses Module** | Course lifecycle, curriculum builder, lesson storage, assignment workflow | `[x]` |
| **Phase 6** | **Exams Module (Core Engine)** | Dual-mode quiz, PRNG Fisher-Yates shuffle, one-time token, snapshot presign | `[x]` |
| **Phase 7** | **Realtime Anti-Cheat & SignalR Engine** | ExamHub, Redis backplane, violation broadcasts, proctor live stream | `[x]` |
| **Phase 8** | **Assessments & Certification Module** | Redis Stream grading consumer, retry/DLQ handling, SHA-256 cert generator | `[x]` |
| **Phase 9** | **Communications Module** | Global/Course announcements, nested discussion threads | `[x]` |
| **Phase 10** | **Frontend Client (SvelteKit V3 RC)** | Student portal, exam runner + anti-cheat worker, instructor & proctor apps | `[ ]` |
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
    - [x] Auto-restore previously buffered student answers from Redis (`exam_answers:{submissionId}`) on page refresh/reconnect.
  - [x] `POST /api/v1/exams/submissions/{submissionId}/answers`: Auto-save student answers buffered in Redis (`exam_answers:{submissionId}`) to prevent database write load.
  - [x] `POST /api/v1/exams/submissions/{submissionId}/snapshots/presign`:
    - [x] Validate active session token.
    - [x] Generate MinIO Presigned PUT URL for `exam-snapshots` bucket (`2-minute expiry`).
  - [x] `POST /api/v1/exams/submissions/{submissionId}/finish`:
    - [x] Flush buffered answers from Redis into PostgreSQL `QuizSubmission` entity via `IExamFinalizerService`.
    - [x] Finalize submission status (`Completed` or `TimedOut` on expiry or `Disqualified` on violation).
    - [x] Invalidate session token and purge answer buffer in Redis.
    - [x] Publish message to Redis Stream `stream:exam-events` (`XADD`).
  - [x] `GET /api/v1/exams/submissions/{submissionId}/result`: Return graded results or simulation instant explanation.

---

## Phase 7: Realtime Anti-Cheat Engine & SignalR (`ExamHub` & Proctor Stream)

- [x] **7.1. SignalR Infrastructure & Redis Backplane**
  - [x] Configure SignalR with StackExchange.Redis backplane in host.
  - [x] Implement `ExamHub` and `NotificationHub` endpoints with JWT authorization.

- [x] **7.2. Hub Implementation (`ExamHub`)**
  - [x] Implement Client-to-Server methods:
    - [x] `JoinExamRoom(Guid submissionId, Guid sessionToken)`: Verify token against Redis, join SignalR group `exam_{submissionId}` and proctor group `proctor_exam_{quizId}`.
    - [x] `Heartbeat(Guid submissionId, Guid sessionToken)`: Update student liveness in Redis with expiration window.
    - [x] `ReportViolation(Guid submissionId, string violationType, string? details)`: Record violation to database JSONB and Redis. Check if violations $\ge \text{MaxAllowedViolations}$; if so, trigger auto-disqualification and broadcast `ForceDisconnectExam("Disqualified")`.
    - [x] `ReportSnapshotUploaded(Guid submissionId, string objectKey)`: Save `ProctoringSnapshot` record and broadcast `ProctorSnapshotReceived` to proctor group.
  - [x] Implement Server-to-Client broadcast events:
    - [x] `SyncTimer(long remainingSeconds, DateTime serverTimeUtc)`
    - [x] `ViolationWarning(int currentViolationCount, int maxAllowedViolations)`
    - [x] `ForceDisconnectExam(string terminationReason)` (`Disqualified`, `SessionReplaced`, `Timeout`)
  - [x] Implement Server-to-Proctor monitor events:
    - [x] `ProctorViolationAlert(Guid studentId, Guid submissionId, string violationType, int count)`
    - [x] `ProctorSnapshotReceived(Guid studentId, string snapshotPresignedViewUrl)`

- [x] **7.3. Proctor Control API Slices**
  - [x] `GET /api/v1/proctor/exams/{quizId}/live-candidates`: Get active candidates in exam room with violation tallies.
  - [x] `POST /api/v1/proctor/submissions/{submissionId}/warn`: Proctor sends custom warning to candidate.
  - [x] `POST /api/v1/proctor/submissions/{submissionId}/force-disconnect`: Proctor forcibly disqualifies candidate.

---

## Phase 8: Assessments & Certification Module (`assessments` schema)

- [x] **8.1. Domain & Persistence**
  - [x] Implement `GradeRecord` entity (`item_type`: `Quiz`/`Assignment`, `score`, `max_score`, `weight_percentage`, `evaluated_at_utc`).
  - [x] Implement `Certificate` entity:
    - [x] `CertificateNumber` (Unique formatted identifier).
    - [x] `CertificateHash`: Cryptographic SHA-256 hash calculated as:
      $$\text{SHA256}(\text{CertNumber} \parallel \text{StudentId} \parallel \text{CourseId} \parallel \text{FinalScore} \parallel \text{IssuedAtUtc})$$
    - [x] `Status`: `Issued`, `Revoked`.
  - [x] Implement `GradingDeadLetter` entity (`stream_message_id`, `submission_id`, `error_message`, `stack_trace`, `failed_at_utc`, `is_resolved`).
  - [x] Implement `AssessmentsDbContext` targeting schema `assessments`.

- [x] **8.2. Redis Streams Background Consumer Worker**
  - [x] Implement `GradingBackgroundWorker` (IHostedService):
    - [x] Read from `stream:grading-queue` with consumer group (`XREADGROUP`).
    - [x] Extract OTel trace context from stream message metadata and create linked span.
    - [x] Execute automated question scoring: evaluate choice options against correct keys, tally `total_score`.
    - [x] Check if `total_score >= passing_score`; if passed, generate digital `Certificate` with SHA-256 hash.
    - [x] Acknowledge message with `XACK`.
  - [x] Implement Retry & Dead Letter Handling:
    - [x] Track retry count in message header or Redis Pending Entries List (PEL).
    - [x] On $< 3$ failures: requeue with exponential backoff delay.
    - [x] On $\ge 3$ failures: publish to `stream:grading-dlq`, persist to `assessments.grading_dead_letters`, and acknowledge (`XACK`) main stream.

- [x] **8.3. Certificate & Dead Letter API Slices**
  - [x] `GET /api/v1/certificates/verify/{certificateHash}`: Public verification endpoint returning certificate metadata and authenticity status.
  - [x] `GET /api/v1/certificates/my-certificates`: List student earned certificates.
  - [x] `GET /api/v1/certificates/{certificateNumber}`: Get certificate details by certificate number.
  - [x] `POST /api/v1/certificates/issue`: Manually issue certificate.
  - [x] `GET /api/v1/admin/assessments/dlq`: Admin query for dead letter entries.
  - [x] `POST /api/v1/admin/assessments/dlq/{id}/re-drive`: Replay and re-process dead letter submission.

---

## Phase 9: Communications Module (`communications` schema)

- [x] **9.1. Domain & Persistence**
  - [x] Implement `Announcement` aggregate (`course_id` nullable for platform vs course level, `is_pinned`).
  - [x] Implement `DiscussionThread` aggregate (`course_id`, `lesson_id` nullable, `is_closed`).
  - [x] Implement `ThreadComment` entity with hierarchical self-referencing `parent_comment_id`.
  - [x] Implement `CommunicationsDbContext` targeting schema `communications`.

- [x] **9.2. Feature Slices**
  - [x] `POST /api/v1/communications/announcements`: Create platform or course announcement (Instructor/Admin).
  - [x] `GET /api/v1/communications/announcements`: Query announcements with filter by course.
  - [x] `POST /api/v1/communications/threads`: Start discussion thread on course or lesson.
  - [x] `GET /api/v1/communications/threads`: List threads with pagination.
  - [x] `POST /api/v1/communications/threads/{id}/comments`: Post comment / nested reply (rejected if `is_closed == true`).
  - [x] `POST /api/v1/communications/threads/{id}/close`: Close thread.

---

## Phase 10: Frontend Client Application (SvelteKit V3 RC, daisyUI 5 & Edra)

- [ ] **10.1. Project Initialization & Design System Foundations**
  - [ ] Initialize SvelteKit V3 RC project in `frontend/` with TypeScript and Svelte 5 runes support.
  - [ ] Configure `vite.config.ts` (consolidated config) with `#lib` subpath alias and `$app/tsconfig`.
  - [ ] Install and configure Tailwind CSS 4 with daisyUI 5 plugin (`dark` default and `light` themes).
  - [ ] Implement Glassmorphism design system in `src/app.css` (`glass-panel`, `glass-card`, `glass-navbar`, `glass-sidebar`, `glass-modal`, `gradient-accent`).
  - [ ] Configure Google Fonts (Inter) and JetBrains Mono typography tokens.
  - [ ] Integrate Edra rich text editor (Tiptap + Svelte 5) with extensions (StarterKit, KaTeX Math, Lowlight Codeblock, Tables, TaskList, Mermaid, Callouts, Slash Commands).
  - [ ] Build reusable `RichEditor.svelte` and `RichRenderer.svelte` wrapper components.
  - [ ] Configure `@sveltejs/adapter-node` and multi-stage production `Dockerfile`.

- [ ] **10.2. Core Client Infrastructure & SignalR**
  - [ ] Implement `ApiClient` (`#lib/api/client.ts`) with typed `ApiResponse<T>` unwrapping and concurrent 401 JWT refresh deduplication.
  - [ ] Implement `AuthStore` (`#lib/stores/auth.svelte.ts`) with Svelte 5 runes (`$state`, `$derived`).
  - [ ] Implement server hooks (`hooks.server.ts`) for session cookie validation, route guards, and CSP security headers.
  - [ ] Implement SignalR connection factory with exponential backoff (`#lib/signalr/connection.ts`).
  - [ ] Implement strongly-typed `ExamHubClient` (`#lib/signalr/exam-hub.ts`) and `NotificationHubClient`.
  - [ ] Implement global Toast notification store and floating glass toast stack.

- [ ] **10.3. Layout & Shared UI Component Library**
  - [ ] Build `Navbar.svelte` with frosted glass styling, dynamic breadcrumbs, role badges, and theme switcher.
  - [ ] Build `Sidebar.svelte` with role-based navigation links and collapsed state.
  - [ ] Build `PageShell.svelte` master layout.
  - [ ] Build core UI atoms: `GlassCard`, `GlassModal`, `StatCard`, `SearchInput`, `ConfirmModal`, `EmptyState`.
  - [ ] Build `FileUpload.svelte` with direct presigned S3 upload and progress bar.

- [ ] **10.4. Authentication Flow**
  - [ ] Implement Login page (`/login`) with email/password form and Google OAuth button.
  - [ ] Implement Register page (`/register`) with client/server validation.
  - [ ] Implement centered glassmorphism auth layout with subtle glow particles.
  - [ ] Implement Logout action, Redis session guard invalidation, and token cleanup.

- [ ] **10.5. Student Portal: Course Catalog & Learning Player**
  - [ ] Course Catalog (`/courses`) with category filters, access type badges, search, and daisyUI pagination.
  - [ ] Course Details (`/courses/[id]`) with curriculum preview accordion and dynamic enrollment CTA.
  - [ ] Enrollment flow handler: `OpenFree` instant enroll, `OpenPaid` mock checkout flow, `PrivateWithKey` enrollment key modal.
  - [ ] Learning Player (`/courses/[id]/learn`) with split sidebar syllabus navigation.
  - [ ] Video Player with streaming MinIO presigned URL.
  - [ ] PDF Document Viewer and Downloadable File handler.
  - [ ] Lesson Discussion sidebar with Edra rich comment composer.

- [ ] **10.6. Student Portal: Assignments, Grades & Certificates**
  - [ ] Assignment Details & Submission view (`/courses/[id]/assignments/[assignmentId]`) with Edra instructions and deadline timer.
  - [ ] Assignment submission component with direct file upload to MinIO.
  - [ ] My Grades page (`/grades`) displaying course score breakdown.
  - [ ] Certificates page (`/certificates`) showing earned digital certificates with SHA-256 integrity hash.
  - [ ] Public Certificate Verification page (`/certificates/verify/[hash]`).

- [ ] **10.7. Strict Realtime Exam Runner & Anti-Cheat Engine**
  - [ ] Pre-Exam Checker (`/exams/[id]/start`): webcam/microphone check, local PiP video preview, fullscreen trigger.
  - [ ] Realtime Exam Runner (`/exams/submissions/[submissionId]`):
    - [ ] Synchronized countdown timer with server time drift compensation.
    - [ ] Question palette grid with answered, unanswered, and flagged indicators.
    - [ ] Question cards supporting SingleChoice, MultipleChoice, TrueFalse, and Essay (Edra editor).
  - [ ] Client Security Interceptors (RealExam mode):
    - [ ] `visibilitychange` detector $\rightarrow$ SignalR `ReportViolation("TabSwitch")`.
    - [ ] `window.onblur` detector $\rightarrow$ SignalR `ReportViolation("WindowFocusLoss")`.
    - [ ] `fullscreenchange` detector $\rightarrow$ SignalR `ReportViolation("FullscreenExit")`.
    - [ ] Context menu and key shortcut lock (`Ctrl+C`, `Ctrl+V`, `Alt+Tab`, `F12`).
  - [ ] Web Worker Snapshot Capture Engine (`snapshot.worker.ts`):
    - [ ] Randomized 30–60s interval worker.
    - [ ] Offscreen canvas frame capture $\rightarrow$ WebP export.
    - [ ] Presigned upload request $\rightarrow$ direct PUT to MinIO bucket `exam-snapshots`.
    - [ ] SignalR `ReportSnapshotUploaded` notification.
  - [ ] Redis answer buffer autosaving with debouncing (Zero DB writes) and auto-recovery on page reload.
  - [ ] Finish exam confirmation modal, stream event publication, and redirect to result.
  - [ ] Simulation Mode instant review screen with answer keys, explanations, and score summary.

- [ ] **10.8. Instructor Dashboard**
  - [ ] Course & Curriculum Builder (`/instructor/courses/create`, `/edit`):
    - [ ] Edra editor for course descriptions and lesson content.
    - [ ] Drag-and-drop section and lesson reordering.
    - [ ] Material upload (video/PDF) via presigned MinIO URLs.
  - [ ] Exam & Question Bank Builder (`/instructor/exams/create`, `/edit`):
    - [ ] Mode selector (Simulation vs RealExam), duration, passing score, max violations.
    - [ ] Question manager with Edra rich prompt/explanation formatting and option answer keys.
  - [ ] Assignment Grading Panel: submission review, score entry, and Edra feedback writer.
  - [ ] Course Announcement & Discussion moderation tools.

- [ ] **10.9. Live Proctor & Examination Monitoring Console**
  - [ ] Live candidate grid view (`/proctor/exams/[quizId]/live`) with liveness indicator and violation badges.
  - [ ] Real-time violation alert feed from SignalR `ProctorViolationAlert`.
  - [ ] Candidate webcam snapshot timeline modal with presigned view links.
  - [ ] Proctor intervention actions: Send warning popup, Force Disconnect / Disqualify candidate.

- [ ] **10.10. Communications Module UI**
  - [ ] Platform & Course Announcements list (`/announcements`) with pinned item priorities.
  - [ ] Discussion Threads listing with search and pagination.
  - [ ] Hierarchical nested comment tree with Edra composer and thread closing indicator.

- [ ] **10.11. Polish, Optimization & Container Wiring**
  - [ ] Responsive layout validation across mobile, tablet, and desktop viewports.
  - [ ] Skeleton loaders, empty state illustrations, and glassmorphic micro-animations.
  - [ ] SvelteKit V3 error boundary and fallback pages (404, 500, unauthorized).
  - [ ] Update `docker/docker-compose.yml` to wire SvelteKit V3 container with backend and MinIO networks.

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
