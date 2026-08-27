# Epic: Dashboard & Data Monitoring Engine (Real-Time Analytics & Observability)

**Target Milestone:** v2.2-dashboard-monitoring  
**Architecture:** Vertical Slice Architecture (VSA) + Domain-Driven Design (DDD) Modular Monolith  
**Tech Stack:** ASP.NET Core (.NET 10), SvelteKit V3 RC (Svelte 5 Runes + Vite 8 + daisyUI 5 + Tailwind CSS 4), PostgreSQL 17 (Multi-Schema), Redis Engine, MinIO S3, SignalR  

---

## 📋 Epic Overview

Modul **Dashboard & Data Monitoring Engine** menyediakan visualisasi analitik dan pemantauan metrik secara real-time yang mengonsolidasikan data dari seluruh bounded context (`identity`, `payments`, `courses`, `exams`, `assessments`, `communications`, dan `customization`). Modul ini melayani 4 persona utama:
1. **Executive Admin / System Operator**: Finansial GMV, konversi pesanan, pertumbuhan user, kesehatan background worker & Dead-Letter Queue (DLQ), utilisasi storage MinIO S3, dan telemetri anti-cheat global.
2. **Instructor**: Funnel kursus & retensi siswa, SLA koreksi tugas, distribusi skor ujian, serta psikometrik butir soal (*Item Difficulty* $p$-value & *Item Discrimination* $D$-index).
3. **Proctor**: Monitoring kandidat ujian aktif, skoring risiko pelanggaran (*Risk-Scoring Engine*), stream timeline snapshot, dan intervensi cepat.
4. **Student**: Pelacakan progres materi, pengingat tenggat tugas $\le 7$ hari, countdown ujian terjadwal, radar kompetensi, dan sertifikat ber-hash SHA-256.

---

## 📌 Issue Matrix & Task Breakdown

### 🔹 Module 1: Backend CQRS Read Models & Query Infrastructure

#### `[ISSUE-DASH-01]` [Backend] Dashboard Query Infrastructure & Cross-Module Aggregation Contracts
- **Description**: Menyiapkan kontrak antarmuka API antar modul dan dasar CQRS read projections untuk query analitik berkinerja tinggi tanpa tracking (`AsNoTracking()`).
- **Scope & Tasks**:
  - [ ] Definisikan DTO kontrak analitik di `MonoSlice.Shared.Abstractions.Contracts` (e.g. `ICoursesAnalyticsApi`, `IExamsAnalyticsApi`, `IAssessmentsAnalyticsApi`, `IPaymentsAnalyticsApi`).
  - [ ] Siapkan helper query agregasi PostgreSQL menggunakan Common Table Expressions (CTE) dan window functions untuk agregasi data multi-tabel efisien.
  - [ ] Implementasikan base caching decorator dengan `ICacheService` untuk mendukung sliding cache keys.
- **Acceptance Criteria**:
  - Semua query read-only menggunakan `AsNoTracking()` dan `CancellationToken`.
  - Latency database untuk query agregasi berada di bawah 100ms untuk dataset simulasi 50k baris.

---

### 🔹 Module 2: Executive Admin & Infrastructure Monitoring Slices

#### `[ISSUE-DASH-02]` [Backend] Executive Financial KPIs & Order Conversion Slice
- **Description**: Implementasi endpoint CQRS query untuk metrik finansial, tren penjualan, dan performa kursus terlaris.
- **Endpoint**: `GET /api/v1/dashboard/admin/revenue-analytics`
- **Role Policy**: `.RequireAuthorization(policy => policy.RequireRole("Admin"))`
- **Scope & Tasks**:
  - [ ] Buat query `GetRevenueAnalyticsQuery` (`public sealed partial class` dengan Sannr validation `[Range]`, `[FutureDate]` jika ada rentang tanggal `FromUtc` - `ToUtc`).
  - [ ] Agregasi metrik: Total GMV, AOV (Average Order Value), tren pendapatan harian (30 hari terakhir), rasio status pesanan (`Paid`, `Pending`, `Failed`, `Expired`).
  - [ ] Peringkat 5 kursus berbayar dengan pendapatan tertinggi (*Top Revenue Courses*).
  - [ ] Simpan hasil query ke Redis key `cache:dashboard:admin:revenue` (TTL 10 menit).
