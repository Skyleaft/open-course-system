<script lang="ts">
	import { authStore } from '#lib/stores/auth.svelte.ts';
	import { coursesApi } from '#lib/api/courses.ts';
	import { examsApi } from '#lib/api/exams.ts';
	import type { Course, QuizExam } from '#lib/api/types.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import StatCard from '#lib/components/ui/StatCard.svelte';
	import {
		BookOpen,
		GraduationCap,
		Award,
		Clock,
		ArrowRight,
		PlayCircle,
		Sparkles,
		Layers,
		ShieldAlert
	} from '@lucide/svelte';
	import { onMount } from 'svelte';

	let enrolledCourses = $state<Course[]>([]);
	let activeExams = $state<QuizExam[]>([]);
	let isLoading = $state(true);

	onMount(async () => {
		try {
			const [coursesRes, examsRes] = await Promise.allSettled([
				coursesApi.getCourses({ pageSize: 4 }),
				examsApi.listExams({ isPublished: true, pageSize: 4 })
			]);

			if (coursesRes.status === 'fulfilled' && coursesRes.value?.items) {
				enrolledCourses = coursesRes.value.items;
			}
			if (examsRes.status === 'fulfilled' && examsRes.value?.items) {
				activeExams = examsRes.value.items;
			}
		} catch (err) {
			console.error(err);
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
					Ready to continue your courses and prepare for upcoming examinations?
				</p>
			</div>

			<div class="flex items-center gap-3">
				<a href="/courses" class="btn btn-primary gradient-accent rounded-xl text-xs font-semibold text-white border-0 shadow-md">
					<BookOpen class="h-4 w-4 mr-1" />
					Explore Courses
				</a>
				<a href="/exams" class="btn btn-ghost glass-card rounded-xl text-xs font-medium border border-white/10 hover:bg-base-100/40">
					<GraduationCap class="h-4 w-4 mr-1" />
					Examinations
				</a>
			</div>
		</div>
	</div>

	<!-- Metric Stat Cards -->
	<div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
		<StatCard
			title="Active Courses"
			value={enrolledCourses.length > 0 ? String(enrolledCourses.length) : '4'}
			description="Modular learning tracks"
			icon={BookOpen}
			color="primary"
		/>
		<StatCard
			title="Available Exams"
			value={activeExams.length > 0 ? String(activeExams.length) : '6'}
			description="Simulations & Proctored"
			icon={GraduationCap}
			color="secondary"
		/>
		<StatCard
			title="Certificates"
			value="3"
			description="Cryptographically signed"
			icon={Award}
			color="accent"
		/>
		<StatCard
			title="Study Hours"
			value="28h"
			description="+4h this week"
			icon={Clock}
			trend="12%"
			trendUp={true}
			color="info"
		/>
	</div>

	<!-- Quick Jump Sections -->
	<div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
		<!-- Active Courses -->
		<GlassCard>
			<div class="space-y-4">
				<div class="flex items-center justify-between border-b border-white/10 pb-3">
					<h3 class="text-base font-bold text-base-content">Continue Learning</h3>
					<a href="/courses" class="text-xs font-medium text-primary hover:underline flex items-center gap-1">
						View All <ArrowRight class="h-3 w-3" />
					</a>
				</div>

				{#if isLoading}
					<div class="space-y-3">
						<div class="h-14 rounded-xl bg-base-200/50 animate-pulse"></div>
						<div class="h-14 rounded-xl bg-base-200/50 animate-pulse"></div>
					</div>
				{:else if enrolledCourses.length > 0}
					<div class="space-y-3">
						{#each enrolledCourses.slice(0, 3) as course (course.id)}
							<div class="flex items-center justify-between rounded-xl bg-base-100/40 p-3.5 border border-white/5 hover:border-primary/20 transition-all">
								<div class="flex items-center gap-3 overflow-hidden">
									<div class="gradient-accent flex h-10 w-10 shrink-0 items-center justify-center rounded-xl text-white font-bold text-xs uppercase">
										{course.title.slice(0, 2)}
									</div>
									<div class="text-left overflow-hidden">
										<div class="text-xs font-bold text-base-content truncate">{course.title}</div>
										<div class="text-[10px] text-base-content/60 flex items-center gap-1">
											<Layers class="w-3 h-3 text-primary" />
											{course.sections?.length || 0} Sections
										</div>
									</div>
								</div>
								<a href="/courses/{course.id}/learn" class="btn btn-ghost btn-sm btn-circle text-primary hover:bg-primary/10 shrink-0">
									<PlayCircle class="h-5 w-5" />
								</a>
							</div>
						{/each}
					</div>
				{:else}
					<div class="p-6 text-center text-xs text-base-content/50">
						No active course enrollments yet. <a href="/courses" class="text-primary font-bold hover:underline">Explore Catalog</a>
					</div>
				{/if}
			</div>
		</GlassCard>

		<!-- Upcoming & Practice Exams -->
		<GlassCard>
			<div class="space-y-4">
				<div class="flex items-center justify-between border-b border-white/10 pb-3">
					<h3 class="text-base font-bold text-base-content">Available Examinations</h3>
					<a href="/exams" class="text-xs font-medium text-primary hover:underline flex items-center gap-1">
						All Exams <ArrowRight class="h-3 w-3" />
					</a>
				</div>

				{#if isLoading}
					<div class="space-y-3">
						<div class="h-14 rounded-xl bg-base-200/50 animate-pulse"></div>
						<div class="h-14 rounded-xl bg-base-200/50 animate-pulse"></div>
					</div>
				{:else if activeExams.length > 0}
					<div class="space-y-3">
						{#each activeExams.slice(0, 3) as exam (exam.id)}
							{@const isReal = exam.mode === 'RealExam'}
							<div class="flex items-center justify-between rounded-xl {isReal ? 'bg-primary/10 border-primary/20' : 'bg-base-100/40 border-white/5'} border p-3.5">
								<div class="space-y-0.5 text-left overflow-hidden mr-2">
									<div class="flex items-center gap-2">
										<span class="badge {isReal ? 'badge-primary' : 'badge-ghost'} badge-xs uppercase font-bold text-[9px]">
											{isReal ? 'Proctored' : 'Simulation'}
										</span>
										<span class="text-xs font-bold text-base-content truncate">{exam.title}</span>
									</div>
									<div class="text-[10px] text-base-content/60">
										{exam.durationMinutes} mins • Passing: {exam.passingScore}%
									</div>
								</div>
								<a
									href="/exams/{exam.id}/start"
									class="btn {isReal ? 'btn-primary gradient-accent' : 'btn-ghost glass-card border border-white/10'} btn-xs rounded-lg text-white border-0 shrink-0 font-semibold"
								>
									{isReal ? 'Start Exam' : 'Practice'}
								</a>
							</div>
						{/each}
					</div>
				{:else}
					<div class="p-6 text-center text-xs text-base-content/50">
						No examinations currently available.
					</div>
				{/if}
			</div>
		</GlassCard>
	</div>
</div>
