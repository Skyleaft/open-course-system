<script lang="ts">
	import { page } from '$app/state';
	import { examsApi } from '#lib/api/exams.ts';
	import type { ExamResultDetailsDto, QuestionReviewDto } from '#lib/api/types.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import RichRenderer from '#lib/components/editor/RichRenderer.svelte';
	import {
		Award,
		CheckCircle2,
		XCircle,
		Clock,
		ArrowRight,
		ShieldCheck,
		Sparkles,
		AlertCircle,
		HelpCircle,
		RotateCcw,
		FileText,
		Layers,
		Check,
		X,
		BookOpen
	} from '@lucide/svelte';
	import { onMount } from 'svelte';

	const submissionId = (page.params.submissionId || '') as string;
	let result = $state<ExamResultDetailsDto | null>(null);
	let isLoading = $state(true);
	let loadError = $state<string | null>(null);
	let selectedFilter = $state<'all' | 'correct' | 'incorrect' | 'essay'>('all');

	const isSimulation = $derived(
		result?.appliedRules ? result.appliedRules.canTabSwitch : result?.mode === 'Simulation'
	);
	const isPassed = $derived(
		result?.isPassed === true || (result?.score != null && Number(result.score) >= 70)
	);
	const scoreValue = $derived(
		result?.score != null ? Math.round(Number(result.score) * 10) / 10 : null
	);

	const questions = $derived(result?.questions || []);
	const totalPointsEarned = $derived(
		questions.reduce((sum, q) => sum + (Number(q.awardedScore) || 0), 0)
	);
	const totalPossiblePoints = $derived(
		questions.reduce((sum, q) => sum + (Number(q.points) || 0), 0)
	);

	const correctCount = $derived(
		questions.filter(
			(q) => q.type !== 'Essay' && q.awardedScore != null && Number(q.awardedScore) > 0
		).length
	);
	const incorrectCount = $derived(
		questions.filter(
			(q) => q.type !== 'Essay' && (q.awardedScore == null || Number(q.awardedScore) === 0)
		).length
	);
	const essayCount = $derived(questions.filter((q) => q.type === 'Essay').length);

	const filteredQuestions = $derived.by(() => {
		if (selectedFilter === 'correct') {
			return questions.filter(
				(q) => q.type !== 'Essay' && q.awardedScore != null && Number(q.awardedScore) > 0
			);
		}
		if (selectedFilter === 'incorrect') {
			return questions.filter(
				(q) => q.type !== 'Essay' && (q.awardedScore == null || Number(q.awardedScore) === 0)
			);
		}
		if (selectedFilter === 'essay') {
			return questions.filter((q) => q.type === 'Essay');
		}
		return questions;
	});

	onMount(async () => {
		try {
			const res = await examsApi.getResult(submissionId);
			if (res) {
				result = res;
			} else {
				loadError = 'Examination result data could not be retrieved.';
			}
		} catch (err: any) {
			console.error('Failed to load exam result:', err);
			loadError = err?.message || 'Failed to retrieve exam results. Please verify your attempt ID.';
		} finally {
			isLoading = false;
		}
	});

	function formatDate(dateStr?: string | null): string {
		if (!dateStr) return 'N/A';
		try {
			return new Date(dateStr).toLocaleString(undefined, {
				dateStyle: 'medium',
				timeStyle: 'short'
			});
		} catch {
			return dateStr;
		}
	}

	function calculateDuration(): string {
		if (!result?.startedAtUtc || !result?.submittedAtUtc) return 'N/A';
		const start = new Date(result.startedAtUtc).getTime();
		const end = new Date(result.submittedAtUtc).getTime();
		const diffMinutes = Math.max(1, Math.round((end - start) / 60000));
		return `${diffMinutes} mins`;
	}
</script>

