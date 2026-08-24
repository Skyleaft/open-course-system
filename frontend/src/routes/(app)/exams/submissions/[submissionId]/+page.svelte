<script lang="ts">
	import { page } from '$app/state';
	import { examsApi } from '#lib/api/exams.ts';
	import type { QuizQuestion, QuestionType } from '#lib/api/types.ts';
	import { ExamHubClient } from '#lib/signalr/exam-hub.svelte.ts';
	import { bindSecurityInterceptors } from '#lib/utils/security.ts';
	import ExamTimer from '#lib/components/exam/ExamTimer.svelte';
	import QuestionPalette from '#lib/components/exam/QuestionPalette.svelte';
	import QuestionCard from '#lib/components/exam/QuestionCard.svelte';
	import ViolationOverlay from '#lib/components/exam/ViolationOverlay.svelte';
	import SnapshotEngine from '#lib/components/exam/SnapshotEngine.svelte';
	import ConfirmModal from '#lib/components/ui/ConfirmModal.svelte';
	import { toast } from '#lib/stores/toast.svelte.ts';
	import { goto } from '$app/navigation';
	import {
		ChevronLeft,
		ChevronRight,
		CheckCircle2,
		Save,
		ShieldAlert,
		LogOut,
		Sparkles,
		AlertCircle
	} from '@lucide/svelte';
	import { onMount, onDestroy } from 'svelte';

	const submissionId = (page.params.submissionId || '') as string;
	let questions = $state<QuizQuestion[]>([]);
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

			// Map questions to match frontend QuizQuestion structure
			questions = res.questions.map((q) => ({
				id: q.id,
				quizId: res.examId,
				examId: res.examId,
				text: q.questionText,
				questionText: q.questionText,
				type: q.type as QuestionType,
				points: Number(q.points),
				orderIndex: q.displayOrder,
				options: (q.options || []).map((o) => ({ id: o.id, text: o.text }))
			}));

			// Restore saved answers from Redis buffer (which is returned with each question)
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

			// 2. Accurately calculate remaining seconds from maxAllowedEndTimeUtc (resuming across tab closures)
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

			// 3. Resolve active session token (from API response or sessionStorage)
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
					// Synchronize timer with server
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
			lastSavedTime = new Date().toLocaleTimeString();
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

		// Immediate autosave on selection
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

		// Debounce autosave for essay typing (1.5s)
		if (saveDebounceTimer) clearTimeout(saveDebounceTimer);
		saveDebounceTimer = setTimeout(() => {
			autoSaveAnswer(qId);
		}, 1500);
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

	async function handleFinishExam() {
		isSubmittingFinal = true;
		try {
			await examsApi.finishExam(submissionId);
			toast.success('Exam finished and submitted for evaluation!');
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
		<div class="glass-navbar sticky top-16 z-30 -mx-4 -mt-4 px-4 py-3 sm:-mx-6 sm:px-6 lg:-mx-8 lg:px-8 border-b">
			<div class="mx-auto flex max-w-7xl items-center justify-between gap-4">
				<div class="flex items-center gap-3">
					<span class="badge {isRealExam ? 'badge-primary' : 'badge-ghost'} font-bold uppercase text-xs">
						{mode}
					</span>
					<div class="hidden sm:flex items-center gap-2 text-xs text-base-content/70">
						{#if isSaving}
							<span class="flex items-center gap-1 text-primary animate-pulse font-medium">
								<Save class="h-3.5 w-3.5" /> Saving answer...
							</span>
						{:else if lastSavedTime}
							<span class="flex items-center gap-1 text-success text-[11px]">
								<CheckCircle2 class="h-3.5 w-3.5" /> Saved at {lastSavedTime}
							</span>
						{/if}
					</div>
				</div>

				<!-- Timer & Finish Action -->
				<div class="flex items-center gap-3">
					<ExamTimer bind:remainingSeconds onTimeout={handleFinishExam} />

					<button
						class="btn btn-primary gradient-accent btn-sm rounded-xl text-white font-bold border-0 shadow-md"
						onclick={() => (isFinishModalOpen = true)}
					>
						Finish Exam
					</button>
				</div>
			</div>
		</div>

		<!-- Main Question Area & Sidebar Palette -->
		<div class="grid grid-cols-1 gap-6 lg:grid-cols-4">
			<!-- Question Card & Navigation Actions -->
			<div class="space-y-6 lg:col-span-3">
				{#if currentQuestion}
					<QuestionCard
						question={currentQuestion}
						index={currentIndex}
						total={questions.length}
						selectedOptionIds={answers[currentQuestion.id]?.selectedOptionIds || []}
						essayText={answers[currentQuestion.id]?.essayText || ''}
						isFlagged={flaggedIds.has(currentQuestion.id)}
						onToggleOption={handleToggleOption}
						onEssayChange={handleEssayChange}
						onToggleFlag={handleToggleFlag}
					/>

					<!-- Bottom Nav Buttons -->
					<div class="flex items-center justify-between">
						<button
							class="btn btn-ghost glass-card btn-sm rounded-xl border border-white/10 gap-1.5"
							disabled={currentIndex === 0}
							onclick={() => (currentIndex -= 1)}
						>
							<ChevronLeft class="h-4 w-4" />
							Previous Question
						</button>

						{#if currentIndex < questions.length - 1}
							<button
								class="btn btn-primary gradient-accent btn-sm rounded-xl text-white font-semibold border-0 shadow-md gap-1.5"
								onclick={() => (currentIndex += 1)}
							>
								Next Question
								<ChevronRight class="h-4 w-4" />
							</button>
						{:else}
							<button
								class="btn btn-success btn-sm rounded-xl text-white font-bold border-0 shadow-lg gap-1.5"
								onclick={() => (isFinishModalOpen = true)}
							>
								Review & Submit
								<CheckCircle2 class="h-4 w-4" />
							</button>
						{/if}
					</div>
				{/if}
			</div>

			<!-- Question Palette Sidebar -->
			<div class="space-y-4">
				<QuestionPalette
					{questions}
					{currentIndex}
					{answers}
					{flaggedIds}
					onSelectQuestion={(idx) => (currentIndex = idx)}
				/>
			</div>
		</div>

		<!-- Final Submission Confirmation Modal -->
		<ConfirmModal
			isOpen={isFinishModalOpen}
			title="Submit Examination"
			message="Are you sure you want to finish and submit your exam? All buffered answers will be finalized and evaluated."
			confirmText="Yes, Finalize Submission"
			isLoading={isSubmittingFinal}
			onConfirm={handleFinishExam}
			onCancel={() => (isFinishModalOpen = false)}
		/>
	{/if}
</div>
