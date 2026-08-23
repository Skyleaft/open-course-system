<script lang="ts">
	import { page } from '$app/state';
	import { coursesApi } from '#lib/api/courses.ts';
	import type { Course, CourseSection } from '#lib/api/types.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import GlassModal from '#lib/components/ui/GlassModal.svelte';
	import FileUpload from '#lib/components/ui/FileUpload.svelte';
	import { toast } from '#lib/stores/toast.svelte.ts';
	import { Plus, Check, PlayCircle, FileText, Download, Layers, ArrowLeft, Send } from '@lucide/svelte';
	import { onMount } from 'svelte';

	const courseId = (page.params.id || '') as string;
	let course = $state<Course | null>(null);
	let isLoading = $state(true);

	// Section Modal
	let isSectionModalOpen = $state(false);
	let newSectionTitle = $state('');

	// Lesson Modal
	let isLessonModalOpen = $state(false);
	let targetSectionId = $state<string | null>(null);
	let newLessonTitle = $state('');
	let newLessonType = $state<'Video' | 'PdfDocument' | 'DownloadableFile'>('Video');
	let newLessonDuration = $state(15);
	let newLessonContentUrl = $state('');

	onMount(async () => {
		await loadCourse();
	});

	async function loadCourse() {
		isLoading = true;
		try {
			course = await coursesApi.getCourseById(courseId);
		} catch {
			// Fallback demo course
			course = {
				id: courseId,
				title: 'Advanced Distributed Systems',
				description: 'Curriculum builder demo',
				accessType: 'OpenFree',
				price: 0,
				isPublished: false,
				sections: []
			};
		} finally {
			isLoading = false;
		}
	}

	async function handleAddSection() {
		if (!newSectionTitle) return;
		try {
			await coursesApi.addSection(courseId, {
				title: newSectionTitle,
				orderIndex: (course?.sections?.length || 0) + 1
			});
			toast.success('Section added!');
			newSectionTitle = '';
			isSectionModalOpen = false;
			await loadCourse();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to add section.');
		}
	}

	async function handleAddLesson() {
		if (!targetSectionId || !newLessonTitle || !newLessonContentUrl) {
			toast.warning('Please complete all lesson details and content URL.');
			return;
		}

		try {
			await coursesApi.addLesson(targetSectionId, {
				title: newLessonTitle,
				type: newLessonType,
				contentUrl: newLessonContentUrl,
				durationMinutes: Number(newLessonDuration) || 0,
				orderIndex: 1
			});
			toast.success('Lesson created!');
			newLessonTitle = '';
			newLessonContentUrl = '';
			isLessonModalOpen = false;
			await loadCourse();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to add lesson.');
		}
	}

	async function handlePublish() {
		try {
			await coursesApi.publishCourse(courseId);
			toast.success('Course published to public catalog!');
			if (course) course.isPublished = true;
		} catch (err: any) {
			toast.error(err?.message || 'Failed to publish course.');
		}
	}
</script>

