Software Architecture & Technical Specification DocumentProject: LMS & Online Examination Platform with Realtime Anti-Cheat EngineArchitecture: Vertical Slice Architecture (VSA) + Domain-Driven Design (DDD) Modular MonolithTech Stack: ASP.NET Core (.NET 10), SvelteKit 2, PostgreSQL (Latest), Redis (Latest: Cache & Streams), MinIO S3, SignalR, OpenTelemetry & Jaeger1. System Overview & Architecture DesignSistem dibangun sebagai Modular Monolith berkinerja tinggi yang menggabungkan prinsip Vertical Slice Architecture (VSA) di level implementasi fitur dan Domain-Driven Design (DDD) di level core business domain. Seluruh bounded context berjalan dalam satu proses host runtime .NET 10, namun memiliki isolasi data mutlak melalui PostgreSQL Multi-Schema.
+---------------------------------------------------------------------------------------------------------+
|                                    CLIENT LAYER (SvelteKit 2 SPA/SSR)                                   |
|                                                                                                         |
|  [ Student Portal ]                 [ Instructor Dashboard ]             [ Proctor / Live Monitor ]     |
|  - Course Catalog & Checkout        - Curriculum & Content Builder       - Live Violation Stream        |
|  - Video / PDF / Lesson Player      - Question Bank & Rubrics            - Force Disconnect / Warn      |
|  - Realtime Exam Runner             - Discussion & Broadcast Manager     - Student Timer Sync           |
|                                                                                                         |
|  Client Interceptors: Fullscreen Lock | Tab Visibility Detector | Audio/Video Analyser | Snapshot Engine|
+------------------------------------+--------------------------------+-----------------------------------+
                                     │ (HTTPS REST / WSS SignalR)     │ Direct PUT (Presigned URL)
                                     ▼                                ▼
+------------------------------------+-------------------+   +--------------------------------------------+
|            ASP.NET Core (.NET 10) Host Layer           |   |       MinIO S3 Object Storage Engine       |
|                                                        |   |                                            |
|  [ In-Memory Cross-Module Bus (MediatR / Events) ]     |   |  Bucket: `exam-snapshots` (Lifecycle 30d)  |
|                                                        |   |  Bucket: `course-materials` (PDF/Video)    |
|  [ OpenTelemetry .NET 10 SDK ]                         |   |  Bucket: `assignment-submissions`          |
|    └── OTLP gRPC ──► OpenTelemetry Collector ──► Jaeger|   +--------------------------------------------+
+-------------------+----------------+-------------------+
                    │                │
                    │ Caching & Auth │ Event Streams (XADD / XREADGROUP)
                    ▼                ▼
+-------------------+----------------+--------------------------------------------------------------------+
|                                         REDIS ENGINE (Latest)                                                |
|                                                                                                         |
|  - Cache Store: Course Curriculum, Lesson Data, Fast Enrollment Lookups                                 |
|  - One-Time Session Token Guard: Single Active Device / Tab Enforcement                                 |
|  - Redis Streams (MAXLEN ~ 100k): `stream:grading-queue`, `stream:proctoring-events`                    |
|  - Dead Letter Stream (DLS): `stream:grading-dlq` (Poison Messages & Alerting)                          |
|  - SignalR Redis Backplane: Inter-instance WebSocket Sync                                               |
+------------------------------------+--------------------------------------------------------------------+
                                     │ Persistent Storage (EF Core 10 / Npgsql)
                                     ▼
