# Software Design Document (SDD)
## Open Course System: Customizable LMS & Online Examination Platform with Realtime Anti-Cheat Engine

**Document Version:** 2.1  
**Status:** Approved & Active  
**Architecture:** Vertical Slice Architecture (VSA) + Domain-Driven Design (DDD) Modular Monolith  
**Backend Framework:** ASP.NET Core (.NET 10)  
**Frontend Framework:** SvelteKit V3 RC (Svelte 5 Runes + Vite 8 + daisyUI 5 + Tailwind CSS 4 + Edra Rich Editor)  
**Persistence & Messaging:** PostgreSQL (Multi-Schema), Redis (Cache, Streams, Backplane), MinIO S3  
**Observability:** OpenTelemetry (OTel) + Jaeger Distributed Tracing  

---

## 1. System Overview & Architectural Topology

Sistem dibangun sebagai **Modular Monolith** berkinerja tinggi yang menggabungkan prinsip **Vertical Slice Architecture (VSA)** di level penataan fitur dan **Domain-Driven Design (DDD)** di level core business domain. Seluruh bounded context berjalan dalam satu proses host runtime .NET 10 namun terisolasi secara mutlak di tingkat database melalui **PostgreSQL Multi-Schema**.

Sebagai **Open Course System**, platform ini dirancang dengan kapabilitas kustomisasi tinggi (white-labeling, dynamic theme tokens, dynamic landing page builder, dan modular feature switchboard) sehingga dapat diadaptasi untuk institusi pendidikan, korporat, maupun penyedia pelatihan mandiri.

```
+---------------------------------------------------------------------------------------------------------+
|                                    CLIENT LAYER (SvelteKit V3 RC SPA/SSR)                               |
|                                                                                                         |
|  [ Student Portal ]                 [ Instructor & Admin Studio ]        [ Proctor / Live Monitor ]     |
|  - Course Catalog & Checkout        - Curriculum & Section Builder       - Realtime Violation Feed      |
|  - Video / PDF / Lesson Player      - Question Bank & Rubrics            - Force Disconnect / Warning   |
|  - Realtime Exam Runner             - Dynamic Theme & Brand Customizer   - Candidate Snapshot Timeline  |
|  - Certificate Verification         - Landing Section & Feature Switch   - Live Liveness Status         |
|                                                                                                         |
|  Client Interceptors: Fullscreen Lock | Tab Visibility Detector | Audio/Video Analyser | Snapshot Engine|
+------------------------------------+--------------------------------+-----------------------------------+
                                     │ (HTTPS REST / WSS SignalR)     │ Direct PUT (Presigned URL)
                                     ▼                                ▼
+------------------------------------+-------------------+   +--------------------------------------------+
|            ASP.NET Core (.NET 10) Host Layer           |   |       MinIO S3 Object Storage Engine       |
|                                                        |   |                                            |
|  [ In-Memory Cross-Module Mediator & Event Bus ]       |   |  Bucket: `exam-snapshots` (Lifecycle 30d)  |
|  [ OpenTelemetry .NET 10 SDK ]                         |   |  Bucket: `course-materials` (PDF/Video)    |
|    └── OTLP gRPC ──► OpenTelemetry Collector ──► Jaeger|   |  Bucket: `assignment-submissions`          |
|                                                        |   |  Bucket: `branding-assets` (Logos/Banners) |
+-------------------+----------------+-------------------+   +--------------------------------------------+
                    │                │
                    │ Caching & Auth │ Event Streams (XADD / XREADGROUP)
                    ▼                ▼
+-------------------+----------------+--------------------------------------------------------------------+
|                                         REDIS ENGINE (Latest)                                           |
|                                                                                                         |
|  - Cache Store: Public Customization (`customization:public`), Course Curriculum, Fast Lookups          |
|  - In-Memory Exam Buffer: `exam_answers:{submissionId}` (Zero DB writes during active exam)             |
|  - One-Time Session Token Guard: Single Active Device / Tab Enforcement                                 |
|  - Redis Streams (MAXLEN ~ 100k): `stream:grading-queue`, `stream:proctoring-events`                    |
|  - Dead Letter Stream (DLS): `stream:grading-dlq` (Poison Messages & Alerting)                          |
|  - SignalR Redis Backplane: Inter-instance WebSocket Sync                                               |
+------------------------------------+--------------------------------------------------------------------+
                                     │ Persistent Storage (EF Core 10 / Npgsql)
                                     ▼
+---------------------------------------------------------------------------------------------------------+
|                                    PostgreSQL Database (Multi-Schema)                                    |
|   Schemas: identity  │  payments  │  courses  │  exams  │  assessments  │  communications │ customization   |
+---------------------------------------------------------------------------------------------------------+
```