<div class="space-y-8">
	<!-- Header -->
	<div class="flex items-center justify-between">
		<a
			href="/instructor/courses"
			class="inline-flex items-center gap-1.5 text-xs font-semibold text-base-content/60 hover:text-primary transition-colors"
		>
			<ArrowLeft class="h-4 w-4" />
			Back to Courses
		</a>

		{#if course && !course.isPublished}
			<button
				class="btn btn-success btn-sm rounded-xl text-white font-bold border-0 shadow-md gap-1.5"
				onclick={handlePublish}
			>
				<Send class="h-3.5 w-3.5" />
				Publish Course
			</button>
		{/if}
	</div>

	{#if isLoading}
		<div class="glass-panel h-80 rounded-3xl animate-pulse"></div>
	{:else if course}
		<div class="glass-panel rounded-3xl border border-white/10 p-8 shadow-2xl space-y-4">
			<div class="flex items-center justify-between">
				<div class="space-y-1">
					<span class="badge {course.isPublished ? 'badge-success text-white' : 'badge-warning'} badge-xs font-semibold">
						{course.isPublished ? 'Published' : 'Draft'}
					</span>
					<h1 class="text-3xl font-extrabold text-base-content tracking-tight">{course.title}</h1>
				</div>
				<button
					class="btn btn-secondary gradient-accent btn-sm rounded-xl text-white font-semibold border-0 shadow-md gap-1.5"
					onclick={() => (isSectionModalOpen = true)}
				>
					<Plus class="h-4 w-4" />
					Add Section
				</button>
			</div>
		</div>

		<!-- Sections & Lessons Builder -->
		<div class="space-y-4">
			{#each course.sections || [] as section, sIdx (section.id || sIdx)}
				<GlassCard class="space-y-4 p-6">
					<div class="flex items-center justify-between border-b border-white/10 pb-3">
						<div class="flex items-center gap-2">
							<span class="gradient-accent flex h-6 w-6 items-center justify-center rounded-lg text-xs font-bold text-white">
								{sIdx + 1}
							</span>
							<h3 class="text-base font-bold text-base-content">{section.title}</h3>
						</div>
						<button
							class="btn btn-ghost btn-xs text-secondary hover:bg-secondary/10 gap-1 font-semibold"
							onclick={() => {
								targetSectionId = section.id;
								isLessonModalOpen = true;
							}}
						>
							<Plus class="h-3.5 w-3.5" />
							Add Lesson
						</button>
					</div>

					<!-- Lessons -->
					<div class="space-y-2">
						{#each section.lessons || [] as lesson (lesson.id)}
							<div class="flex items-center justify-between rounded-xl bg-base-100/40 p-3 text-xs border border-white/5">
								<div class="flex items-center gap-2.5">
									{#if lesson.type === 'Video'}
										<PlayCircle class="h-4 w-4 text-primary" />
									{:else if lesson.type === 'PdfDocument'}
										<FileText class="h-4 w-4 text-secondary" />
									{:else}
										<Download class="h-4 w-4 text-accent" />
									{/if}
									<span class="font-semibold text-base-content">{lesson.title}</span>
								</div>
								<span class="text-base-content/50">{lesson.durationMinutes}m</span>
							</div>
						{:else}
							<div class="text-center py-4 text-xs text-base-content/50">
								No lessons in this section yet.
							</div>
						{/each}
					</div>
				</GlassCard>
			{:else}
				<div class="glass-card p-12 text-center rounded-3xl border border-white/5 space-y-3">
					<Layers class="h-8 w-8 text-secondary mx-auto opacity-50" />
					<h3 class="text-base font-bold">Curriculum is empty</h3>
					<p class="text-xs text-base-content/60">Create your first section to organize lessons.</p>
				</div>
			{/each}
		</div>
	{/if}

	<!-- Add Section Modal -->
	<GlassModal
		isOpen={isSectionModalOpen}
		title="Create Section"
		onClose={() => (isSectionModalOpen = false)}
	>
		<div class="space-y-3">
			<label class="text-xs font-semibold" for="s-title">Section Title</label>
			<input
				id="s-title"
				type="text"
				class="glass-input input input-sm h-11 w-full rounded-xl text-sm"
				placeholder="e.g. Module 1: Core Fundamentals"
				bind:value={newSectionTitle}
			/>
		</div>

		{#snippet actions()}
			<button class="btn btn-ghost btn-sm rounded-xl" onclick={() => (isSectionModalOpen = false)}>
				Cancel
			</button>
			<button
				class="btn btn-secondary gradient-accent btn-sm rounded-xl text-white font-semibold border-0"
				onclick={handleAddSection}
			>
				Save Section
			</button>
		{/snippet}
	</GlassModal>

	<!-- Add Lesson Modal -->
	<GlassModal
		isOpen={isLessonModalOpen}
		title="Add Lesson to Section"
		onClose={() => (isLessonModalOpen = false)}
	>
		<div class="space-y-4">
			<div class="space-y-1.5">
				<label class="text-xs font-semibold" for="l-title">Lesson Title</label>
				<input
					id="l-title"
					type="text"
					class="glass-input input input-sm h-11 w-full rounded-xl text-sm"
					placeholder="e.g. Overview of Event Sourcing"
					bind:value={newLessonTitle}
				/>
			</div>

			<div class="grid grid-cols-2 gap-3">
				<div class="space-y-1.5">
					<label class="text-xs font-semibold" for="l-type">Material Type</label>
					<select
						id="l-type"
						class="glass-input select select-sm h-11 w-full rounded-xl text-sm"
						bind:value={newLessonType}
					>
						<option value="Video">Video Streaming</option>
						<option value="PdfDocument">PDF Document</option>
						<option value="DownloadableFile">Downloadable Material</option>
					</select>
				</div>

				<div class="space-y-1.5">
					<label class="text-xs font-semibold" for="l-dur">Duration (mins)</label>
					<input
						id="l-dur"
						type="number"
						class="glass-input input input-sm h-11 w-full rounded-xl text-sm"
						bind:value={newLessonDuration}
					/>
				</div>
			</div>

			<div class="space-y-1.5">
				<label class="text-xs font-semibold" for="l-url">Storage URL / MinIO Object Path</label>
				<input
					id="l-url"
					type="text"
					class="glass-input input input-sm h-11 w-full rounded-xl text-sm font-mono text-xs"
					placeholder="https://... or minio/course-materials/lesson1.mp4"
					bind:value={newLessonContentUrl}
				/>
			</div>
		</div>

		{#snippet actions()}
			<button class="btn btn-ghost btn-sm rounded-xl" onclick={() => (isLessonModalOpen = false)}>
				Cancel
			</button>
			<button
				class="btn btn-secondary gradient-accent btn-sm rounded-xl text-white font-semibold border-0"
				onclick={handleAddLesson}
			>
				Create Lesson
			</button>
		{/snippet}
	</GlassModal>
</div>
