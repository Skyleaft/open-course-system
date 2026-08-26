<script lang="ts">
	import { page } from '$app/state';
	import {
		proctorApi,
		type LiveCandidate,
		type CandidateViolation,
		type CandidateSnapshotItem
	} from '#lib/api/proctor.ts';
	import { examsApi } from '#lib/api/exams.ts';
	import type { ExamSummaryDto } from '#lib/api/types.ts';
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
		CheckCircle2,
		Clock,
		Search,
		Eye,
		Volume2,
		Lock,
		RefreshCw,
		SlidersHorizontal,
		Maximize2,
		Megaphone,
		History,
		AlertCircle,
		VolumeX
	} from '@lucide/svelte';
	import { onMount, onDestroy } from 'svelte';

	const quizId = (page.params.quizId || '') as string;

	// State
	let exam = $state<ExamSummaryDto | null>(null);
	let candidates = $state<LiveCandidate[]>([]);
	let violationsFeed = $state<CandidateViolation[]>([]);
	let isPageLoading = $state(true);
	let isConnectingHub = $state(true);
	let searchQuery = $state('');
	let statusFilter = $state<'all' | 'active' | 'flagged' | 'disqualified'>('all');
	let gridDensity = $state<'standard' | 'compact'>('standard');
	let soundAlertEnabled = $state(true);

	// Modals & Action Target State
	let isWarnModalOpen = $state(false);
	let isDisconnectModalOpen = $state(false);
	let isSnapshotModalOpen = $state(false);
	let isBroadcastModalOpen = $state(false);
	let isInspectModalOpen = $state(false);

	let selectedCandidate = $state<LiveCandidate | null>(null);
	let candidateSnapshots = $state<CandidateSnapshotItem[]>([]);
	let isLoadingSnapshots = $state(false);
	let selectedSnapshotIndex = $state(0);

	let warnTargetSubmissionId = $state<string | null>(null);
	let warnTargetStudentName = $state<string>('Candidate');
	let warnMessage = $state('Please ensure your face is clearly visible in the camera and remain in full screen.');
	let broadcastMessage = $state('Reminder to all candidates: Please ensure your workstation is clear of unauthorized items.');
	let disconnectReason = $state('Academic integrity policy violation');
	let isActionLoading = $state(false);

	let examHub: ExamHubClient | null = null;
	let timerInterval: ReturnType<typeof setInterval> | null = null;

	// Fast warning presets
	const warningPresets = [
		'Please ensure your face remains clearly visible in the camera frame.',
		'Tab switching and exiting fullscreen mode is strictly prohibited.',
		'Excessive ambient voice or background noise detected.',
		'Multiple faces or unauthorized movement detected near your screen.',
		'Keyboard shortcuts and external apps must not be accessed during exam.'
	];

	onMount(async () => {
		await loadInitialData();

		// Set up tick timer to decrement local remaining seconds smoothly
		timerInterval = setInterval(() => {
			candidates = candidates.map((c) => ({
				...c,
				remainingSeconds: Math.max(0, c.remainingSeconds - 1)
			}));
		}, 1000);

		// Connect to ExamHub SignalR
		await initSignalR();
	});

	onDestroy(() => {
		if (timerInterval) clearInterval(timerInterval);
		if (examHub) examHub.stop();
	});

	async function loadInitialData() {
		isPageLoading = true;
		try {
			// Fetch Exam details
			const examRes = await examsApi.getExamById(quizId);
			if (examRes) {
				exam = examRes as unknown as ExamSummaryDto;
			}

			// Fetch Live Candidates
			const candidatesRes = await proctorApi.getLiveCandidates(quizId);
			candidates = candidatesRes || [];

			// Populate initial violation feed from recorded violations
			const initialFeed: CandidateViolation[] = [];
			for (const cand of candidates) {
				if (cand.violations && cand.violations.length > 0) {
					for (const v of cand.violations) {
						initialFeed.push({
							id: `${cand.submissionId}-${v.timestampUtc}`,
							studentId: cand.studentId,
							studentName: cand.studentName,
							submissionId: cand.submissionId,
							violationType: v.violationType,
							details: v.details,
							timestampUtc: v.timestampUtc,
							violationCount: cand.violationCount
						});
					}
				}
			}
			violationsFeed = initialFeed.sort(
				(a, b) => new Date(b.timestampUtc).getTime() - new Date(a.timestampUtc).getTime()
			);
		} catch (err: any) {
			console.error('Failed to load live candidates from API:', err);
			toast.error(err?.message || 'Failed to fetch live candidates from server.');
			candidates = [];
			violationsFeed = [];
		} finally {
			isPageLoading = false;
		}
	}

	async function initSignalR() {
		isConnectingHub = true;
		try {
			examHub = new ExamHubClient();
			await examHub.start();
			await examHub.joinProctorRoom(quizId);

			// 1. Candidate Joined
			examHub.onCandidateJoined((studentId, submissionId) => {
				const existing = candidates.find((c) => c.submissionId === submissionId);
				if (existing) {
					existing.isOnline = true;
					existing.status = 'InProgress';
				} else {
					// Refresh list to pull full metadata
					proctorApi.getLiveCandidates(quizId).then((res) => {
						if (res) candidates = res;
					});
				}
				toast.info('New candidate connected to examination session.');
			});

			// 2. Candidate Status Changed
			examHub.onCandidateStatusChanged((submissionId, newStatus) => {
				candidates = candidates.map((c) =>
					c.submissionId === submissionId ? { ...c, status: newStatus, isOnline: newStatus === 'InProgress' } : c
				);
			});

			// 3. Violation Alert
			examHub.onProctorViolationAlert((studentId, submissionId, violationType, count, reason) => {
				const target = candidates.find((c) => c.submissionId === submissionId || c.studentId === studentId);
				if (target) {
					target.violationCount = count;
				}

				const violationItem: CandidateViolation = {
					id: `${submissionId}-${Date.now()}`,
					studentId,
					studentName: target?.studentName || 'Candidate',
					submissionId,
					violationType,
					details: reason,
					timestampUtc: new Date().toISOString(),
					violationCount: count
				};

				violationsFeed = [violationItem, ...violationsFeed.slice(0, 49)];

				if (soundAlertEnabled) {
					playNotificationChime();
				}

				toast.warning(
					`Violation Alert: ${target?.studentName || 'Candidate'} — ${violationType} (${count}/${exam?.ruleConfig?.maxAllowedViolations || 3})`
				);
			});

			// 4. Proctor Snapshot Received
			examHub.onProctorSnapshotReceived((studentId, submissionId, snapshotUrl, timestampUtc) => {
				const target = candidates.find((c) => c.submissionId === submissionId || c.studentId === studentId);
				if (target) {
					target.latestSnapshotPresignedUrl = snapshotUrl;
					target.latestSnapshotTimeUtc = timestampUtc;
					target.snapshotsCaptured = (target.snapshotsCaptured || 0) + 1;
				}
			});

			// 5. Room Broadcast Sent
			examHub.onRoomBroadcastSent((msg) => {
				toast.success(`Broadcast Sent: "${msg}"`);
			});
		} catch (err) {
			console.error('Failed to initialize SignalR for Proctor Room:', err);
		} finally {
			isConnectingHub = false;
		}
	}

	function playNotificationChime() {
		try {
			const ctx = new (window.AudioContext || (window as any).webkitAudioContext)();
			const osc = ctx.createOscillator();
			const gain = ctx.createGain();
			osc.type = 'sine';
			osc.frequency.setValueAtTime(587.33, ctx.currentTime); // D5
			osc.frequency.setValueAtTime(880, ctx.currentTime + 0.1); // A5
			gain.gain.setValueAtTime(0.15, ctx.currentTime);
			gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.35);
			osc.connect(gain);
			gain.connect(ctx.destination);
			osc.start();
			osc.stop(ctx.currentTime + 0.35);
		} catch {
			// AudioContext blocked by policy
		}
	}

	function formatRemainingTime(seconds: number): string {
		if (seconds <= 0) return '00:00';
		const m = Math.floor(seconds / 60);
		const s = seconds % 60;
		return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
	}

	function formatRelativeTime(dateStr?: string): string {
		if (!dateStr) return 'Never';
		const diffMs = Date.now() - new Date(dateStr).getTime();
		const diffSec = Math.floor(diffMs / 1000);
		if (diffSec < 10) return 'Just now';
		if (diffSec < 60) return `${diffSec}s ago`;
		const diffMin = Math.floor(diffSec / 60);
		return `${diffMin}m ago`;
	}

	// Filtered candidates
	const filteredCandidates = $derived(
		candidates.filter((c) => {
			const search = searchQuery.toLowerCase().trim();
			const matchesSearch =
				!search ||
				c.studentName.toLowerCase().includes(search) ||
				(c.studentEmail && c.studentEmail.toLowerCase().includes(search)) ||
				c.studentId.toLowerCase().includes(search);

			if (!matchesSearch) return false;

			if (statusFilter === 'active') return c.status === 'InProgress';
			if (statusFilter === 'flagged') return c.violationCount > 0;
			if (statusFilter === 'disqualified') return c.status === 'Disqualified';
			return true;
		})
	);

	// Summary stats
	const activeCount = $derived(candidates.filter((c) => c.status === 'InProgress').length);
	const flaggedCount = $derived(candidates.filter((c) => c.violationCount > 0).length);
	const disqualifiedCount = $derived(candidates.filter((c) => c.status === 'Disqualified').length);

	// Action Handlers
	function openWarnModal(c: LiveCandidate) {
		warnTargetSubmissionId = c.submissionId;
		warnTargetStudentName = c.studentName;
		isWarnModalOpen = true;
	}

	function openDisconnectModal(c: LiveCandidate) {
		warnTargetSubmissionId = c.submissionId;
		warnTargetStudentName = c.studentName;
		isDisconnectModalOpen = true;
	}

	async function openSnapshotTimeline(c: LiveCandidate) {
		selectedCandidate = c;
		selectedSnapshotIndex = 0;
		isSnapshotModalOpen = true;
		isLoadingSnapshots = true;
		try {
			const res = await proctorApi.getCandidateSnapshots(c.submissionId);
			candidateSnapshots = res || [];
		} catch (err) {
			console.warn('Failed to load candidate snapshots:', err);
			candidateSnapshots = [];
		} finally {
			isLoadingSnapshots = false;
		}
	}

	async function handleSendWarning() {
		if (!warnTargetSubmissionId || !warnMessage.trim()) return;
		isActionLoading = true;
		try {
			await proctorApi.sendWarning(warnTargetSubmissionId, warnMessage.trim());
			toast.success(`Warning dispatched to ${warnTargetStudentName}.`);
			isWarnModalOpen = false;
		} catch (err: any) {
			toast.error(err?.message || 'Failed to send warning.');
		} finally {
			isActionLoading = false;
		}
	}

	async function handleForceDisconnect() {
		if (!warnTargetSubmissionId) return;
		isActionLoading = true;
		try {
			await proctorApi.forceDisconnect(warnTargetSubmissionId, disconnectReason.trim());
			toast.success(`${warnTargetStudentName} forcibly disqualified.`);
			candidates = candidates.map((c) =>
				c.submissionId === warnTargetSubmissionId ? { ...c, status: 'Disqualified', isOnline: false } : c
			);
			isDisconnectModalOpen = false;
		} catch (err: any) {
			toast.error(err?.message || 'Failed to disconnect candidate.');
		} finally {
			isActionLoading = false;
		}
	}

	async function handleBroadcastRoom() {
		if (!broadcastMessage.trim()) return;
		isActionLoading = true;
		try {
			await proctorApi.broadcastExamMessage(quizId, broadcastMessage.trim());
			toast.success('Room announcement broadcasted to all active candidates.');
			isBroadcastModalOpen = false;
		} catch (err: any) {
			toast.error(err?.message || 'Failed to broadcast room message.');
		} finally {
			isActionLoading = false;
		}
	}
