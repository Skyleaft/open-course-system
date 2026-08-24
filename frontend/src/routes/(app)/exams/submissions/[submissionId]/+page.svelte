<script lang="ts">
	import { page } from '$app/state';
	import { examsApi } from '#lib/api/exams.ts';
	import type { QuizQuestion } from '#lib/api/types.ts';
	import { ExamHubClient } from '#lib/signalr/exam-hub.ts';
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
		Sparkles
	} from '@lucide/svelte';
	import { onMount, onDestroy } from 'svelte';

	const submissionId = (page.params.submissionId || '') as string;
	let questions = $state<QuizQuestion[]>([]);
	let currentIndex = $state(0);
	let remainingSeconds = $state(3600);
	let mode = $state('RealExam');

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

	// Modals
	let isFinishModalOpen = $state(false);
	let isSubmittingFinal = $state(false);

	let examHub = $state<ExamHubClient | null>(null);
	let unbindSecurity: (() => void) | null = null;
	let cameraStream = $state<MediaStream | null>(null);

	const currentQuestion = $derived(questions[currentIndex]);
	const isRealExam = $derived(mode === 'RealExam');

	onMount(async () => {
		const sessionToken = sessionStorage.getItem(`exam_token_${submissionId}`) || 'session-token-fallback';

		// 1. Initialize SignalR ExamHub
		examHub = new ExamHubClient();
		await examHub.start();
		await examHub.joinExamRoom(submissionId, sessionToken);

		// 2. Setup Server-to-Client listeners
		examHub.onSyncTimer((remaining) => {
			remainingSeconds = remaining;
		});

		examHub.onViolationWarning((count, max) => {
			currentViolations = count;
			maxViolations = max;
			toast.warning(`Security Violation Warning: ${count} of ${max}`);
		});

		examHub.onForceDisconnectExam((reason) => {
			isDisqualified = true;
			terminationReason = reason;
			toast.error(`Exam terminated: ${reason}`);
		});

		// 3. Bind security interceptors (if RealExam)
		if (isRealExam) {
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

		// 4. Fetch Questions & Recover buffered answers from Redis
		try {
			const res = await examsApi.getQuestions(submissionId);
			if (res?.questions && res.questions.length > 0) {
				questions = res.questions;
				mode = res.mode || 'RealExam';
				remainingSeconds = res.remainingSeconds || 3600;
				if (res.savedAnswers) {
					answers = res.savedAnswers;
				}
			} else {
				loadMockQuestions();
			}
		} catch {
			loadMockQuestions();
		}
	});

	onDestroy(() => {
		if (unbindSecurity) unbindSecurity();
		if (examHub) examHub.stop();
		if (cameraStream) {
			cameraStream.getTracks().forEach((t) => t.stop());
		}
	});

	function loadMockQuestions() {
		questions = [
			{
				id: 'q-1',
				quizId: 'ex-1',
				text: 'In the **Raft consensus algorithm**, which state is a node in when it first starts up before election timeout?',
				type: 'SingleChoice',
				points: 4,
				orderIndex: 1,
				options: [
					{ id: 'opt-1', text: 'Follower' },
					{ id: 'opt-2', text: 'Candidate' },
					{ id: 'opt-3', text: 'Leader' },
					{ id: 'opt-4', text: 'Observer' }
				]
			},
			{
				id: 'q-2',
				quizId: 'ex-1',
				text: 'Select all mechanisms used by **Redis Streams** to guarantee message durability and consumer group distribution:',
				type: 'MultipleChoice',
				points: 6,
				orderIndex: 2,
				options: [
					{ id: 'opt-5', text: 'Append-Only File (AOF) persistence' },
					{ id: 'opt-6', text: 'Pending Entries List (PEL) for message acknowledgment' },
					{ id: 'opt-7', text: 'Automatic round-robin consumer dispatch' },
					{ id: 'opt-8', text: 'Single consumer locking across all groups' }
				]
			},
			{
				id: 'q-3',
				quizId: 'ex-1',
				text: 'Explain the benefits of **Zero DB Writes** during active exam runtime using Redis In-Memory Answer Buffering:',
				type: 'Essay',
				points: 10,
				orderIndex: 3,
				options: []
			}
		];
	}

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
			console.warn('Autosave failed, answer remains in local memory:', err);
		} finally {
			isSaving = false;
		}
	}

	function handleToggleOption(optionId: string, isSingle: boolean) {
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
		const qId = currentQuestion.id;
		answers[qId] = {
			...answers[qId],
			selectedOptionIds: answers[qId]?.selectedOptionIds || [],
			essayText: text
		};
		autoSaveAnswer(qId);
	}

	function handleToggleFlag() {
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
			// Fallback redirect to results
			goto(`/exams/submissions/${submissionId}/result`);
		} finally {
			isSubmittingFinal = false;
			isFinishModalOpen = false;
		}
	}
</script>

<div class="space-y-6">
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
</div>
