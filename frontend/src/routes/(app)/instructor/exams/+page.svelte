<script lang="ts">
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import { Plus, Edit3, GraduationCap, ShieldAlert, Award, ArrowRight } from '@lucide/svelte';

	let exams = $state([
		{
			id: 'ex-1',
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
			title: 'Practice Simulation: In-Memory Storage & Redis',
			mode: 'Simulation',
			durationMinutes: 30,
			passingScore: 60,
			maxAllowedViolations: 0,
			isPublished: true,
			questionsCount: 15
		}
	]);
</script>

<div class="space-y-8">
	<!-- Header -->
	<div class="glass-panel flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 rounded-3xl border border-white/10 p-8 shadow-2xl backdrop-blur-2xl">
		<div class="space-y-1">
			<div class="inline-flex items-center gap-2 rounded-lg bg-secondary/10 border border-secondary/20 px-3 py-1 text-xs font-semibold text-secondary">
				<GraduationCap class="h-3.5 w-3.5" />
				Exam Studio
			</div>
			<h1 class="text-3xl font-extrabold tracking-tight text-base-content">
				Exam Authoring & Question Banks
			</h1>
			<p class="text-xs text-base-content/70">
				Configure examination parameters, dual-mode settings, and author rich question sets.
			</p>
		</div>

		<a
			href="/instructor/exams/create"
			class="btn btn-secondary gradient-accent rounded-xl text-xs font-bold text-white border-0 shadow-lg gap-1.5 self-start sm:self-auto"
		>
			<Plus class="h-4 w-4" />
			Author New Exam
		</a>
	</div>

	<!-- Exams Grid -->
	<div class="grid grid-cols-1 gap-6 sm:grid-cols-2">
		{#each exams as exam (exam.id)}
			{@const isReal = exam.mode === 'RealExam'}
			<GlassCard class="flex flex-col justify-between p-6 border {isReal ? 'border-primary/20' : 'border-white/10'} space-y-4">
				<div class="space-y-3">
					<div class="flex items-center justify-between">
						<span class="badge {isReal ? 'badge-primary' : 'badge-ghost'} badge-xs font-bold uppercase">
							{exam.mode}
						</span>
						<span class="badge {exam.isPublished ? 'badge-success text-white' : 'badge-warning'} badge-xs font-semibold">
							{exam.isPublished ? 'Published' : 'Draft'}
						</span>
					</div>

					<h3 class="text-base font-bold text-base-content leading-snug">{exam.title}</h3>

					<div class="flex items-center gap-4 text-xs text-base-content/60">
						<span>{exam.durationMinutes} mins</span>
						<span>&bull;</span>
						<span>Pass: {exam.passingScore}%</span>
						{#if isReal}
							<span>&bull;</span>
							<span class="text-warning">Violations: {exam.maxAllowedViolations}</span>
						{/if}
					</div>
				</div>

				<div class="pt-3 border-t border-white/10 flex items-center justify-between text-xs">
					<span class="text-base-content/50">{exam.questionsCount} Questions</span>
					<span class="text-xs font-semibold text-secondary">Manage Soal</span>
				</div>
			</GlassCard>
		{/each}
	</div>
</div>
