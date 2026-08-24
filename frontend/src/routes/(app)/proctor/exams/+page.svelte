<script lang="ts">
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import { ShieldAlert, Users, Radio, ArrowRight, Clock } from '@lucide/svelte';

	let activeExams = $state([
		{
			id: 'ex-1',
			title: 'Distributed Consensus & Raft Protocol Final',
			activeCandidatesCount: 14,
			durationMinutes: 60,
			maxAllowedViolations: 3
		}
	]);
</script>

<div class="space-y-8">
	<!-- Header -->
	<div class="glass-panel flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 rounded-3xl border border-white/10 p-8 shadow-2xl backdrop-blur-2xl">
		<div class="space-y-1">
			<div class="inline-flex items-center gap-2 rounded-lg bg-warning/10 border border-warning/20 px-3 py-1 text-xs font-semibold text-warning">
				<Radio class="h-3.5 w-3.5 animate-pulse text-error" />
				Live Exam Proctoring
			</div>
			<h1 class="text-3xl font-extrabold tracking-tight text-base-content">
				Active Examination Rooms
			</h1>
			<p class="text-xs text-base-content/70">
				Monitor active student candidates in real-time, view webcam snapshots, and manage violations.
			</p>
		</div>
	</div>

	<!-- Room Cards -->
	<div class="grid grid-cols-1 gap-6 sm:grid-cols-2">
		{#each activeExams as room (room.id)}
			<GlassCard class="flex flex-col justify-between p-6 border-warning/20 space-y-4">
				<div class="space-y-3">
					<div class="flex items-center justify-between">
						<span class="badge badge-error badge-xs font-bold uppercase text-white gap-1">
							<span class="h-1.5 w-1.5 rounded-full bg-white animate-ping"></span>
							Live In-Progress
						</span>
						<span class="text-xs font-semibold text-warning flex items-center gap-1">
							<Users class="h-3.5 w-3.5" />
							{room.activeCandidatesCount} Candidates
						</span>
					</div>

					<h3 class="text-lg font-bold text-base-content leading-snug">{room.title}</h3>

					<div class="flex items-center gap-4 text-xs text-base-content/60">
						<span class="flex items-center gap-1">
							<Clock class="h-3.5 w-3.5" />
							{room.durationMinutes} mins
						</span>
						<span>&bull;</span>
						<span class="text-warning">Threshold: {room.maxAllowedViolations} violations</span>
					</div>
				</div>

				<div class="pt-4 border-t border-white/10 flex justify-end">
					<a
						href="/proctor/exams/{room.id}/live"
						class="btn btn-warning btn-sm rounded-xl font-bold text-warning-content shadow-md gap-1.5"
					>
						Enter Proctor Console
						<ArrowRight class="h-4 w-4" />
					</a>
				</div>
			</GlassCard>
		{/each}
	</div>
</div>
