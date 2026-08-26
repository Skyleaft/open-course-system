<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { coursesApi } from '$lib/api/courses.ts';
	import type { Course } from '$lib/api/types.ts';
	import GlassCard from '$lib/components/ui/GlassCard.svelte';
	import SyllabusTree from '$lib/components/course/SyllabusTree.svelte';
	import EnrollmentFlow from '$lib/components/course/EnrollmentFlow.svelte';
	import RichRenderer from '$lib/components/editor/RichRenderer.svelte';
	import { BookOpen, Layers, Clock, ArrowLeft, Sparkles, CheckCircle2, FileText, Award, Users } from 'lucide-svelte';

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

<div class="space-y-8 max-w-6xl mx-auto pb-16">
	<!-- Back link -->
	<a href="/courses" class="btn btn-sm btn-ghost gap-2 text-base-content/70 hover:text-base-content">
		<ArrowLeft class="w-4 h-4" />
		<span>Back to Catalog</span>
	</a>

	{#if isLoading}
		<div class="h-80 rounded-3xl bg-base-200/50 animate-pulse flex items-center justify-center">
			<span class="loading loading-spinner loading-lg text-primary"></span>
		</div>
	{:else if course}
		<!-- Course Hero Overview Banner -->
		<div class="glass-panel relative overflow-hidden rounded-3xl border border-base-content/10 p-6 sm:p-10 shadow-2xl backdrop-blur-2xl">
			<div class="grid grid-cols-1 gap-8 lg:grid-cols-3 lg:items-center">
				<div class="space-y-4 lg:col-span-2 text-left">
					<div class="flex items-center gap-2 flex-wrap">
						<span class="badge badge-primary badge-sm font-bold uppercase text-[10px]">{course.accessType}</span>
						<span class="text-xs text-base-content/60 flex items-center gap-1">
							<Layers class="w-3.5 h-3.5 text-primary" />
							{course.sections?.length || 0} Sections
						</span>
						<span class="text-xs text-base-content/60 flex items-center gap-1">
							<Users class="w-3.5 h-3.5 text-secondary" />
							{course.enrolledStudentsCount || 0} Students Enrolled
						</span>
						{#if course.exams && course.exams.length > 0}
							<span class="text-xs text-base-content/60 flex items-center gap-1">
								<FileText class="w-3.5 h-3.5 text-secondary" />
								{course.exams.length} Exams Attached
							</span>
						{/if}
					</div>

					<h1 class="text-3xl font-extrabold text-base-content tracking-tight sm:text-4xl">
						{course.title}
					</h1>

					{#if course.description}
						<div class="max-w-2xl text-xs text-base-content/80 leading-relaxed pt-1">
							<RichRenderer content={course.description} />
						</div>
					{:else}
						<p class="text-xs text-base-content/70 leading-relaxed max-w-2xl">
							In-depth comprehensive curriculum designed to build practical and theoretical expertise.
						</p>
					{/if}

					<div class="flex flex-wrap gap-4 pt-2 text-xs text-base-content/75">
						<div class="flex items-center gap-1.5">
							<CheckCircle2 class="w-4 h-4 text-success" />
							Self-Paced Learning
						</div>
						<div class="flex items-center gap-1.5">
							<Award class="w-4 h-4 text-success" />
							Verifiable Certificate
						</div>
						<div class="flex items-center gap-1.5">
							<CheckCircle2 class="w-4 h-4 text-success" />
							Examination Integration
						</div>
					</div>
				</div>

				<!-- Enrollment CTA Card -->
				<div class="glass-card rounded-2xl border border-base-content/10 p-6 shadow-xl space-y-4 text-center">
					<div class="space-y-1">
						<div class="text-xs text-base-content/60 uppercase tracking-wider font-bold">Access Fee</div>
						<div class="text-3xl font-black text-primary font-mono">
							{#if course.accessType === 'OpenPaid'}
								${Number(course.price || 0).toFixed(2)}
							{:else}
								Free
							{/if}
						</div>
					</div>

					{#if course.isEnrolled}
						<a
							href="/courses/{course.id}/learn"
							class="btn btn-primary w-full rounded-2xl font-bold text-primary-content shadow-lg h-12 text-sm"
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
			<div class="flex items-center justify-between border-b border-base-content/10 pb-3">
				<div>
					<h2 class="text-xl font-bold text-base-content flex items-center gap-2">
						<BookOpen class="w-5 h-5 text-primary" />
						Course Curriculum & Milestones
					</h2>
					<p class="text-xs text-base-content/60">Explore sections, lessons, assignments, and integrated examinations</p>
				</div>
			</div>

			<SyllabusTree
				sections={course.sections || []}
				assignments={course.assignments || []}
				exams={course.exams || []}
				isEnrolled={course.isEnrolled}
				courseId={course.id}
			/>
		</div>
	{:else}
		<div class="glass-card p-12 text-center rounded-3xl border border-base-content/10 space-y-2">
			<h3 class="text-lg font-bold text-base-content">Course Not Found</h3>
			<p class="text-xs text-base-content/60">The requested course does not exist or has been removed.</p>
		</div>
	{/if}
</div>
