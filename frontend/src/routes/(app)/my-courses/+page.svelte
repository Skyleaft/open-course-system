<script lang="ts">
	import { coursesApi } from '#lib/api/courses.ts';
	import type { EnrolledCourseDto } from '#lib/api/types.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import {
		BookOpen,
		GraduationCap,
		Award,
		PlayCircle,
		CheckCircle2,
		Clock,
		Search,
		Layers,
		FileText,
		ArrowRight,
		Sparkles,
		Flame
	} from '@lucide/svelte';
	import { onMount } from 'svelte';

	let enrolledCourses = $state<EnrolledCourseDto[]>([]);
	let isLoading = $state(true);
	let searchTerm = $state('');
	let activeTab = $state<'all' | 'in_progress' | 'completed'>('all');

	onMount(async () => {
		try {
			const res = await coursesApi.getEnrolledCourses();
			enrolledCourses = res || [];
		} catch (err) {
			console.error('Failed to load enrolled courses:', err);
		} finally {
			isLoading = false;
		}
	});

	const filteredCourses = $derived.by(() => {
		let list = enrolledCourses;

		if (activeTab === 'in_progress') {
			list = list.filter((c) => c.progressPercent < 100);
		} else if (activeTab === 'completed') {
			list = list.filter((c) => c.progressPercent >= 100);
		}

		if (searchTerm.trim()) {
			const term = searchTerm.trim().toLowerCase();
			list = list.filter(
				(c) =>
					c.title.toLowerCase().includes(term) ||
					(c.description && c.description.toLowerCase().includes(term))
			);
		}

		return list;
	});

	const totalEnrolled = $derived(enrolledCourses.length);
	const inProgressCount = $derived(enrolledCourses.filter((c) => c.progressPercent < 100).length);
	const completedCount = $derived(enrolledCourses.filter((c) => c.progressPercent >= 100).length);
	const avgProgress = $derived(
		totalEnrolled > 0
			? Math.round(enrolledCourses.reduce((sum, c) => sum + c.progressPercent, 0) / totalEnrolled)
			: 0
	);
</script>