---

## 2. Bounded Contexts & Module Responsibilities

1. **Identity (`identity` schema)**:
   - Autentikasi JWT dengan refresh token rotation.
   - OAuth2 Google Register & Login (`POST /api/v1/auth/google`).
   - Role-Based Access Control (`Student`, `Instructor`, `Admin`, `Proctor`).
   - Single Active Device/Session guard berbasis Redis.

2. **Payments (`payments` schema)**:
   - Pembuatan order kursus `OpenPaid`.
   - Webhook payment gateway dengan verifikasi tanda tangan HMAC-SHA256.
   - Idempotensi transaksi dan publishing `OrderPaidIntegrationEvent`.

3. **Courses / Catalog (`courses` schema)**:
   - Manajemen siklus hidup kursus dan akses (`OpenFree`, `OpenPaid`, `PrivateWithKey`).
   - Silabus modular: Sections $\rightarrow$ Lessons (Text, Video, PDF, Downloadable Files).
   - Penugasan (Assignments) & Submissions.
   - **Course-Exam Association**: Mengaitkan satu atau lebih `QuizExam` yang reusable ke dalam kurikulum kursus melalui entitas `CourseExam`.

4. **Exams (`exams` schema)**:
   - **Question Bank Engine**: Pool bank soal mandiri (`QuestionBank`), dikategorisasi dan ditandai (tags) dengan audit trail (`CreatedBy`, `UpdatedBy`), mendukung reusabilitas lintas ujian.
   - **Section-Based Exam Engine**: Ujian (`QuizExam`) mandiri (tanpa dependensi statis ke satu kursus), tersusun dari `QuizSection` yang mereferensikan pertanyaan dari `QuestionBank`.
   - **Dual-Mode Engine**: `Simulation` (bebas tab switch, feedback instan) vs `RealExam` (strict fullscreen, oncam/onmic, anti-cheat, diskualifikasi otomatis).
   - **Deterministic PRNG Fisher-Yates Shuffle**: Seed acak disimpan di sesi submission untuk pengacakan konsisten.
   - **High-Concurrency Redis Autosave**: Penampungan jawaban di cache Redis selama ujian; batch-flush ke PostgreSQL saat submit atau timeout.

5. **Assessments (`assessments` schema)**:
   - Background worker mengonsumsi `stream:grading-queue` dari Redis Stream.
   - Dead-letter stream (`stream:grading-dlq`) & retry exponential backoff.
   - Penerbitan sertifikat digital ber-hash kriptografis SHA-256.

6. **Communications (`communications` schema)**:
   - Pengumuman global dan spesifik kursus (Announcements).
   - Forum diskusi bertingkat (Discussion Threads & Nested Comments).

7. **Realtime Proctoring & SignalR**:
   - `ExamHub` untuk sinkronisasi timer, heartbeat, broadcast pelanggaran, dan direct proctor commands (warning / force disconnect).
   - `NotificationHub` untuk broadcast sistem dan notifikasi nilai.

8. **Customization & Website Settings (`customization` schema)**:
   - Manajemen identitas branding platform (Site Name, Tagline, Favicon, Light/Dark Logos, Footer copyright).
   - Dynamic Theme & Styling Engine (OKLCH color palettes, daisyUI themes, typography font choices, glassmorphism overrides).
   - Modular Feature Switchboard (Public catalog, registration mode, payment gateway activation, certificate issuance, proctoring defaults, maintenance mode).
   - Landing Page Section Builder (Hero, Stats, Featured Courses, Feature Matrix, Testimonials, FAQ, CTA).
   - High-speed Redis caching (`customization:public`) dengan instant invalidation dan SSR hydration di SvelteKit.

---

## 3. Domain Aggregate Models & Invariants

