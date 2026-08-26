<script lang="ts">
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import { examsApi } from '#lib/api/exams.ts';
	import { proctorApi, type LiveCandidate } from '#lib/api/proctor.ts';
	import type { ExamSummaryDto } from '#lib/api/types.ts';
	import {
		ShieldAlert,
		Users,
		Radio,
		ArrowRight,
		Clock,
		Search,
		Camera,
		Lock,
		Volume2,
		Eye,
		RefreshCw,
		SlidersHorizontal,
		CheckCircle2,
		AlertTriangle
	} from '@lucide/svelte';
	import { onMount } from 'svelte';

	interface ProctorRoomCard extends ExamSummaryDto {
		activeCandidatesCount: number;
		flaggedCount: number;
	}

	let rooms = $state<ProctorRoomCard[]>([]);
	let isLoading = $state(true);
	let searchQuery = $state('');
	let filterPreset = $state<'all' | 'camera' | 'strict'>('all');

	onMount(() => {
		loadExamRooms();
	});

	async function loadExamRooms() {
		isLoading = true;
		try {
			// Fetch published exams
			const res = await examsApi.listExams({ pageSize: 50, isPublished: true });
			const rawExams = (res?.items || []) as ExamSummaryDto[];

			// Try to fetch live candidate counts for each exam in parallel
			const enrichedRooms: ProctorRoomCard[] = await Promise.all(
				rawExams.map(async (exam) => {
					let activeCandidatesCount = 0;
					let flaggedCount = 0;
					try {
						const candidates = await proctorApi.getLiveCandidates(exam.id);
						if (candidates && candidates.length > 0) {
							activeCandidatesCount = candidates.filter((c) => c.status === 'InProgress').length;
							flaggedCount = candidates.filter((c) => (c.violationCount || 0) > 0).length;
						}
					} catch {
						// Default to 0 if no active candidates
					}
					return {
						...exam,
						activeCandidatesCount,
						flaggedCount
					};
				})
			);

			rooms = enrichedRooms;
		} catch (err) {
			console.error('Failed to load exam rooms:', err);
		} finally {
			isLoading = false;
		}
	}

	const filteredRooms = $derived(
		rooms.filter((room) => {
			const matchesSearch =
				!searchQuery.trim() ||
				room.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
				(room.description && room.description.toLowerCase().includes(searchQuery.toLowerCase()));

			if (!matchesSearch) return false;

			if (filterPreset === 'camera') {
				return room.ruleConfig?.requireCamera === true;
			}
			if (filterPreset === 'strict') {
				return (
					room.ruleConfig?.forceFullscreen === true &&
					room.ruleConfig?.restrictClipboardAndMouse === true
				);
			}
			return true;
		})
	);

	const totalActiveTestTakers = $derived(
		rooms.reduce((acc, r) => acc + (r.activeCandidatesCount || 0), 0)
	);
	const totalFlaggedAlerts = $derived(
		rooms.reduce((acc, r) => acc + (r.flaggedCount || 0), 0)
	);
</script>

