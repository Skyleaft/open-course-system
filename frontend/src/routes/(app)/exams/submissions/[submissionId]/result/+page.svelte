<script lang="ts">
	import { page } from '$app/state';
	import { examsApi } from '#lib/api/exams.ts';
	import type { QuizSubmission, QuizQuestion } from '#lib/api/types.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import RichRenderer from '#lib/components/editor/RichRenderer.svelte';
	import { Award, CheckCircle2, XCircle, Clock, ArrowRight, ShieldCheck, Sparkles } from '@lucide/svelte';
	import { onMount } from 'svelte';

	const submissionId = (page.params.submissionId || '') as string;
	let submission = $state<QuizSubmission | null>(null);
	let reviewQuestions = $state<Array<QuizQuestion & {
		selectedOptionIds?: string[];
		essayText?: string;
		awardedScore?: number;
		isCorrect?: boolean;
	}>>([]);
	let isLoading = $state(true);

	const isSimulation = $derived(submission?.mode === 'Simulation');
	const isPassed = $derived((submission?.totalScore || 0) >= 70);

	onMount(async () => {
		try {
			const res = await examsApi.getResult(submissionId);
			if (res?.submission) {
				submission = res.submission;
				reviewQuestions = res.questionsWithReview || [];
			} else {
				loadMockResult();
			}
		} catch {
			loadMockResult();
		} finally {
			isLoading = false;
		}
	});

	function loadMockResult() {
		submission = {
			id: submissionId,
			quizId: 'ex-1',
			studentId: 'st-1',
			mode: 'Simulation',
			startedAtUtc: new Date(Date.now() - 3600000).toISOString(),
			maxAllowedEndTimeUtc: new Date().toISOString(),
			finishedAtUtc: new Date().toISOString(),
			status: 'Completed',
			totalScore: 88.5,
			activeSessionToken: 'token-123',
			violations: []
		};

		reviewQuestions = [
			{
				id: 'q-1',
				quizId: 'ex-1',
				text: 'In the **Raft consensus algorithm**, which state is a node in when it first starts up before election timeout?',
				type: 'SingleChoice',
				points: 4,
				orderIndex: 1,
				selectedOptionIds: ['opt-1'],
				isCorrect: true,
				awardedScore: 4,
				options: [
					{ id: 'opt-1', text: 'Follower', isCorrect: true },
					{ id: 'opt-2', text: 'Candidate', isCorrect: false },
					{ id: 'opt-3', text: 'Leader', isCorrect: false }
				],
				explanation: 'All Raft nodes begin their lifecycle in the Follower state. If no heartbeat is heard within the election timeout, they transition to Candidate.'
			},
			{
				id: 'q-2',
				quizId: 'ex-1',
				text: 'Select all mechanisms used by **Redis Streams** to guarantee message durability and consumer group distribution:',
				type: 'MultipleChoice',
				points: 6,
				orderIndex: 2,
				selectedOptionIds: ['opt-5', 'opt-6'],
				isCorrect: true,
				awardedScore: 6,
				options: [
					{ id: 'opt-5', text: 'Append-Only File (AOF) persistence', isCorrect: true },
					{ id: 'opt-6', text: 'Pending Entries List (PEL) for message acknowledgment', isCorrect: true },
					{ id: 'opt-7', text: 'Single consumer locking', isCorrect: false }
				],
				explanation: 'Redis Streams tracks unacknowledged items in the Pending Entries List (PEL) and persists entries to disk via AOF.'
			}
		];
	}
</script>