+---------------------------------------------------------------------------------------------------------+
|                                    PostgreSQL Database (Latest)                                         |
|   Schemas: identity  │  payments  │  courses  │  exams  │  assessments  │  communications               |
2. Bounded Contexts & Module Responsibility
- **Identity (`identity` schema)**: Autentikasi JWT, refresh token rotation, OAuth2 (Google Register & Login via `POST /api/v1/auth/google`), manajemen akun & `LastSeen`, serta otorisasi berbasis Role (`Student`, `Instructor`, `Admin`, `Proctor`) dengan single active session guard.
- **Payments (`payments` schema)**: Pembuatan order kursus, integrasi payment gateway webhook, validasi tanda tangan (HMAC), dan idempotensi transaksi.
- **Courses (`courses` schema)**: Manajemen siklus hidup kursus, kontrol akses (OpenFree, OpenPaid, PrivateWithKey), struktur silabus (Sections, Lessons: Text/PDF/Video/File), serta Assignments & Submissions.
- **Exams (`exams` schema)**: Dual-mode quiz engine (Simulation vs RealExam), bank soal, randomisasi urutan soal berbasis PRNG seed, penerbitan One-Time Exam Token, validasi batas proctoring, dan presigning upload snapshot webcam ke MinIO.
- **Assessments (`assessments` schema)**: Pemrosesan evaluasi nilai asinkron dari Redis Streams, kalkulasi passing score, dead letter stream logging, serta penerbitan sertifikat digital ber-hash SHA-256.
- **Communications (`communications` schema)**: Pengumuman kursus (Announcements), forum diskusi bertingkat (Discussion Threads & Nested Comments).
- **Realtime**: ASP.NET Core SignalR Hubs (ExamHub, NotificationHub) dengan Redis Pub/Sub backplane untuk sinkronisasi timer ujian, broadcast pelanggaran real-time, dan deteksi diskoneksi.
  - Id: UUID
  - UserId: UUID
  - CourseId: UUID
  - Amount: Decimal
  - Currency: String
  - Status: OrderStatus (Pending, Paid, Expired, Failed)
  - ExternalPaymentReference: String? (Unique index)
  - CreatedAtUtc: DateTime
  - PaidAtUtc: DateTime?
3.2 Module: Courses
Aggregate Root 1: Course
Root: Course
Child Entities: CourseSection, Lesson, Assignment, CourseExam
Invariants:
- Tipe akses kursus (AccessType):
  - OpenFree: Terbuka tanpa syarat, user dapat langsung meng-enroll dirinya.
  - OpenPaid: Membutuhkan verifikasi pembayaran sukses dari modul Payments.
  - PrivateWithKey: Wajib menyertakan enrollment key rahasia yang dicocokkan menggunakan hash BCrypt.
- Lesson bertipe Text (default) menyimpan konten rich-text (Edra Tiptap JSON schema) pada TextContent dengan ContentUrl opsional. Lesson bertipe Video, PdfDocument, atau DownloadableFile menyimpan path/URL storage MinIO pada ContentUrl dan tidak menyimpan file binary pada database.
- Course dapat mengaitkan ujian yang dapat digunakan kembali (reusable QuizExams) melalui child entity CourseExam.

[Course Aggregate Root]
  - Id: UUID
  - Title: String
  - Description: String
  - AccessType: CourseAccessType (OpenFree, OpenPaid, PrivateWithKey)
  - Price: Decimal
  - EnrollmentKeyHash: String?
  - IsPublished: Boolean
  - CreatedAtUtc: DateTime
  - UpdatedAtUtc: DateTime?
  └── CourseSection (Entity) [1..*]
        - Id: UUID
        - Title: String
        - OrderIndex: Int
        └── Lesson (Entity) [0..*]
              - Id: UUID
              - Title: String
              - Type: LessonType (Text, Video, PdfDocument, DownloadableFile)
              - ContentUrl: String?
              - TextContent: String?
              - DurationMinutes: Int
              - OrderIndex: Int
  └── Assignment (Entity) [0..*]
        - Id: UUID
        - Title: String
        - Instruction: String
        - DeadlineUtc: DateTime
        - MaxScore: Decimal
  └── CourseExam (Entity) [0..*]
        - Id: UUID
        - CourseId: UUID
        - ExamId: UUID
        - OrderIndex: Int
        - IsMandatory: Boolean
        - CreatedAtUtc: DateTime

Aggregate Root 2: CourseEnrollment & AssignmentSubmission
Invariants:
- Satu siswa hanya boleh memiliki satu record CourseEnrollment aktif per kursus (Unique(UserId, CourseId)).
- Siswa hanya dapat mengunggah AssignmentSubmission sebelum Assignment.DeadlineUtc.

