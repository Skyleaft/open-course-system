<script lang="ts">
	import { fade, scale } from 'svelte/transition';
	import { cubicOut } from 'svelte/easing';
	import {
		X,
		Check,
		Award,
		FileText,
		CheckCircle2,
		AlertCircle,
		Save,
		GraduationCap,
		HelpCircle,
		ShieldAlert,
		Sparkles,
		Percent,
		User,
		ExternalLink
	} from 'lucide-svelte';
	import type { ExamResultDetailsDto, QuestionReviewDto } from '$lib/api/types.ts';
	import { examsApi } from '$lib/api/exams.ts';
	import { assessmentsApi } from '$lib/api/assessments.ts';
	import { toast } from '$lib/stores/toast.svelte.ts';
	import RichRenderer from '$lib/components/editor/RichRenderer.svelte';

	interface Props {
		isOpen: boolean;
		submissionId: string | null;
		courseId?: string;
		studentName?: string;
		studentEmail?: string;
		studentId?: string;
		onClose: () => void;
		onGraded?: (updatedResult: ExamResultDetailsDto) => void;
	}

	let {
		isOpen,
		submissionId,
		courseId = '',
		studentName = '',
		studentEmail = '',
		studentId = '',
		onClose,
		onGraded
	}: Props = $props();

	let result = $state<ExamResultDetailsDto | null>(null);
	let isLoading = $state(false);
	let isSaving = $state(false);
	let isIssuingCert = $state(false);
	let certIssuedNumber = $state<string | null>(null);

	// Editable essay scores: Record<questionId, number>
	let essayScores = $state<Record<string, number>>({});
	let essayFeedbacks = $state<Record<string, string>>({});

	$effect(() => {
		if (isOpen && submissionId) {
			loadSubmissionResult(submissionId);
		} else {
			result = null;
			essayScores = {};
			essayFeedbacks = {};
			certIssuedNumber = null;
		}
	});

	async function loadSubmissionResult(id: string) {
		isLoading = true;
		try {
			const data = await examsApi.getResult(id);
			result = data;
			
			// Initialize essay score inputs
			const scores: Record<string, number> = {};
			const feedbacks: Record<string, string> = {};
			for (const q of data.questions) {
				if (q.type === 'Essay') {
					scores[q.questionId] = q.awardedScore ?? 0;
					feedbacks[q.questionId] = '';
				}
			}
			essayScores = scores;
			essayFeedbacks = feedbacks;
		} catch (err: any) {
			toast.error(err?.message || 'Failed to load candidate submission details.');
			onClose();
		} finally {
			isLoading = false;
		}
	}

	const essayQuestions = $derived(
		result?.questions.filter((q) => q.type === 'Essay') || []
	);

	const totalQuestions = $derived(result?.questions.length || 0);
	const totalMaxPoints = $derived(
		result?.questions.reduce((acc, q) => acc + (q.points || 0), 0) || 0
	);

	const currentTotalEarned = $derived.by(() => {
		if (!result) return 0;
		return result.questions.reduce((acc, q) => {
			if (q.type === 'Essay') {
				return acc + (essayScores[q.questionId] || 0);
			}
			return acc + (q.awardedScore || 0);
		}, 0);
	});

	const projectedScorePercentage = $derived.by(() => {
		if (totalMaxPoints <= 0) return 0;
		return Math.min(100, Math.round((currentTotalEarned / totalMaxPoints) * 100 * 10) / 10);
	});

	const isPassingProjected = $derived.by(() => {
		if (!result) return false;
		return projectedScorePercentage >= (result.appliedRules?.maxTabSwitchesAllowed ? 60 : 70); // passing percentage
	});

	function setQuickScore(questionId: string, maxPoints: number, factor: number) {
		essayScores[questionId] = Math.round(maxPoints * factor * 10) / 10;
	}

	async function handleSaveGrades() {
		if (!submissionId) return;

		const grades = Object.entries(essayScores).map(([questionId, score]) => ({
			questionId,
			score: Number(score) || 0,
			feedback: essayFeedbacks[questionId]?.trim() || undefined
		}));

		isSaving = true;
		try {
			const updated = await examsApi.gradeEssaySubmission(submissionId, grades);
			result = updated;
			toast.success(`Essay evaluations saved! Final Score: ${updated.score}% (${updated.isPassed ? 'PASSED' : 'FAILED'})`);
			onGraded?.(updated);
		} catch (err: any) {
			toast.error(err?.message || 'Failed to submit essay evaluations.');
		} finally {
			isSaving = false;
		}
	}

	async function handleIssueCertificate() {
		const targetStudentId = studentId || result?.examId; // fallback
		if (!targetStudentId || !courseId || !result) {
			toast.warning('Student ID or Course ID is missing for certificate generation.');
			return;
		}

		isIssuingCert = true;
		try {
			const finalScore = result.score ?? projectedScorePercentage;
			const cert = await assessmentsApi.issueCertificate(targetStudentId, courseId, finalScore);
			certIssuedNumber = cert.certificateNumber;
			toast.success(`Certificate ${cert.certificateNumber} issued successfully for this student!`);
		} catch (err: any) {
			toast.error(err?.message || 'Failed to issue certificate.');
		} finally {
			isIssuingCert = false;
		}
	}

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape' && isOpen && !isSaving && !isIssuingCert) {
			onClose();
		}
	}
