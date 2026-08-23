<script lang="ts">
	import { coursesApi } from '#lib/api/courses.ts';
	import type { Course } from '#lib/api/types.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import { Plus, Edit3, BookOpen, Layers, CheckCircle2, FileText, ArrowRight } from '@lucide/svelte';
	import { onMount } from 'svelte';

	let courses = $state<Course[]>([]);
	let isLoading = $state(true);

	onMount(async () => {
		try {
			const res = await coursesApi.getCourses();
			courses = res.items || [];
		} catch {
			courses = [];
		} finally {
			isLoading = false;
		}
	});
</script>

<div class="space-y-8">
	<!-- Header -->
	<div class="glass-panel flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 rounded-3xl border border-white/10 p-8 shadow-2xl backdrop-blur-2xl">
		<div class="space-y-1">
			<div class="inline-flex items-center gap-2 rounded-lg bg-secondary/10 border border-secondary/20 px-3 py-1 text-xs font-semibold text-secondary">
				<Layers class="h-3.5 w-3.5" />
				Instructor Studio
			</div>
			<h1 class="text-3xl font-extrabold tracking-tight text-base-content">
				Course Authoring & Management
			</h1>
			<p class="text-xs text-base-content/70">
				Build curriculum, upload lesson materials, and configure student assignments.
			</p>
		</div>

		<a
			href="/instructor/courses/create"
			class="btn btn-secondary gradient-accent rounded-xl text-xs font-bold text-white border-0 shadow-lg gap-1.5 self-start sm:self-auto"
		>
			<Plus class="h-4 w-4" />
			Create New Course
		</a>
	</div>

	<!-- Course List -->
	{#if isLoading}
		<div class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
			{#each Array(3) as _}
				<div class="glass-panel h-56 rounded-2xl animate-pulse"></div>
			{/each}
		</div>
	{:else if courses.length > 0}
		<div class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
			{#each courses as course (course.id)}
				<GlassCard class="flex flex-col justify-between p-5 space-y-4">
					<div class="space-y-2">
						<div class="flex items-center justify-between">
							<span class="badge badge-primary badge-xs font-bold uppercase">{course.accessType}</span>
							<span class="badge {course.isPublished ? 'badge-success text-white' : 'badge-warning'} badge-xs font-semibold">
								{course.isPublished ? 'Published' : 'Draft'}
							</span>
						</div>
						<h3 class="text-base font-bold text-base-content line-clamp-1">{course.title}</h3>
						<p class="text-xs text-base-content/65 line-clamp-2">{course.description || 'No description provided.'}</p>
					</div>

					<div class="flex items-center justify-between pt-3 border-t border-white/10 text-xs">
						<a
							href="/instructor/courses/{course.id}/edit"
							class="btn btn-ghost btn-xs text-secondary hover:bg-secondary/10 gap-1 font-semibold"
						>
							<Edit3 class="h-3.5 w-3.5" />
							Curriculum
						</a>
						<a
							href="/instructor/courses/{course.id}/assignments"
							class="btn btn-ghost btn-xs text-base-content/70 hover:bg-base-100/40 gap-1"
						>
							<FileText class="h-3.5 w-3.5" />
							Grading
						</a>
					</div>
				</GlassCard>
			{/each}
		</div>
	{:else}
		<div class="glass-card p-12 text-center rounded-3xl border border-white/5 space-y-3">
			<BookOpen class="h-8 w-8 text-secondary mx-auto opacity-50" />
			<h3 class="text-base font-bold text-base-content">No courses created yet</h3>
			<p class="text-xs text-base-content/60">Get started by creating your first course syllabus.</p>
			<a
				href="/instructor/courses/create"
				class="btn btn-secondary gradient-accent btn-sm rounded-xl text-white font-semibold border-0 shadow-md"
			>
				Create Course
			</a>
		</div>
	{/if}
</div>