```
+---------------------------------------------------------------------------------------------------+
|                                       DOMAIN AGGREGATE TREES                                      |
+---------------------------------------------------------------------------------------------------+
|                                                                                                   |
|  [COURSES BOUNDED CONTEXT]                                                                        |
|  Course (Aggregate Root)                                                                          |
|  ├── CourseSection (Entity) [1..*]                                                                |
|  │   └── Lesson (Entity) [0..*]                                                                   |
|  ├── Assignment (Entity) [0..*]                                                                   |
|  └── CourseExam (Entity) [0..*] (Ref -> ExamId)                                                    |
|                                                                                                   |
|  CourseEnrollment (Aggregate Root)                                                                |
|  AssignmentSubmission (Aggregate Root)                                                            |
|                                                                                                   |
|  -----------------------------------------------------------------------------------------------  |
|                                                                                                   |
|  [EXAMS BOUNDED CONTEXT]                                                                          |
|  QuestionBank (Aggregate Root - Question Package)                                                 |
|  - Id, Title, Description, Category, Tags, CreatedBy, UpdatedBy, CreatedAtUtc, UpdatedAtUtc      |
|  └── BankQuestion (Entity) [0..*]                                                                |
|      - Id, BankId, QuestionText, Type, Points, OrderIndex, Explanation, Options (JSONB)           |
|                                                                                                   |
|  QuizExam (Aggregate Root)                                                                        |
|  - Id, InstructorId, Title, Description, Mode, DurationMinutes, PassingScore, MaxAllowedViolations|
|  - MaxAttempts, AvailableFromUtc, AvailableToUtc, IsPublished, ShuffleQuestions, ShuffleOptions   |
|  - CreatedBy, UpdatedBy, CreatedAtUtc, UpdatedAtUtc                                               |
|  └── QuizSection (Entity) [1..*]                                                                  |
|      - Id, ExamId, QuestionBankId (FK -> QuestionBank), Title, Description, OrderIndex            |
|      - PointsOverride, QuestionCount                                                             |
|                                                                                                   |
|  QuizSubmission (Aggregate Root)                                                                  |
|  - Id, ExamId, StudentId, Mode, StartedAtUtc, MaxAllowedEndTimeUtc, FinishedAtUtc, Status         |
|  - Score, IsPassed, RandomSeed, ActiveSessionToken, Violations (JSONB)                            |
|  ├── StudentAnswer (Entity) [0..*] (Ref -> BankQuestion.Id)                                       |
|  └── ProctoringSnapshot (Entity) [0..*]                                                           |
|                                                                                                   |
|  -----------------------------------------------------------------------------------------------  |
|                                                                                                   |
|  [CUSTOMIZATION BOUNDED CONTEXT]                                                                  |
|  SiteSetting (Aggregate Root)                                                                     |
|  - Id, Category, SettingKey, Value (JSONB), IsPublic, Description, UpdatedBy, UpdatedAtUtc         |
|                                                                                                   |
|  LandingSection (Aggregate Root)                                                                  |
|  - Id, SectionType, Title, Subtitle, OrderIndex, IsActive, Config (JSONB), CreatedAtUtc            |
|                                                                                                   |
|  SettingsAuditLog (Audit Record)                                                                  |
|  - Id, SettingKey, OldValue (JSONB), NewValue (JSONB), ChangedBy, ChangedAtUtc, IpAddress         |
|                                                                                                   |
+---------------------------------------------------------------------------------------------------+
```

### 3.1 Course Aggregate
- **Root**: `Course`
- **Child Entities**: `CourseSection`, `Lesson`, `Assignment`, `CourseExam`
- **Invariants**:
  - `AccessType.OpenPaid` mewajibkan `Price > 0`.
  - `AccessType.PrivateWithKey` mewajibkan `EnrollmentKeyHash` terisi.
  - Kursus dapat menautkan ujian mandiri melalui entitas `CourseExam` (`ExamId`, `OrderIndex`, `IsMandatory`). Ujian yang sama dapat ditautkan ke lebih dari satu kursus.