3.3 Module: Exams (Dual-Mode, Question Bank & Proctoring Engine)
Aggregate Root 1: QuestionBank (Independent Reusable Question Package / Pool)
Root: QuestionBank
Child Entities: BankQuestion
Invariants:
- QuestionBank bersifat independen dan decoupled sebagai wadah / paket kumpulan soal yang dapat digunakan berulang kali pada berbagai ujian atau kursus.
- Memiliki audit trail lengkap (CreatedBy, UpdatedBy, CreatedAtUtc, UpdatedAtUtc).
- Berisi kumpulan BankQuestion. Setiap pertanyaan memiliki tipe tertentu (SingleChoice, MultipleChoice, Essay, TrueFalse), bobot poin default, opsi jawaban (JSONB), serta penjelasan/kunci.

[QuestionBank Aggregate Root]
  - Id: UUID
  - Title: String
  - Description: String?
  - Category: String?
  - Tags: List<String>
  - CreatedBy: UUID
  - UpdatedBy: UUID?
  - CreatedAtUtc: DateTime
  - UpdatedAtUtc: DateTime?
  └── BankQuestion (Entity) [0..*]
        - Id: UUID
        - BankId: UUID
        - QuestionText: String
        - Type: QuestionType (SingleChoice, MultipleChoice, Essay, TrueFalse)
        - Points: Decimal
        - OrderIndex: Int
        - Explanation: String?
        - Options: List<QuestionOption> (JSONB)

Aggregate Root 2: QuizExam (Section-based Dual-Mode Exam Engine)
Root: QuizExam
Child Entities: QuizSection
Invariants:
- Ujian bersifat independen (tidak terikat langsung pada CourseId di level aggregate), memungkinkan satu ujian dipakai pada banyak kursus atau sebagai ujian sertifikasi mandiri.
- Memiliki audit trail (CreatedBy, UpdatedBy, CreatedAtUtc, UpdatedAtUtc).
- Ujian tersusun atas satu atau lebih QuizSection. Setiap section mereferensikan QuestionBank (paket soal) dengan nomor urut, opsi batasan jumlah soal (QuestionCount), dan bobot poin yang dapat dioverride (PointsOverride).
- Tipe mode (QuizMode):
  - Simulation: Kuis latihan, tidak ada pembatasan ganti tab, pelanggaran tidak dihitung, kunci jawaban langsung dapat dilihat setelah submit.
  - RealExam: Ujian formal berintegritas tinggi. Wajib mode Fullscreen, oncam & onmic (client-side), anti switch-tab aktif, dan batasan maksimal pelanggaran sebelum diskualifikasi.
- Ujian berstatus Published tidak dapat diubah daftar section/soal atau bobot nilainya.

[QuizExam Aggregate Root]
  - Id: UUID
  - InstructorId: UUID
  - Title: String
  - Description: String?
  - Mode: QuizMode (Simulation, RealExam)
  - DurationMinutes: Int
  - PassingScore: Decimal
  - MaxAllowedViolations: Int
  - MaxAttempts: Int
  - AvailableFromUtc: DateTime?
  - AvailableToUtc: DateTime?
  - IsPublished: Boolean
  - ShuffleQuestions: Boolean
  - ShuffleOptions: Boolean
  - CreatedBy: UUID
  - UpdatedBy: UUID?
  - CreatedAtUtc: DateTime
  - UpdatedAtUtc: DateTime?
  └── QuizSection (Entity) [1..*]
        - Id: UUID
        - ExamId: UUID
        - QuestionBankId: UUID (FK -> QuestionBank)
        - Title: String
        - Description: String?
        - OrderIndex: Int
        - PointsOverride: Decimal?
        - QuestionCount: Int?

Aggregate Root 3: QuizSubmission
Root: QuizSubmission
Child Entities / Value Objects: StudentAnswer, ProctoringViolation (VO), ProctoringSnapshot (Entity)
Invariants:
- MaxAllowedEndTimeUtc dihitung mutlak saat inisiasi sesi: $\text{StartedAtUtc} + \text{Duration}$.
- One-Time Token: Setiap submission aktif memegang satu ActiveSessionToken. Jika token di Redis berbeda dengan payload request, akses ditolak (kicked).
- Deterministic Shuffle Seed: Setiap submission menyimpan RandomSeed. Soal dari section diacak menggunakan algoritma Fisher-Yates berbasis PRNG dengan seed tersebut.
- Auto-Disqualification: Jika Mode == RealExam dan jumlah Violations $\ge \text{MaxAllowedViolations}$, status submission seketika bertransisi ke Disqualified.