</script>

<svelte:window onkeydown={handleKeydown} />

{#if isOpen}
	<div
		class="fixed inset-0 z-[1000] flex items-center justify-center p-3 sm:p-6 overflow-y-auto bg-black/80 backdrop-blur-md"
		role="dialog"
		aria-modal="true"
		transition:fade={{ duration: 160 }}
	>
		<!-- Backdrop click -->
		<div
			class="fixed inset-0 -z-10"
			onclick={() => {
				if (!isSaving && !isIssuingCert) onClose();
			}}
			role="presentation"
		></div>

		<div
			class="relative w-full max-w-4xl overflow-hidden rounded-3xl bg-base-100/95 backdrop-blur-2xl border border-base-content/10 shadow-2xl p-6 sm:p-8 space-y-6 my-auto max-h-[92vh] overflow-y-auto flex flex-col"
			transition:scale={{ duration: 200, start: 0.95, easing: cubicOut }}
		>
			<!-- Header -->
			<div class="flex items-start justify-between gap-4 pb-4 border-b border-base-content/10">
				<div class="flex items-center gap-3.5">
					<div
						class="w-12 h-12 rounded-2xl bg-primary/10 text-primary border border-primary/20 flex items-center justify-center shadow-xs shrink-0"
					>
						<FileText class="w-6 h-6" />
					</div>
					<div>
						<div class="flex items-center gap-2 flex-wrap">
							<h3 class="text-lg font-black text-base-content tracking-tight">
								Candidate Submission Review & Essay Grading
							</h3>
							{#if result?.status === 'Completed'}
								<span class="badge badge-success text-white badge-xs font-bold">Completed</span>
							{:else if result?.status === 'TimedOut'}
								<span class="badge badge-warning badge-xs font-bold">Timed Out</span>
							{:else if result?.status === 'Disqualified'}
								<span class="badge badge-error text-white badge-xs font-bold">Disqualified</span>
							{:else if result?.status}
								<span class="badge badge-info text-white badge-xs font-semibold">{result.status}</span>
							{/if}
						</div>
						<p class="text-xs text-base-content/60 mt-1 flex items-center gap-2 flex-wrap">
							<span class="flex items-center gap-1 font-semibold text-base-content/80">
								<User class="w-3 h-3 text-primary" />
								{studentName || 'Student Submission'}
							</span>
							{#if studentEmail}
								<span class="text-base-content/40">•</span>
								<span>{studentEmail}</span>
							{/if}
							{#if result?.examTitle}
								<span class="text-base-content/40">•</span>
								<strong class="text-primary">{result.examTitle}</strong>
							{/if}
						</p>
					</div>
				</div>

				<button
					type="button"
					class="btn btn-ghost btn-circle btn-sm text-base-content/50 hover:text-base-content"
					onclick={onClose}
					disabled={isSaving || isIssuingCert}
					aria-label="Close"
				>
					<X class="w-4 h-4" />
				</button>
			</div>

			{#if isLoading}
				<div class="py-16 text-center space-y-3">
					<span class="loading loading-spinner loading-lg text-primary"></span>
					<p class="text-xs text-base-content/60 font-medium">Loading candidate responses & test paper...</p>
				</div>
			{:else if result}
				<!-- Score & Summary Banner -->
				<div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
					<div class="p-3.5 rounded-2xl bg-base-200/50 border border-base-content/5 space-y-1">
						<p class="text-[10px] uppercase font-bold text-base-content/50 tracking-wider">Total Questions</p>
						<p class="text-lg font-black text-base-content">{totalQuestions} Items</p>
						<p class="text-[10px] text-base-content/50 font-medium">{essayQuestions.length} Essay Prompt(s)</p>
					</div>

					<div class="p-3.5 rounded-2xl bg-base-200/50 border border-base-content/5 space-y-1">
						<p class="text-[10px] uppercase font-bold text-base-content/50 tracking-wider">Points Earned</p>
						<p class="text-lg font-black text-base-content font-mono">{currentTotalEarned.toFixed(1)} / {totalMaxPoints} pts</p>
						<p class="text-[10px] text-base-content/50 font-medium">Max raw question points</p>
					</div>

					<div class="p-3.5 rounded-2xl bg-primary/10 border border-primary/20 space-y-1">
						<p class="text-[10px] uppercase font-bold text-primary tracking-wider">Calculated Score</p>
						<p class="text-lg font-black text-primary font-mono">{projectedScorePercentage}%</p>
						<div class="flex items-center gap-1">
							{#if (result.isPassed !== null && result.isPassed !== undefined ? result.isPassed : isPassingProjected)}
								<span class="badge badge-success text-white badge-xs font-bold">Passed</span>
							{:else}
								<span class="badge badge-error text-white badge-xs font-bold">Failed</span>
							{/if}
						</div>
					</div>

					<div class="p-3.5 rounded-2xl bg-base-200/50 border border-base-content/5 space-y-1">
						<p class="text-[10px] uppercase font-bold text-base-content/50 tracking-wider">Submitted Date</p>
						<p class="text-xs font-bold text-base-content truncate">
							{result.submittedAtUtc ? new Date(result.submittedAtUtc).toLocaleDateString() : 'Active'}
						</p>
						<p class="text-[10px] text-base-content/50 font-mono">
							{result.submittedAtUtc ? new Date(result.submittedAtUtc).toLocaleTimeString() : 'In Progress'}
						</p>
					</div>
				</div>

				<!-- Questions List -->
				<div class="space-y-4">
					<div class="flex items-center justify-between">
						<h4 class="text-xs font-black uppercase tracking-wider text-base-content/80 flex items-center gap-1.5">
							<FileText class="w-3.5 h-3.5 text-primary" />
							<span>Questions & Answers Evaluation ({result.questions.length})</span>
						</h4>
						<span class="text-[10px] text-base-content/50 font-normal">
							Review choices and grade essay responses below
						</span>
					</div>

					<div class="space-y-3.5">
						{#each result.questions as q, idx (q.questionId || idx)}
							{@const isEssay = q.type === 'Essay'}
							{@const isObjective = !isEssay}

							<div
								class="p-4 sm:p-5 rounded-2xl border transition-all {isEssay
									? 'border-primary/30 bg-primary/5 shadow-xs'
									: 'border-base-content/10 bg-base-200/30'}"
							>
								<!-- Question Header -->
								<div class="flex items-start justify-between gap-3 mb-2.5">
									<div class="flex items-center gap-2 flex-wrap">
										<span class="w-6 h-6 rounded-lg bg-base-300 text-base-content/70 font-mono font-bold text-xs flex items-center justify-center shrink-0">
											{idx + 1}
										</span>
										<span class="badge badge-sm font-bold text-[10px] {isEssay ? 'badge-primary' : 'badge-outline badge-primary'}">
											{q.type}
										</span>
										<span class="badge badge-sm badge-neutral font-mono text-[10px] font-bold">
											{q.points} pts max
										</span>
									</div>

									{#if isObjective}
										<div class="flex items-center gap-1 shrink-0 font-mono text-xs font-bold">
											<span class="text-base-content/60">Score:</span>
											<span class={(q.awardedScore ?? 0) > 0 ? 'text-success' : 'text-error'}>
												{q.awardedScore ?? 0} / {q.points}
											</span>
										</div>
									{/if}
								</div>

								<!-- Question Statement -->
								<div class="text-sm font-semibold text-base-content mb-3">
									<RichRenderer content={q.questionText} />
								</div>

								{#if isObjective}
									<!-- Objective Choices Preview -->
									<div class="grid grid-cols-1 sm:grid-cols-2 gap-2 mt-2 pt-2 border-t border-base-content/10">
										{#each q.options as opt, optIdx}
											{@const isSelectedByStudent = q.selectedOptionIds.includes(opt.id)}
											{@const isCorrectAnswer = opt.isCorrect}

											<div
												class="p-2.5 rounded-xl text-xs flex items-center gap-2 border transition-all {isSelectedByStudent && isCorrectAnswer
													? 'bg-success/15 border-success/40 text-success font-semibold'
													: isSelectedByStudent && !isCorrectAnswer
														? 'bg-error/15 border-error/40 text-error font-semibold'
														: isCorrectAnswer
															? 'bg-success/5 border-success/20 text-success/80'
															: 'bg-base-100/50 border-base-content/10 text-base-content/70'}"
											>
												<span
													class="w-5 h-5 rounded-full flex items-center justify-center text-[10px] font-bold shrink-0 {isSelectedByStudent && isCorrectAnswer
														? 'bg-success text-success-content'
														: isSelectedByStudent && !isCorrectAnswer
															? 'bg-error text-error-content'
															: isCorrectAnswer
																? 'bg-success/20 text-success'
																: 'bg-base-300 text-base-content/60'}"
												>
													{String.fromCharCode(65 + optIdx)}
												</span>
												<span class="truncate flex-1">{opt.text}</span>
												{#if isSelectedByStudent && isCorrectAnswer}
													<span class="badge badge-xs badge-success text-white font-bold">Selected ✓</span>
												{:else if isSelectedByStudent}
													<span class="badge badge-xs badge-error text-white font-bold">Selected ✗</span>
												{:else if isCorrectAnswer}
													<span class="badge badge-xs badge-ghost text-[9px]">Key</span>
												{/if}
											</div>
										{/each}
									</div>
								{:else}
									<!-- Essay Response & Interactive Grading Studio -->
									<div class="space-y-3 mt-3 pt-3 border-t border-primary/20">
										<!-- Student Essay Answer -->
										<div class="space-y-1.5">
											<label class="text-[11px] font-bold uppercase tracking-wider text-base-content/60">
												Candidate Written Essay Response:
											</label>
											<div class="p-3.5 rounded-2xl bg-base-100/80 border border-base-content/15 text-xs text-base-content font-normal leading-relaxed min-h-[70px]">
												{#if q.essayText && q.essayText.trim()}
													<RichRenderer content={q.essayText} />
												{:else}
													<span class="text-base-content/40 italic">No essay response submitted by candidate.</span>
												{/if}
											</div>
										</div>

										<!-- Score Assignment Form -->
										<div class="p-3 rounded-2xl bg-primary/10 border border-primary/20 flex items-center justify-between gap-4 flex-wrap">
											<div class="flex items-center gap-2">
												<Award class="w-4 h-4 text-warning" />
												<span class="text-xs font-bold text-base-content">Awarded Score:</span>
												<input
													type="number"
													step="0.5"
													min="0"
													max={q.points}
													bind:value={essayScores[q.questionId]}
													class="input input-sm w-20 bg-base-100 text-center font-mono font-bold text-sm rounded-xl border-primary/40 focus:border-primary"
												/>
												<span class="text-xs font-mono font-bold text-base-content/60">/ {q.points} pts</span>
											</div>

											<div class="flex items-center gap-1.5">
												<span class="text-[10px] text-base-content/50 uppercase font-semibold mr-1">Quick:</span>
												<button
													type="button"
													class="btn btn-xs btn-ghost rounded-lg font-bold hover:bg-base-200"
													onclick={() => setQuickScore(q.questionId, q.points, 0)}
												>
													0 pts
												</button>
												<button
													type="button"
													class="btn btn-xs btn-ghost rounded-lg font-bold hover:bg-base-200"
													onclick={() => setQuickScore(q.questionId, q.points, 0.5)}
												>
													50%
												</button>
												<button
													type="button"
													class="btn btn-xs btn-primary btn-outline rounded-lg font-bold"
													onclick={() => setQuickScore(q.questionId, q.points, 1)}
												>
													Max ({q.points})
												</button>
											</div>
										</div>
									</div>
								{/if}
							</div>
						{/each}
					</div>
				</div>
			{/if}

			<!-- Footer Action Buttons -->
			<div class="flex items-center justify-between gap-3 pt-4 border-t border-base-content/10 mt-auto">
				<div>
					{#if certIssuedNumber}
						<span class="badge badge-success text-white font-bold gap-1 text-xs py-2 px-3">
							<GraduationCap class="w-3.5 h-3.5" />
							Certificate Issued: {certIssuedNumber}
						</span>
					{:else if courseId && (result?.isPassed || isPassingProjected)}
						<button
							type="button"
							class="btn btn-sm btn-accent text-white rounded-xl font-bold gap-1.5 shadow-xs"
							onclick={handleIssueCertificate}
							disabled={isIssuingCert || isSaving || isLoading}
						>
							{#if isIssuingCert}
								<span class="loading loading-spinner loading-xs"></span>
							{:else}
								<GraduationCap class="w-4 h-4" />
							{/if}
							<span>Issue Certificate</span>
						</button>
					{/if}
				</div>

				<div class="flex items-center gap-2">
					<button
						type="button"
						class="btn btn-sm btn-ghost rounded-xl font-semibold"
						onclick={onClose}
						disabled={isSaving || isIssuingCert}
					>
						Close
					</button>

					{#if essayQuestions.length > 0}
						<button
							type="button"
							class="btn btn-sm btn-primary rounded-xl font-bold shadow-md gap-1.5 px-6"
							onclick={handleSaveGrades}
							disabled={isSaving || isLoading}
						>
							{#if isSaving}
								<span class="loading loading-spinner loading-xs"></span>
							{:else}
								<Save class="w-4 h-4" />
							{/if}
							<span>Save & Finalize Grades</span>
						</button>
					{/if}
				</div>
			</div>
		</div>
	</div>
{/if}