### 3.2 QuestionBank Aggregate
- **Root**: `QuestionBank`
- **Child Entities**: `BankQuestion`
- **Invariants**:
  - Merepresentasikan wadah/paket kumpulan soal yang mandiri dan decoupled dari ujian atau kursus tertentu.
  - Memiliki audit tracking (`CreatedBy`, `UpdatedBy`, `CreatedAtUtc`, `UpdatedAtUtc`).
  - Mengelola daftar `BankQuestion`. Setiap pertanyaan menyimpan default points, options JSONB, explanation, serta orderIndex.
  - Opsi jawaban minimal 2 untuk pilihan ganda, dan tepat 1 kunci benar untuk `SingleChoice` & `TrueFalse`.

### 3.3 QuizExam Aggregate
- **Root**: `QuizExam`
- **Child Entities**: `QuizSection`
- **Invariants**:
  - Tidak memiliki dependensi langsung terhadap `CourseId`, sehingga reusable.
  - Memiliki audit tracking (`CreatedBy`, `UpdatedBy`).
  - Terdiri dari satu atau lebih `QuizSection`. Setiap section mereferensikan paket `QuestionBank` dengan urutan, opsi `PointsOverride` (override nilai soal di tingkat seksi), dan batas jumlah soal `QuestionCount`.
  - Ujian berstatus `IsPublished = true` tidak dapat dimodifikasi struktur section dan soalnya jika telah memiliki submission aktif.

### 3.4 QuizSubmission Aggregate
- **Root**: `QuizSubmission`
- **Child Entities**: `StudentAnswer`, `ProctoringSnapshot`
- **Invariants**:
  - `MaxAllowedEndTimeUtc = StartedAtUtc + DurationMinutes`.
  - Satu submission memegang satu `ActiveSessionToken`. Jika token di Redis berbeda, akses langsung ditolak.
  - Pengacakan soal dan opsi jawaban menggunakan PRNG Fisher-Yates berbasis `RandomSeed`.
  - Pada mode `RealExam`, jika `Violations.Count >= MaxAllowedViolations`, submission otomatis berstatus `Disqualified`.

### 3.5 SiteSetting Aggregate
- **Root**: `SiteSetting`
- **Invariants**:
  - `SettingKey` unik di seluruh sistem (misal: `branding.general`, `theme.styling`, `features.toggles`, `security.proctoring_defaults`, `localization.general`).
  - Nilai konfigurasi disimpan sebagai JSONB dengan skema ter-validasi per `Category`.
  - Properti `IsPublic = true` mengekspos setting ke endpoint publik tanpa memerlukan autentikasi. Setting privat (seperti secret keys, internal proctoring thresholds) dilindungi otorisasi `Admin`.
  - Setiap pembaruan setting otomatis merekam snapshot ke `SettingsAuditLog` dan menginvalidasi cache Redis `customization:public`.

### 3.6 LandingSection Aggregate
- **Root**: `LandingSection`
- **Invariants**:
  - `SectionType` didukung: `Hero`, `StatsCounter`, `FeaturedCourses`, `FeaturesGrid`, `Testimonials`, `FaqAccordion`, `CtaBanner`.
  - `OrderIndex` menentukan posisi rendering pada landing page publik.
  - Bagian non-aktif (`IsActive = false`) difilter secara otomatis dari endpoint publik.

---

## 4. PostgreSQL Multi-Schema Database DDL