[QuizSubmission Aggregate Root]
  - Id: UUID
  - ExamId: UUID
  - StudentId: UUID
  - Mode: QuizMode (Simulation, RealExam)
  - StartedAtUtc: DateTime
  - MaxAllowedEndTimeUtc: DateTime
  - FinishedAtUtc: DateTime?
  - Status: SubmissionStatus (InProgress, Completed, Disqualified, TimedOut)
  - Score: Decimal?
  - IsPassed: Boolean?
  - RandomSeed: Int
  - ActiveSessionToken: String
  - Violations: List<ViolationRecord> (JSONB)
  └── StudentAnswer (Entity) [0..*]
        - Id: UUID
        - SubmissionId: UUID
        - QuestionId: UUID (FK -> QuestionBank)
        - SelectedOptionIds: List<UUID> (JSONB)
        - EssayText: String?
        - AwardedScore: Decimal?
        - AnsweredAtUtc: DateTime
  └── ProctoringSnapshot (Entity) [0..*]
        - Id: UUID
        - SubmissionId: UUID
        - StorageKey: String
        - CapturedAtUtc: DateTime
3.4 Module: Assessments (Async Grading & Certification)Aggregate Root: GradeRecord & CertificateInvariants:Penilaian otomatis dikonsumsi dari Redis Stream stream:grading-queue.Jika proses evaluasi gagal hingga 3 kali percobaan, event dipindahkan ke stream:grading-dlq dan dicatat ke tabel grading_dead_letters.Certificate Integrity: Sertifikat memuat CertificateHash yang dihitung secara kriptografis menggunakan algoritma SHA-256:$$\text{Hash} = \text{SHA256}(\text{CertNumber} \parallel \text{StudentId} \parallel \text{CourseId} \parallel \text{FinalScore} \parallel \text{IssuedAtUtc})$$3.5 Module: CommunicationsAggregate Root: Announcement & DiscussionThreadInvariants:Pengumuman global memiliki CourseId = NULL, sedangkan pengumuman spesifik merujuk pada CourseId tertentu.Thread diskusi yang berstatus IsClosed = TRUE menolak penambahan komentar baru.4. PostgreSQL Multi-Schema Database DDL (Latest)SQL-- 1. SETUP SCHEMAS & EXTENSIONS
CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS payments;
CREATE SCHEMA IF NOT EXISTS courses;
CREATE SCHEMA IF NOT EXISTS exams;
CREATE SCHEMA IF NOT EXISTS assessments;
CREATE SCHEMA IF NOT EXISTS communications;

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
    title VARCHAR(255) NOT NULL,
    description TEXT,
    access_type VARCHAR(50) NOT NULL, -- OpenFree, OpenPaid, PrivateWithKey
    price NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    enrollment_key_hash VARCHAR(255),
    is_published BOOLEAN NOT NULL DEFAULT FALSE,
    xmin XID
);