- **Acceptance Criteria**:
  - Mengembalikan DTO `ApiResponse<RevenueAnalyticsDto>` dengan status 200 OK.
  - Otorisasi ketat hanya untuk role `Admin`.

#### `[ISSUE-DASH-03]` [Backend] Asynchronous Infrastructure & Dead-Letter Queue (DLQ) Health Slice
- **Description**: Endpoint observabilitas untuk memantau lag Redis Stream, antrian pesan gagal/beracun (`stream:grading-dlq`), dan utilisasi storage MinIO S3.
- **Endpoint**: `GET /api/v1/dashboard/admin/system-health`
- **Role Policy**: `.RequireAuthorization(policy => policy.RequireRole("Admin"))`
- **Scope & Tasks**:
  - [ ] Buat query `GetSystemHealthQuery`.
  - [ ] Query tabel `assessments.grading_dead_letters` untuk menghitung `UnresolvedDlqCount` dan mengambil 5 entri kegagalan grading terakhir dengan stack trace.
  - [ ] Query status consumer group lag pada Redis stream `stream:grading-queue`.
  - [ ] Integrasikan pengecekan metrik bucket MinIO S3 (`exam-snapshots`, `course-materials`, `assignment-submissions`) melalui `IObjectStorageService`.
  - [ ] Caching cepat pada Redis `cache:dashboard:admin:system-health` (TTL 10 detik).
- **Acceptance Criteria**:
  - Menghasilkan peringatan (*warning status*) jika terdapat pesan DLQ yang belum terselesaikan (`is_resolved = false`).

#### `[ISSUE-DASH-04]` [Backend] Global Anti-Cheat Telemetry & Violation Analytics Slice
- **Description**: Agregasi data integritas ujian dan frekuensi pelanggaran anti-cheat di seluruh platform.
- **Endpoint**: `GET /api/v1/dashboard/admin/security-violations`
- **Role Policy**: `.RequireAuthorization(policy => policy.RequireRole("Admin"))`
- **Scope & Tasks**:
  - [ ] Buat query `GetSecurityViolationsSummaryQuery`.
  - [ ] Agregasi data dari `exams.quiz_submissions`: total submission, jumlah peserta terdiskualifikasi (`status = 'Disqualified'`), dan breakdown tipe pelanggaran (`TabSwitch`, `DevTools`, `FullscreenExit`, `MultipleFaces`, `AudioSpikes`).
  - [ ] Identifikasi ujian dengan tingkat pelanggaran tertinggi (*High-Risk Exams*).
- **Acceptance Criteria**:
  - Mengembalikan persentase tingkat diskualifikasi global dan ringkasan pelanggaran per kategori.

---

### 🔹 Module 3: Instructor Analytics Studio & Psychometrics

#### `[ISSUE-DASH-05]` [Backend] Instructor Overview & Course Funnel Analytics Slice
- **Description**: Endpoint analitik performa kursus yang dikelola oleh instruktur terkait retensi dan SLA koreksi tugas.
- **Endpoint**: `GET /api/v1/dashboard/instructor/courses/{courseId}/analytics` & `GET /api/v1/dashboard/instructor/overview`
- **Role Policy**: `.RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"))`
- **Scope & Tasks**:
  - [ ] Buat query `GetCourseAnalyticsQuery` dengan validasi kepemilikan kursus (`WHERE instructor_id = CurrentUser.Id`).
  - [ ] Hitung:
    - Total siswa terdaftar (*Enrollments*).
    - *Course Completion Rate* (% siswa yang menyelesaikan semua materi dan tugas).
    - *Drop-off Rate* per seksi (`courses.sections`).
    - *Assignment Grading Backlog*: Total submission tugas yang belum dinilai (`score IS NULL`) dan estimasi waktu tunggu siswa.
    - *Unanswered Discussions*: Jumlah pertanyaan siswa yang belum dibalas pengajar di thread diskusi materi.