```sql
-- 1. SETUP SCHEMAS & EXTENSIONS
CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS payments;
CREATE SCHEMA IF NOT EXISTS courses;
CREATE SCHEMA IF NOT EXISTS exams;
CREATE SCHEMA IF NOT EXISTS assessments;
CREATE SCHEMA IF NOT EXISTS communications;
CREATE SCHEMA IF NOT EXISTS customization;

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ============================================================================
-- SCHEMA: payments
-- ============================================================================
CREATE TABLE payments.orders (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    course_id UUID NOT NULL,
    amount NUMERIC(12, 2) NOT NULL,
    currency VARCHAR(10) NOT NULL DEFAULT 'IDR',
    status VARCHAR(50) NOT NULL, -- Pending, Paid, Expired, Failed
    external_payment_reference VARCHAR(255),
    created_at_utc TIMESTAMPTZ NOT NULL,
    paid_at_utc TIMESTAMPTZ
);
CREATE INDEX idx_orders_user ON payments.orders(user_id);
CREATE UNIQUE INDEX uq_orders_ext_ref ON payments.orders(external_payment_reference) WHERE external_payment_reference IS NOT NULL;

-- ============================================================================
-- SCHEMA: courses
-- ============================================================================
CREATE TABLE courses.courses (
    id UUID PRIMARY KEY,
    instructor_id UUID NOT NULL,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    thumbnail_url VARCHAR(1000),
    access_type VARCHAR(50) NOT NULL, -- OpenFree, OpenPaid, PrivateWithKey
    price NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    enrollment_key_hash VARCHAR(255),
    is_published BOOLEAN NOT NULL DEFAULT FALSE,
    created_at_utc TIMESTAMPTZ NOT NULL,
    updated_at_utc TIMESTAMPTZ,
    xmin XID
);
CREATE INDEX idx_courses_instructor ON courses.courses(instructor_id);
CREATE INDEX idx_courses_published ON courses.courses(is_published);

CREATE TABLE courses.sections (
    id UUID PRIMARY KEY,
    course_id UUID NOT NULL REFERENCES courses.courses(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    order_index INT NOT NULL DEFAULT 1
);
CREATE INDEX idx_sections_course ON courses.sections(course_id);

CREATE TABLE courses.lessons (
    id UUID PRIMARY KEY,
    section_id UUID NOT NULL REFERENCES courses.sections(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    type VARCHAR(50) NOT NULL, -- Text, Video, PdfDocument, DownloadableFile
    content_url VARCHAR(1000),
    text_content TEXT,
    duration_minutes INT NOT NULL DEFAULT 0,
    order_index INT NOT NULL DEFAULT 1,
    created_at_utc TIMESTAMPTZ NOT NULL
);
CREATE INDEX idx_lessons_section ON courses.lessons(section_id);

CREATE TABLE courses.assignments (
    id UUID PRIMARY KEY,
    course_id UUID NOT NULL REFERENCES courses.courses(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    instruction TEXT NOT NULL,
    deadline_utc TIMESTAMPTZ NOT NULL,
    max_score NUMERIC(5, 2) NOT NULL DEFAULT 100.00,
    created_at_utc TIMESTAMPTZ NOT NULL
);
CREATE INDEX idx_assignments_course ON courses.assignments(course_id);

CREATE TABLE courses.assignment_submissions (
    id UUID PRIMARY KEY,
    assignment_id UUID NOT NULL REFERENCES courses.assignments(id) ON DELETE CASCADE,
    student_id UUID NOT NULL,
    file_url VARCHAR(1000) NOT NULL,
    submitted_at_utc TIMESTAMPTZ NOT NULL,
    score NUMERIC(5, 2),
    feedback TEXT,
    graded_at_utc TIMESTAMPTZ
);
CREATE UNIQUE INDEX uq_assignment_student ON courses.assignment_submissions(assignment_id, student_id);

CREATE TABLE courses.enrollments (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    course_id UUID NOT NULL REFERENCES courses.courses(id) ON DELETE CASCADE,
    enrolled_at_utc TIMESTAMPTZ NOT NULL
);
CREATE UNIQUE INDEX uq_enrollment_user_course ON courses.enrollments(user_id, course_id);
CREATE INDEX idx_enrollments_user ON courses.enrollments(user_id);

CREATE TABLE courses.course_exams (
    id UUID PRIMARY KEY,
    course_id UUID NOT NULL REFERENCES courses.courses(id) ON DELETE CASCADE,
    exam_id UUID NOT NULL,
    order_index INT NOT NULL DEFAULT 1,
    is_mandatory BOOLEAN NOT NULL DEFAULT TRUE,
    created_at_utc TIMESTAMPTZ NOT NULL
);
CREATE INDEX idx_course_exams_course ON courses.course_exams(course_id);
CREATE INDEX idx_course_exams_exam ON courses.course_exams(exam_id);

-- ============================================================================
-- SCHEMA: exams
-- ============================================================================
CREATE TABLE exams.question_banks (
    id UUID PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    category VARCHAR(100),
    tags TEXT[] NOT NULL DEFAULT '{}',
    created_by UUID NOT NULL,
    updated_by UUID,
    created_at_utc TIMESTAMPTZ NOT NULL,
    updated_at_utc TIMESTAMPTZ
);
CREATE INDEX idx_question_banks_created_by ON exams.question_banks(created_by);
CREATE INDEX idx_question_banks_category ON exams.question_banks(category);

CREATE TABLE exams.bank_questions (
    id UUID PRIMARY KEY,
    bank_id UUID NOT NULL REFERENCES exams.question_banks(id) ON DELETE CASCADE,
    question_text TEXT NOT NULL,
    type VARCHAR(50) NOT NULL, -- SingleChoice, MultipleChoice, Essay, TrueFalse
    points NUMERIC(5, 2) NOT NULL DEFAULT 1.00,
    order_index INT NOT NULL DEFAULT 1,
    explanation TEXT,
    options JSONB NOT NULL DEFAULT '[]' -- Array of { id: UUID, text: string, isCorrect: boolean }
);
CREATE INDEX idx_bank_questions_bank ON exams.bank_questions(bank_id);

CREATE TABLE exams.quiz_exams (
    id UUID PRIMARY KEY,
    instructor_id UUID NOT NULL,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    mode VARCHAR(50) NOT NULL, -- Simulation, RealExam
    duration_minutes INT NOT NULL DEFAULT 60,
    passing_score NUMERIC(5, 2) NOT NULL DEFAULT 70.00,
    max_allowed_violations INT NOT NULL DEFAULT 3,
    max_attempts INT NOT NULL DEFAULT 1,
    available_from_utc TIMESTAMPTZ,
    available_to_utc TIMESTAMPTZ,
    is_published BOOLEAN NOT NULL DEFAULT FALSE,
    shuffle_questions BOOLEAN NOT NULL DEFAULT TRUE,
    shuffle_options BOOLEAN NOT NULL DEFAULT TRUE,
    created_by UUID NOT NULL,
    updated_by UUID,
    created_at_utc TIMESTAMPTZ NOT NULL,
    updated_at_utc TIMESTAMPTZ,
    xmin XID
);
CREATE INDEX idx_exams_instructor ON exams.quiz_exams(instructor_id);
CREATE INDEX idx_exams_is_published ON exams.quiz_exams(is_published);
CREATE INDEX idx_exams_created_by ON exams.quiz_exams(created_by);

CREATE TABLE exams.quiz_sections (
    id UUID PRIMARY KEY,
    exam_id UUID NOT NULL REFERENCES exams.quiz_exams(id) ON DELETE CASCADE,
    question_bank_id UUID NOT NULL REFERENCES exams.question_banks(id) ON DELETE RESTRICT,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    order_index INT NOT NULL DEFAULT 1,
    points_override NUMERIC(5, 2),
    question_count INT
);
CREATE INDEX idx_quiz_sections_exam ON exams.quiz_sections(exam_id);
CREATE INDEX idx_quiz_sections_bank ON exams.quiz_sections(question_bank_id);

CREATE TABLE exams.quiz_submissions (
    id UUID PRIMARY KEY,
    exam_id UUID NOT NULL REFERENCES exams.quiz_exams(id) ON DELETE CASCADE,
    student_id UUID NOT NULL,
    mode VARCHAR(50) NOT NULL,
    started_at_utc TIMESTAMPTZ NOT NULL,
    max_allowed_end_time_utc TIMESTAMPTZ NOT NULL,
    submitted_at_utc TIMESTAMPTZ,
    status VARCHAR(50) NOT NULL, -- InProgress, Completed, Disqualified, TimedOut
    score NUMERIC(5, 2),
    is_passed BOOLEAN,
    random_seed INT NOT NULL DEFAULT 0,
    active_session_token VARCHAR(255) NOT NULL,
    violations JSONB NOT NULL DEFAULT '[]'
);
CREATE INDEX idx_submissions_exam_student ON exams.quiz_submissions(exam_id, student_id);
CREATE INDEX idx_submissions_student ON exams.quiz_submissions(student_id);
CREATE INDEX idx_submissions_status ON exams.quiz_submissions(status);

CREATE TABLE exams.student_answers (
    id UUID PRIMARY KEY,
    submission_id UUID NOT NULL REFERENCES exams.quiz_submissions(id) ON DELETE CASCADE,
    question_id UUID NOT NULL REFERENCES exams.question_banks(id) ON DELETE RESTRICT,
    selected_option_ids JSONB NOT NULL DEFAULT '[]',
    essay_text TEXT,
    awarded_score NUMERIC(5, 2),
    answered_at_utc TIMESTAMPTZ NOT NULL
);
CREATE UNIQUE INDEX uq_student_submission_question ON exams.student_answers(submission_id, question_id);

CREATE TABLE exams.proctoring_snapshots (
    id UUID PRIMARY KEY,
    submission_id UUID NOT NULL REFERENCES exams.quiz_submissions(id) ON DELETE CASCADE,
    storage_key VARCHAR(1000) NOT NULL,
    captured_at_utc TIMESTAMPTZ NOT NULL
);
CREATE INDEX idx_snapshots_sub ON exams.proctoring_snapshots(submission_id);

-- ============================================================================
-- SCHEMA: assessments
-- ============================================================================
CREATE TABLE assessments.grade_records (
    id UUID PRIMARY KEY,
    student_id UUID NOT NULL,
    course_id UUID NOT NULL,
    item_type VARCHAR(50) NOT NULL, -- Quiz, Assignment
    reference_id UUID NOT NULL,
    score NUMERIC(5, 2) NOT NULL,
    max_score NUMERIC(5, 2) NOT NULL,
    weight_percentage NUMERIC(5, 2) NOT NULL DEFAULT 100.00,
    evaluated_at_utc TIMESTAMPTZ NOT NULL
);
CREATE INDEX idx_grades_student ON assessments.grade_records(student_id, course_id);

CREATE TABLE assessments.certificates (
    id UUID PRIMARY KEY,
    certificate_number VARCHAR(100) NOT NULL UNIQUE,
    student_id UUID NOT NULL,
    course_id UUID NOT NULL,
    final_score NUMERIC(5, 2) NOT NULL,
    certificate_hash VARCHAR(64) NOT NULL UNIQUE,
    status VARCHAR(50) NOT NULL, -- Issued, Revoked
    issued_at_utc TIMESTAMPTZ NOT NULL
);
CREATE UNIQUE INDEX uq_cert_student_course ON assessments.certificates(student_id, course_id);

CREATE TABLE assessments.grading_dead_letters (
    id UUID PRIMARY KEY,
    stream_message_id VARCHAR(100) NOT NULL,
    submission_id UUID NOT NULL,
    error_message TEXT NOT NULL,
    stack_trace TEXT,
    failed_at_utc TIMESTAMPTZ NOT NULL,
    is_resolved BOOLEAN NOT NULL DEFAULT FALSE
);

-- ============================================================================
-- SCHEMA: communications
-- ============================================================================
CREATE TABLE communications.announcements (
    id UUID PRIMARY KEY,
    course_id UUID, -- NULL = Global Platform Announcement
    author_id UUID NOT NULL,
    title VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    is_pinned BOOLEAN NOT NULL DEFAULT FALSE,
    created_at_utc TIMESTAMPTZ NOT NULL
);
CREATE INDEX idx_announcements_course ON communications.announcements(course_id);

CREATE TABLE communications.discussion_threads (
    id UUID PRIMARY KEY,
    course_id UUID NOT NULL,
    lesson_id UUID, -- NULL = Course-level General Thread
    author_id UUID NOT NULL,
    title VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    is_closed BOOLEAN NOT NULL DEFAULT FALSE,
    created_at_utc TIMESTAMPTZ NOT NULL
);
CREATE INDEX idx_threads_course ON communications.discussion_threads(course_id);

CREATE TABLE communications.thread_comments (
    id UUID PRIMARY KEY,
    thread_id UUID NOT NULL REFERENCES communications.discussion_threads(id) ON DELETE CASCADE,
    author_id UUID NOT NULL,
    parent_comment_id UUID REFERENCES communications.thread_comments(id) ON DELETE CASCADE,
    content TEXT NOT NULL,
    created_at_utc TIMESTAMPTZ NOT NULL
);
CREATE INDEX idx_comments_thread ON communications.thread_comments(thread_id);

-- ============================================================================
-- SCHEMA: customization
-- ============================================================================
CREATE TABLE customization.site_settings (
    id UUID PRIMARY KEY,
    category VARCHAR(50) NOT NULL, -- 'Branding', 'Theme', 'Features', 'Localization', 'Security', 'Landing'
    setting_key VARCHAR(100) NOT NULL UNIQUE,
    value JSONB NOT NULL,
    is_public BOOLEAN NOT NULL DEFAULT FALSE,
    description TEXT,
    updated_by UUID,
    updated_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    xmin XID
);
CREATE INDEX idx_settings_category ON customization.site_settings(category);
CREATE INDEX idx_settings_public ON customization.site_settings(is_public);

CREATE TABLE customization.landing_sections (
    id UUID PRIMARY KEY,
    section_type VARCHAR(50) NOT NULL, -- 'Hero', 'StatsCounter', 'FeaturedCourses', 'FeaturesGrid', 'Testimonials', 'FaqAccordion', 'CtaBanner'
    title VARCHAR(255),
    subtitle TEXT,
    order_index INT NOT NULL DEFAULT 1,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    config JSONB NOT NULL DEFAULT '{}',
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at_utc TIMESTAMPTZ
);
CREATE INDEX idx_landing_sections_active ON customization.landing_sections(is_active, order_index);

CREATE TABLE customization.settings_audit_logs (
    id UUID PRIMARY KEY,
    setting_key VARCHAR(100) NOT NULL,
    old_value JSONB,
    new_value JSONB NOT NULL,
    changed_by UUID NOT NULL,
    changed_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ip_address VARCHAR(45)
);
CREATE INDEX idx_settings_audit_key ON customization.settings_audit_logs(setting_key);
```

