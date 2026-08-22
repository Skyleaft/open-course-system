-- ============================================================================
-- 1. SETUP SCHEMAS & EXTENSIONS
-- ============================================================================
CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS payments;
CREATE SCHEMA IF NOT EXISTS courses;
CREATE SCHEMA IF NOT EXISTS exams;
CREATE SCHEMA IF NOT EXISTS assessments;
CREATE SCHEMA IF NOT EXISTS communications;

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ============================================================================
-- SCHEMA: identity
-- ============================================================================
CREATE TABLE IF NOT EXISTS identity.users (
    id UUID PRIMARY KEY,
    email VARCHAR(256) NOT NULL UNIQUE,
    password_hash VARCHAR(500) NOT NULL,
    full_name VARCHAR(256) NOT NULL,
    roles VARCHAR(50)[] NOT NULL DEFAULT '{"Student"}',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at_utc TIMESTAMPTZ NOT NULL,
    updated_at_utc TIMESTAMPTZ
);
CREATE INDEX IF NOT EXISTS idx_users_email ON identity.users(email);

CREATE TABLE IF NOT EXISTS identity.refresh_tokens (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    token VARCHAR(500) NOT NULL UNIQUE,
    expires_at_utc TIMESTAMPTZ NOT NULL,
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
    created_at_utc TIMESTAMPTZ NOT NULL,
    replaced_by_token VARCHAR(500)
);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user ON identity.refresh_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_token ON identity.refresh_tokens(token);