- **Acceptance Criteria**:
  - Instruktur hanya dapat mengakses data kursus milik sendiri (403 Forbidden jika mengakses kursus instruktur lain).

#### `[ISSUE-DASH-06]` [Backend] Exam Score Distribution & Psychometric Item Analysis ($p$ & $D$ Index) Slice
- **Description**: Endpoint analisis psikometrik butir soal bank soal dan distribusi kurva nilai ujian.
- **Endpoint**: `GET /api/v1/dashboard/instructor/exams/{examId}/analytics`
- **Role Policy**: `.RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"))`
- **Scope & Tasks**:
  - [ ] Buat query `GetExamAnalyticsQuery`.
  - [ ] Kalkulasi statistik deskriptif nilai dari `exams.quiz_submissions`: Mean, Median, Min, Max, Standar Deviasi, dan Pass/Fail Ratio.
  - [ ] Implementasikan formula psikometrik pada jawaban `exams.student_answers`:
    - **Tingkat Kesukaran ($p$-value)**:
      $$p = \frac{\text{Benar}}{\text{Total Peserta}}$$
    - **Daya Pembeda ($D$-index)**:
      $$D = \frac{U - L}{N_{\text{group}}}$$
      (Kelompok atas 27% vs Kelompok bawah 27%).
    - Tandai butir soal dengan $D < 0.20$ sebagai butir yang perlu direvisi (*Poor Discrimination Alert*).
- **Acceptance Criteria**:
  - Kalkulasi matematis akurat dan terverifikasi oleh unit test.

---

### 🔹 Module 4: Proctor Real-Time Operations Room

#### `[ISSUE-DASH-07]` [Backend] Proctor Live Session Summary & Risk Scoring Slice
- **Description**: Endpoint ringkasan ruang pengawas ujian untuk memetakan peserta ujian aktif dan menghitung skor risiko pelanggaran.
- **Endpoint**: `GET /api/v1/dashboard/proctor/live-summary`
- **Role Policy**: `.RequireAuthorization(policy => policy.RequireRole("Proctor", "Instructor", "Admin"))`
- **Scope & Tasks**:
  - [ ] Buat query `GetProctorLiveSummaryQuery`.
  - [ ] Ambil data submission dengan status `InProgress` yang memiliki sesi aktif.
  - [ ] Hitung skor risiko real-time per kandidat:
    $$\text{RiskScore} = (\text{TabSwitches} \times 2) + (\text{DevTools} \times 5) + (\text{AudioSpikes} \times 1.5) + (\text{FaceLost} \times 3)$$
  - [ ] Urutkan daftar kandidat berdasarkan skor risiko tertinggi (*High-Risk Flag*).
- **Acceptance Criteria**:
  - Data disajikan dengan latensi query $< 30\text{ms}$ dari buffer cache / snapshot Redis.

---

### 🔹 Module 5: Student Learning & Examination Hub

#### `[ISSUE-DASH-08]` [Backend] Student Personal Learning Progress & Urgent Deadlines Slice
- **Description**: Endpoint personal siswa untuk memuat status belajar terpadu, countdown ujian, dan sertifikat terbit.
- **Endpoint**: `GET /api/v1/dashboard/student/overview`
- **Role Policy**: `.RequireAuthorization(policy => policy.RequireRole("Student", "Instructor", "Admin"))`
- **Scope & Tasks**:
  - [ ] Buat query `GetStudentDashboardOverviewQuery` (`WHERE user_id = CurrentUser.Id`).
  - [ ] Query progres kursus yang sedang diikuti + link cepat *Continue Learning* ke lesson terakhir yang dibuka.
  - [ ] Ambil daftar tugas dengan batas waktu mendekati ($\le 7$ hari).
  - [ ] Ambil jadwal ujian mendatang yang telah dibuka aksesnya dengan waktu countdown.
  - [ ] Ambil daftar sertifikat terbitan beserta nomor sertifikat dan link verifikasi SHA-256.
  - [ ] Hitung distribusi kompetensi siswa berdasarkan tag kategori bank soal yang dijawab benar.
