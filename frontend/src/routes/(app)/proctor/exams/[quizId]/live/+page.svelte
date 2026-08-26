<script lang="ts">
	import { page } from '$app/state';
	import { proctorApi, type LiveCandidate, type CandidateViolation } from '#lib/api/proctor.ts';
	import { ExamHubClient } from '#lib/signalr/exam-hub.svelte.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import GlassModal from '#lib/components/ui/GlassModal.svelte';
	import { toast } from '#lib/stores/toast.svelte.ts';
	import {
		ShieldAlert,
		Radio,
		Users,
		AlertTriangle,
		Camera,
		Send,
		UserX,
		X,
		ArrowLeft,
		CheckCircle2
	} from '@lucide/svelte';
	import { onMount, onDestroy } from 'svelte';

	const quizId = (page.params.quizId || '') as string;

	let candidates = $state<LiveCandidate[]>([]);
	let violationsFeed = $state<CandidateViolation[]>([]);
	let selectedCandidate = $state<LiveCandidate | null>(null);

	// Modals
	let isWarnModalOpen = $state(false);
	let isDisconnectModalOpen = $state(false);
	let isSnapshotModalOpen = $state(false);
	let targetSubmissionId = $state<string | null>(null);
	let warnMessage = $state('Please stay focused on the exam window and ensure your camera remains unobstructed.');
	let isActionLoading = $state(false);

	let examHub: ExamHubClient | null = null;

	onMount(async () => {
		// 1. Initial candidates load
		try {
			const res = await proctorApi.getLiveCandidates(quizId);
			if (res && res.length > 0) {
				candidates = res;
			} else {
				loadMockCandidates();
			}
		} catch {
			loadMockCandidates();
		}

		// 2. Connect to ExamHub for live broadcasts
		examHub = new ExamHubClient();
		await examHub.start();

		examHub.onProctorViolationAlert((studentId, submissionId, violationType, count) => {
			const violationItem: CandidateViolation = {
				id: Math.random().toString(36).substring(2, 9),
				studentId,
				submissionId,
				violationType,
				timestampUtc: new Date().toISOString(),
				violationCount: count
			};
			violationsFeed = [violationItem, ...violationsFeed.slice(0, 19)]; // keep latest 20

			// Update candidate violation count in grid
			const target = candidates.find((c) => c.submissionId === submissionId || c.studentId === studentId);
			if (target) {
				target.violationCount = count;
			}
			toast.warning(`Violation: ${target?.studentName || 'Candidate'} — ${violationType} (${count})`);
		});

		examHub.onProctorSnapshotReceived((studentId, snapshotUrl) => {
			const target = candidates.find((c) => c.studentId === studentId);
			if (target) {
				target.latestSnapshotPresignedUrl = snapshotUrl;
			}
		});
	});

	onDestroy(() => {
		if (examHub) examHub.stop();
	});

	function loadMockCandidates() {
		candidates = [
			{
				studentId: 's-1',
				studentName: 'Alex Mercer',
				submissionId: 'sub-1',
				startedAtUtc: new Date().toISOString(),
				maxAllowedEndTimeUtc: new Date(Date.now() + 3600000).toISOString(),
				violationCount: 1,
				lastHeartbeatUtc: new Date().toISOString(),
				status: 'InProgress'
			},
			{
				studentId: 's-2',
				studentName: 'Beatrix Kiddo',
				submissionId: 'sub-2',
				startedAtUtc: new Date().toISOString(),
				maxAllowedEndTimeUtc: new Date(Date.now() + 3600000).toISOString(),
				violationCount: 0,
				lastHeartbeatUtc: new Date().toISOString(),
				status: 'InProgress'
			},
			{
				studentId: 's-3',
				studentName: 'Carlos Ramirez',
				submissionId: 'sub-3',
				startedAtUtc: new Date().toISOString(),
				maxAllowedEndTimeUtc: new Date(Date.now() + 3600000).toISOString(),
				violationCount: 2,
				lastHeartbeatUtc: new Date().toISOString(),
				status: 'InProgress'
			}
		];
	}

	async function handleSendWarning() {
		if (!targetSubmissionId || !warnMessage) return;
		isActionLoading = true;
		try {
			await proctorApi.sendWarning(targetSubmissionId, warnMessage);
			toast.success('Warning dispatched to candidate screen.');
			isWarnModalOpen = false;
		} catch (err: any) {
			toast.error(err?.message || 'Failed to send warning.');
		} finally {
			isActionLoading = false;
		}
	}

	async function handleForceDisconnect() {
		if (!targetSubmissionId) return;
		isActionLoading = true;
		try {
			await proctorApi.forceDisconnect(targetSubmissionId, 'Disqualified by proctor for integrity violation');
			toast.success('Candidate forcibly disconnected and disqualified.');
			candidates = candidates.map((c) =>
				c.submissionId === targetSubmissionId ? { ...c, status: 'Disqualified' } : c
			);
			isDisconnectModalOpen = false;
		} catch (err: any) {
			toast.error(err?.message || 'Failed to disconnect candidate.');
		} finally {
			isActionLoading = false;
		}
	}
