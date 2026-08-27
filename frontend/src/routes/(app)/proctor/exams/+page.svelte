<script lang="ts">
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import { proctorApi, type ProctorCourseRoom, type ProctorRoomExam } from '#lib/api/proctor.ts';
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
		AlertTriangle,
		BookOpen,
		GraduationCap,
		FileText,
		Layers
	} from '@lucide/svelte';
	import { onMount } from 'svelte';

	let courseRooms = $state<ProctorCourseRoom[]>([]);
	let isLoading = $state(true);
	let searchQuery = $state('');
	let filterPreset = $state<'all' | 'camera' | 'strict'>('all');

	onMount(() => {
		loadCourseRooms();
	});

	async function loadCourseRooms() {
		isLoading = true;
		try {
			const res = await proctorApi.getProctorRooms();
			courseRooms = res || [];
		} catch (err) {
			console.error('Failed to load course proctor rooms:', err);
		} finally {
			isLoading = false;
		}
	}

	const filteredCourseRooms = $derived(
		courseRooms.filter((room) => {
			const q = searchQuery.toLowerCase().trim();
			const matchesSearch =
				!q ||
				room.courseTitle.toLowerCase().includes(q) ||
				(room.courseDescription && room.courseDescription.toLowerCase().includes(q)) ||
				(room.instructorName && room.instructorName.toLowerCase().includes(q)) ||
				room.exams.some((e) => e.title.toLowerCase().includes(q));

			if (!matchesSearch) return false;

			if (filterPreset === 'camera') {
				return room.exams.some((e) => e.ruleConfig?.requireCamera === true);
			}
			if (filterPreset === 'strict') {
				return room.exams.some(
					(e) =>
						e.ruleConfig?.forceFullscreen === true &&
						e.ruleConfig?.restrictClipboardAndMouse === true
				);
			}
			return true;
		})
	);

	const totalActiveTestTakers = $derived(
		courseRooms.reduce((acc, r) => acc + (r.totalActiveCandidates || 0), 0)
	);
	const totalFlaggedAlerts = $derived(
		courseRooms.reduce((acc, r) => acc + (r.totalFlaggedViolations || 0), 0)
	);
	const totalMonitoredExams = $derived(
		courseRooms.reduce((acc, r) => acc + (r.exams?.length || 0), 0)
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
					Course Examination Rooms
				</h1>
				<p class="text-xs md:text-sm text-base-content/70 leading-relaxed">
					Supervise course examinations in real time. Each course with scheduled assessments creates an active proctoring room to monitor student webcam feeds, tab switches, and security integrity.
				</p>
			</div>

			<!-- Quick Stat Counter Cards -->
			<div class="flex items-center gap-3 flex-wrap sm:flex-nowrap">
				<div class="glass-card rounded-2xl p-4 border border-white/10 min-w-[100px] text-center shadow-lg">
					<div class="text-2xl font-black text-primary font-mono">{courseRooms.length}</div>
					<div class="text-[10px] font-bold text-base-content/60 uppercase tracking-wider">Course Rooms</div>
				</div>
				<div class="glass-card rounded-2xl p-4 border border-white/10 min-w-[100px] text-center shadow-lg">
					<div class="text-2xl font-black text-secondary font-mono">{totalMonitoredExams}</div>
					<div class="text-[10px] font-bold text-base-content/60 uppercase tracking-wider">Total Exams</div>
				</div>
				<div class="glass-card rounded-2xl p-4 border border-success/30 bg-success/5 min-w-[110px] text-center shadow-lg">
					<div class="text-2xl font-black text-success font-mono flex items-center justify-center gap-1">
						{#if totalActiveTestTakers > 0}
							<span class="h-2 w-2 rounded-full bg-success animate-ping"></span>
						{/if}
						{totalActiveTestTakers}
					</div>
					<div class="text-[10px] font-bold text-success/80 uppercase tracking-wider">Active Students</div>
				</div>
				<div class="glass-card rounded-2xl p-4 border border-warning/30 bg-warning/5 min-w-[100px] text-center shadow-lg">
					<div class="text-2xl font-black text-warning font-mono">{totalFlaggedAlerts}</div>
					<div class="text-[10px] font-bold text-warning/80 uppercase tracking-wider">Flags</div>
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
				placeholder="Search by course, exam, or instructor..."
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
					All Courses
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
				onclick={loadCourseRooms}
				disabled={isLoading}
				title="Refresh rooms"
			>
				<RefreshCw class="h-4 w-4 {isLoading ? 'animate-spin text-primary' : 'text-base-content/70'}" />
			</button>
		</div>
	</div>

	<!-- Course Examination Rooms Grid -->
	{#if isLoading}
		<div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
			{#each [1, 2, 3, 4] as _}
				<div class="glass-card h-72 rounded-3xl animate-pulse border border-white/10"></div>
			{/each}
		</div>
	{:else if filteredCourseRooms.length === 0}
		<div class="glass-card p-12 text-center rounded-3xl border border-white/10 space-y-4 max-w-lg mx-auto my-12 shadow-xl">
			<div class="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-base-300 text-base-content/40">
				<ShieldAlert class="h-7 w-7" />
			</div>
			<div class="space-y-1">
				<h3 class="text-lg font-bold text-base-content">No Course Examination Rooms Found</h3>
				<p class="text-xs text-base-content/60">
					{searchQuery
						? 'No courses with exams match your search criteria.'
						: 'No published courses currently have attached examination assessments.'}
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
		<div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
			{#each filteredCourseRooms as room (room.courseId)}
				{@const hasActive = room.totalActiveCandidates > 0}
				{@const hasFlags = room.totalFlaggedViolations > 0}

				<GlassCard
					class="flex flex-col justify-between p-6 rounded-3xl border transition-all duration-300 shadow-xl hover:shadow-2xl space-y-5 {hasActive
						? 'border-primary/40 bg-primary/5'
						: 'border-white/10 hover:border-white/20'}"
				>
					<!-- Course Room Header -->
					<div class="space-y-4">
						<div class="flex items-start justify-between gap-4">
							<!-- Course Info & Thumbnail -->
							<div class="flex items-start gap-3.5 min-w-0">
								{#if room.thumbnailUrl}
									<img
										src={room.thumbnailUrl}
										alt={room.courseTitle}
										class="h-12 w-12 rounded-2xl object-cover border border-white/10 shrink-0 shadow-md"
									/>
								{:else}
									<div
										class="h-12 w-12 rounded-2xl bg-primary/10 text-primary border border-primary/20 flex items-center justify-center shrink-0 shadow-md font-bold text-base"
									>
										<BookOpen class="h-6 w-6" />
									</div>
								{/if}

								<div class="min-w-0 space-y-1">
									<div class="flex items-center gap-2 flex-wrap">
										<span
											class="badge {hasActive ? 'badge-error text-white' : 'badge-neutral'} badge-xs font-bold uppercase gap-1 shadow-sm"
										>
											{#if hasActive}
												<span class="h-1.5 w-1.5 rounded-full bg-white animate-ping"></span>
												Live Active ({room.totalActiveCandidates})
											{:else}
												<span class="h-1.5 w-1.5 rounded-full bg-base-content/40"></span>
												Ready
											{/if}
										</span>

										{#if hasFlags}
											<span class="badge badge-warning badge-xs font-bold text-warning-content gap-1">
												<AlertTriangle class="h-2.5 w-2.5" />
												{room.totalFlaggedViolations} Flagged
											</span>
										{/if}

										<span class="text-[11px] text-base-content/50 font-medium">
											{room.enrolledStudentsCount} Enrolled
										</span>
									</div>

									<h3 class="text-base font-bold text-base-content leading-snug truncate" title={room.courseTitle}>
										{room.courseTitle}
									</h3>

									{#if room.instructorName}
										<div class="flex items-center gap-1.5 text-xs text-base-content/60">
											<GraduationCap class="h-3.5 w-3.5 text-primary" />
											<span>Instructor: <strong class="text-base-content/80">{room.instructorName}</strong></span>
										</div>
									{/if}
								</div>
							</div>
						</div>

						{#if room.courseDescription}
							<p class="text-xs text-base-content/60 line-clamp-2 leading-relaxed">
								{room.courseDescription}
							</p>
						{/if}

						<!-- Attached Exams List / Sub-rooms -->
						<div class="space-y-2 pt-2 border-t border-white/10">
							<div class="flex items-center justify-between text-[11px] font-bold uppercase tracking-wider text-base-content/60">
								<span class="flex items-center gap-1.5">
									<Layers class="h-3.5 w-3.5 text-primary" />
									Course Assessments ({room.exams.length})
								</span>
							</div>

							<div class="space-y-2">
								{#each room.exams as exam (exam.examId)}
									{@const examIsActive = exam.activeCandidatesCount > 0}

									<div
										class="p-3.5 rounded-2xl bg-base-100/60 border border-white/10 hover:border-primary/30 transition-all flex flex-col sm:flex-row sm:items-center justify-between gap-3"
									>
										<!-- Exam Details -->
										<div class="space-y-1.5 min-w-0">
											<div class="flex items-center gap-2 flex-wrap">
												<span class="text-xs font-bold text-base-content truncate" title={exam.title}>
													{exam.title}
												</span>
												{#if examIsActive}
													<span class="badge badge-error badge-xs text-white font-bold animate-pulse">
														{exam.activeCandidatesCount} Active
													</span>
												{/if}
											</div>

											<!-- Rule Presets Pills -->
											<div class="flex flex-wrap items-center gap-1.5 text-[10px]">
												<span class="flex items-center gap-1 text-base-content/60 font-mono">
													<Clock class="h-3 w-3" />
													{exam.durationMinutes}m
												</span>
												<span class="text-base-content/30">•</span>
												<span class="text-base-content/60 font-mono">
													{exam.totalQuestions} Qs
												</span>

												{#if exam.ruleConfig?.requireCamera}
													<span class="badge badge-xs bg-primary/10 border-primary/20 text-primary font-semibold gap-1">
														<Camera class="h-2.5 w-2.5" />
														Snapshot ({exam.ruleConfig?.snapshotIntervalSeconds || 45}s)
													</span>
												{/if}
												{#if exam.ruleConfig?.forceFullscreen}
													<span class="badge badge-xs bg-secondary/10 border-secondary/20 text-secondary font-semibold gap-1">
														<Lock class="h-2.5 w-2.5" />
														Lockdown
													</span>
												{/if}
												{#if exam.ruleConfig?.requireMicrophone}
													<span class="badge badge-xs bg-warning/10 border-warning/20 text-warning font-semibold gap-1">
														<Volume2 class="h-2.5 w-2.5" />
														Audio
													</span>
												{/if}
											</div>
										</div>

										<!-- Enter Live Exam Proctor Button -->
										<div class="shrink-0 flex items-center justify-end">
											<a
												href="/proctor/exams/{exam.examId}/live"
												class="btn btn-primary btn-xs sm:btn-sm rounded-xl font-bold text-white shadow-md shadow-primary/20 hover:scale-105 transition-all gap-1.5"
											>
												<span>Enter Proctor Room</span>
												<ArrowRight class="h-3.5 w-3.5" />
											</a>
										</div>
									</div>
								{/each}
							</div>
						</div>
					</div>
				</GlassCard>
			{/each}
		</div>
	{/if}
</div>