- **Acceptance Criteria**:
  - Respon terisolasi hanya untuk user yang sedang login.

---

### 🔹 Module 6: Tiered Redis Caching & Invalidation Events

#### `[ISSUE-DASH-09]` [Backend] Event-Driven Cache Invalidation Consumers
- **Description**: Mekanisme invalidasi cache dashboard otomatis saat terjadi perubahan data transaksional.
- **Scope & Tasks**:
  - [ ] Daftarkan handler event integrasi:
    - `OrderPaidIntegrationEvent` $\rightarrow$ Hapus cache `cache:dashboard:admin:revenue*`.
    - `ExamSubmittedIntegrationEvent` $\rightarrow$ Hapus cache `cache:dashboard:instructor:exams:{examId}` dan `cache:dashboard:student:{studentId}`.
    - `AssignmentGradedIntegrationEvent` $\rightarrow$ Hapus cache `cache:dashboard:student:{studentId}`.
    - `CertificateIssuedIntegrationEvent` $\rightarrow$ Hapus cache `cache:dashboard:student:{studentId}`.
- **Acceptance Criteria**:
  - Cache dibersihkan secara instan tanpa mengunci proses utama.

---

### 🔹 Module 7: Frontend SvelteKit Glassmorphism Components & Visualizations

#### `[ISSUE-DASH-10]` [Frontend] Reusable Glassmorphism Telemetry & Chart Library
- **Description**: Membangun komponen UI visualizer berbasis Svelte 5 Runes, daisyUI 5, dan Tailwind CSS 4 yang konsisten dengan estetika glassmorphism sistem.
- **Scope & Tasks**:
  - [ ] Buat komponen `TrendLineChart.svelte` (Lightweight SVG line chart untuk kurva pendapatan dan tren pendaftaran harian).
  - [ ] Buat komponen `ScoreHistogram.svelte` (Histogram distribusi nilai dengan indikator median & passing grade).
  - [ ] Buat komponen `CompetencyRadarChart.svelte` (Radar chart SVG untuk pemetaan keahlian siswa).
  - [ ] Buat komponen `ItemAnalysisTable.svelte` (Tabel interaktif tingkat kesukaran $p$ dan daya pembeda $D$ dengan badge alert status).
  - [ ] Perbarui `StatCard.svelte` dengan dukungan pill delta (+/- % perubahan) dan indikator status *glow*.
- **Acceptance Criteria**:
  - Render 100% reaktif tanpa ketergantungan library charting eksternal yang berat.
  - Tampilan responsif pada perangkat mobile, tablet, dan desktop dengan dark mode OKLCH tokens.

#### `[ISSUE-DASH-11]` [Frontend] Executive Admin Observability View (`/admin/dashboard`)
- **Description**: Halaman dashboard administrator untuk memantau pendapatan, kesehatan antrian DLQ, dan keamanan sistem.
- **Scope & Tasks**:
  - [ ] Integrasikan pemanggilan API `GET /api/v1/dashboard/admin/*` dengan state management reaktif.
  - [ ] Tampilkan section KPI Finansial (Total GMV, AOV, Grafik Konversi Order).
  - [ ] Tampilkan section Observabilitas Sistem: Widget status DLQ (Badge merah berkedip jika ada error grading yang belum di-resolve, tombol review error stack trace), utilisasi MinIO S3.
  - [ ] Tampilkan telemetri anti-cheat global (Tingkat diskualifikasi dan breakdown tipe pelanggaran).