<div class="max-w-5xl mx-auto space-y-8 py-6">
	{#if isLoading}
		<div class="space-y-6">
			<div class="glass-panel h-80 rounded-3xl animate-pulse"></div>
			<div class="space-y-4">
				<div class="h-40 rounded-2xl bg-base-200/50 animate-pulse"></div>
				<div class="h-40 rounded-2xl bg-base-200/50 animate-pulse"></div>
			</div>
		</div>
	{:else if loadError}
		<div class="glass-panel max-w-lg mx-auto p-8 rounded-3xl border border-error/30 text-center space-y-4 my-12 shadow-2xl">
			<div class="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-error/20 text-error">
				<AlertCircle class="h-8 w-8" />
			</div>
			<h2 class="text-xl font-bold text-base-content">Result Not Found</h2>
			<p class="text-xs text-base-content/70">{loadError}</p>
			<div class="pt-2 flex justify-center gap-3">
				<a href="/my-courses" class="btn btn-ghost btn-sm rounded-xl">Back to My Courses</a>
				<a href="/dashboard" class="btn btn-primary btn-sm rounded-xl">Dashboard</a>
			</div>
		</div>
	{:else if result}
		<!-- Header & Breadcrumb -->
		<div class="flex flex-wrap items-center justify-between gap-4">
			<div class="space-y-1">
				<div class="flex items-center gap-2 text-xs font-semibold text-base-content/60">
					<a href="/dashboard" class="hover:text-primary transition-colors">Dashboard</a>
					<span>/</span>
					<a href="/my-courses" class="hover:text-primary transition-colors">My Courses</a>
					<span>/</span>
					<span class="text-base-content/90">Exam Result</span>
				</div>
				<h1 class="text-2xl sm:text-3xl font-extrabold text-base-content tracking-tight">
					{result.examTitle}
				</h1>
			</div>

			<div class="flex items-center gap-2">
				<span class="badge {result.mode === 'RealExam' ? 'badge-primary' : 'badge-secondary'} font-bold uppercase text-xs">
					{result.mode}
				</span>
				<span class="badge {result.status === 'Completed' ? 'badge-success text-white' : result.status === 'TimedOut' ? 'badge-warning text-white' : 'badge-error text-white'} font-bold text-xs">
					{result.status}
				</span>
			</div>
		</div>

		<!-- Score Banner -->
		<div class="glass-panel text-center rounded-3xl border border-white/10 p-8 sm:p-12 shadow-2xl relative overflow-hidden space-y-6">
			<!-- Background Glow Accent -->
			<div class="pointer-events-none absolute -top-24 -left-24 h-72 w-72 rounded-full {isPassed ? 'bg-success/15' : 'bg-warning/15'} blur-3xl"></div>
			<div class="pointer-events-none absolute -bottom-24 -right-24 h-72 w-72 rounded-full bg-primary/10 blur-3xl"></div>

			<!-- Status Icon -->
			<div class="mx-auto flex h-20 w-20 items-center justify-center rounded-3xl {isPassed ? 'bg-success/20 text-success border border-success/30' : 'bg-warning/20 text-warning border border-warning/30'} shadow-xl">
				{#if isPassed}
					<Award class="h-10 w-10" />
				{:else}
					<XCircle class="h-10 w-10" />
				{/if}
			</div>

			<!-- Grade and Percentage -->
			<div class="space-y-2 relative z-10">
				<span class="badge {isPassed ? 'badge-success' : 'badge-warning'} badge-sm font-bold uppercase tracking-wider text-white">
					{isPassed ? 'Passed' : 'Needs Improvement'}
				</span>
				<h2 class="text-3xl font-extrabold text-base-content tracking-tight sm:text-4xl">
					Examination Summary
				</h2>
				<div class="text-5xl sm:text-6xl font-black {isPassed ? 'text-success' : 'text-warning'} pt-2 tracking-tight">
					{scoreValue != null ? `${scoreValue}%` : 'Grading...'}
				</div>
				<p class="text-xs font-semibold text-base-content/60">
					{totalPointsEarned} of {totalPossiblePoints} points earned
				</p>
			</div>

			<!-- Metrics Grid -->
			<div class="grid grid-cols-2 sm:grid-cols-4 gap-3 border-t border-white/10 pt-6 text-xs text-base-content/70">
				<div class="glass-card rounded-2xl p-3 border border-white/5 space-y-1">
					<div class="text-[10px] uppercase font-bold text-base-content/50">Correct Answers</div>
					<div class="text-base font-bold text-success flex items-center justify-center gap-1">
						<Check class="h-4 w-4" /> {correctCount} / {questions.length}
					</div>
				</div>
				<div class="glass-card rounded-2xl p-3 border border-white/5 space-y-1">
					<div class="text-[10px] uppercase font-bold text-base-content/50">Incorrect Answers</div>
					<div class="text-base font-bold text-error flex items-center justify-center gap-1">
						<X class="h-4 w-4" /> {incorrectCount}
					</div>
				</div>
				<div class="glass-card rounded-2xl p-3 border border-white/5 space-y-1">
					<div class="text-[10px] uppercase font-bold text-base-content/50">Duration</div>
					<div class="text-base font-bold text-base-content flex items-center justify-center gap-1">
						<Clock class="h-4 w-4 text-primary" /> {calculateDuration()}
					</div>
				</div>
				<div class="glass-card rounded-2xl p-3 border border-white/5 space-y-1">
					<div class="text-[10px] uppercase font-bold text-base-content/50">Submitted On</div>
					<div class="text-xs font-bold text-base-content truncate">
						{formatDate(result.submittedAtUtc)}
					</div>
				</div>
			</div>

			<!-- Navigation Actions -->
			<div class="flex flex-wrap justify-center gap-3 pt-2">
				<a href="/my-courses" class="btn btn-ghost glass-card btn-sm rounded-xl border border-white/10 hover:bg-base-100/40">
					My Courses
				</a>
				<a href="/dashboard" class="btn btn-ghost glass-card btn-sm rounded-xl border border-white/10 hover:bg-base-100/40">
					Return to Dashboard
				</a>
				{#if isSimulation}
					<a href="/exams/{result.examId}/start" class="btn btn-secondary btn-sm rounded-xl font-bold shadow-md gap-1.5">
						<RotateCcw class="h-4 w-4" />
						Retake Simulation
					</a>
				{/if}
				{#if isPassed}
					<a href="/certificates" class="btn btn-primary gradient-accent btn-sm rounded-xl text-white font-bold border-0 shadow-md gap-1.5">
						View Certificate
						<ArrowRight class="h-4 w-4" />
					</a>
				{/if}
			</div>
		</div>

		<!-- Question Review Section with Explanations -->
		{#if questions.length > 0}
			<div class="space-y-6">
				<div class="flex flex-wrap items-center justify-between gap-4">
					<div class="flex items-center gap-2 text-lg font-bold text-base-content">
						<Sparkles class="h-5 w-5 text-primary" />
						Question Breakdown & Answers
					</div>

					<!-- Filter Tabs -->
					<div class="tabs tabs-boxed bg-base-200/60 p-1 rounded-2xl text-xs">
						<button
							class="tab tab-sm rounded-xl font-semibold transition-all {selectedFilter === 'all' ? 'tab-active !bg-primary !text-white' : ''}"
							onclick={() => (selectedFilter = 'all')}
						>
							All ({questions.length})
						</button>
						<button
							class="tab tab-sm rounded-xl font-semibold transition-all {selectedFilter === 'correct' ? 'tab-active !bg-success !text-white' : ''}"
							onclick={() => (selectedFilter = 'correct')}
						>
							Correct ({correctCount})
						</button>
						<button
							class="tab tab-sm rounded-xl font-semibold transition-all {selectedFilter === 'incorrect' ? 'tab-active !bg-error !text-white' : ''}"
							onclick={() => (selectedFilter = 'incorrect')}
						>
							Incorrect ({incorrectCount})
						</button>
						{#if essayCount > 0}
							<button
								class="tab tab-sm rounded-xl font-semibold transition-all {selectedFilter === 'essay' ? 'tab-active !bg-warning !text-white' : ''}"
								onclick={() => (selectedFilter = 'essay')}
							>
								Essay ({essayCount})
							</button>
						{/if}
					</div>
				</div>

				<!-- Questions List -->
				<div class="space-y-4">
					{#each filteredQuestions as q, idx (q.questionId)}
						{@const isQuestionCorrect = q.type !== 'Essay' && q.awardedScore != null && Number(q.awardedScore) > 0}
						{@const isEssay = q.type === 'Essay'}

						<GlassCard class="space-y-4 p-6 border {isEssay ? 'border-warning/30' : isQuestionCorrect ? 'border-success/30' : 'border-error/30'}">
							<!-- Question Header -->
							<div class="flex items-center justify-between border-b border-white/10 pb-3 text-xs">
								<div class="flex items-center gap-2">
									<span class="font-bold text-sm text-base-content">Question {idx + 1}</span>
									<span class="badge badge-ghost badge-sm uppercase text-[10px] font-bold">
										{q.type}
									</span>
								</div>

								<div class="flex items-center gap-2">
									{#if isEssay}
										<span class="badge badge-warning badge-sm font-bold text-white">
											{q.awardedScore != null ? `${q.awardedScore} / ${q.points} pts` : `Pending Evaluation (${q.points} pts)`}
										</span>
									{:else if isQuestionCorrect}
										<span class="badge badge-success badge-sm font-bold text-white flex items-center gap-1">
											<CheckCircle2 class="h-3 w-3" />
											+{q.awardedScore || q.points} / {q.points} pts
										</span>
									{:else}
										<span class="badge badge-error badge-sm font-bold text-white flex items-center gap-1">
											<XCircle class="h-3 w-3" />
											0 / {q.points} pts
										</span>
									{/if}
								</div>
							</div>

							<!-- Prompt Text -->
							<div class="text-sm font-medium text-base-content leading-relaxed">
								<RichRenderer content={q.questionText} />
							</div>

							<!-- Options review (SingleChoice / MultipleChoice) -->
							{#if q.options && q.options.length > 0}
								<div class="space-y-2 pt-2 text-xs">
									{#each q.options as opt}
										{@const isSelected = q.selectedOptionIds?.includes(opt.id)}
										{@const isOptionCorrect = opt.isCorrect}

										<div
											class="flex items-center justify-between rounded-xl p-3.5 border transition-all {isOptionCorrect
												? 'border-success/40 bg-success/15 font-semibold text-success'
												: isSelected
													? 'border-error/40 bg-error/15 font-semibold text-error'
													: 'border-white/5 bg-base-100/30 text-base-content/60'}"
										>
											<div class="flex items-center gap-2.5">
												<span class="flex h-5 w-5 shrink-0 items-center justify-center rounded-lg border text-[11px] font-bold {isOptionCorrect ? 'border-success bg-success text-white' : isSelected ? 'border-error bg-error text-white' : 'border-white/20 bg-base-200/50'}">
													{#if isOptionCorrect}
														✓
													{:else if isSelected}
														✕
													{/if}
												</span>
												<span>{opt.text}</span>
											</div>

											<div class="flex items-center gap-1.5 shrink-0">
												{#if isOptionCorrect}
													<span class="badge badge-success badge-xs font-bold text-white">Correct Answer</span>
												{/if}
												{#if isSelected}
													<span class="badge {isOptionCorrect ? 'badge-info' : 'badge-error'} badge-xs font-bold text-white">Your Selection</span>
												{/if}
											</div>
										</div>
									{/each}
								</div>
							{/if}

							<!-- Essay Student Answer -->
							{#if isEssay}
								<div class="rounded-2xl bg-base-100/50 border border-white/10 p-4 space-y-2 text-xs">
									<div class="font-bold uppercase text-[10px] tracking-wider text-base-content/60">Your Submitted Essay Response</div>
									{#if q.essayText}
										<div class="text-base-content leading-relaxed">
											<RichRenderer content={q.essayText} />
										</div>
									{:else}
										<p class="text-base-content/40 italic">No response submitted for this essay.</p>
									{/if}
								</div>
							{/if}

							<!-- Explanation / Key Insights -->
							{#if q.explanation}
								<div class="rounded-2xl bg-primary/10 border border-primary/20 p-4 text-xs text-primary space-y-1.5">
									<div class="font-bold uppercase text-[10px] tracking-wider flex items-center gap-1">
										<Sparkles class="h-3.5 w-3.5" /> Explanation & Solution Key
									</div>
									<p class="leading-relaxed text-base-content/85">{q.explanation}</p>
								</div>
							{/if}
						</GlassCard>
					{/each}
				</div>
			</div>
		{/if}
	{/if}
</div>
