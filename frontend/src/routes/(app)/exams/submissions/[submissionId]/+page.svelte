<script lang="ts">
	import { page } from '$app/state';
	import { examsApi } from '$lib/api/exams.ts';
	import type { QuizQuestion, QuestionType, StudentExamSectionDto } from '$lib/api/types.ts';
	import { ExamHubClient } from '$lib/signalr/exam-hub.svelte.ts';
	import { bindSecurityInterceptors } from '$lib/utils/security.ts';
	import ExamTimer from '$lib/components/exam/ExamTimer.svelte';
	import QuestionPalette from '$lib/components/exam/QuestionPalette.svelte';
	import QuestionCard from '$lib/components/exam/QuestionCard.svelte';
	import ViolationOverlay from '$lib/components/exam/ViolationOverlay.svelte';
	import SnapshotEngine from '$lib/components/exam/SnapshotEngine.svelte';
	import { toast } from '$lib/stores/toast.svelte.ts';
	import { goto } from '$app/navigation';
	import {
		ChevronLeft,
		ChevronRight,
		CheckCircle2,
		Save,
		ShieldAlert,
		LogOut,
		Sparkles,
		AlertCircle,
		Folder,
		Layers,
		CheckSquare,
		HelpCircle,
		Send
	} from 'lucide-svelte';
	import { onMount, onDestroy } from 'svelte';

	const submissionId = (page.params.submissionId || '') as string;
	let questions = $state<QuizQuestion[]>([]);
	let sections = $state<StudentExamSectionDto[]>([]);
	let currentIndex = $state(0);
	let remainingSeconds = $state(0);
	let mode = $state('RealExam');
	let examTitle = $state('Examination');
	let isLoading = $state(true);
	let loadError = $state<string | null>(null);

	// Answers map keyed by questionId
	let answers = $state<Record<string, { selectedOptionIds: string[]; essayText?: string }>>({});
	let flaggedIds = $state<Set<string>>(new Set());

	// Security & Anti-Cheat State
	let currentViolations = $state(0);
	let maxViolations = $state(3);
	let isDisqualified = $state(false);
	let terminationReason = $state<string | undefined>(undefined);

	// Autosave status
	let isSaving = $state(false);
	let lastSavedTime = $state<string | null>(null);
	let saveDebounceTimer: ReturnType<typeof setTimeout> | null = null;

	// Modals
	let isFinishModalOpen = $state(false);
	let isSubmittingFinal = $state(false);

	let examHub = $state<ExamHubClient | null>(null);
	let unbindSecurity: (() => void) | null = null;
	let cameraStream = $state<MediaStream | null>(null);

	const currentQuestion = $derived(questions[currentIndex]);
	const isRealExam = $derived(mode === 'RealExam');

	function isAnswered(questionId: string): boolean {
		const ans = answers[questionId];
		if (!ans) return false;
		return (ans.selectedOptionIds && ans.selectedOptionIds.length > 0) || Boolean(ans.essayText?.trim());
	}

	// Section groupings for top navigation & progress
	interface SectionSummary {
		id: string;
		title: string;
		description?: string | null;
		startIndex: number;
		endIndex: number;
		totalQuestions: number;
		answeredCount: number;
		flaggedCount: number;
	}

	const sectionSummaries = $derived.by<SectionSummary[]>(() => {
		if (!questions || questions.length === 0) return [];

		const map = new Map<string, SectionSummary>();
		const defaultSectionId = 'default-section';

		// Seed known sections if available
		if (sections && sections.length > 0) {
			for (const sec of sections) {
				map.set(sec.id, {
					id: sec.id,
					title: sec.title || `Section ${sec.orderIndex + 1}`,
					description: sec.description,
					startIndex: -1,
					endIndex: -1,
					totalQuestions: 0,
					answeredCount: 0,
					flaggedCount: 0
				});
			}
		}

		questions.forEach((q, idx) => {
			const sId = q.sectionId || defaultSectionId;
			let summary = map.get(sId);
			if (!summary) {
				const title = q.sectionTitle || (sections.length > 0 ? 'General Section' : 'Main Section');
				summary = {
					id: sId,
					title,
					description: null,
					startIndex: idx,
					endIndex: idx,
					totalQuestions: 0,
					answeredCount: 0,
					flaggedCount: 0
				};
				map.set(sId, summary);
			}

			if (summary.startIndex === -1) {
				summary.startIndex = idx;
			}
			summary.endIndex = idx;
			summary.totalQuestions++;

			if (isAnswered(q.id)) {
				summary.answeredCount++;
			}
			if (flaggedIds.has(q.id)) {
				summary.flaggedCount++;
			}
		});

		return Array.from(map.values()).filter((s) => s.totalQuestions > 0);
	});

	const hasMultipleSections = $derived(sectionSummaries.length > 1);

	// Current active section
	const currentSection = $derived.by(() => {
		if (!currentQuestion) return null;
		const sId = currentQuestion.sectionId || 'default-section';
		return sectionSummaries.find((s) => s.id === sId) || null;
	});

	// Current question's index within its section
	const currentSectionQuestionIndex = $derived.by(() => {
		if (!currentSection) return 0;
		return Math.max(0, currentIndex - currentSection.startIndex);
	});

	// Total answered across exam
	const totalAnswered = $derived(
		questions.reduce((acc, q) => acc + (isAnswered(q.id) ? 1 : 0), 0)
	);

	onMount(async () => {
		try {
			// 1. Fetch Questions and recover state from backend API
			const res = await examsApi.getQuestions(submissionId);

			if (!res || !res.questions || res.questions.length === 0) {
				loadError = 'No examination questions found for this attempt.';
				isLoading = false;
				return;
			}

			examTitle = res.title || 'Examination';
			mode = res.mode || 'RealExam';
			sections = res.sections || [];

			// Map questions to match frontend QuizQuestion structure
			questions = res.questions.map((q) => ({
				id: q.id,
				quizId: res.examId,
				examId: res.examId,
				sectionId: q.sectionId,
				sectionTitle: q.sectionTitle,
				text: q.questionText,
				questionText: q.questionText,
				type: q.type as QuestionType,
				points: Number(q.points),
				orderIndex: q.displayOrder,
				options: (q.options || []).map((o) => ({ id: o.id, text: o.text }))
			}));

			// Restore saved answers from Redis buffer
			const restoredAnswers: Record<string, { selectedOptionIds: string[]; essayText?: string }> = {};
			for (const q of res.questions) {
				if ((q.selectedOptionIds && q.selectedOptionIds.length > 0) || q.essayText) {
					restoredAnswers[q.id] = {
						selectedOptionIds: (q.selectedOptionIds || []).map(String),
						essayText: q.essayText || ''
					};
				}
			}
			answers = restoredAnswers;

			// 2. Accurately calculate remaining seconds from maxAllowedEndTimeUtc
			if (res.maxAllowedEndTimeUtc) {
				const endMs = new Date(res.maxAllowedEndTimeUtc).getTime();
				const nowMs = Date.now();
				const diffSeconds = Math.floor((endMs - nowMs) / 1000);
				remainingSeconds = Math.max(0, diffSeconds);

				if (diffSeconds <= 0) {
					toast.warning('Exam time limit has expired. Finalizing submission...');
					await handleFinishExam();
					return;
				}
			}

			// 3. Resolve active session token
			const sessionToken =
				res.activeSessionToken ||
				sessionStorage.getItem(`exam_token_${submissionId}`) ||
				'';

			if (res.activeSessionToken) {
				sessionStorage.setItem(`exam_token_${submissionId}`, res.activeSessionToken);
			}

			// 4. Connect to SignalR ExamHub
			if (sessionToken) {
				examHub = new ExamHubClient();
				await examHub.start();
				await examHub.joinExamRoom(submissionId, sessionToken);

				// Server-to-Client Listeners
				examHub.onSyncTimer((serverRemaining) => {
					remainingSeconds = serverRemaining;
				});

				examHub.onViolationWarning((count, max) => {
					currentViolations = count;
					maxViolations = max;
					toast.warning(`Security Violation Warning: ${count} of ${max}`);
				});

				examHub.onForceDisconnectExam((reason) => {
					isDisqualified = true;
					terminationReason = reason;
					toast.error(`Exam session terminated: ${reason}`);
				});
			}

			// 5. Bind security interceptors (for RealExam mode)
			if (mode === 'RealExam' && examHub) {
				unbindSecurity = bindSecurityInterceptors({
					onTabSwitch: () => {
						examHub?.reportViolation(submissionId, 'TabSwitch', 'Student switched tabs');
					},
					onWindowBlur: () => {
						examHub?.reportViolation(submissionId, 'WindowFocusLoss', 'Student unfocused exam window');
					},
					onFullscreenExit: () => {
						examHub?.reportViolation(submissionId, 'FullscreenExit', 'Student exited fullscreen mode');
					}
				});
			}
		} catch (err: any) {
			console.error('Failed to load exam session:', err);
			const errorMsg = err?.message || 'Failed to load exam session. Please check your network connection.';
			loadError = errorMsg;
			toast.error(errorMsg);
		} finally {
			isLoading = false;
		}
	});

	onDestroy(() => {
		if (saveDebounceTimer) clearTimeout(saveDebounceTimer);
		if (unbindSecurity) unbindSecurity();
		if (examHub) examHub.stop();
		if (cameraStream) {
			cameraStream.getTracks().forEach((t) => t.stop());
		}
	});

	async function autoSaveAnswer(questionId: string) {
		const ans = answers[questionId];
		if (!ans) return;

		isSaving = true;
		try {
			await examsApi.saveAnswer(submissionId, {
				questionId,
				selectedOptionIds: ans.selectedOptionIds,
				essayText: ans.essayText
			});
			lastSavedTime = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
		} catch (err) {
			console.warn('Autosave to Redis failed, answer remains in local memory:', err);
		} finally {
			isSaving = false;
		}
	}

	function handleToggleOption(optionId: string, isSingle: boolean) {
		if (!currentQuestion) return;
		const qId = currentQuestion.id;
		const existing = answers[qId]?.selectedOptionIds || [];

		let updated: string[];
		if (isSingle) {
			updated = [optionId];
		} else {
			if (existing.includes(optionId)) {
				updated = existing.filter((id) => id !== optionId);
			} else {
				updated = [...existing, optionId];
			}
		}

		answers[qId] = {
			...answers[qId],
			selectedOptionIds: updated
		};

		autoSaveAnswer(qId);
	}

	function handleEssayChange(text: string) {
		if (!currentQuestion) return;
		const qId = currentQuestion.id;
		answers[qId] = {
			...answers[qId],
			selectedOptionIds: answers[qId]?.selectedOptionIds || [],
			essayText: text
		};

		if (saveDebounceTimer) clearTimeout(saveDebounceTimer);
		saveDebounceTimer = setTimeout(() => {
			autoSaveAnswer(qId);
		}, 1200);
	}

	function handleToggleFlag() {
		if (!currentQuestion) return;
		const qId = currentQuestion.id;
		const next = new Set(flaggedIds);
		if (next.has(qId)) {
			next.delete(qId);
		} else {
			next.add(qId);
		}
		flaggedIds = next;
	}

	function jumpToSection(sectionSummary: SectionSummary) {
		if (sectionSummary.startIndex >= 0 && sectionSummary.startIndex < questions.length) {
			currentIndex = sectionSummary.startIndex;
		}
	}

	async function handleFinishExam() {
		isSubmittingFinal = true;
		try {
			await examsApi.finishExam(submissionId);
			toast.success('Exam successfully submitted for evaluation!');
			goto(`/exams/submissions/${submissionId}/result`);
		} catch (err: any) {
			toast.error(err?.message || 'Failed to submit exam.');
			goto(`/exams/submissions/${submissionId}/result`);
		} finally {
			isSubmittingFinal = false;
			isFinishModalOpen = false;
		}
	}