</script>

<div class="space-y-6">
	<!-- Top Bar -->
	<div class="flex items-center justify-between border-b border-white/10 pb-4">
		<a
			href="/proctor/exams"
			class="inline-flex items-center gap-1.5 text-xs font-semibold text-base-content/60 hover:text-primary transition-colors"
		>
			<ArrowLeft class="h-4 w-4" />
			Back to Rooms
		</a>

		<div class="flex items-center gap-3">
			<span class="inline-flex items-center gap-1.5 badge badge-error badge-sm font-bold uppercase text-white">
				<span class="h-2 w-2 rounded-full bg-white animate-ping"></span>
				Live Proctoring Feed
			</span>
			<span class="text-xs font-semibold text-base-content/70">
				{candidates.length} Active Candidates
			</span>
		</div>
	</div>

	<!-- Main Grid & Realtime Feed -->
	<div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
		<!-- Candidate Video/Status Cards Grid -->
		<div class="space-y-4 lg:col-span-2">
			<div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
				{#each candidates as c (c.submissionId)}
					<GlassCard class="p-4 space-y-3 border {c.violationCount >= 3 || c.status === 'Disqualified' ? 'border-error/40 bg-error/5' : c.violationCount > 0 ? 'border-warning/30' : 'border-white/10'}">
						<!-- Candidate Header -->
						<div class="flex items-center justify-between">
							<div class="flex items-center gap-2">
								<span class="h-2 w-2 rounded-full {c.status === 'Disqualified' ? 'bg-error' : 'bg-success animate-pulse'}"></span>
								<span class="text-xs font-bold text-base-content">{c.studentName}</span>
							</div>

							<span class="badge {c.violationCount === 0 ? 'badge-success text-white' : c.violationCount >= 3 ? 'badge-error text-white' : 'badge-warning'} badge-xs font-bold">
								⚠ {c.violationCount} Violations
							</span>
						</div>

						<!-- Snapshot Preview Frame -->
						<div class="relative aspect-video w-full overflow-hidden rounded-xl bg-black/60 border border-white/10 flex items-center justify-center">
							{#if c.latestSnapshotPresignedUrl}
								<img
									src={c.latestSnapshotPresignedUrl}
									alt="Candidate Snapshot"
									class="h-full w-full object-cover"
								/>
							{:else}
								<div class="flex flex-col items-center gap-1 text-[10px] text-base-content/40">
									<Camera class="h-6 w-6 opacity-30" />
									<span>Awaiting snapshot tick</span>
								</div>
							{/if}
						</div>

						<!-- Proctor Actions Bar -->
						<div class="flex items-center justify-between pt-2 border-t border-white/10">
							<button
								class="btn btn-warning btn-xs rounded-lg font-semibold gap-1 text-warning-content"
								onclick={() => {
									targetSubmissionId = c.submissionId;
									isWarnModalOpen = true;
								}}
								disabled={c.status === 'Disqualified'}
							>
								<AlertTriangle class="h-3 w-3" />
								Warn
							</button>

							<button
								class="btn btn-error btn-xs rounded-lg font-semibold gap-1 text-white"
								onclick={() => {
									targetSubmissionId = c.submissionId;
									isDisconnectModalOpen = true;
								}}
								disabled={c.status === 'Disqualified'}
							>
								<UserX class="h-3 w-3" />
								Disqualify
							</button>
						</div>
					</GlassCard>
				{/each}
			</div>
		</div>

		<!-- Realtime Violation Alert Feed Sidebar -->
		<div class="space-y-4">
			<div class="glass-card rounded-2xl border border-white/10 p-4 space-y-3">
				<div class="flex items-center justify-between border-b border-white/10 pb-2">
					<div class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-base-content">
						<ShieldAlert class="h-4 w-4 text-warning" />
						Realtime Alert Stream
					</div>
					<span class="badge badge-ghost badge-xs font-mono">{violationsFeed.length}</span>
				</div>

				<div class="space-y-2 max-h-[600px] overflow-y-auto pr-1">
					{#each violationsFeed as v (v.id)}
						<div class="rounded-xl bg-base-100/40 p-3 border border-warning/20 space-y-1 text-xs animate-in slide-in-from-top-2">
							<div class="flex items-center justify-between">
								<span class="font-bold text-error">{v.violationType}</span>
								<span class="text-[10px] text-base-content/50">
									{new Date(v.timestampUtc).toLocaleTimeString()}
								</span>
							</div>
							<div class="text-[11px] text-base-content/70">
								Candidate ID: <span class="font-mono text-primary">{v.studentId.substring(0, 8)}...</span>
							</div>
							<div class="text-[10px] font-semibold text-warning">
								Total Violations: {v.violationCount}
							</div>
						</div>
					{:else}
						<div class="text-center py-12 text-xs text-base-content/40 space-y-2">
							<CheckCircle2 class="h-6 w-6 text-success mx-auto opacity-50" />
							<p>No active violations detected. Room integrity intact.</p>
						</div>
					{/each}
				</div>
			</div>
		</div>
	</div>

	<!-- Send Warning Modal -->
	<GlassModal
		isOpen={isWarnModalOpen}
		title="Send Direct Warning to Candidate"
		onClose={() => (isWarnModalOpen = false)}
	>
		<div class="space-y-3">
			<label class="text-xs font-semibold" for="warn-txt">Warning Message (Displayed on student screen)</label>
			<textarea
				id="warn-txt"
				class="glass-input textarea h-24 w-full rounded-xl text-xs"
				bind:value={warnMessage}
			></textarea>
		</div>

		{#snippet actions()}
			<button class="btn btn-ghost btn-sm rounded-xl" onclick={() => (isWarnModalOpen = false)}>
				Cancel
			</button>
			<button
				class="btn btn-warning btn-sm rounded-xl font-bold text-warning-content shadow-md"
				onclick={handleSendWarning}
				disabled={isActionLoading}
			>
				{#if isActionLoading}
					<span class="loading loading-spinner loading-xs"></span>
				{/if}
				Send Warning
			</button>
		{/snippet}
	</GlassModal>

	<!-- Force Disconnect Modal -->
	<GlassModal
		isOpen={isDisconnectModalOpen}
		title="Disqualify Candidate"
		onClose={() => (isDisconnectModalOpen = false)}
	>
		<div class="space-y-3 text-xs text-base-content/80">
			<p>
				Are you sure you want to forcibly disconnect and disqualify this candidate? All buffered answers in Redis will be flushed to PostgreSQL for audit purposes and the session will be terminated immediately.
			</p>
		</div>

		{#snippet actions()}
			<button class="btn btn-ghost btn-sm rounded-xl" onclick={() => (isDisconnectModalOpen = false)}>
				Cancel
			</button>
			<button
				class="btn btn-error btn-sm rounded-xl font-bold text-white shadow-lg"
				onclick={handleForceDisconnect}
				disabled={isActionLoading}
			>
				{#if isActionLoading}
					<span class="loading loading-spinner loading-xs"></span>
				{/if}
				Confirm Disqualification
			</button>
		{/snippet}
	</GlassModal>
</div>
