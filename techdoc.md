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
- **Courses (`courses` schema)**: Manajemen siklus hidup kursus, kontrol akses (OpenFree, OpenPaid, PrivateWithKey), struktur silabus (Sections, Lessons: PDF/Video/File), serta Assignments & Submissions.
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
3.2 Module: CoursesAggregate Root 1: CourseRoot: CourseChild Entities: CourseSection, Lesson, AssignmentInvariants:Tipe akses kursus (AccessType):OpenFree: Terbuka tanpa syarat, user dapat langsung meng-enroll dirinya.OpenPaid: Membutuhkan verifikasi pembayaran sukses dari modul Payments.PrivateWithKey: Wajib menyertakan enrollment key rahasia yang dicocokkan menggunakan hash BCrypt.Lesson bertipe Video, PdfDocument, atau DownloadableFile hanya menyimpan path/URL storage MinIO dan tidak menyimpan file binary pada database.[Course Aggregate Root]
  - Id: UUID
  - Title: String
  - Description: String
  - AccessType: CourseAccessType (OpenFree, OpenPaid, PrivateWithKey)
  - Price: Decimal
  - EnrollmentKeyHash: String?
  - IsPublished: Boolean
  └── CourseSection (Entity) [1..*]
        - Id: UUID
        - Title: String
        - OrderIndex: Int
        └── Lesson (Entity) [0..*]
              - Id: UUID
              - Title: String
              - Type: LessonType (Video, PdfDocument, DownloadableFile)
              - ContentUrl: String
              - DurationMinutes: Int
              - OrderIndex: Int
  └── Assignment (Entity) [0..*]
        - Id: UUID
        - Title: String
        - Instruction: String
        - DeadlineUtc: DateTime
        - MaxScore: Decimal
Aggregate Root 2: CourseEnrollment & AssignmentSubmissionInvariants:Satu siswa hanya boleh memiliki satu record CourseEnrollment aktif per kursus (Unique(UserId, CourseId)).Siswa hanya dapat mengunggah AssignmentSubmission sebelum Assignment.DeadlineUtc.3.3 Module: Exams (Dual-Mode & Proctoring Engine)Aggregate Root 1: QuizExamRoot: QuizExamChild Entity: QuizQuestionInvariants:Tipe mode (QuizMode):Simulation: Kuis latihan, tidak ada pembatasan ganti tab, pelanggaran tidak dihitung, kunci jawaban langsung dapat dilihat setelah submit.RealExam: Ujian formal berintegritas tinggi. Wajib mode Fullscreen, oncam & onmic (client-side), anti switch-tab aktif, dan batasan maksimal pelanggaran sebelum diskualifikasi.Ujian berstatus Published tidak dapat diubah daftar pertanyaan atau bobot nilainya.Aggregate Root 2: QuizSubmissionRoot: QuizSubmissionChild Entities / Value Objects: QuizAnswer, ProctoringViolation (VO), ProctoringSnapshot (VO)Invariants:MaxAllowedEndTimeUtc dihitung mutlak saat inisiasi sesi: $\text{StartedAtUtc} + \text{Duration}$.One-Time Token: Setiap submission aktif memegang satu ActiveSessionToken. Jika token di Redis berbeda dengan payload request, akses ditolak (kicked).Deterministic Shuffle Seed: Setiap submission menyimpan RandomSeed. Soal diacak menggunakan algoritma Fisher-Yates berbasis PRNG dengan seed tersebut.Auto-Disqualification: Jika Mode == RealExam dan jumlah Violations $\ge \text{MaxAllowedViolations}$, status submission seketika bertransisi ke Disqualified.[QuizSubmission Aggregate Root]
  - Id: UUID
  - QuizId: UUID
  - StudentId: UUID
  - Mode: QuizMode (Simulation, RealExam)
  - StartedAtUtc: DateTime
  - MaxAllowedEndTimeUtc: DateTime
  - FinishedAtUtc: DateTime?
  - Status: SubmissionStatus (InProgress, Completed, Disqualified, TimedOut)
  - TotalScore: Decimal
  - RandomSeed: Int
  - ActiveSessionToken: UUID
  - Violations: List<ProctoringViolation> (JSONB)
  └── QuizAnswer (Entity) [0..*]
        - Id: UUID
        - QuestionId: UUID
        - SelectedOptionIds: List<UUID> (JSONB / Array)
        - EssayText: String?
        - AwardedScore: Decimal?
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
    type VARCHAR(50) NOT NULL, -- Video, PdfDocument, DownloadableFile
    content_url TEXT NOT NULL,
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