<div class="max-w-4xl mx-auto space-y-8 py-6">
	{#if isLoading}
		<div class="glass-panel h-96 rounded-3xl animate-pulse"></div>
	{:else if submission}
		<!-- Score Banner -->
		<div class="glass-panel text-center rounded-3xl border border-white/10 p-8 sm:p-12 shadow-2xl space-y-6">
			<div class="mx-auto flex h-20 w-20 items-center justify-center rounded-3xl {isPassed ? 'bg-success/20 text-success border border-success/30' : 'bg-warning/20 text-warning border border-warning/30'} shadow-xl">
				{#if isPassed}
					<Award class="h-10 w-10" />
				{:else}
					<XCircle class="h-10 w-10" />
				{/if}
			</div>

			<div class="space-y-2">
				<span class="badge {isPassed ? 'badge-success' : 'badge-warning'} badge-sm font-bold uppercase tracking-wider text-white">
					{isPassed ? 'Passed' : 'Needs Improvement'}
				</span>
				<h1 class="text-3xl font-extrabold text-base-content tracking-tight sm:text-4xl">
					Examination Results
				</h1>
				<div class="text-5xl font-black {isPassed ? 'text-success' : 'text-warning'} pt-2">
					{submission.totalScore}%
				</div>
			</div>

			<div class="flex flex-wrap items-center justify-center gap-6 text-xs text-base-content/70 border-t border-white/10 pt-4">
				<div class="flex items-center gap-1.5">
					<Clock class="h-4 w-4 text-primary" />
					Status: <span class="font-semibold text-base-content">{submission.status}</span>
				</div>
				<div class="flex items-center gap-1.5">
					<ShieldCheck class="h-4 w-4 text-secondary" />
					Violations: <span class="font-semibold text-base-content">{submission.violations?.length || 0}</span>
				</div>
			</div>

			<div class="flex justify-center gap-3 pt-2">
				<a href="/dashboard" class="btn btn-ghost glass-card btn-sm rounded-xl border border-white/10 hover:bg-base-100/40">
					Return to Dashboard
				</a>
				{#if isPassed}
					<a href="/certificates" class="btn btn-primary gradient-accent btn-sm rounded-xl text-white font-bold border-0 shadow-md">
						View Certificates
						<ArrowRight class="h-4 w-4 ml-1" />
					</a>
				{/if}
			</div>
		</div>

		<!-- Simulation Instant Review Section with Explanations -->
		{#if isSimulation && reviewQuestions.length > 0}
			<div class="space-y-4">
				<div class="flex items-center gap-2 text-base font-bold text-base-content px-1">
					<Sparkles class="h-5 w-5 text-primary" />
					Simulation Answer Key & Explanations
				</div>

				<div class="space-y-4">
					{#each reviewQuestions as q, idx (q.id)}
						<GlassCard class="space-y-4 p-6 border {q.isCorrect ? 'border-success/30' : 'border-error/30'}">
							<div class="flex items-center justify-between border-b border-white/10 pb-2 text-xs">
								<span class="font-bold">Question {idx + 1}</span>
								<span class="badge {q.isCorrect ? 'badge-success text-white' : 'badge-error text-white'} badge-xs font-bold">
									{q.isCorrect ? `+${q.points} pts` : '0 pts'}
								</span>
							</div>

							<div class="text-sm font-medium text-base-content">
								<RichRenderer content={q.text} />
							</div>

							<!-- Options review -->
							<div class="space-y-2 pt-1 text-xs">
								{#each q.options as opt}
									{@const isSelected = q.selectedOptionIds?.includes(opt.id || '')}
									<div
										class="flex items-center justify-between rounded-xl p-3 border {opt.isCorrect
											? 'border-success/40 bg-success/15 font-semibold text-success'
											: isSelected
												? 'border-error/40 bg-error/15 font-semibold text-error'
												: 'border-white/5 bg-base-100/30 text-base-content/60'}"
									>
										<span>{opt.text}</span>
										{#if opt.isCorrect}
											<span class="badge badge-success badge-xs font-bold text-white">Correct Answer</span>
										{:else if isSelected}
											<span class="badge badge-error badge-xs font-bold text-white">Your Selection</span>
										{/if}
									</div>
								{/each}
							</div>

							{#if q.explanation}
								<div class="rounded-xl bg-primary/10 border border-primary/20 p-3.5 text-xs text-primary space-y-1">
									<div class="font-bold uppercase text-[10px] tracking-wider">Explanation</div>
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