- **Acceptance Criteria**:
  - Admin dapat mereview pesan DLQ dan mengeksekusi aksi retry langsung dari UI dashboard.

#### `[ISSUE-DASH-12]` [Frontend] Instructor Analytics Studio View (`/instructor/analytics`)
- **Description**: Antarmuka studio pengajar untuk menganalisis performa kursus dan butir soal ujian.
- **Scope & Tasks**:
  - [ ] Selector kursus dan ujian interaktif.
  - [ ] Visualisasi funnel kursus & drop-off seksi belajar.
  - [ ] Tampilan kurva normal distribusi nilai ujian dan persentase kelulusan siswa.
  - [ ] Tabel psikometrik bank soal: Tampilkan badge hijau (Baik), kuning (Perlu Tinjauan), merah (Daya Pembeda Rendah) pada setiap soal.
  - [ ] Quick-action list untuk tugas siswa yang menunggu penilaian (*Pending Grading Queue*).
- **Acceptance Criteria**:
  - Memberikan insight langsung bagi instruktur untuk merevisi soal yang bermasalah.

#### `[ISSUE-DASH-13]` [Frontend] Student Learning & Examination Hub Upgrade (`/dashboard`)
- **Description**: Pembaruan antarmuka dashboard siswa utama dengan pelacak progres materi, pengingat deadline mendesak, dan galeri sertifikat.
- **Scope & Tasks**:
  - [ ] Integrasikan `GET /api/v1/dashboard/student/overview`.
  - [ ] Card kursus aktif dengan progress bar persentase dan tombol *Continue Learning*.
  - [ ] Widget *Urgent Tasks & Exams*: Menampilkan countdown untuk ujian terjadwal dan deadline tugas $\le 7$ hari.
  - [ ] Widget Radar Kompetensi berdasarkan tag bank soal.
  - [ ] Galeri sertifikat terbit dengan badge SHA-256 dan tombol salin URL verifikasi publik.
- **Acceptance Criteria**:
  - Transisi halus, zero layout shift, dan waktu muat $< 300\text{ms}$.

---

### 🔹 Module 8: Testing, Verification & Quality Assurance

#### `[ISSUE-DASH-14]` [Testing] Automated Unit & Integration Test Suite
- **Description**: Pengujian otomatis komprehensif untuk seluruh handler CQRS dashboard dan perhitungan metrik.
- **Scope & Tasks**:
  - [ ] Unit test perhitungan psikometrik ($p$-value, $D$-index) dengan berbagai skenario data batas di `tests/MonoSlice.Modules.Exams.Tests/`.
  - [ ] Unit test query agregasi finansial GMV dan filter rentang tanggal di `tests/MonoSlice.Modules.Orders.Tests/`.
  - [ ] Integration test otorisasi role policy (memastikan Student tidak dapat mengakses endpoint Admin/Instructor).
  - [ ] Pengujian cache invalidation saat event transaksional dipublikasikan.
- **Acceptance Criteria**:
  - Seluruh pengujian lolos via `dotnet test` dengan code coverage $\ge 85\%$.

---

## 🎯 Definition of Done (DoD)

1. **Architecture & Standards**: Mengikuti Vertical Slice Architecture (VSA), CQRS class partials dengan Sannr validation, dan otorisasi role eksplisit `.RequireAuthorization(policy => policy.RequireRole(...))`.
2. **Database & Multi-Schema Isolation**: Seluruh query agregasi bersifat read-only (`AsNoTracking()`), efisien, dan menghormati batasan isolasi skema PostgreSQL.
3. **Caching**: Redis multi-tier caching terpasang dengan TTL terukur dan event-driven invalidation.
4. **UI & UX Quality**: Desain visual Glassmorphism berstandar tinggi (daisyUI 5 + Tailwind CSS 4) dengan animasi transisi halus dan responsivitas mobile.
5. **Testing**: 100% unit test dan integration test lulus pada environment CI/CD.