-- ============================================================================
-- SCHEMA: payments
-- ============================================================================
CREATE TABLE IF NOT EXISTS payments.orders (
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
CREATE INDEX IF NOT EXISTS idx_orders_user ON payments.orders(user_id);
CREATE UNIQUE INDEX IF NOT EXISTS uq_orders_ext_ref ON payments.orders(external_payment_reference) WHERE external_payment_reference IS NOT NULL;

-- ============================================================================
-- SCHEMA: courses
-- ============================================================================
CREATE TABLE IF NOT EXISTS courses.courses (
    id UUID PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    access_type VARCHAR(50) NOT NULL, -- OpenFree, OpenPaid, PrivateWithKey
    price NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    enrollment_key_hash VARCHAR(255),
    is_published BOOLEAN NOT NULL DEFAULT FALSE,
    xmin XID
);

CREATE TABLE IF NOT EXISTS courses.course_sections (
    id UUID PRIMARY KEY,
    course_id UUID NOT NULL REFERENCES courses.courses(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    order_index INT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_sections_course ON courses.course_sections(course_id);

CREATE TABLE IF NOT EXISTS courses.lessons (
    id UUID PRIMARY KEY,
    section_id UUID NOT NULL REFERENCES courses.course_sections(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    type VARCHAR(50) NOT NULL, -- Video, PdfDocument, DownloadableFile
    content_url TEXT NOT NULL,
    duration_minutes INT NOT NULL DEFAULT 0,
    order_index INT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_lessons_section ON courses.lessons(section_id);

CREATE TABLE IF NOT EXISTS courses.assignments (
    id UUID PRIMARY KEY,
    course_id UUID NOT NULL REFERENCES courses.courses(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    instruction TEXT NOT NULL,
    deadline_utc TIMESTAMPTZ NOT NULL,
    max_score NUMERIC(5, 2) NOT NULL
);

CREATE TABLE IF NOT EXISTS courses.assignment_submissions (
    id UUID PRIMARY KEY,
    assignment_id UUID NOT NULL REFERENCES courses.assignments(id) ON DELETE CASCADE,
    student_id UUID NOT NULL,
    file_attachment_url TEXT NOT NULL,
    student_notes TEXT,
    submitted_at_utc TIMESTAMPTZ NOT NULL,
    score NUMERIC(5, 2),
    feedback TEXT
);
CREATE UNIQUE INDEX IF NOT EXISTS uq_assignment_student ON courses.assignment_submissions(assignment_id, student_id);

CREATE TABLE IF NOT EXISTS courses.course_enrollments (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    course_id UUID NOT NULL REFERENCES courses.courses(id) ON DELETE CASCADE,
    enrolled_at_utc TIMESTAMPTZ NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);
CREATE UNIQUE INDEX IF NOT EXISTS uq_enrollment_user_course ON courses.course_enrollments(user_id, course_id);

-- ============================================================================
-- SCHEMA: exams
-- ============================================================================
CREATE TABLE IF NOT EXISTS exams.quiz_exams (
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
CREATE INDEX IF NOT EXISTS idx_exams_course ON exams.quiz_exams(course_id);

CREATE TABLE IF NOT EXISTS exams.quiz_questions (
    id UUID PRIMARY KEY,
    quiz_id UUID NOT NULL REFERENCES exams.quiz_exams(id) ON DELETE CASCADE,
    text TEXT NOT NULL,
    type VARCHAR(50) NOT NULL, -- SingleChoice, MultipleChoice, Essay, TrueFalse
    points NUMERIC(5, 2) NOT NULL,
    order_index INT NOT NULL,
    options JSONB NOT NULL, -- Array of { id: UUID, text: string, isCorrect: boolean }
    explanation TEXT
);
CREATE INDEX IF NOT EXISTS idx_questions_quiz ON exams.quiz_questions(quiz_id, order_index);

CREATE TABLE IF NOT EXISTS exams.quiz_submissions (
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
CREATE INDEX IF NOT EXISTS idx_submissions_quiz_student ON exams.quiz_submissions(quiz_id, student_id);

CREATE TABLE IF NOT EXISTS exams.student_answers (
    id UUID PRIMARY KEY,
    submission_id UUID NOT NULL REFERENCES exams.quiz_submissions(id) ON DELETE CASCADE,
    question_id UUID NOT NULL,
    selected_option_ids UUID[] NOT NULL DEFAULT '{}',
    essay_text TEXT,
    awarded_score NUMERIC(5, 2),
    answered_at_utc TIMESTAMPTZ NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS uq_student_submission_question ON exams.student_answers(submission_id, question_id);

CREATE TABLE IF NOT EXISTS exams.proctoring_snapshots (
    id UUID PRIMARY KEY,
    submission_id UUID NOT NULL REFERENCES exams.quiz_submissions(id) ON DELETE CASCADE,
    storage_object_key VARCHAR(500) NOT NULL,
    captured_at_utc TIMESTAMPTZ NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_snapshots_sub ON exams.proctoring_snapshots(submission_id);

-- ============================================================================
-- SCHEMA: assessments
-- ============================================================================
CREATE TABLE IF NOT EXISTS assessments.grade_records (
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
CREATE INDEX IF NOT EXISTS idx_grades_student ON assessments.grade_records(student_id, course_id);

CREATE TABLE IF NOT EXISTS assessments.certificates (
    id UUID PRIMARY KEY,
    certificate_number VARCHAR(100) NOT NULL UNIQUE,
    student_id UUID NOT NULL,
    course_id UUID NOT NULL,
    final_score NUMERIC(5, 2) NOT NULL,
    certificate_hash VARCHAR(64) NOT NULL UNIQUE,
    status VARCHAR(50) NOT NULL, -- Issued, Revoked
    issued_at_utc TIMESTAMPTZ NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS uq_cert_student_course ON assessments.certificates(student_id, course_id);

CREATE TABLE IF NOT EXISTS assessments.grading_dead_letters (
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
CREATE TABLE IF NOT EXISTS communications.announcements (
    id UUID PRIMARY KEY,
    course_id UUID, -- NULL = Global Platform Announcement
    author_id UUID NOT NULL,
    title VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    is_pinned BOOLEAN NOT NULL DEFAULT FALSE,
    created_at_utc TIMESTAMPTZ NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_announcements_course ON communications.announcements(course_id);

CREATE TABLE IF NOT EXISTS communications.discussion_threads (
    id UUID PRIMARY KEY,
    course_id UUID NOT NULL,
    lesson_id UUID, -- NULL = Course-level General Thread
    author_id UUID NOT NULL,
    title VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    is_closed BOOLEAN NOT NULL DEFAULT FALSE,
    created_at_utc TIMESTAMPTZ NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_threads_course ON communications.discussion_threads(course_id);

CREATE TABLE IF NOT EXISTS communications.thread_comments (
    id UUID PRIMARY KEY,
    thread_id UUID NOT NULL REFERENCES communications.discussion_threads(id) ON DELETE CASCADE,
    author_id UUID NOT NULL,
    parent_comment_id UUID REFERENCES communications.thread_comments(id) ON DELETE CASCADE,
    content TEXT NOT NULL,
    created_at_utc TIMESTAMPTZ NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_comments_thread ON communications.thread_comments(thread_id);