<div class="space-y-8 pb-12">
	<!-- Top Hero / Banner -->
	<div
		class="glass-panel relative overflow-hidden rounded-3xl border border-white/10 p-6 md:p-8 shadow-2xl backdrop-blur-2xl bg-gradient-to-r from-base-100/60 via-base-200/40 to-primary/5"
	>
		<div class="relative z-10 flex flex-col md:flex-row md:items-center md:justify-between gap-6">
			<div class="space-y-2 max-w-2xl">
				<div
					class="inline-flex items-center gap-2 rounded-xl bg-error/15 border border-error/30 px-3 py-1 text-xs font-bold uppercase tracking-wider text-error shadow-sm"
				>
					<Radio class="h-3.5 w-3.5 animate-pulse" />
					Live Anti-Cheat Supervisor
				</div>
				<h1 class="text-3xl md:text-4xl font-extrabold tracking-tight text-base-content">
					Active Examination Rooms
				</h1>
				<p class="text-xs md:text-sm text-base-content/70 leading-relaxed">
					Supervise candidate sessions in real time, monitor automated anti-cheat violation telemetry,
					inspect periodic webcam snapshot feeds, and enforce academic integrity.
				</p>
			</div>

			<!-- Quick Stat Counter Cards -->
			<div class="flex items-center gap-3">
				<div class="glass-card rounded-2xl p-4 border border-white/10 min-w-[120px] text-center shadow-lg">
					<div class="text-2xl font-black text-primary font-mono">{rooms.length}</div>
					<div class="text-[11px] font-semibold text-base-content/60 uppercase tracking-wider">Rooms</div>
				</div>
				<div class="glass-card rounded-2xl p-4 border border-success/30 bg-success/5 min-w-[130px] text-center shadow-lg">
					<div class="text-2xl font-black text-success font-mono flex items-center justify-center gap-1">
						<span class="h-2 w-2 rounded-full bg-success animate-ping"></span>
						{totalActiveTestTakers}
					</div>
					<div class="text-[11px] font-semibold text-success/80 uppercase tracking-wider">Active Students</div>
				</div>
				<div class="glass-card rounded-2xl p-4 border border-warning/30 bg-warning/5 min-w-[120px] text-center shadow-lg">
					<div class="text-2xl font-black text-warning font-mono">{totalFlaggedAlerts}</div>
					<div class="text-[11px] font-semibold text-warning/80 uppercase tracking-wider">Flags</div>
				</div>
			</div>
		</div>
	</div>

	<!-- Controls & Filter Toolbar -->
	<div class="flex flex-col sm:flex-row items-center justify-between gap-4">
		<!-- Search Input -->
		<div class="relative w-full sm:w-80">
			<Search class="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-base-content/40" />
			<input
				type="text"
				placeholder="Search room title or description..."
				bind:value={searchQuery}
				class="glass-input input input-sm w-full pl-10 rounded-2xl text-xs bg-base-100/50 border-white/10 focus:border-primary/50"
			/>
		</div>

		<!-- Filter Pill Switches & Refresh -->
		<div class="flex items-center gap-2 w-full sm:w-auto justify-end">
			<div class="join bg-base-200/50 p-1 rounded-2xl border border-white/10">
				<button
					class="btn btn-xs rounded-xl join-item {filterPreset === 'all' ? 'btn-primary font-bold shadow' : 'btn-ghost text-base-content/60'}"
					onclick={() => (filterPreset = 'all')}
				>
					All Rooms
				</button>
				<button
					class="btn btn-xs rounded-xl join-item {filterPreset === 'camera' ? 'btn-primary font-bold shadow' : 'btn-ghost text-base-content/60'}"
					onclick={() => (filterPreset = 'camera')}
				>
					<Camera class="h-3 w-3 mr-1" />
					Webcam Monitored
				</button>
				<button
					class="btn btn-xs rounded-xl join-item {filterPreset === 'strict' ? 'btn-primary font-bold shadow' : 'btn-ghost text-base-content/60'}"
					onclick={() => (filterPreset = 'strict')}
				>
					<Lock class="h-3 w-3 mr-1" />
					Strict Lockdown
				</button>
			</div>

			<button
				class="btn btn-ghost btn-sm btn-circle rounded-xl border border-white/10"
				onclick={loadExamRooms}
				disabled={isLoading}
				title="Refresh rooms"
			>
				<RefreshCw class="h-4 w-4 {isLoading ? 'animate-spin text-primary' : 'text-base-content/70'}" />
			</button>
		</div>
	</div>

	<!-- Examination Rooms Grid -->
	{#if isLoading}
		<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
			{#each [1, 2, 3, 4, 5, 6] as _}
				<div class="glass-card h-64 rounded-3xl animate-pulse border border-white/10"></div>
			{/each}
		</div>
	{:else if filteredRooms.length === 0}
		<div class="glass-card p-12 text-center rounded-3xl border border-white/10 space-y-4 max-w-lg mx-auto my-12">
			<div class="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-base-300 text-base-content/40">
				<ShieldAlert class="h-7 w-7" />
			</div>
			<div class="space-y-1">
				<h3 class="text-lg font-bold text-base-content">No Examination Rooms Found</h3>
				<p class="text-xs text-base-content/60">
					{searchQuery
						? 'No published exams match your search criteria.'
						: 'No published examination rooms are currently active for supervision.'}
				</p>
			</div>
			{#if searchQuery || filterPreset !== 'all'}
				<button
					class="btn btn-sm btn-ghost rounded-xl border border-white/10 text-xs"
					onclick={() => {
						searchQuery = '';
						filterPreset = 'all';
					}}
				>
					Clear Filters
				</button>
			{/if}
		</div>
	{:else}
		<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
			{#each filteredRooms as room (room.id)}
				<GlassCard
					class="flex flex-col justify-between p-6 rounded-3xl border-white/10 hover:border-primary/40 transition-all duration-300 shadow-xl hover:shadow-2xl group space-y-4"
				>
					<!-- Top Card Meta -->
					<div class="space-y-3">
						<div class="flex items-center justify-between">
							<span
								class="badge {room.activeCandidatesCount > 0 ? 'badge-error text-white' : 'badge-ghost text-base-content/70'} badge-sm font-bold uppercase gap-1.5 shadow-sm"
							>
								{#if room.activeCandidatesCount > 0}
									<span class="h-2 w-2 rounded-full bg-white animate-ping"></span>
									Live Active
								{:else}
									<span class="h-1.5 w-1.5 rounded-full bg-base-content/40"></span>
									Ready
								{/if}
							</span>

							<span
								class="inline-flex items-center gap-1 text-xs font-bold {room.activeCandidatesCount > 0 ? 'text-primary' : 'text-base-content/60'}"
							>
								<Users class="h-3.5 w-3.5" />
								{room.activeCandidatesCount} Candidates
							</span>
						</div>

						<div>
							<h3 class="text-base font-bold text-base-content group-hover:text-primary transition-colors leading-snug line-clamp-1">
								{room.title}
							</h3>
							<p class="text-xs text-base-content/60 mt-1 line-clamp-2 min-h-[32px]">
								{room.description || 'Comprehensive exam assessment module with configurable integrity rules.'}
							</p>
						</div>

						<!-- Rule Presets Feature Badges -->
						<div class="flex flex-wrap items-center gap-1.5 pt-1">
							{#if room.ruleConfig?.requireCamera}
								<span class="badge badge-xs bg-primary/10 border-primary/20 text-primary font-semibold gap-1">
									<Camera class="h-2.5 w-2.5" />
									Snapshot ({room.ruleConfig?.snapshotIntervalSeconds || 45}s)
								</span>
							{/if}
							{#if room.ruleConfig?.forceFullscreen}
								<span class="badge badge-xs bg-secondary/10 border-secondary/20 text-secondary font-semibold gap-1">
									<Lock class="h-2.5 w-2.5" />
									Fullscreen Lock
								</span>
							{/if}
							{#if room.ruleConfig?.requireMicrophone}
								<span class="badge badge-xs bg-warning/10 border-warning/20 text-warning font-semibold gap-1">
									<Volume2 class="h-2.5 w-2.5" />
									Audio Monitor
								</span>
							{/if}
							<span class="badge badge-xs badge-ghost font-mono">
								Limit: {room.ruleConfig?.maxAllowedViolations ?? 3} Violations
							</span>
						</div>
					</div>

					<!-- Bottom Action & Info -->
					<div class="pt-4 border-t border-white/10 flex items-center justify-between">
						<div class="flex items-center gap-3 text-xs text-base-content/60">
							<span class="flex items-center gap-1">
								<Clock class="h-3.5 w-3.5" />
								{room.durationMinutes}m
							</span>
							<span>&bull;</span>
							<span>{room.questionsCount || 0} Questions</span>
						</div>

						<a
							href="/proctor/exams/{room.id}/live"
							class="btn btn-primary btn-sm rounded-xl font-bold text-white shadow-lg shadow-primary/20 hover:scale-105 transition-all gap-1.5"
						>
							Enter Room
							<ArrowRight class="h-3.5 w-3.5" />
						</a>
					</div>
				</GlassCard>
			{/each}
		</div>
	{/if}
</div>
