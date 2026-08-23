<script lang="ts">
	import { examsApi } from '#lib/api/exams.ts';
	import type { QuizExam } from '#lib/api/types.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import { GraduationCap, Clock, Award, ShieldAlert, Sparkles, ArrowRight } from '@lucide/svelte';
	import { onMount } from 'svelte';

	let exams = $state<QuizExam[]>([]);
	let isLoading = $state(true);

	onMount(async () => {
		try {
			// Demo/catalog exams fallback
			exams = [
				{
					id: 'ex-1',
					courseId: 'c-1',
					title: 'Distributed Consensus & Raft Protocol Final',
					mode: 'RealExam',
					durationMinutes: 60,
					passingScore: 75,
					maxAllowedViolations: 3,
					isPublished: true,
					questionsCount: 25
				},
				{
					id: 'ex-2',
					courseId: 'c-1',
					title: 'Practice Simulation: In-Memory Storage & Redis',
					mode: 'Simulation',
					durationMinutes: 30,
					passingScore: 60,
					maxAllowedViolations: 0,
					isPublished: true,
					questionsCount: 15
				}
			];
		} finally {
			isLoading = false;
		}
	});
</script>

<div class="space-y-8">
	<!-- Header -->
	<div class="glass-panel relative overflow-hidden rounded-3xl border border-white/10 p-8 shadow-2xl backdrop-blur-2xl">
		<div class="space-y-2">
			<div class="inline-flex items-center gap-2 rounded-lg bg-primary/10 border border-primary/20 px-3 py-1 text-xs font-semibold text-primary">
				<Sparkles class="h-3.5 w-3.5" />
				Examination Center
			</div>
			<h1 class="text-3xl font-extrabold tracking-tight text-base-content sm:text-4xl">
				Active Examinations & Tests
			</h1>
			<p class="text-xs text-base-content/70 sm:text-sm max-w-xl">
				Select a practice simulation test or an officially proctored examination to demonstrate your skills.
			</p>
		</div>
	</div>

	<!-- Exam Cards Grid -->
	<div class="grid grid-cols-1 gap-6 sm:grid-cols-2">
		{#each exams as exam (exam.id)}
			{@const isReal = exam.mode === 'RealExam'}
			<GlassCard hover={true} class="flex flex-col justify-between p-6 border {isReal ? 'border-primary/30' : 'border-white/10'}">
				<div class="space-y-4">
					<div class="flex items-center justify-between">
						<span class="badge {isReal ? 'badge-primary' : 'badge-ghost'} badge-sm font-bold uppercase">
							{exam.mode}
						</span>
						<span class="text-xs font-semibold text-base-content/60 flex items-center gap-1">
							<Clock class="h-3.5 w-3.5" />
							{exam.durationMinutes} mins
						</span>
					</div>

					<div class="space-y-1">
						<h3 class="text-lg font-bold text-base-content leading-snug">{exam.title}</h3>
						<div class="flex items-center gap-3 text-xs text-base-content/60 pt-1">
							<span class="flex items-center gap-1">
								<Award class="h-3.5 w-3.5 text-success" />
								Passing: {exam.passingScore}%
							</span>
							{#if isReal}
								<span class="flex items-center gap-1 text-warning font-medium">
									<ShieldAlert class="h-3.5 w-3.5" />
									Max Violations: {exam.maxAllowedViolations}
								</span>
							{/if}
						</div>
					</div>
				</div>

				<div class="mt-6 pt-4 border-t border-white/10 flex items-center justify-between">
					<span class="text-xs text-base-content/50">
						{exam.questionsCount || 20} Questions
					</span>

					<a
						href="/exams/{exam.id}/start"
						class="btn btn-primary {isReal ? 'gradient-accent' : 'btn-ghost glass-card border border-white/10'} btn-sm rounded-xl font-semibold text-white border-0 shadow-md gap-1"
					>
						{isReal ? 'Start Exam' : 'Launch Practice'}
						<ArrowRight class="h-3.5 w-3.5" />
					</a>
				</div>
			</GlassCard>
		{/each}
	</div>
</div>