</script>

<div class="space-y-6 pb-12">
	<!-- Top Navigation & Room Status Bar -->
	<div
		class="glass-panel rounded-3xl border border-white/10 p-5 shadow-2xl backdrop-blur-2xl bg-base-100/60 flex flex-col md:flex-row md:items-center md:justify-between gap-4"
	>
		<div class="flex items-center gap-4">
			<a
				href="/proctor/exams"
				class="btn btn-circle btn-ghost btn-sm rounded-xl border border-white/10 text-base-content/70 hover:text-primary transition-colors"
				title="Back to Exam Rooms"
			>
				<ArrowLeft class="h-4 w-4" />
			</a>

			<div class="space-y-1">
				<div class="flex items-center gap-2">
					<span
						class="inline-flex items-center gap-1.5 badge badge-error badge-sm font-bold uppercase text-white shadow-sm"
					>
						<span class="h-2 w-2 rounded-full bg-white animate-ping"></span>
						Live Proctoring Console
					</span>
					<span class="badge badge-ghost badge-sm font-mono text-[11px] border-white/10">
						Room #{quizId.substring(0, 8)}
					</span>
				</div>
				<h1 class="text-xl md:text-2xl font-extrabold text-base-content tracking-tight">
					{exam?.title || 'Examination Monitoring Console'}
				</h1>
			</div>
		</div>

		<!-- Right Side Controls & Actions -->
		<div class="flex items-center flex-wrap gap-2">
			<!-- Sound alert toggle -->
			<button
				class="btn btn-ghost btn-sm rounded-xl border border-white/10 text-xs gap-1.5 {soundAlertEnabled ? 'text-primary' : 'text-base-content/40'}"
				onclick={() => (soundAlertEnabled = !soundAlertEnabled)}
				title={soundAlertEnabled ? 'Sound alert enabled' : 'Sound alert muted'}
			>
				{#if soundAlertEnabled}
					<Volume2 class="h-4 w-4" />
					<span class="hidden sm:inline">Audio Alert</span>
				{:else}
					<VolumeX class="h-4 w-4" />
					<span class="hidden sm:inline">Muted</span>
				{/if}
			</button>

			<!-- Room Broadcast Trigger -->
			<button
				class="btn btn-warning btn-sm rounded-xl font-bold text-warning-content shadow-md gap-1.5"
				onclick={() => (isBroadcastModalOpen = true)}
			>
				<Megaphone class="h-4 w-4" />
				Broadcast Room
			</button>

			<!-- Manual Refresh -->
			<button
				class="btn btn-ghost btn-sm btn-circle rounded-xl border border-white/10"
				onclick={loadInitialData}
				title="Refresh candidate data"
			>
				<RefreshCw class="h-4 w-4 {isPageLoading ? 'animate-spin text-primary' : 'text-base-content/70'}" />
			</button>
		</div>
	</div>

	<!-- Room Rules & Metrics Pill Strip -->
	<div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
		<div class="glass-card rounded-2xl p-3 border border-white/10 flex items-center justify-between">
			<div>
				<div class="text-[10px] uppercase font-bold text-base-content/60">Active Test-Takers</div>
				<div class="text-xl font-black text-primary font-mono">{activeCount}</div>
			</div>
			<div class="h-9 w-9 rounded-xl bg-primary/10 flex items-center justify-center text-primary">
				<Users class="h-5 w-5" />
			</div>
		</div>

		<div class="glass-card rounded-2xl p-3 border border-warning/30 bg-warning/5 flex items-center justify-between">
			<div>
				<div class="text-[10px] uppercase font-bold text-warning/80">Flagged Sessions</div>
				<div class="text-xl font-black text-warning font-mono">{flaggedCount}</div>
			</div>
			<div class="h-9 w-9 rounded-xl bg-warning/15 flex items-center justify-center text-warning">
				<AlertTriangle class="h-5 w-5" />
			</div>
		</div>

		<div class="glass-card rounded-2xl p-3 border border-error/30 bg-error/5 flex items-center justify-between">
			<div>
				<div class="text-[10px] uppercase font-bold text-error/80">Disqualified</div>
				<div class="text-xl font-black text-error font-mono">{disqualifiedCount}</div>
			</div>
			<div class="h-9 w-9 rounded-xl bg-error/15 flex items-center justify-center text-error">
				<UserX class="h-5 w-5" />
			</div>
		</div>

		<div class="glass-card rounded-2xl p-3 border border-white/10 flex items-center justify-between">
			<div>
				<div class="text-[10px] uppercase font-bold text-base-content/60">Snapshot Interval</div>
				<div class="text-xl font-black text-base-content font-mono">
					{exam?.ruleConfig?.snapshotIntervalSeconds || 45}s
				</div>
			</div>
			<div class="h-9 w-9 rounded-xl bg-base-200 flex items-center justify-center text-base-content/70">
				<Camera class="h-5 w-5" />
			</div>
		</div>
	</div>

	<!-- Main Workspace: Candidate Grid & Live Alert Sidebar -->
	<div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
		<!-- Candidate Feeds Column (2 Cols on Large Screens) -->
		<div class="lg:col-span-2 space-y-4">
			<!-- Filter & Density Toolbar -->
			<div class="flex flex-col sm:flex-row items-center justify-between gap-3">
				<div class="relative w-full sm:w-72">
					<Search class="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-base-content/40" />
					<input
						type="text"
						placeholder="Search candidate name or email..."
						bind:value={searchQuery}
						class="glass-input input input-sm w-full pl-10 rounded-2xl text-xs bg-base-100/50 border-white/10"
					/>
				</div>

				<div class="flex items-center gap-2 w-full sm:w-auto justify-end">
					<!-- Filter Pills -->
					<div class="join bg-base-200/50 p-1 rounded-2xl border border-white/10">
						<button
							class="btn btn-xs rounded-xl join-item {statusFilter === 'all' ? 'btn-primary font-bold shadow' : 'btn-ghost text-base-content/60'}"
							onclick={() => (statusFilter = 'all')}
						>
							All ({candidates.length})
						</button>
						<button
							class="btn btn-xs rounded-xl join-item {statusFilter === 'flagged' ? 'btn-warning font-bold shadow text-warning-content' : 'btn-ghost text-base-content/60'}"
							onclick={() => (statusFilter = 'flagged')}
						>
							Flagged ({flaggedCount})
						</button>
						<button
							class="btn btn-xs rounded-xl join-item {statusFilter === 'disqualified' ? 'btn-error font-bold shadow text-white' : 'btn-ghost text-base-content/60'}"
							onclick={() => (statusFilter = 'disqualified')}
						>
							Disqualified ({disqualifiedCount})
						</button>
					</div>

					<!-- Density Toggle -->
					<button
						class="btn btn-ghost btn-sm btn-circle rounded-xl border border-white/10"
						onclick={() => (gridDensity = gridDensity === 'standard' ? 'compact' : 'standard')}
						title="Toggle Grid Density"
					>
						<SlidersHorizontal class="h-4 w-4 text-base-content/70" />
					</button>
				</div>
			</div>

			<!-- Candidates Tiles Grid -->
			{#if isPageLoading}
				<div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
					{#each [1, 2, 3, 4] as _}
						<div class="glass-card h-72 rounded-3xl animate-pulse border border-white/10"></div>
					{/each}
				</div>
			{:else if candidates.length === 0}
				<div class="glass-card p-12 text-center rounded-3xl border border-white/10 space-y-4">
					<div class="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-primary/10 text-primary border border-primary/20">
						<Radio class="h-7 w-7 animate-pulse" />
					</div>
					<div class="space-y-1">
						<h3 class="text-base font-bold text-base-content">Awaiting Candidate Connections</h3>
						<p class="text-xs text-base-content/60 max-w-md mx-auto leading-relaxed">
							No student candidates are currently taking this exam. When students launch their exam attempt, their live webcam frames, telemetry, and integrity status will appear here automatically via real-time SignalR stream.
						</p>
					</div>
				</div>
			{:else if filteredCandidates.length === 0}
				<div class="glass-card p-12 text-center rounded-3xl border border-white/10 space-y-3">
					<Users class="h-8 w-8 mx-auto text-base-content/30" />
					<p class="text-xs font-semibold text-base-content/60">
						No candidates match your current filter.
					</p>
					<button
						class="btn btn-xs btn-ghost border border-white/10 rounded-xl"
						onclick={() => {
							searchQuery = '';
							statusFilter = 'all';
						}}
					>
						Reset Filters
					</button>
				</div>
			{:else}
				<div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
					{#each filteredCandidates as c (c.submissionId)}
						{@const maxViolations = exam?.ruleConfig?.maxAllowedViolations ?? 3}
						{@const isCritical = c.violationCount >= maxViolations || c.status === 'Disqualified'}
						{@const isWarning = c.violationCount > 0 && !isCritical}

						<GlassCard
							class="p-4 space-y-3 rounded-3xl border transition-all duration-300 shadow-lg {isCritical
								? 'border-error/50 bg-error/5 ring-1 ring-error/30'
								: isWarning
									? 'border-warning/40 bg-warning/5'
									: 'border-white/10 hover:border-primary/40'}"
						>
							<!-- Candidate Tile Header -->
							<div class="flex items-center justify-between gap-2">
								<div class="flex items-center gap-2.5 min-w-0">
									<div class="relative">
										{#if c.studentAvatarUrl}
											<img
												src={c.studentAvatarUrl}
												alt={c.studentName}
												class="h-8 w-8 rounded-full object-cover border border-white/20"
											/>
										{:else}
											<div
												class="h-8 w-8 rounded-full bg-primary/20 text-primary font-bold text-xs flex items-center justify-center border border-primary/30"
											>
												{c.studentName.charAt(0).toUpperCase()}
											</div>
										{/if}
										<span
											class="absolute -bottom-0.5 -right-0.5 h-2.5 w-2.5 rounded-full border-2 border-base-100 {c.isOnline && c.status === 'InProgress' ? 'bg-success animate-pulse' : 'bg-base-content/40'}"
										></span>
									</div>

									<div class="min-w-0">
										<div class="text-xs font-bold text-base-content truncate leading-tight">
											{c.studentName}
										</div>
										<div class="text-[10px] text-base-content/50 truncate font-mono">
											{c.studentEmail || c.studentId.substring(0, 10)}
										</div>
									</div>
								</div>

								<!-- Violations Badge -->
								<div class="flex flex-col items-end">
									<span
										class="badge {isCritical ? 'badge-error text-white' : isWarning ? 'badge-warning text-warning-content' : 'badge-success text-white'} badge-xs font-bold gap-1"
									>
										{#if isCritical}
											⚠ Critical: {c.violationCount}/{maxViolations}
										{:else if isWarning}
											⚠ {c.violationCount} Violations
										{:else}
											✓ Clean (0)
										{/if}
									</span>
									<span class="text-[9px] text-base-content/50 font-mono mt-0.5">
										{c.status}
									</span>
								</div>
							</div>

							<!-- Web Camera Snapshot Frame -->
							<div
								class="relative aspect-video w-full overflow-hidden rounded-2xl bg-black/80 border border-white/10 flex items-center justify-center group/snap cursor-pointer"
								onclick={() => openSnapshotTimeline(c)}
								role="button"
								tabindex="0"
								onkeydown={(e) => e.key === 'Enter' && openSnapshotTimeline(c)}
							>
								{#if c.latestSnapshotPresignedUrl}
									<img
										src={c.latestSnapshotPresignedUrl}
										alt="Latest Snapshot"
										class="h-full w-full object-cover transition-transform duration-300 group-hover/snap:scale-105"
									/>
									<div
										class="absolute inset-0 bg-black/40 opacity-0 group-hover/snap:opacity-100 transition-opacity flex items-center justify-center gap-1 text-white text-xs font-semibold"
									>
										<Eye class="h-4 w-4" />
										Inspect Timeline ({c.snapshotsCaptured || 1})
									</div>

									<!-- Timestamp Tag -->
									<div
										class="absolute bottom-2 left-2 rounded-lg bg-black/70 backdrop-blur-md px-2 py-0.5 text-[9px] font-mono text-white/90 border border-white/10"
									>
										{formatRelativeTime(c.latestSnapshotTimeUtc)}
									</div>
								{:else}
									<div class="flex flex-col items-center gap-1.5 text-base-content/40 p-4 text-center">
										<Camera class="h-6 w-6 opacity-30 animate-pulse" />
										<span class="text-[10px]">Awaiting camera snapshot capture...</span>
									</div>
								{/if}
							</div>

							<!-- Session Meta: Timer & Actions -->
							<div class="flex items-center justify-between pt-1 text-xs">
								<div class="flex items-center gap-1.5 text-base-content/70 font-mono text-[11px]">
									<Clock class="h-3.5 w-3.5 text-primary" />
									<span>{formatRemainingTime(c.remainingSeconds)}</span>
								</div>

								<!-- Action Button Group -->
								<div class="flex items-center gap-1.5">
									<button
										class="btn btn-ghost btn-xs rounded-lg border border-white/10 hover:bg-base-200 text-[11px]"
										onclick={() => openSnapshotTimeline(c)}
										title="View Snapshot Gallery"
									>
										<History class="h-3 w-3" />
										Snaps
									</button>

									<button
										class="btn btn-warning btn-xs rounded-lg font-bold text-warning-content shadow-sm gap-1 text-[11px]"
										onclick={() => openWarnModal(c)}
										disabled={c.status === 'Disqualified'}
									>
										<AlertTriangle class="h-3 w-3" />
										Warn
									</button>

									<button
										class="btn btn-error btn-xs rounded-lg font-bold text-white shadow-sm gap-1 text-[11px]"
										onclick={() => openDisconnectModal(c)}
										disabled={c.status === 'Disqualified'}
									>
										<UserX class="h-3 w-3" />
										Disqualify
									</button>
								</div>
							</div>
						</GlassCard>
					{/each}
				</div>
			{/if}
		</div>

		<!-- Real-time Violation Alert Feed (Right Sidebar) -->
		<div class="space-y-4">
			<div class="glass-card rounded-3xl border border-white/10 p-5 space-y-4 shadow-xl">
				<div class="flex items-center justify-between border-b border-white/10 pb-3">
					<div class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-base-content">
						<ShieldAlert class="h-4 w-4 text-warning animate-bounce" />
						Real-Time Integrity Stream
					</div>
					<span class="badge badge-warning badge-sm font-mono font-bold">
						{violationsFeed.length} Events
					</span>
				</div>

				<!-- Feed Item List -->
				<div class="space-y-2.5 max-h-[640px] overflow-y-auto pr-1">
					{#each violationsFeed as v (v.id)}
						<div
							class="rounded-2xl bg-base-100/50 p-3.5 border border-warning/20 space-y-2 text-xs hover:border-warning/50 transition-colors shadow-sm"
						>
							<div class="flex items-center justify-between">
								<span class="font-bold text-error flex items-center gap-1.5">
									<AlertCircle class="h-3.5 w-3.5" />
									{v.violationType}
								</span>
								<span class="text-[10px] text-base-content/50 font-mono">
									{new Date(v.timestampUtc).toLocaleTimeString()}
								</span>
							</div>

							<div class="text-[11px] text-base-content/80 font-medium">
								{v.studentName || 'Student'} ({v.studentId.substring(0, 8)}...)
							</div>

							{#if v.details}
								<div class="text-[10px] text-base-content/60 bg-base-200/50 p-1.5 rounded-lg font-mono">
									{v.details}
								</div>
							{/if}

							<div class="flex items-center justify-between pt-1 border-t border-white/5 text-[10px]">
								<span class="font-semibold text-warning">
									Accumulated: {v.violationCount} Violations
								</span>

								<button
									class="btn btn-ghost btn-xs text-primary font-bold hover:underline p-0 h-auto min-h-0"
									onclick={() => {
										const target = candidates.find((c) => c.submissionId === v.submissionId);
										if (target) openWarnModal(target);
									}}
								>
									Quick Warn &rarr;
								</button>
							</div>
						</div>
					{:else}
						<div class="text-center py-16 text-xs text-base-content/40 space-y-3">
							<CheckCircle2 class="h-8 w-8 text-success mx-auto opacity-60" />
							<div class="space-y-1">
								<p class="font-bold text-base-content/70">No Violations Detected</p>
								<p class="text-[11px]">Room integrity is verified and active.</p>
							</div>
						</div>
					{/each}
				</div>
			</div>
		</div>
	</div>

	<!-- Modals Section -->

	<!-- 1. Send Direct Warning Modal -->
	<GlassModal
		isOpen={isWarnModalOpen}
		title="Dispatch Warning to {warnTargetStudentName}"
		onClose={() => (isWarnModalOpen = false)}
	>
		<div class="space-y-4 text-xs">
			<p class="text-base-content/70">
				This message will immediately interrupt the candidate's screen with an official proctor warning notice.
			</p>

			<!-- Quick Preset Buttons -->
			<div class="space-y-1.5">
				<span class="block text-[11px] font-bold text-base-content/70 uppercase tracking-wider">
					Quick Presets
				</span>
				<div class="flex flex-wrap gap-1.5">
					{#each warningPresets as preset}
						<button
							type="button"
							class="btn btn-xs rounded-xl border border-white/10 bg-base-100/50 hover:bg-warning/10 hover:border-warning/40 text-[10px] text-left normal-case"
							onclick={() => (warnMessage = preset)}
						>
							{preset}
						</button>
					{/each}
				</div>
			</div>

			<div class="space-y-1.5">
				<label for="custom-warn-text" class="text-[11px] font-bold text-base-content/70 uppercase tracking-wider">
					Warning Text
				</label>
				<textarea
					id="custom-warn-text"
					class="glass-input textarea h-24 w-full rounded-2xl text-xs p-3 bg-base-100/50 border-white/10 focus:border-warning/50"
					bind:value={warnMessage}
				></textarea>
			</div>
		</div>

		{#snippet actions()}
			<button class="btn btn-ghost btn-sm rounded-xl" onclick={() => (isWarnModalOpen = false)}>
				Cancel
			</button>
			<button
				class="btn btn-warning btn-sm rounded-xl font-bold text-warning-content shadow-lg gap-1.5"
				onclick={handleSendWarning}
				disabled={isActionLoading || !warnMessage.trim()}
			>
				{#if isActionLoading}
					<span class="loading loading-spinner loading-xs"></span>
				{:else}
					<Send class="h-3.5 w-3.5" />
				{/if}
				Send Warning
			</button>
		{/snippet}
	</GlassModal>

	<!-- 2. Force Disconnect / Disqualify Modal -->
	<GlassModal
		isOpen={isDisconnectModalOpen}
		title="Disqualify Candidate: {warnTargetStudentName}"
		onClose={() => (isDisconnectModalOpen = false)}
	>
		<div class="space-y-4 text-xs">
			<div class="rounded-2xl bg-error/10 border border-error/30 p-3.5 space-y-2 text-error">
				<div class="flex items-center gap-2 font-bold">
					<AlertTriangle class="h-4 w-4" />
					Irreversible Disqualification Action
				</div>
				<p class="text-[11px] text-base-content/80">
					This will immediately terminate the candidate's active exam attempt, trigger an atomic flush of all
					buffered answers in Redis to PostgreSQL, set the submission status to <strong>Disqualified</strong>, and
					permanently disconnect the candidate.
				</p>
			</div>

			<div class="space-y-1.5">
				<label for="disqualify-reason" class="text-[11px] font-bold text-base-content/70 uppercase tracking-wider">
					Disqualification Reason
				</label>
				<input
					id="disqualify-reason"
					type="text"
					class="glass-input input input-sm w-full rounded-2xl text-xs bg-base-100/50 border-white/10 focus:border-error/50"
					bind:value={disconnectReason}
				/>
			</div>
		</div>

		{#snippet actions()}
			<button class="btn btn-ghost btn-sm rounded-xl" onclick={() => (isDisconnectModalOpen = false)}>
				Cancel
			</button>
			<button
				class="btn btn-error btn-sm rounded-xl font-bold text-white shadow-lg gap-1.5"
				onclick={handleForceDisconnect}
				disabled={isActionLoading}
			>
				{#if isActionLoading}
					<span class="loading loading-spinner loading-xs"></span>
				{:else}
					<UserX class="h-3.5 w-3.5" />
				{/if}
				Confirm Disqualification
			</button>
		{/snippet}
	</GlassModal>

	<!-- 3. Room Broadcast Announcement Modal -->
	<GlassModal
		isOpen={isBroadcastModalOpen}
		title="Broadcast Announcement to Exam Room"
		onClose={() => (isBroadcastModalOpen = false)}
	>
		<div class="space-y-4 text-xs">
			<p class="text-base-content/70">
				Send a synchronous alert notification banner to all {candidates.length} active candidates currently taking
				this exam.
			</p>

			<div class="space-y-1.5">
				<label for="broadcast-txt" class="text-[11px] font-bold text-base-content/70 uppercase tracking-wider">
					Broadcast Message
				</label>
				<textarea
					id="broadcast-txt"
					class="glass-input textarea h-24 w-full rounded-2xl text-xs p-3 bg-base-100/50 border-white/10 focus:border-primary/50"
					bind:value={broadcastMessage}
				></textarea>
			</div>
		</div>

		{#snippet actions()}
			<button class="btn btn-ghost btn-sm rounded-xl" onclick={() => (isBroadcastModalOpen = false)}>
				Cancel
			</button>
			<button
				class="btn btn-primary btn-sm rounded-xl font-bold text-white shadow-lg gap-1.5"
				onclick={handleBroadcastRoom}
				disabled={isActionLoading || !broadcastMessage.trim()}
			>
				{#if isActionLoading}
					<span class="loading loading-spinner loading-xs"></span>
				{:else}
					<Megaphone class="h-3.5 w-3.5" />
				{/if}
				Broadcast Announcement
			</button>
		{/snippet}
	</GlassModal>

	<!-- 4. Snapshot Timeline Gallery Modal -->
	<GlassModal
		isOpen={isSnapshotModalOpen}
		title="Snapshot Timeline: {selectedCandidate?.studentName || 'Candidate'}"
		onClose={() => (isSnapshotModalOpen = false)}
	>
		<div class="space-y-4 text-xs">
			{#if isLoadingSnapshots}
				<div class="aspect-video w-full rounded-2xl bg-base-300 animate-pulse"></div>
			{:else if candidateSnapshots.length === 0}
				<div class="text-center py-12 space-y-2 text-base-content/40">
					<Camera class="h-8 w-8 mx-auto opacity-30" />
					<p>No snapshots have been recorded for this session yet.</p>
				</div>
			{:else}
				{@const currentSnap = candidateSnapshots[selectedSnapshotIndex] || candidateSnapshots[0]}
				<!-- Large Preview Frame -->
				<div class="relative aspect-video w-full overflow-hidden rounded-2xl bg-black border border-white/10">
					<img
						src={currentSnap.presignedUrl}
						alt="Candidate Snapshot Frame"
						class="h-full w-full object-contain"
					/>
					<div
						class="absolute bottom-3 left-3 bg-black/80 backdrop-blur-md px-3 py-1 rounded-xl text-white font-mono text-[11px] border border-white/10"
					>
						Captured: {new Date(currentSnap.capturedAtUtc).toLocaleTimeString()} ({new Date(
							currentSnap.capturedAtUtc
						).toLocaleDateString()})
					</div>
					<div
						class="absolute top-3 right-3 bg-black/80 backdrop-blur-md px-3 py-1 rounded-xl text-white font-mono text-[11px] border border-white/10"
					>
						Frame {selectedSnapshotIndex + 1} of {candidateSnapshots.length}
					</div>
				</div>

				<!-- Thumbnail Carousel Strip -->
				<div class="flex items-center gap-2 overflow-x-auto pb-2">
					{#each candidateSnapshots as snap, idx}
						<button
							type="button"
							class="relative flex-shrink-0 aspect-video h-16 rounded-xl overflow-hidden border-2 transition-all {selectedSnapshotIndex === idx ? 'border-primary scale-105 shadow-md' : 'border-white/10 opacity-60 hover:opacity-100'}"
							onclick={() => (selectedSnapshotIndex = idx)}
						>
							<img src={snap.presignedUrl} alt="Thumbnail" class="h-full w-full object-cover" />
							<div class="absolute bottom-0 inset-x-0 bg-black/70 text-[8px] text-white font-mono text-center">
								{new Date(snap.capturedAtUtc).toLocaleTimeString()}
							</div>
						</button>
					{/each}
				</div>
			{/if}
		</div>

		{#snippet actions()}
			<button class="btn btn-ghost btn-sm rounded-xl" onclick={() => (isSnapshotModalOpen = false)}>
				Close Timeline
			</button>
		{/snippet}
	</GlassModal>
</div>