CREATE TABLE courses.course_sections (
    id UUID PRIMARY KEY,
    course_id UUID NOT NULL REFERENCES courses.courses(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    order_index INT NOT NULL
);
CREATE INDEX idx_sections_course ON courses.course_sections(course_id);

CREATE TABLE courses.lessons (
    id UUID PRIMARY KEY,
    section_id UUID NOT NULL REFERENCES courses.course_sections(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    type VARCHAR(50) NOT NULL, -- Text, Video, PdfDocument, DownloadableFile
    content_url TEXT,
    text_content TEXT,
    duration_minutes INT NOT NULL DEFAULT 0,
    order_index INT NOT NULL
);
CREATE INDEX idx_lessons_section ON courses.lessons(section_id);

CREATE TABLE courses.assignments (
    id UUID PRIMARY KEY,
    course_id UUID NOT NULL REFERENCES courses.courses(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    instruction TEXT NOT NULL,
    deadline_utc TIMESTAMPTZ NOT NULL,
    max_score NUMERIC(5, 2) NOT NULL
);

CREATE TABLE courses.assignment_submissions (
    id UUID PRIMARY KEY,
    assignment_id UUID NOT NULL REFERENCES courses.assignments(id) ON DELETE CASCADE,
    student_id UUID NOT NULL,
    file_attachment_url TEXT NOT NULL,
    student_notes TEXT,
    submitted_at_utc TIMESTAMPTZ NOT NULL,
    score NUMERIC(5, 2),
    feedback TEXT
);
CREATE UNIQUE INDEX uq_assignment_student ON courses.assignment_submissions(assignment_id, student_id);

CREATE TABLE courses.course_enrollments (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    course_id UUID NOT NULL REFERENCES courses.courses(id) ON DELETE CASCADE,
    enrolled_at_utc TIMESTAMPTZ NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);
CREATE UNIQUE INDEX uq_enrollment_user_course ON courses.course_enrollments(user_id, course_id);

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
    finished_at_utc TIMESTAMPTZ,
    status VARCHAR(50) NOT NULL, -- InProgress, Completed, Disqualified, TimedOut
    score NUMERIC(5, 2),
    is_passed BOOLEAN,
    random_seed INT NOT NULL DEFAULT 0,
    active_session_token VARCHAR(255) NOT NULL,
    violations JSONB NOT NULL DEFAULT '[]'
);
CREATE INDEX idx_submissions_exam_student ON exams.quiz_submissions(exam_id, student_id);

CREATE TABLE exams.student_answers (
    id UUID PRIMARY KEY,
    submission_id UUID NOT NULL REFERENCES exams.quiz_submissions(id) ON DELETE CASCADE,
    question_id UUID NOT NULL REFERENCES exams.question_banks(id) ON DELETE RESTRICT,
    selected_option_ids UUID[] NOT NULL DEFAULT '{}',
    essay_text TEXT,
    awarded_score NUMERIC(5, 2),
    answered_at_utc TIMESTAMPTZ NOT NULL
);
CREATE UNIQUE INDEX uq_student_submission_question ON exams.student_answers(submission_id, question_id);

CREATE TABLE exams.proctoring_snapshots (
    id UUID PRIMARY KEY,
    submission_id UUID NOT NULL REFERENCES exams.quiz_submissions(id) ON DELETE CASCADE,
    storage_object_key VARCHAR(1000) NOT NULL,
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
5. Storage Architecture & MinIO S3 IntegrationPenyimpanan file binary menggunakan objek storage kompatibel S3 (MinIO) untuk mengeliminasi beban transmisi file pada server backend .NET.[ SvelteKit 2 Client ] 
        │ 
        │ 1. Request Presigned URL: POST /api/v1/exams/{id}/snapshots/presign
        ▼
[ ASP.NET Core Host (.NET 10) ]
        │
        │ 2. Validasi Token & Generate S3 Presigned URL (AWS SDK for .NET)
        ▼
[ MinIO S3 Storage ] ◄─── 3. Upload File Langsung via HTTP PUT (2 Menit Expiry) ─── [ SvelteKit 2 Client ]
Bucket Configuration MatrixBucket NameAccess PolicyRetention & LifecycleAllowed MIME Typesexam-snapshotsPrivateExpiration: 30 Hariimage/webp, image/jpegcourse-materialsPrivate (Signed URL)Permanentvideo/mp4, application/pdf, application/zipassignment-submissionsPrivateRetain 1 Tahunapplication/pdf, application/zip, image/*6. Realtime SignalR Protocol & Client-Side Anti-Cheat Engine6.1 Strict Anti-Cheat Flow (RealExam Mode)[ SvelteKit Exam Initializer ]
         │
         ├──► 1. navigator.mediaDevices.getUserMedia({ video: true, audio: true })
         │       Render local PiP preview (Audio/Video TIDAK di-stream ke server)
         │
         ├──► 2. document.documentElement.requestFullscreen() (Wajib Layar Penuh)
         │
         ├──► 3. Web Worker Periodic Snapshot Loop (Interval Acak: 30-60 Detik)
         │       Canvas Draw Video Frame ──► Export WebP ──► Direct PUT MinIO Presigned URL
         │
         └──► 4. Security Interceptors (Active Listeners)
                 ├── 'visibilitychange' (Tab Switch)   ──► SignalR `ReportViolation`
                 ├── 'blur' (Window Focus Loss)       ──► SignalR `ReportViolation`
                 ├── 'fullscreenchange' (Exit Screen) ──► SignalR `ReportViolation`
                 └── 'contextmenu' & Keyboard Lock    ──► e.preventDefault()
6.2 SignalR Message Contract+----------------------------------------------------------------------------------------------------+
|                                    SignalR Hub Contract: `ExamHub`                                  |
+----------------------------------------------------------------------------------------------------+
| Client-to-Server Methods:                                                                          |
| - JoinExamRoom(Guid submissionId, Guid sessionToken)                                               |
| - Heartbeat(Guid submissionId, Guid sessionToken)                                                  |
| - ReportViolation(Guid submissionId, string violationType, string? details)                       |
| - ReportSnapshotUploaded(Guid submissionId, string objectKey)                                      |
|                                                                                                    |
| Server-to-Client Broadcast Events:                                                                 |
| - SyncTimer(long remainingSeconds, DateTime serverTimeUtc)                                         |
| - ViolationWarning(int currentViolationCount, int maxAllowedViolations)                             |
| - ForceDisconnectExam(string terminationReason)  // "Disqualified" / "SessionReplaced" / "Timeout"  |
|                                                                                                    |
| Server-to-Proctor Monitor Events:                                                                  |
| - ProctorViolationAlert(Guid studentId, Guid submissionId, string violationType, int count)        |
| - ProctorSnapshotReceived(Guid studentId, string snapshotPresignedViewUrl)                         |
+----------------------------------------------------------------------------------------------------+

### 6.3 High-Concurrency Exam Answer Autosave with Redis Buffering & Auto-Flush on Timeout
Untuk menangani beban konkurensi tinggi saat ribuan peserta ujian mengklik opsi jawaban atau mengetik esai secara berkala, sistem menerapkan **Redis In-Memory Answer Buffering & Disaster Recovery**:
1. **Autosave Interception (`POST /api/v1/exams/submissions/{submissionId}/answers`)**:
   - Seluruh pembaruan jawaban peserta disimpan langsung ke dalam Redis Cache (`exam_answers:{submissionId}`) dengan TTL 4 jam.
   - **PostgreSQL tidak dipanggil (Zero DB writes)** selama proses pengerjaan ujian berlangsung, mencegah connection pool exhaustion dan disk I/O bottleneck.
2. **Reconnection & State Auto-Recovery (`GET /api/v1/exams/submissions/{submissionId}/questions`)**:
   - Jika peserta mengalami putus koneksi, browser crash, atau refresh halaman, query pengambilan soal otomatis mengambil data `exam_answers:{submissionId}` dari Redis dan mengisi kembali `SelectedOptionIds` dan `EssayText` pada setiap soal.
3. **Batch Flush on Finalization (`POST /api/v1/exams/submissions/{submissionId}/finish`)**:
   - Saat peserta menyelesaikan ujian secara normal, seluruh jawaban yang terkumpul di Redis di-flush ke entitas `QuizSubmission` dalam database PostgreSQL dalam satu transaksi atomic EF Core melalui `IExamFinalizerService`.
   - Buffer jawaban di Redis kemudian dibersihkan (`DEL exam_answers:{submissionId}`).
4. **Auto-Flush on Timeout & Abandonment (`ExamHub.Heartbeat` & Timeout Guard)**:
   - Jika peserta meninggalkan ujian dan tidak kembali hingga waktu habis (`DateTime.UtcNow > MaxAllowedEndTimeUtc`), SignalR Heartbeat atau timeout processor otomatis melakukan batch-flush terhadap seluruh jawaban yang tersimpan di Redis, menghitung nilai objektif secara otomatis, dan mengubah status submission menjadi `TimedOut`. Jawaban peserta tetap tersimpan dan dinilai.
5. **Auto-Flush on Proctor Disqualification (`POST /api/v1/proctor/submissions/{submissionId}/force-disconnect`)**:
   - Saat pengawas mendiskualifikasi peserta, seluruh jawaban di Redis tetap di-flush ke database PostgreSQL untuk keperluan jejak audit sebelum sesi dibatalkan.
6. **Dual Time Dimensions & Late-Start Capping**:
   - `QuizExam.AvailableFromUtc` & `QuizExam.AvailableToUtc`: Rentang jadwal dibukanya kuis.
   - `QuizExam.DurationMinutes`: Batas waktu countdown per attempt.
   - `QuizSubmission.MaxAllowedEndTimeUtc = Min(StartedAtUtc + DurationMinutes, AvailableToUtc)`.


7. Redis Streams Event-Driven Pipeline & DLSUntuk menjamin skalabilitas saat ribuan submission kuis terjadi secara simultan, evaluasi nilai diproses secara asinkron menggunakan Redis Streams Consumer Groups dengan kebijakan dead-letter dan stream trimming.[ Submission Finished Slice ]
             │
             ▼
      (XADD Stream) ──► stream:grading-queue (MAXLEN ~ 50000)
                              │
                              ▼
      [ Assessments Background Worker (XREADGROUP) ]
                              │
               Berhasil? ─────┴───── Gagal?
                  │                    │
                  ▼                    ▼
               (XACK)         [ Cek Retry Count di PEL ]
                                       │
                           Retry < 3 ──┴── Retry >= 3
                               │               │
                               ▼               ▼
                        (Re-queue Delay)  (XADD stream:grading-dlq)
                                               │
                                               ├──► Simpan ke `grading_dead_letters`
                                               └──► (XACK stream utama)
Trimming Strategy: Menggunakan approximate limit (MAXLEN ~ 100000) pada semua aliran event untuk menjaga alokasi memori Redis stabil di bawah beban tinggi.Poison Message Handling: Pesan pada stream:grading-dlq dapat ditinjau dan diproses ulang (re-driven) melalui endpoint administrative setelah akar permasalahan diselesaikan.8. Observability & Distributed Tracing (OpenTelemetry & Jaeger)Sistem mengadopsi standar OpenTelemetry (OTel) dengan mengekspor jejak (traces) ke OpenTelemetry Collector, yang kemudian meneruskannya ke Jaeger All-in-One.8.1 Tracing Context & Span CoverageASP.NET Core Ingress: Seluruh HTTP Request dan WebSocket handshake diberi span ID global (W3C TraceContext: traceparent).Database Spans: Npgsql & EF Core menginjeksi query SQL yang dieksekusi ke dalam trace span.Worker & Stream Spans: Saat background worker membaca pesan dari Redis Stream, span context diekstrak dari metadata stream untuk melanjutkan jejak dari request awal siswa hingga nilai selesai dikalkulasi.8.2 Jaeger Tracing Topology[ Client Request ] ──► [ .NET 10 API Host ] ──► [ PostgreSQL / Redis ]
                               │
                       (OTLP gRPC: 4317)
                               ▼
                   [ OTel Collector Daemon ]
                               │
                       (OTLP gRPC: 4317)
                               ▼
                   [ Jaeger All-in-One Engine ]
                               │
                   (Web UI: http://localhost:16686)
9. Infrastructure Orchestration (Docker Compose)Berikut konfigurasi orkestrasi lengkap seluruh komponen infrastruktur dalam satu jaringan terisolasi:YAMLservices:
  # =========================================================================
  # 1. DATABASE ENGINE (PostgreSQL - Latest)
  # =========================================================================
  postgres:
    image: postgres:alpine
    container_name: lms-postgres
    restart: unless-stopped
    environment:
      POSTGRES_DB: ${POSTGRES_DB:-lms_db}
      POSTGRES_USER: ${POSTGRES_USER:-lms_user}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-lms_secret_password}
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./init-scripts:/docker-entrypoint-initdb.d
    networks:
      - lms-network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER:-lms_user} -d ${POSTGRES_DB:-lms_db}"]
      interval: 5s
      timeout: 5s
      retries: 5

  # =========================================================================
  # 2. IN-MEMORY & STREAM ENGINE (Redis - Latest)
  # =========================================================================
  redis:
    image: redis:alpine
    container_name: lms-redis
    restart: unless-stopped
    command: ["redis-server", "--appendonly", "yes", "--requirepass", "${REDIS_PASSWORD:-redis_secret_password}"]
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    networks:
      - lms-network
    healthcheck:
      test: ["CMD", "redis-cli", "-a", "${REDIS_PASSWORD:-redis_secret_password}", "ping"]
      interval: 5s
      timeout: 3s
      retries: 5

  # =========================================================================
  # 3. S3 OBJECT STORAGE (MinIO)
  # =========================================================================
  minio:
    image: minio/minio:latest
    container_name: lms-minio
    restart: unless-stopped
    command: server /data --console-address ":9001"
    environment:
      MINIO_ROOT_USER: ${MINIO_ROOT_USER:-minioadmin}
      MINIO_ROOT_PASSWORD: ${MINIO_ROOT_PASSWORD:-minioadmin123}
    ports:
      - "9000:9000"   # S3 API
      - "9001:9001"   # Web Console UI
    volumes:
      - minio_data:/data
    networks:
      - lms-network
    healthcheck:
      test: ["CMD", "mc", "ready", "local"]
      interval: 5s
      timeout: 5s
      retries: 5

  minio-init:
    image: minio/mc:latest
    container_name: lms-minio-init
    depends_on:
      minio:
        condition: service_healthy
    networks:
      - lms-network
    entrypoint: >
      /bin/sh -c "
      mc alias set myminio http://minio:9000 ${MINIO_ROOT_USER:-minioadmin} ${MINIO_ROOT_PASSWORD:-minioadmin123};
      mc mb --ignore-existing myminio/exam-snapshots;
      mc mb --ignore-existing myminio/course-materials;
      mc mb --ignore-existing myminio/assignment-submissions;
      mc anonymous set none myminio/exam-snapshots;
      mc anonymous set none myminio/course-materials;
      mc anonymous set none myminio/assignment-submissions;
      exit 0;
      "

  # =========================================================================
  # 4. OBSERVABILITY (Jaeger & OpenTelemetry Collector)
  # =========================================================================
  jaeger:
    image: jaegertracing/all-in-one:latest
    container_name: lms-jaeger
    restart: unless-stopped
    environment:
      - COLLECTOR_OTLP_ENABLED=true
    ports:
      - "16686:16686" # Jaeger Web UI
      - "4317"        # Internal gRPC
    networks:
      - lms-network

  otel-collector:
    image: otel/opentelemetry-collector-contrib:latest
    container_name: lms-otel-collector
    restart: unless-stopped
    command: ["--config=/etc/otelcol-contrib/config.yaml"]
    volumes:
      - ./otel/otel-collector-config.yaml:/etc/otelcol-contrib/config.yaml
    ports:
      - "4317:4317" # OTLP gRPC Ingest from API Host
      - "4318:4318" # OTLP HTTP Ingest
    depends_on:
      - jaeger
    networks:
      - lms-network

  # =========================================================================
  # 5. BACKEND HOST (.NET 10 Modular Monolith)
  # =========================================================================
  backend:
    build:
      context: ../backend
      dockerfile: src/Host/Dockerfile
    image: lms-backend:latest
    container_name: lms-api
    restart: unless-stopped
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_HTTP_PORTS=8080
      - ConnectionStrings__Database=Host=postgres;Port=5432;Database=${POSTGRES_DB:-lms_db};Username=${POSTGRES_USER:-lms_user};Password=${POSTGRES_PASSWORD:-lms_secret_password};
      - ConnectionStrings__Redis=redis:6379,password=${REDIS_PASSWORD:-redis_secret_password}
      - Minio__Endpoint=minio:9000
      - Minio__PublicEndpoint=http://localhost:9000
      - Minio__AccessKey=${MINIO_ROOT_USER:-minioadmin}
      - Minio__SecretKey=${MINIO_ROOT_PASSWORD:-minioadmin123}
      - Minio__UseSSL=false
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
      - OTEL_EXPORTER_OTLP_PROTOCOL=grpc
      - OTEL_SERVICE_NAME=lms-modular-api
    ports:
      - "8080:8080"
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
      minio:
        condition: service_healthy
      otel-collector:
        condition: service_started
    networks:
      - lms-network

  # =========================================================================
  # 6. FRONTEND APPLICATION (SvelteKit 2)
  # =========================================================================
  frontend:
    build:
      context: ../frontend
      dockerfile: Dockerfile
    image: lms-frontend:latest
    container_name: lms-web
    restart: unless-stopped
    environment:
      - PUBLIC_API_URL=http://localhost:8080
      - PUBLIC_SIGNALR_URL=http://localhost:8080/hubs
      - PUBLIC_MINIO_URL=http://localhost:9000
      - PORT=3001
      - HOST=0.0.0.0
    ports:
      - "3001:3001"
    depends_on:
      - backend
    networks:
      - lms-network

networks:
  lms-network:
    driver: bridge

volumes:
  postgres_data:
  redis_data:
  minio_data: