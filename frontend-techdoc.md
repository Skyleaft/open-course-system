# Frontend Technical Specification Document: LMS & Online Examination Platform

**Platform:** Web Client (Student Portal, Instructor Dashboard, Proctor Live Monitoring)  
**Framework:** SvelteKit V3 RC (Svelte 5 + Vite 8)  
**UI Library:** daisyUI 5 (Tailwind CSS 4)  
**Design Aesthetic:** Glassmorphism (Frosted glass panels, translucent depth, vibrant glow accents)  
**Rich Text Editor:** Edra (Tiptap + Svelte 5 Runes)  
**Realtime:** SignalR (`@microsoft/signalr` on ExamHub & NotificationHub)  
**Target Location:** `frontend/` (Root directory)  

---

## 1. Architecture & Technical Overview

```
+----------------------------------------------------------------------------------------------------+
|                                    SVELTEKIT V3 RC CLIENT LAYER                                    |
|                                                                                                    |
|  [ Student Portal ]                 [ Instructor Dashboard ]             [ Proctor / Live Console ] |
|  - Course Catalog & Checkout        - Curriculum & Content Builder       - Live Violation Stream    |
|  - Video / PDF / Lesson Player      - Question Bank & Rubrics            - Force Disconnect / Warn  |
|  - Realtime Exam Runner             - Discussion & Broadcast Manager     - Student Timer Sync       |
|  - Certificate Verification         - Submissions & Grading              - Candidate Snapshot Grid  |
|                                                                                                    |
|  Rich Text: Edra (Tiptap + KaTeX + Lowlight + Mermaid + Task Lists + Callouts + Slash Commands)    |
|  Theme & Components: daisyUI 5 (dark / light themes) + Custom Glassmorphism Utility Engine        |
|  Client Interceptors: Fullscreen Lock | Tab Visibility | Window Blur | Web Worker Snapshot Loop    |
+------------------------------------+--------------------------------+------------------------------+
                                     │ (HTTPS REST / WSS SignalR)     │ Direct PUT (Presigned URL)
                                     ▼                                ▼
+------------------------------------+-------------------+   +---------------------------------------+
|    ASP.NET Core (.NET 10) Host Layer (REST / SignalR)  |   |     MinIO S3 Object Storage Engine    |
+--------------------------------------------------------+   +---------------------------------------+
```

---

## 2. Framework & Library Specifications

### 2.1 SvelteKit V3 RC Conventions
- **Configuration:** Consolidated inside `vite.config.ts` (no `svelte.config.js`).
- **Path Alias:** `#lib` subpath imports (replacing legacy `$lib`).
- **Shallow Routing:** `goto(url, { state, shallow: true })`.
- **Data Invalidation:** `refreshAll()` (replacing `invalidateAll()`).
- **TypeScript:** Extends `$app/tsconfig`.
- **State Management:** Svelte 5 native runes (`$state`, `$derived`, `$effect`, `$props`).

### 2.2 Styling & Design System: daisyUI 5 + Glassmorphism
- **Base Engine:** Tailwind CSS 4 with `@plugin "daisyui"`.
- **Themes:** `dark` (primary/default) and `light` via `data-theme` attribute.
- **Glassmorphism Layers:**
  - `glass-panel`: Frosted glass cards with `backdrop-blur-xl`, `bg-base-100/60`, `border-white/10`, and `shadow-lg`.
  - `glass-card`: Inner subtle translucent containers with `backdrop-blur-md` and `bg-base-200/50`.
  - `glass-navbar`: Sticky top bar with `backdrop-blur-2xl` and `bg-base-100/80`.
  - `glass-modal`: Dialog backdrop with deep blur and glowing borders.
  - `gradient-accent`: Tailored radiant gradients for primary CTAs and interactive highlights.

### 2.3 Rich Text Editor: Edra
- **Core:** Tiptap framework ported to Svelte 5 runes.
- **Extensions Installed:**
  - StarterKit (Headings, bold, italic, strikethrough, blockquote, bullet/ordered lists)
  - Mathematics (KaTeX inline and block LaTeX formulas for exam questions)
  - Code Block (Syntax highlighting via `lowlight` with light/dark themes)
  - Tables (Table creation, row/column management, cell merging)
  - Task List (Interactive checklists for assignments)
  - Media & Mermaid (Diagrams and image/video embeds)
  - Callouts (Custom admonition/alert blocks)
  - Slash Commands (`/` popover menu for rapid block creation)
  - Drag Handles (Notion-like block dragging and reordering)