---

## 5. High-Concurrency Exam Buffering & Anti-Cheat Protocol

### 5.1 Autosave with Redis Buffering & Disaster Recovery
1. **Zero-DB Autosave (`POST /api/v1/exams/submissions/{submissionId}/answers`)**:
   - Seluruh jawaban yang diisi peserta dikirim ke endpoint autosave dan disimpan langsung ke Redis key `exam_answers:{submissionId}` (Hash / JSON) dengan TTL 4 jam.
   - Tidak ada operasi I/O database PostgreSQL selama pengerjaan ujian.
2. **Reconnection Recovery (`GET /api/v1/exams/submissions/{submissionId}/questions`)**:
   - Saat halaman direfresh atau browser crash, query ini mengambil daftar soal teracak sekaligus memuat buffer jawaban dari Redis.
3. **Atomic Flush on Finalization (`POST /api/v1/exams/submissions/{submissionId}/finish`)**:
   - Jawaban dari Redis di-flush ke tabel `exams.student_answers` dalam satu transaksi PostgreSQL atomic EF Core.
   - Redis buffer dihapus (`DEL exam_answers:{submissionId}`).
4. **Auto-Flush on Timeout / Abandonment**:
   - Jika waktu habis (`DateTime.UtcNow > MaxAllowedEndTimeUtc`), SignalR heartbeat atau background timeout checker mengeksekusi batch flush dan auto-grading.

