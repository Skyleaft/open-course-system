<script lang="ts">
	import { page } from '$app/state';
	import { coursesApi } from '#lib/api/courses.ts';
	import { communicationsApi } from '#lib/api/communications.ts';
	import type { Course, Lesson, DiscussionThread } from '#lib/api/types.ts';
	import LessonPlayer from '#lib/components/course/LessonPlayer.svelte';
	import SyllabusTree from '#lib/components/course/SyllabusTree.svelte';
	import RichEditor from '#lib/components/editor/RichEditor.svelte';
	import RichRenderer from '#lib/components/editor/RichRenderer.svelte';
	import { toast } from '#lib/stores/toast.svelte.ts';
	import { ArrowLeft, MessageSquare, BookOpen, Send, CheckCircle2 } from '@lucide/svelte';
	import { onMount } from 'svelte';

	const courseId = (page.params.id || '') as string;
	let course = $state<Course | null>(null);
	let activeLesson = $state<Lesson | null>(null);
	let threads = $state<DiscussionThread[]>([]);
	let isLoading = $state(true);

	let newThreadTitle = $state('');
	let newThreadContent = $state('');
	let isPostingThread = $state(false);

	onMount(async () => {
		try {
			course = await coursesApi.getCourseById(courseId);
			if (course?.sections?.[0]?.lessons?.[0]) {
				activeLesson = course.sections[0].lessons[0];
				await loadThreads(activeLesson.id);
			}
		} catch (err) {
			console.error(err);
		} finally {
			isLoading = false;
		}
	});

	async function selectLesson(lesson: Lesson) {
		activeLesson = lesson;
		await loadThreads(lesson.id);
	}

	async function loadThreads(lessonId: string) {
		try {
			threads = await communicationsApi.getThreads(courseId, lessonId);
		} catch {
			threads = [];
		}
	}

	async function handleCreateThread() {
		if (!newThreadTitle || !newThreadContent) {
			toast.warning('Please enter both title and question details.');
			return;
		}

		isPostingThread = true;
		try {
			await communicationsApi.createThread({
				courseId,
				lessonId: activeLesson?.id || null,
				title: newThreadTitle,
				content: newThreadContent
			});
			toast.success('Discussion thread created!');
			newThreadTitle = '';
			newThreadContent = '';
			if (activeLesson) {
				await loadThreads(activeLesson.id);
			}
		} catch (err: any) {
			toast.error(err?.message || 'Failed to post thread.');
		} finally {
			isPostingThread = false;
		}
	}
</script>

<div class="space-y-6">
	<!-- Top Bar -->
	<div class="flex items-center justify-between border-b border-white/10 pb-4">
		<a
			href="/courses/{courseId}"
			class="inline-flex items-center gap-1.5 text-xs font-semibold text-base-content/60 hover:text-primary transition-colors"
		>
			<ArrowLeft class="h-4 w-4" />
			Course Overview
		</a>

		<div class="flex items-center gap-2">
			<span class="text-xs font-bold text-base-content">{course?.title || 'Loading...'}</span>
		</div>
	</div>

	{#if isLoading}
		<div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
			<div class="lg:col-span-2 h-[500px] glass-panel rounded-3xl animate-pulse"></div>
			<div class="h-[500px] glass-panel rounded-3xl animate-pulse"></div>
		</div>
	{:else if course && activeLesson}
		<div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
			<!-- Main Content Player -->
			<div class="space-y-6 lg:col-span-2">
				<LessonPlayer lesson={activeLesson} />

				<!-- Lesson Discussion Section with Edra -->
				<div class="glass-card rounded-3xl border border-white/10 p-6 space-y-6 shadow-xl">
					<div class="flex items-center justify-between border-b border-white/10 pb-3">
						<div class="flex items-center gap-2 font-bold text-base text-base-content">
							<MessageSquare class="h-4 w-4 text-primary" />
							Lesson Q&A & Discussion ({threads.length})
						</div>
					</div>

					<!-- New Thread Composer with Edra -->
					<div class="glass-panel rounded-2xl border border-white/10 p-4 space-y-3">
						<input
							type="text"
							class="glass-input input input-sm h-10 w-full rounded-xl text-sm font-semibold"
							placeholder="Ask a question or discuss this lesson..."
							bind:value={newThreadTitle}
						/>

						<RichEditor
							placeholder="Describe your question in detail..."
							minHeight="140px"
							onUpdate={(json) => (newThreadContent = json)}
						/>

						<div class="flex justify-end pt-1">
							<button
								class="btn btn-primary gradient-accent btn-sm rounded-xl text-white font-semibold border-0 shadow-md gap-1.5"
								onclick={handleCreateThread}
								disabled={isPostingThread}
							>
								{#if isPostingThread}
									<span class="loading loading-spinner loading-xs"></span>
								{:else}
									<Send class="h-3.5 w-3.5" />
									Post Question
								{/if}
							</button>
						</div>
					</div>

					<!-- Existing Threads List -->
					<div class="space-y-3">
						{#each threads as thread (thread.id)}
							<div class="glass-panel rounded-2xl border border-white/5 p-4 space-y-2">
								<div class="flex items-center justify-between">
									<h4 class="text-xs font-bold text-base-content">{thread.title}</h4>
									<span class="text-[10px] text-base-content/50">
										{new Date(thread.createdAtUtc).toLocaleDateString()}
									</span>
								</div>
								<RichRenderer content={thread.content} class="text-xs text-base-content/80" />
							</div>
						{:else}
							<div class="text-center py-6 text-xs text-base-content/50">
								No discussions yet. Be the first to ask a question!
							</div>
						{/each}
					</div>
				</div>
			</div>

			<!-- Syllabus Sidebar Navigator -->
			<div class="space-y-4">
				<div class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-base-content/60 px-1">
					<BookOpen class="h-4 w-4" />
					Course Syllabus
				</div>

				<div class="sticky top-20">
					<SyllabusTree
						sections={course.sections || []}
						activeLessonId={activeLesson.id}
						isEnrolled={true}
						onSelectLesson={selectLesson}
					/>
				</div>
			</div>
		</div>
	{/if}
</div>