- **Data Format:** Tiptap JSON schema stored in PostgreSQL `TEXT` columns and re-hydrated in read-only / interactive modes.

---

## 3. Directory Layout (`frontend/`)

```
frontend/
├── vite.config.ts                 # SvelteKit V3 & daisyUI Vite config
├── tsconfig.json                  # Extends $app/tsconfig
├── package.json
├── Dockerfile                     # Multi-stage Node.js build with adapter-node
├── src/
│   ├── app.html
│   ├── app.css                    # Tailwind 4, daisyUI 5, Glassmorphism classes
│   ├── hooks.server.ts            # Server-side auth guard & session cookie handler
│   ├── hooks.client.ts            # Client error tracking & SignalR init
│   ├── lib/                       # Imported via #lib alias
│   │   ├── api/
│   │   │   ├── client.ts          # Fetch envelope wrapper + auto-refresh queue
│   │   │   ├── auth.ts            # Login, register, Google OAuth, me
│   │   │   ├── courses.ts         # Catalog, syllabus, lessons, assignments
│   │   │   ├── exams.ts           # Quiz metadata, start attempt, buffer answers, finish
│   │   │   ├── payments.ts        # Mock checkout and order verification
│   │   │   ├── assessments.ts     # Grades and certificate verification
│   │   │   ├── communications.ts  # Announcements and discussion threads
│   │   │   └── proctor.ts         # Live candidates, warn, force disconnect
│   │   ├── signalr/
│   │   │   ├── connection.ts      # HubConnectionBuilder factory with backoff retry
│   │   │   ├── exam-hub.ts        # Strongly-typed ExamHub wrapper
│   │   │   └── notification-hub.ts
│   │   ├── stores/
│   │   │   ├── auth.svelte.ts     # Svelte 5 rune auth store
│   │   │   ├── exam.svelte.ts     # Active exam state, answer buffers, timer
│   │   │   └── toast.svelte.ts    # Global notification toasts
│   │   ├── components/
│   │   │   ├── layout/
│   │   │   │   ├── Navbar.svelte
│   │   │   │   ├── Sidebar.svelte
│   │   │   │   ├── Footer.svelte
│   │   │   │   └── PageShell.svelte
│   │   │   ├── ui/
│   │   │   │   ├── GlassCard.svelte
│   │   │   │   ├── GlassModal.svelte
│   │   │   │   ├── StatCard.svelte
│   │   │   │   ├── FileUpload.svelte
│   │   │   │   └── ConfirmModal.svelte
│   │   │   ├── editor/
│   │   │   │   ├── RichEditor.svelte      # Edra main editor component
│   │   │   │   └── RichRenderer.svelte    # Readonly Edra renderer
│   │   │   ├── exam/
│   │   │   │   ├── PreExamChecker.svelte  # Camera, mic, fullscreen validator
│   │   │   │   ├── QuestionPalette.svelte # Number grid with status flags
│   │   │   │   ├── QuestionCard.svelte    # SingleChoice/MultiChoice/Essay/TF
│   │   │   │   ├── SectionBuilder.svelte  # Multi-section editor with QuestionBank package linking
│   │   │   │   ├── QuestionBankPackageSelector.svelte # Reusable question package picker
│   │   │   │   ├── ExamTimer.svelte       # Server drift compensated countdown
│   │   │   │   ├── ViolationAlert.svelte  # Red alert overlay & disqualification
│   │   │   │   └── SnapshotEngine.svelte  # Offscreen canvas capture & MinIO PUT
│   │   │   ├── course/
│   │   │   │   ├── CourseCard.svelte
│   │   │   │   ├── SyllabusTree.svelte
│   │   │   │   ├── CourseExamAttachment.svelte # Attach/detach reusable exams to courses
│   │   │   │   └── LessonPlayer.svelte    # Video/PDF/File player with MinIO
│   │   │   └── proctor/
│   │   │       ├── CandidateGrid.svelte
│   │   │       ├── ViolationFeed.svelte
│   │   │       └── SnapshotTimeline.svelte
│   │   ├── utils/
│   │   │   ├── time.ts                    # Time drift & formatter
│   │   │   └── security.ts                # Interceptor binding & cleanup
│   │   └── workers/
│   │       └── snapshot.worker.ts         # Random interval timer Web Worker
│   └── routes/
│       ├── +layout.svelte
│       ├── +layout.server.ts
│       ├── +page.svelte                   # Landing page
│       ├── (auth)/
│       │   ├── login/+page.svelte
│       │   ├── register/+page.svelte
│       │   └── +layout.svelte
│       ├── (app)/
│       │   ├── +layout.svelte
│       │   ├── dashboard/+page.svelte
│       │   ├── courses/
│       │   │   ├── +page.svelte
│       │   │   ├── [id]/
│       │   │   │   ├── +page.svelte
│       │   │   │   ├── learn/+page.svelte
│       │   │   │   └── assignments/[assignmentId]/+page.svelte
│       │   ├── exams/
│       │   │   ├── +page.svelte
│       │   │   ├── [id]/start/+page.svelte
│       │   │   └── submissions/[submissionId]/
│       │   │       ├── +page.svelte       # Strict exam runner
│       │   │       └── result/+page.svelte
│       │   ├── certificates/
│       │   │   ├── +page.svelte
│       │   │   └── verify/[hash]/+page.svelte
│       │   ├── grades/+page.svelte
│       │   └── announcements/+page.svelte
│       ├── (instructor)/
│       │   ├── +layout.svelte
│       │   ├── courses/
│       │   │   ├── +page.svelte
│       │   │   ├── create/+page.svelte
│       │   │   └── [id]/
│       │   │       ├── edit/+page.svelte
│       │   │       ├── assignments/+page.svelte
│       │   │       └── discussions/+page.svelte
│       │   ├── exams/
│       │   │   ├── +page.svelte
│       │   │   ├── create/+page.svelte
│       │   │   └── [id]/edit/+page.svelte
│       │   ├── questions/
│       │   │   └── +page.svelte           # Independent Question Bank Repository
│       │   └── announcements/+page.svelte
│       └── (proctor)/
│           ├── +layout.svelte
│           └── exams/[quizId]/live/+page.svelte
```