</script>

<div class="space-y-6">
	{#if isLoading}
		<div class="space-y-6 max-w-7xl mx-auto py-8">
			<div class="glass-panel h-16 rounded-2xl animate-pulse"></div>
			<div class="grid grid-cols-1 gap-6 lg:grid-cols-4">
				<div class="lg:col-span-3 h-96 rounded-3xl bg-base-200/50 animate-pulse"></div>
				<div class="h-96 rounded-3xl bg-base-200/50 animate-pulse"></div>
			</div>
		</div>
	{:else if loadError}
		<div class="glass-panel max-w-xl mx-auto p-8 rounded-3xl border border-error/30 text-center space-y-4 my-12 shadow-2xl">
			<div class="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-error/20 text-error">
				<AlertCircle class="h-8 w-8" />
			</div>
			<h2 class="text-xl font-bold text-base-content">Unable to Load Examination</h2>
			<p class="text-xs text-base-content/70">{loadError}</p>
			<div class="pt-2 flex justify-center gap-3">
				<a href="/my-courses" class="btn btn-ghost btn-sm rounded-xl">Back to My Courses</a>
				<a href="/dashboard" class="btn btn-primary btn-sm rounded-xl">Dashboard</a>
			</div>
		</div>
	{:else if questions.length > 0}
		<!-- Headless Background Snapshot Capture Engine (RealExam only) -->
		{#if isRealExam && examHub}
			<SnapshotEngine {submissionId} stream={cameraStream} {examHub} />
		{/if}

		<!-- Violation Overlay / Alert Modal -->
		<ViolationOverlay
			{currentViolations}
			{maxViolations}
			{isDisqualified}
			{terminationReason}
		/>

		<!-- Top Sticky Exam Bar -->
		<div class="glass-navbar sticky top-16 z-30 -mx-4 -mt-4 px-4 py-3 sm:-mx-6 sm:px-6 lg:-mx-8 lg:px-8 border-b border-base-content/10">
			<div class="mx-auto flex max-w-7xl items-center justify-between gap-4">
				<!-- Left: Title & Mode & Autosave -->
				<div class="flex items-center gap-3 min-w-0">
					<span class="badge {isRealExam ? 'badge-primary' : 'badge-neutral'} font-bold uppercase text-[11px] flex-shrink-0">
						{mode}
					</span>
					<div class="min-w-0 hidden md:block">
						<h1 class="text-sm font-bold text-base-content truncate max-w-xs lg:max-w-md" title={examTitle}>
							{examTitle}
						</h1>
					</div>
					<div class="hidden sm:flex items-center gap-2 text-xs text-base-content/70">
						{#if isSaving}
							<span class="flex items-center gap-1 text-primary animate-pulse font-medium">
								<Save class="h-3.5 w-3.5" /> Saving...
							</span>
						{:else if lastSavedTime}
							<span class="flex items-center gap-1 text-success text-[11px]">
								<CheckCircle2 class="h-3.5 w-3.5" /> Saved at {lastSavedTime}
							</span>
						{/if}
					</div>
				</div>

				<!-- Right: Timer & Finish Action -->
				<div class="flex items-center gap-3 flex-shrink-0">
					<ExamTimer bind:remainingSeconds onTimeout={handleFinishExam} />

					<button
						type="button"
						class="btn btn-primary btn-sm rounded-xl font-bold shadow-md shadow-primary/20 gap-1.5"
						onclick={() => (isFinishModalOpen = true)}
					>
						<span>Finish Exam</span>
					</button>
				</div>
			</div>
		</div>

		<!-- Section Navigation Bar (Split per Section) -->
		{#if hasMultipleSections}
			<div class="glass-card rounded-2xl p-3 border border-base-content/10 shadow-lg">
				<div class="flex items-center justify-between pb-2 border-b border-base-content/10 mb-2">
					<div class="flex items-center gap-2">
						<Layers class="w-4 h-4 text-primary" />
						<span class="text-xs font-bold uppercase tracking-wider text-base-content/70">
							Exam Sections ({sectionSummaries.length})
						</span>
					</div>
					<span class="text-xs font-medium text-base-content/60">
						Overall: <strong class="text-primary">{totalAnswered}</strong> / {questions.length} Answered
					</span>
				</div>

				<div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-2.5">
					{#each sectionSummaries as sec, secIdx (sec.id)}
						{@const isSecActive = currentSection?.id === sec.id}
						{@const isCompleted = sec.answeredCount === sec.totalQuestions}
						{@const progressPercent = Math.round((sec.answeredCount / sec.totalQuestions) * 100)}

						<button
							type="button"
							class="text-left p-2.5 rounded-xl border transition-all duration-200 cursor-pointer flex flex-col justify-between gap-1.5 {isSecActive
								? 'bg-primary/15 border-primary shadow-md shadow-primary/10 ring-1 ring-primary/40'
								: 'bg-base-100/50 border-base-content/10 hover:bg-base-100 hover:border-base-content/25'}"
							onclick={() => jumpToSection(sec)}
						>
							<div class="flex items-center justify-between gap-2">
								<div class="flex items-center gap-1.5 min-w-0">
									<Folder class="w-3.5 h-3.5 {isSecActive ? 'text-primary' : 'text-base-content/50'} flex-shrink-0" />
									<span class="text-xs font-bold truncate text-base-content" title={sec.title}>
										{secIdx + 1}. {sec.title}
									</span>
								</div>
								{#if isCompleted}
									<CheckCircle2 class="w-3.5 h-3.5 text-success flex-shrink-0" />
								{:else if sec.flaggedCount > 0}
									<span class="badge badge-warning badge-xs text-[9px] px-1 font-bold">
										{sec.flaggedCount}⚑
									</span>
								{/if}
							</div>

							<!-- Progress Info & Bar -->
							<div class="space-y-1">
								<div class="flex items-center justify-between text-[10px] text-base-content/70">
									<span>{sec.answeredCount} / {sec.totalQuestions} Qs</span>
									<span class="font-bold {isCompleted ? 'text-success' : 'text-primary'}">{progressPercent}%</span>
								</div>
								<div class="w-full bg-base-300 h-1 rounded-full overflow-hidden">
									<div
										class="h-full transition-all duration-300 {isCompleted ? 'bg-success' : 'bg-primary'}"
										style="width: {progressPercent}%"
									></div>
								</div>
							</div>
						</button>
					{/each}
				</div>
			</div>
		{/if}

		<!-- Main Question Area & Sidebar Palette -->
		<div class="grid grid-cols-1 gap-6 lg:grid-cols-4">
			<!-- Question Card & Navigation Actions -->
			<div class="space-y-6 lg:col-span-3">
				{#if currentQuestion}
					<QuestionCard
						question={currentQuestion}
						index={currentIndex}
						total={questions.length}
						sectionIndex={currentSectionQuestionIndex}
						sectionTotal={currentSection?.totalQuestions}
						selectedOptionIds={answers[currentQuestion.id]?.selectedOptionIds || []}
						essayText={answers[currentQuestion.id]?.essayText || ''}
						isFlagged={flaggedIds.has(currentQuestion.id)}
						onToggleOption={handleToggleOption}
						onEssayChange={handleEssayChange}
						onToggleFlag={handleToggleFlag}
					/>

					<!-- Bottom Nav Buttons -->
					<div class="flex items-center justify-between gap-3">
						<button
							type="button"
							class="btn btn-ghost glass-card btn-sm rounded-xl border border-base-content/10 gap-1.5"
							disabled={currentIndex === 0}
							onclick={() => (currentIndex -= 1)}
						>
							<ChevronLeft class="h-4 w-4" />
							<span>Previous Question</span>
						</button>

						<div class="flex items-center gap-2">
							{#if currentIndex < questions.length - 1}
								<button
									type="button"
									class="btn btn-primary btn-sm rounded-xl font-semibold shadow-md shadow-primary/20 gap-1.5"
									onclick={() => (currentIndex += 1)}
								>
									<span>Next Question</span>
									<ChevronRight class="h-4 w-4" />
								</button>
							{:else}
								<button
									type="button"
									class="btn btn-success btn-sm rounded-xl text-white font-bold shadow-lg shadow-success/20 gap-1.5"
									onclick={() => (isFinishModalOpen = true)}
								>
									<span>Review & Submit</span>
									<CheckCircle2 class="h-4 w-4" />
								</button>
							{/if}
						</div>
					</div>
				{/if}
			</div>

			<!-- Question Palette Sidebar (Section Split Enabled) -->
			<div class="space-y-4">
				<QuestionPalette
					{questions}
					{sections}
					{currentIndex}
					{answers}
					{flaggedIds}
					onSelectQuestion={(idx) => (currentIndex = idx)}
				/>
			</div>
		</div>

		<!-- Final Submission Confirmation Modal with Section Breakdown -->
		{#if isFinishModalOpen}
			<div class="modal modal-open z-50">
				<div class="modal-box bg-base-100/95 backdrop-blur-xl border border-base-content/10 shadow-2xl max-w-lg space-y-4">
					<div class="flex items-center justify-between border-b border-base-content/10 pb-3">
						<h3 class="font-bold text-base text-base-content flex items-center gap-2">
							<Send class="w-5 h-5 text-primary" />
							Submit Examination
						</h3>
						<button
							type="button"
							class="btn btn-xs btn-circle btn-ghost"
							onclick={() => (isFinishModalOpen = false)}
						>✕</button>
					</div>

					<p class="text-xs text-base-content/70">
						Please review your examination status across all sections before submitting.
					</p>

					<!-- Section Progress Summary List -->
					<div class="space-y-2 max-h-56 overflow-y-auto pr-1">
						{#each sectionSummaries as sec, idx}
							{@const isAllDone = sec.answeredCount === sec.totalQuestions}
							<div class="p-3 rounded-xl bg-base-200/50 border border-base-content/10 flex items-center justify-between gap-3">
								<div class="space-y-0.5 min-w-0">
									<p class="text-xs font-bold text-base-content truncate">
										{idx + 1}. {sec.title}
									</p>
									<p class="text-[11px] text-base-content/60">
										{sec.answeredCount} of {sec.totalQuestions} answered
										{#if sec.flaggedCount > 0}
											• <span class="text-warning font-semibold">{sec.flaggedCount} flagged</span>
										{/if}
									</p>
								</div>
								{#if isAllDone}
									<span class="badge badge-success badge-sm font-bold gap-1">
										<CheckCircle2 class="w-3 h-3" /> Complete
									</span>
								{:else}
									<span class="badge badge-warning badge-sm font-bold">
										{sec.totalQuestions - sec.answeredCount} Unanswered
									</span>
								{/if}
							</div>
						{/each}
					</div>

					<!-- Warning banner if incomplete -->
					{#if totalAnswered < questions.length}
						<div class="p-3 rounded-xl bg-warning/10 border border-warning/30 flex items-start gap-2.5 text-xs text-warning-content">
							<AlertCircle class="w-4 h-4 text-warning flex-shrink-0 mt-0.5" />
							<div>
								<span class="font-bold">Unanswered Questions:</span>
								<span>You have <strong>{questions.length - totalAnswered}</strong> unanswered question(s). Unanswered questions will receive 0 points.</span>
							</div>
						</div>
					{/if}

					<div class="modal-action pt-2">
						<button
							type="button"
							class="btn btn-sm btn-ghost"
							onclick={() => (isFinishModalOpen = false)}
							disabled={isSubmittingFinal}
						>
							Return to Exam
						</button>
						<button
							type="button"
							class="btn btn-sm btn-primary gap-1.5"
							onclick={handleFinishExam}
							disabled={isSubmittingFinal}
						>
							{#if isSubmittingFinal}
								<span class="loading loading-spinner loading-xs"></span>
							{:else}
								<Send class="w-4 h-4" />
							{/if}
							Confirm & Submit Final
						</button>
					</div>
				</div>
				<div class="modal-backdrop bg-black/40 backdrop-blur-sm" onclick={() => (isFinishModalOpen = false)}></div>
			</div>
		{/if}
	{/if}
</div>