### 5.2 Deterministic Fisher-Yates Question & Option Shuffle
- Saat memulai attempt (`StartExam`), dihasilkan `randomSeed = Random.Shared.Next()`.
- Algoritma PRNG Fisher-Yates mengacak urutan section, daftar soal dari Question Bank, dan opsi pilihan ganda secara deterministik menggunakan seed tersebut sehingga urutan konsisten pada setiap query ulang sesi siswa.

---

## 6. SvelteKit V3 RC Frontend Architecture

- **Path Aliasing**: Menggunakan `#lib` untuk seluruh shared services, API wrappers, dan UI components.
- **Design System & Dynamic Theming**: Glassmorphism (`backdrop-blur-xl`, translucent surface, vibrant glow borders) didukung oleh daisyUI 5 & Tailwind CSS 4. Token OKLCH dan variabel warna diinjeksikan secara reaktif dari payload `customization:public`.
- **White-Label & Dynamic Metadata**: Site title, favicon, logo (light & dark mode), hero banners, dan footer copyright dimuat saat SSR di `+layout.server.ts` tanpa latency.
- **Rich Text Engine**: Edra (Tiptap + Svelte 5 Runes) untuk editing soal, LaTeX KaTeX formulas, code syntax highlighting, dan callouts.
- **Anti-Cheat Loop**: Web Worker snapshot generator acak (30–60s) dengan direct PUT ke MinIO presigned URL, fullscreen lock, dan tab visibility listeners.
- **Modular Admin Customization Studio**: Antarmuka visual untuk mengubah palet warna, konfigurasi landing page builder, toggle modul/fitur, dan upload aset brand ke MinIO.
