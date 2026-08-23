<script lang="ts">
	import { page } from '$app/state';
	import { coursesApi } from '#lib/api/courses.ts';
	import type { Course } from '#lib/api/types.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import SyllabusTree from '#lib/components/course/SyllabusTree.svelte';
	import EnrollmentFlow from '#lib/components/course/EnrollmentFlow.svelte';
	import { BookOpen, Layers, Clock, ArrowLeft, Sparkles, CheckCircle2 } from '@lucide/svelte';
	import { onMount } from 'svelte';

	const courseId = (page.params.id || '') as string;
	let course = $state<Course | null>(null);
	let isLoading = $state(true);

	onMount(async () => {
		try {
			course = await coursesApi.getCourseById(courseId);
		} catch (err) {
			console.error(err);
		} finally {
			isLoading = false;
		}
	});
</script>

<div class="space-y-8">
	<!-- Back link -->
	<a href="/courses" class="inline-flex items-center gap-1.5 text-xs font-semibold text-base-content/60 hover:text-primary transition-colors">
		<ArrowLeft class="h-4 w-4" />
		Back to Catalog
	</a>

	{#if isLoading}
		<div class="glass-panel h-80 rounded-3xl animate-pulse border border-white/5"></div>
	{:else if course}
		<!-- Course Hero Overview Banner -->
		<div class="glass-panel relative overflow-hidden rounded-3xl border border-white/10 p-8 sm:p-12 shadow-2xl backdrop-blur-2xl">
			<div class="grid grid-cols-1 gap-8 lg:grid-cols-3 lg:items-center">
				<div class="space-y-4 lg:col-span-2 text-left">
					<div class="flex items-center gap-2">
						<span class="badge badge-primary badge-sm font-semibold uppercase">{course.accessType}</span>
						<span class="text-xs text-base-content/60 flex items-center gap-1">
							<Layers class="h-3.5 w-3.5" />
							{course.sections?.length || 0} Sections
						</span>
					</div>

					<h1 class="text-3xl font-extrabold text-base-content tracking-tight sm:text-4xl">
						{course.title}
					</h1>

					<p class="text-sm text-base-content/70 leading-relaxed max-w-2xl">
						{course.description || 'In-depth comprehensive curriculum designed to build practical and theoretical expertise.'}
					</p>

					<div class="flex flex-wrap gap-4 pt-2 text-xs text-base-content/75">
						<div class="flex items-center gap-1.5">
							<CheckCircle2 class="h-4 w-4 text-success" />
							Self-Paced Learning
						</div>
						<div class="flex items-center gap-1.5">
							<CheckCircle2 class="h-4 w-4 text-success" />
							Verifiable Certificate
						</div>
						<div class="flex items-center gap-1.5">
							<CheckCircle2 class="h-4 w-4 text-success" />
							Hands-On Assignments
						</div>
					</div>
				</div>

				<!-- Enrollment CTA Card -->
				<div class="glass-card rounded-2xl border border-white/10 p-6 shadow-xl space-y-4 text-center">
					<div class="space-y-1">
						<div class="text-xs text-base-content/60">Access Fee</div>
						<div class="text-3xl font-black text-primary">
							{#if course.accessType === 'OpenPaid'}
								${course.price?.toFixed(2)}
							{:else}
								Free
							{/if}
						</div>
					</div>

					{#if course.isEnrolled}
						<a
							href="/courses/{course.id}/learn"
							class="btn btn-primary gradient-accent w-full rounded-2xl font-bold text-white border-0 shadow-lg h-12 text-sm"
						>
							Go to Course Player
						</a>
					{:else}
						<EnrollmentFlow {course} onEnrolled={() => (course!.isEnrolled = true)} />
					{/if}
				</div>
			</div>
		</div>

		<!-- Syllabus Preview Section -->
		<div class="space-y-4">
			<div class="flex items-center justify-between border-b border-white/10 pb-3">
				<div>
					<h2 class="text-xl font-bold text-base-content">Course Syllabus</h2>
					<p class="text-xs text-base-content/60">Explore sections and lessons included in this course</p>
				</div>
			</div>

			<SyllabusTree sections={course.sections || []} isEnrolled={course.isEnrolled} />
		</div>
	{:else}
		<div class="glass-card p-12 text-center rounded-3xl border border-white/5 space-y-2">
			<h3 class="text-lg font-bold">Course Not Found</h3>
			<p class="text-xs text-base-content/60">The requested course does not exist or has been removed.</p>
		</div>
	{/if}
</div>
