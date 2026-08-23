<script lang="ts">
	import { examsApi } from '#lib/api/exams.ts';
	import type { QuizExam } from '#lib/api/types.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import { toast } from '#lib/stores/toast.svelte.ts';
	import { GraduationCap, Clock, Award, ShieldAlert, Sparkles, ArrowRight, Search } from '@lucide/svelte';
	import { onMount } from 'svelte';

	let exams = $state<QuizExam[]>([]);
	let isLoading = $state(true);
	let searchTerm = $state('');
	let selectedMode = $state<'All' | 'RealExam' | 'Simulation'>('All');

	onMount(async () => {
		await loadExams();
	});

	async function loadExams() {
		isLoading = true;
		try {
			const res = await examsApi.listExams({
				isPublished: true,
				mode: selectedMode === 'All' ? undefined : selectedMode,
				search: searchTerm.trim() || undefined
			});
			exams = res.items || [];
		} catch (err: any) {
			toast.error(err?.message || 'Failed to load examinations.');
		} finally {
			isLoading = false;
		}
	}
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

	<!-- Search & Filters -->
	<div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
		<div class="relative flex-1 max-w-md">
			<Search class="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-base-content/40" />
			<input
				type="text"
				class="input input-sm h-11 pl-10 w-full rounded-2xl bg-base-100/60 border-base-content/15 text-sm focus:border-primary"
				placeholder="Search exams by title or topic..."
				bind:value={searchTerm}
				onkeydown={(e) => e.key === 'Enter' && loadExams()}
			/>
		</div>

		<!-- Mode Filter Tabs -->
		<div class="glass-card flex items-center gap-1 rounded-2xl p-1 border border-white/10 self-start sm:self-auto">
			<button
				class="btn btn-xs rounded-xl font-semibold transition-all {selectedMode === 'All'
					? 'btn-secondary text-white shadow-xs'
					: 'btn-ghost text-base-content/70'}"
				onclick={() => {
					selectedMode = 'All';
					loadExams();
				}}
			>
				All
			</button>
			<button
				class="btn btn-xs rounded-xl font-semibold transition-all {selectedMode === 'RealExam'
					? 'btn-secondary text-white shadow-xs'
					: 'btn-ghost text-base-content/70'}"
				onclick={() => {
					selectedMode = 'RealExam';
					loadExams();
				}}
			>
				Proctored
			</button>
			<button
				class="btn btn-xs rounded-xl font-semibold transition-all {selectedMode === 'Simulation'
					? 'btn-secondary text-white shadow-xs'
					: 'btn-ghost text-base-content/70'}"
				onclick={() => {
					selectedMode = 'Simulation';
					loadExams();
				}}
			>
				Simulations
			</button>
		</div>
	</div>

	<!-- Exam Cards Grid -->
	{#if isLoading}
		<div class="grid grid-cols-1 gap-6 sm:grid-cols-2">
			<div class="glass-panel h-56 rounded-3xl animate-pulse"></div>
			<div class="glass-panel h-56 rounded-3xl animate-pulse"></div>
		</div>
	{:else if exams.length > 0}
		<div class="grid grid-cols-1 gap-6 sm:grid-cols-2">
			{#each exams as exam (exam.id)}
				{@const isReal = exam.mode === 'RealExam'}
				<GlassCard hover={true} class="flex flex-col justify-between p-6 border {isReal ? 'border-primary/30' : 'border-white/10'} shadow-xl">
					<div class="space-y-4">
						<div class="flex items-center justify-between">
							<span class="badge {isReal ? 'badge-primary' : 'badge-ghost'} badge-sm font-bold uppercase">
								{exam.mode === 'RealExam' ? 'Proctored' : 'Simulation'}
							</span>
							<span class="text-xs font-semibold text-base-content/60 flex items-center gap-1">
								<Clock class="h-3.5 w-3.5" />
								{exam.durationMinutes} mins
							</span>
						</div>

						<div class="space-y-1.5">
							<h3 class="text-lg font-bold text-base-content leading-snug">{exam.title}</h3>
							{#if exam.description}
								<p class="text-xs text-base-content/65 line-clamp-2">{exam.description}</p>
							{/if}
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
							{exam.questionsCount || 0} Questions
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
	{:else}
		<div class="glass-card p-14 text-center rounded-3xl border border-white/5 space-y-4">
			<GraduationCap class="h-10 w-10 text-secondary mx-auto opacity-50" />
			<h3 class="text-base font-bold">No Examinations Available</h3>
			<p class="text-xs text-base-content/60 max-w-sm mx-auto">
				There are currently no published examinations matching your criteria. Check back soon!
			</p>
		</div>
	{/if}
</div>