<div class="space-y-8">
	<!-- Top Hero / Stats Banner -->
	<div class="glass-panel relative overflow-hidden rounded-3xl border border-white/10 p-6 sm:p-8 shadow-2xl">
		<div class="pointer-events-none absolute -top-24 -right-24 h-72 w-72 rounded-full bg-primary/15 blur-3xl"></div>
		<div class="pointer-events-none absolute -bottom-24 -left-24 h-72 w-72 rounded-full bg-secondary/10 blur-3xl"></div>

		<div class="relative z-10 flex flex-col md:flex-row md:items-center md:justify-between gap-6">
			<div class="space-y-2">
				<div class="inline-flex items-center gap-2 rounded-full bg-primary/10 px-3 py-1 text-xs font-semibold text-primary">
					<GraduationCap class="h-3.5 w-3.5" />
					<span>Student Dashboard</span>
				</div>
				<h1 class="text-3xl font-extrabold tracking-tight text-base-content sm:text-4xl">
					My Enrolled Courses
				</h1>
				<p class="text-sm text-base-content/70 max-w-xl">
					Track your ongoing curriculum, resume lessons where you left off, and complete assignments and exams to earn certificates.
				</p>
			</div>

			<!-- Quick Stats Cards -->
			<div class="grid grid-cols-2 sm:grid-cols-3 gap-3">
				<div class="glass-card rounded-2xl p-4 border border-white/10 space-y-1">
					<span class="text-[10px] font-bold uppercase tracking-wider text-base-content/60">Enrolled</span>
					<div class="text-2xl font-black text-base-content">{totalEnrolled}</div>
				</div>
				<div class="glass-card rounded-2xl p-4 border border-white/10 space-y-1">
					<span class="text-[10px] font-bold uppercase tracking-wider text-base-content/60">In Progress</span>
					<div class="text-2xl font-black text-primary">{inProgressCount}</div>
				</div>
				<div class="glass-card rounded-2xl p-4 border border-white/10 space-y-1 col-span-2 sm:col-span-1">
					<span class="text-[10px] font-bold uppercase tracking-wider text-base-content/60">Completed</span>
					<div class="text-2xl font-black text-success">{completedCount}</div>
				</div>
			</div>
		</div>
	</div>

	<!-- Controls & Filters Bar -->
	<div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
		<!-- Tabs -->
		<div class="tabs tabs-boxed bg-base-200/60 p-1 rounded-2xl self-start">
			<button
				class="tab tab-sm rounded-xl font-semibold transition-all {activeTab === 'all' ? 'tab-active !bg-primary !text-white' : ''}"
				onclick={() => (activeTab = 'all')}
			>
				All ({totalEnrolled})
			</button>
			<button
				class="tab tab-sm rounded-xl font-semibold transition-all {activeTab === 'in_progress' ? 'tab-active !bg-primary !text-white' : ''}"
				onclick={() => (activeTab = 'in_progress')}
			>
				In Progress ({inProgressCount})
			</button>
			<button
				class="tab tab-sm rounded-xl font-semibold transition-all {activeTab === 'completed' ? 'tab-active !bg-primary !text-white' : ''}"
				onclick={() => (activeTab = 'completed')}
			>
				Completed ({completedCount})
			</button>
		</div>

		<!-- Search & Catalog link -->
		<div class="flex items-center gap-3 w-full sm:w-auto">
			<div class="relative flex-1 sm:w-64">
				<Search class="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-base-content/50" />
				<input
					type="text"
					placeholder="Search enrolled courses..."
					bind:value={searchTerm}
					class="input input-sm w-full rounded-xl bg-base-200/50 pl-9 border-white/10 focus:border-primary/50 text-xs"
				/>
			</div>

			<a href="/courses" class="btn btn-outline btn-sm rounded-xl text-xs gap-1.5 shrink-0">
				<BookOpen class="h-3.5 w-3.5" />
				Explore Catalog
			</a>
		</div>
	</div>

	<!-- Courses Grid / Content Area -->
	{#if isLoading}
		<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
			{#each Array(3) as _}
				<div class="glass-panel h-80 rounded-3xl animate-pulse"></div>
			{/each}
		</div>
	{:else if filteredCourses.length === 0}
		<div class="glass-panel rounded-3xl p-12 border border-white/10 text-center space-y-4 max-w-lg mx-auto my-8">
			<div class="mx-auto flex h-16 w-16 items-center justify-center rounded-3xl bg-primary/10 text-primary">
				<BookOpen class="h-8 w-8" />
			</div>
			<div class="space-y-1">
				<h3 class="text-xl font-bold text-base-content">
					{searchTerm ? 'No Matching Enrolled Courses' : activeTab === 'completed' ? 'No Completed Courses Yet' : 'No Enrolled Courses'}
				</h3>
				<p class="text-xs text-base-content/60 leading-relaxed">
					{searchTerm
						? 'Try adjusting your search terms.'
						: activeTab === 'completed'
							? 'Continue working through your active courses to finish and earn certificates.'
							: 'Browse through our extensive catalog of courses and enroll to begin your learning journey.'}
				</p>
			</div>
			<div class="pt-2">
				<a href="/courses" class="btn btn-primary gradient-accent btn-sm rounded-xl text-white font-bold border-0 shadow-md">
					Browse Course Catalog
				</a>
			</div>
		</div>
	{:else}
		<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
			{#each filteredCourses as course (course.id)}
				{@const isFinished = course.progressPercent >= 100}
				{@const learnUrl = course.lastAccessedLessonId
					? `/courses/${course.id}/learn?lessonId=${course.lastAccessedLessonId}`
					: `/courses/${course.id}/learn`}

				<div class="glass-panel flex flex-col justify-between rounded-3xl border border-white/10 p-6 shadow-xl hover:border-primary/40 transition-all duration-300 hover:shadow-2xl group">
					<div class="space-y-4">
						<!-- Top Thumbnail / Banner -->
						<div class="relative h-40 w-full overflow-hidden rounded-2xl bg-base-200/50">
							{#if course.thumbnailUrl}
								<img
									src={course.thumbnailUrl}
									alt={course.title}
									class="h-full w-full object-cover transition-transform duration-500 group-hover:scale-105"
								/>
							{:else}
								<div class="flex h-full w-full items-center justify-center gradient-accent text-white/30">
									<BookOpen class="h-16 w-16" />
								</div>
							{/if}

							<div class="absolute top-3 left-3 flex items-center gap-1.5">
								<span class="badge {course.accessType === 'OpenFree' ? 'badge-success' : 'badge-primary'} badge-sm font-bold uppercase text-[10px] shadow-md">
									{course.accessType === 'OpenFree' ? 'Free' : course.accessType}
								</span>
							</div>

							{#if isFinished}
								<div class="absolute top-3 right-3">
									<span class="badge badge-success badge-sm font-bold text-white shadow-md flex items-center gap-1">
										<CheckCircle2 class="h-3 w-3" /> Completed
									</span>
								</div>
							{/if}
						</div>

						<!-- Title & Details -->
						<div class="space-y-1.5">
							<a
								href="/courses/{course.id}"
								class="text-lg font-bold text-base-content hover:text-primary transition-colors line-clamp-1"
							>
								{course.title}
							</a>
							<p class="text-xs text-base-content/70 line-clamp-2 leading-relaxed">
								{course.description || 'No description provided.'}
							</p>
						</div>

						<!-- Course Curriculum Summary Stats -->
						<div class="flex items-center gap-3 text-xs text-base-content/60 pt-1 border-t border-white/10">
							<div class="flex items-center gap-1">
								<Layers class="h-3.5 w-3.5 text-primary" />
								<span>{course.completedLessonsCount} / {course.totalLessonsCount} Lessons</span>
							</div>
							{#if course.totalAssignmentsCount > 0}
								<div class="flex items-center gap-1">
									<FileText class="h-3.5 w-3.5 text-secondary" />
									<span>{course.completedAssignmentsCount} / {course.totalAssignmentsCount} Assignments</span>
								</div>
							{/if}
						</div>

						<!-- Progress Bar -->
						<div class="space-y-1.5 pt-1">
							<div class="flex items-center justify-between text-xs font-semibold">
								<span class="text-base-content/60">Progress</span>
								<span class="{isFinished ? 'text-success' : 'text-primary'} font-bold">
									{course.progressPercent}%
								</span>
							</div>
							<div class="h-2 w-full overflow-hidden rounded-full bg-base-200/80">
								<div
									class="h-full transition-all duration-500 {isFinished ? 'bg-success' : 'gradient-accent'}"
									style="width: {Math.min(100, Math.max(0, course.progressPercent))}%"
								></div>
							</div>
						</div>
					</div>

					<!-- Bottom Action Buttons -->
					<div class="pt-5 flex items-center gap-2">
						<a
							href={learnUrl}
							class="btn {isFinished ? 'btn-ghost glass-card border border-white/10' : 'btn-primary gradient-accent text-white border-0 shadow-md'} btn-sm rounded-xl flex-1 font-bold gap-1.5"
						>
							<PlayCircle class="h-4 w-4" />
							{isFinished ? 'Review Course' : course.progressPercent > 0 ? 'Resume Learning' : 'Start Learning'}
						</a>

						{#if isFinished}
							<a
								href="/certificates"
								class="btn btn-success btn-sm rounded-xl text-white font-bold border-0 shadow-md gap-1"
								title="View Certificate"
							>
								<Award class="h-4 w-4" />
							</a>
						{/if}
					</div>
				</div>
			{/each}
		</div>
	{/if}
</div>