---

## 4. Key Subsystems & Interaction Patterns

### 4.1 Resilient API Client & Session Management
- Tokens stored in memory (access token) and secure HTTP-Only cookies (refresh token).
- Concurrent 401 interceptor queues parallel calls while a single `POST /api/v1/auth/refresh-token` executes.
- Standard envelope unwrap: extracts `data` from `ApiResponse<T>` and handles `ApiErrorResponse`.

### 4.2 Strict Realtime Exam Runner & Anti-Cheat Protocol
1. **Pre-flight Check:** Webcam & microphone validation via `navigator.mediaDevices.getUserMedia`, local Picture-in-Picture (PiP) preview, fullscreen prompt.
2. **Security Interceptors (RealExam mode):**
   - `document.addEventListener("visibilitychange")` $\rightarrow$ SignalR `ReportViolation("TabSwitch")`
   - `window.addEventListener("blur")` $\rightarrow$ SignalR `ReportViolation("WindowFocusLoss")`
   - `document.addEventListener("fullscreenchange")` $\rightarrow$ SignalR `ReportViolation("FullscreenExit")`
   - Keyboard & contextmenu suppression (`e.preventDefault()` for Ctrl+C, Ctrl+V, F12, Alt+Tab, Right Click).
3. **Web Worker Snapshot Engine:**
   - Worker triggers snapshots at randomized 30–60s intervals.
   - Video frame rendered on offscreen canvas $\rightarrow$ converted to WebP blob.
   - Client requests presigned URL via `POST /api/v1/exams/submissions/{submissionId}/snapshots/presign`.
   - Direct HTTP PUT upload to MinIO S3 bucket `exam-snapshots`.
   - Broadcast `ReportSnapshotUploaded` over SignalR `ExamHub`.
4. **Answer Autosaving & Disaster Recovery:**
   - Answers debounced and buffered to Redis cache (`exam_answers:{submissionId}`) via `POST /api/v1/exams/submissions/{submissionId}/answers`. Zero database writes during active attempt.
   - On page refresh or network reconnect, `GET /api/v1/exams/submissions/{submissionId}/questions` automatically recovers and fills saved answers.
5. **Synchronized Timer:**
   - Server clock offset calculated on connection handshake: `drift = Date.now() - serverTimeUtc`.
   - Countdown timer compensates for local drift, triggers auto-submission upon reaching 0.

### 4.3 Live Proctor Console
- Subscribes to `proctor_exam_{quizId}` SignalR group.
- Candidate grid with real-time liveness, violation count badges, and webcam snapshot timeline.
- Instant proctor commands: Send custom warning banner or force disqualification disconnect.