-- ============================================================================
-- SCHEMA: exams
-- ============================================================================
CREATE TABLE exams.quiz_exams (
    id UUID PRIMARY KEY,
    course_id UUID NOT NULL,
    title VARCHAR(255) NOT NULL,
    mode VARCHAR(50) NOT NULL, -- Simulation, RealExam
    duration_minutes INT NOT NULL,
    passing_score NUMERIC(5, 2) NOT NULL,
    max_allowed_violations INT NOT NULL DEFAULT 0,
    settings JSONB NOT NULL DEFAULT '{}',
    is_published BOOLEAN NOT NULL DEFAULT FALSE,
    xmin XID
);
CREATE INDEX idx_exams_course ON exams.quiz_exams(course_id);

CREATE TABLE exams.quiz_questions (
    id UUID PRIMARY KEY,
    quiz_id UUID NOT NULL REFERENCES exams.quiz_exams(id) ON DELETE CASCADE,
    text TEXT NOT NULL,
    type VARCHAR(50) NOT NULL, -- SingleChoice, MultipleChoice, Essay, TrueFalse
    points NUMERIC(5, 2) NOT NULL,
    order_index INT NOT NULL,
    options JSONB NOT NULL, -- Array of { id: UUID, text: string, isCorrect: boolean }
    explanation TEXT
);
CREATE INDEX idx_questions_quiz ON exams.quiz_questions(quiz_id, order_index);

CREATE TABLE exams.quiz_submissions (
    id UUID PRIMARY KEY,
    quiz_id UUID NOT NULL REFERENCES exams.quiz_exams(id) ON DELETE CASCADE,
    student_id UUID NOT NULL,
    mode VARCHAR(50) NOT NULL,
    started_at_utc TIMESTAMPTZ NOT NULL,
    max_allowed_end_time_utc TIMESTAMPTZ NOT NULL,
    finished_at_utc TIMESTAMPTZ,
    status VARCHAR(50) NOT NULL, -- InProgress, Completed, Disqualified, TimedOut
    total_score NUMERIC(5, 2) NOT NULL DEFAULT 0.00,
    random_seed INT NOT NULL DEFAULT 0,
    active_session_token UUID,
    violations JSONB NOT NULL DEFAULT '[]'
);
CREATE INDEX idx_submissions_quiz_student ON exams.quiz_submissions(quiz_id, student_id);

CREATE TABLE exams.student_answers (
    id UUID PRIMARY KEY,
    submission_id UUID NOT NULL REFERENCES exams.quiz_submissions(id) ON DELETE CASCADE,
    question_id UUID NOT NULL,
    selected_option_ids UUID[] NOT NULL DEFAULT '{}',
    essay_text TEXT,
    awarded_score NUMERIC(5, 2),
    answered_at_utc TIMESTAMPTZ NOT NULL
);
CREATE UNIQUE INDEX uq_student_submission_question ON exams.student_answers(submission_id, question_id);

CREATE TABLE exams.proctoring_snapshots (
    id UUID PRIMARY KEY,
    submission_id UUID NOT NULL REFERENCES exams.quiz_submissions(id) ON DELETE CASCADE,
    storage_object_key VARCHAR(500) NOT NULL,
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

### 6.3 High-Concurrency Exam Answer Autosave with Redis Buffering
Untuk menangani beban konkurensi tinggi saat ribuan peserta ujian mengklik opsi jawaban atau mengetik esai secara berkala, sistem menerapkan **Redis In-Memory Answer Buffering**:
1. **Autosave Interception (`POST /api/v1/exams/submissions/{submissionId}/answers`)**:
   - Seluruh pembaruan jawaban peserta disimpan langsung ke dalam Redis Hash/Cache (`exam_answers:{submissionId}`) dengan TTL 4 jam.
   - **PostgreSQL tidak dipanggil (Zero DB writes)** selama proses pengerjaan ujian berlangsung, mencegah connection pool exhaustion dan disk I/O bottleneck.
2. **Batch Flush on Finalization (`POST /api/v1/exams/submissions/{submissionId}/finish`)**:
   - Saat peserta menyelesaikan ujian, seluruh jawaban yang terkumpul di Redis di-flush ke entitas `QuizSubmission` dalam database PostgreSQL dalam satu transaksi atomic EF Core.
   - Buffer jawaban di Redis kemudian dibersihkan (`DEL exam_answers:{submissionId}`).

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