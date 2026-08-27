<script lang="ts">
	import { authStore } from '#lib/stores/auth.svelte.ts';
	import { dashboardApi, type StudentDashboardOverview } from '#lib/api/dashboard.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import StatCard from '#lib/components/ui/StatCard.svelte';
	import CompetencyRadarChart from '#lib/components/ui/CompetencyRadarChart.svelte';
	import {
		BookOpen,
		GraduationCap,
		Award,
		Clock,
		ArrowRight,
		PlayCircle,
		Sparkles,
		AlertCircle,
		CheckCircle2,
		Calendar,
		Flame
	} from '@lucide/svelte';
	import { onMount } from 'svelte';

	let overview = $state<StudentDashboardOverview | null>(null);
	let isLoading = $state(true);

	onMount(async () => {
		try {
			const res = await dashboardApi.getStudentDashboardOverview();
			if (res) {
				overview = res;
			}
		} catch (err) {
			console.error('Failed to load student dashboard:', err);
		} finally {
			isLoading = false;
		}
	});
</script>

<div class="space-y-8">
	<!-- Welcome Banner -->
	<div class="glass-panel relative overflow-hidden rounded-3xl border border-white/10 p-8 shadow-2xl backdrop-blur-2xl">
		<div class="relative z-10 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
			<div class="space-y-2">
				<div class="inline-flex items-center gap-2 rounded-lg bg-primary/10 border border-primary/20 px-3 py-1 text-xs font-semibold text-primary">
					<Sparkles class="h-3.5 w-3.5" />
					Student Learning Center
				</div>
				<h1 class="text-3xl font-extrabold tracking-tight text-base-content sm:text-4xl">
					Hello, {authStore.user?.fullName || 'Student'}! 👋
				</h1>
				<p class="text-xs text-base-content/70 sm:text-sm">
					Track your modular learning progress, upcoming assignment deadlines, and exam milestones.
				</p>
			</div>

			<div class="flex items-center gap-3">
				<a href="/courses" class="btn btn-primary gradient-accent rounded-xl text-xs font-semibold text-white border-0 shadow-md">
					<BookOpen class="h-4 w-4 mr-1" />
					Explore Catalog
				</a>
				<a href="/my-courses" class="btn btn-ghost glass-card rounded-xl text-xs font-medium border border-white/10 hover:bg-base-100/40">
					<GraduationCap class="h-4 w-4 mr-1 text-primary" />
					My Courses
				</a>
			</div>
		</div>
	</div>

	<!-- Metric Stat Cards -->
	<div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
		<StatCard
			title="Active Courses"
			value={overview ? String(overview.activeCoursesCount) : '0'}
			description="Modular learning tracks"
			icon={BookOpen}
			color="primary"
		/>
		<StatCard
			title="Completed"
			value={overview ? String(overview.completedCoursesCount) : '0'}
			description="Fully mastered courses"
			icon={CheckCircle2}
			color="success"
		/>
		<StatCard
			title="Certificates"
			value={overview ? String(overview.certificatesCount) : '0'}
			description="Cryptographically signed"
			icon={Award}
			color="accent"
		/>
		<StatCard
			title="Urgent Deadlines"
			value={overview ? String(overview.pendingAssignmentsCount) : '0'}
			description="Tasks due within 7 days"
			icon={Clock}
			color={overview && overview.pendingAssignmentsCount > 0 ? 'warning' : 'info'}
		/>
	</div>

	<!-- Two-column Section: In-Progress Courses & Urgent Deadlines / Radar -->
	<div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
		<!-- In-Progress Courses (Span 2 cols on LG) -->
		<div class="lg:col-span-2 space-y-6">
			<GlassCard>
				<div class="space-y-4">
					<div class="flex items-center justify-between border-b border-white/10 pb-3">
						<h3 class="text-base font-bold text-base-content flex items-center gap-2">
							<BookOpen class="h-4 w-4 text-primary" />
							In-Progress Courses
						</h3>
						<a href="/my-courses" class="text-xs font-medium text-primary hover:underline flex items-center gap-1">
							View All <ArrowRight class="h-3 w-3" />
						</a>
					</div>

					{#if isLoading}
						<div class="space-y-3">
							<div class="h-20 rounded-xl bg-base-200/50 animate-pulse"></div>
							<div class="h-20 rounded-xl bg-base-200/50 animate-pulse"></div>
						</div>
					{:else if overview && overview.enrolledCourses.length > 0}
						<div class="space-y-3">
							{#each overview.enrolledCourses as course (course.courseId)}
								<div class="rounded-2xl bg-base-100/40 border border-white/5 p-4 hover:border-primary/30 transition-all">
									<div class="flex items-center justify-between gap-4 mb-2.5">
										<div class="flex items-center gap-3 min-w-0">
											<div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
												<BookOpen class="h-5 w-5" />
											</div>
											<div class="space-y-0.5 overflow-hidden">
												<h4 class="text-xs font-bold text-base-content truncate">{course.title}</h4>
												<div class="text-[10px] text-base-content/60">
													{course.completedLessons} / {course.totalLessons} Lessons Completed
												</div>
											</div>
										</div>

										{#if course.lastLessonId}
											<a
												href="/courses/{course.courseId}/learn?lessonId={course.lastLessonId}"
												class="btn btn-primary gradient-accent btn-xs rounded-lg text-white border-0 shrink-0 font-semibold gap-1"
											>
												<PlayCircle class="h-3.5 w-3.5" />
												Continue
											</a>
										{:else}
											<a
												href="/courses/{course.courseId}/learn"
												class="btn btn-ghost glass-card btn-xs rounded-lg border border-white/10 shrink-0 text-primary"
											>
												Start
											</a>
										{/if}
									</div>

									<!-- Progress Bar -->
									<div class="space-y-1">
										<div class="flex justify-between text-[10px] text-base-content/60">
											<span>Progress</span>
											<span class="font-bold text-primary">{course.progressPercentage}%</span>
										</div>
										<progress
											class="progress progress-primary w-full h-1.5 bg-base-300/60"
											value={course.progressPercentage}
											max="100"
										></progress>
									</div>
								</div>
							{/each}
						</div>
					{:else}
						<div class="p-8 text-center text-xs text-base-content/50">
							No active course enrollments yet. <a href="/courses" class="text-primary font-bold hover:underline">Explore Catalog</a>
						</div>
					{/if}
				</div>
			</GlassCard>

			<!-- Urgent Deadlines -->
			<GlassCard>
				<div class="space-y-4">
					<div class="flex items-center justify-between border-b border-white/10 pb-3">
						<h3 class="text-base font-bold text-base-content flex items-center gap-2">
							<Clock class="h-4 w-4 text-warning" />
							Upcoming Deadlines & Milestones
						</h3>
						<span class="text-[10px] text-base-content/50 font-medium">Within 7 days</span>
					</div>

					{#if isLoading}
						<div class="h-16 rounded-xl bg-base-200/50 animate-pulse"></div>
					{:else if overview && overview.upcomingDeadlines.length > 0}
						<div class="space-y-2.5">
							{#each overview.upcomingDeadlines as item (item.id)}
								<div class="flex items-center justify-between rounded-xl {item.isUrgent ? 'bg-error/10 border-error/20' : 'bg-base-100/40 border-white/5'} border p-3.5 transition-colors">
									<div class="space-y-0.5 overflow-hidden pr-2">
										<div class="flex items-center gap-2">
											<span class="badge {item.isUrgent ? 'badge-error' : 'badge-ghost'} badge-xs font-bold text-[9px] uppercase">
												{item.itemType}
											</span>
											<span class="text-xs font-bold text-base-content truncate">{item.title}</span>
										</div>
										<div class="text-[10px] text-base-content/60">
											{item.courseTitle} • Due: {new Date(item.deadlineUtc).toLocaleDateString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })}
										</div>
									</div>

									<div class="text-right shrink-0">
										<div class="text-[11px] font-extrabold {item.isUrgent ? 'text-error flex items-center gap-1' : 'text-base-content/70'}">
											{#if item.isUrgent}<Flame class="h-3.5 w-3.5 animate-bounce" />{/if}
											{item.remainingHours}h left
										</div>
									</div>
								</div>
							{/each}
						</div>
					{:else}
						<div class="p-6 text-center text-xs text-base-content/50">
							🎉 All caught up! No urgent assignments or exams due soon.
						</div>
					{/if}
				</div>
			</GlassCard>
		</div>

		<!-- Right Column: Competency Radar & Certifications (1 col on LG) -->
		<div class="space-y-6">
			<GlassCard>
				<div class="space-y-4">
					<div class="border-b border-white/10 pb-3">
						<h3 class="text-base font-bold text-base-content">Competency Radar</h3>
						<p class="text-[10px] text-base-content/60">Domain mastery across courses</p>
					</div>

					{#if isLoading}
						<div class="h-56 rounded-xl bg-base-200/50 animate-pulse"></div>
					{:else if overview && overview.competencyRadar.length > 0}
						<CompetencyRadarChart points={overview.competencyRadar} size={220} />
					{:else}
						<div class="p-6 text-center text-xs text-base-content/50">
							No competency data yet.
						</div>
					{/if}
				</div>
			</GlassCard>

			<GlassCard>
				<div class="space-y-4">
					<div class="flex items-center justify-between border-b border-white/10 pb-3">
						<h3 class="text-base font-bold text-base-content flex items-center gap-2">
							<Award class="h-4 w-4 text-accent" />
							Certificates
						</h3>
						<a href="/certificates" class="text-xs font-medium text-accent hover:underline flex items-center gap-1">
							View <ArrowRight class="h-3 w-3" />
						</a>
					</div>

					<div class="p-4 rounded-xl bg-accent/10 border border-accent/20 text-center space-y-2">
						<div class="text-2xl font-extrabold text-accent">{overview?.certificatesCount ?? 0}</div>
						<p class="text-xs text-base-content/70">Verified Digital Certificates Earned</p>
						<a href="/certificates" class="btn btn-accent btn-xs rounded-lg text-white border-0 shadow-sm font-semibold">
							Browse Credentials
						</a>
					</div>
				</div>
			</GlassCard>
		</div>
	</div>
</div>
