<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { coursesApi } from '$lib/api/courses.ts';
	import { communicationsApi } from '$lib/api/communications.ts';
	import type { Course, Lesson, Assignment, CourseExam, DiscussionThread } from '$lib/api/types.ts';
	import LessonPlayer from '$lib/components/course/LessonPlayer.svelte';
	import SyllabusTree from '$lib/components/course/SyllabusTree.svelte';
	import RichEditor from '$lib/components/editor/RichEditor.svelte';
	import RichRenderer from '$lib/components/editor/RichRenderer.svelte';
	import GlassCard from '$lib/components/ui/GlassCard.svelte';
	import { toast } from '$lib/stores/toast.svelte.ts';
	import {
		ArrowLeft,
		MessageSquare,
		BookOpen,
		Send,
		CheckCircle2,
		GraduationCap,
		FileCheck,
		Clock,
		Award,
		ShieldAlert,
		ArrowRight,
		ExternalLink,
		Sparkles
	} from 'lucide-svelte';

	const courseId = (page.params.id || '') as string;
	let course = $state<Course | null>(null);
	let activeLesson = $state<Lesson | null>(null);
	let activeAssignment = $state<Assignment | null>(null);
	let activeExam = $state<CourseExam | null>(null);
	let activeMode = $state<'lesson' | 'assignment' | 'exam'>('lesson');

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
				activeMode = 'lesson';
				await loadThreads(activeLesson.id);
			} else if (course?.assignments?.[0]) {
				activeAssignment = course.assignments[0];
				activeMode = 'assignment';
			} else if (course?.exams?.[0]) {
				activeExam = course.exams[0];
				activeMode = 'exam';
			}
		} catch (err) {
			console.error(err);
		} finally {
			isLoading = false;
		}
	});

	async function selectLesson(lesson: Lesson) {
		activeLesson = lesson;
		activeAssignment = null;
		activeExam = null;
		activeMode = 'lesson';
		await loadThreads(lesson.id);
	}

	function selectAssignment(assignment: Assignment) {
		activeAssignment = assignment;
		activeLesson = null;
		activeExam = null;
		activeMode = 'assignment';
	}

	function selectExam(exam: CourseExam) {
		activeExam = exam;
		activeLesson = null;
		activeAssignment = null;
		activeMode = 'exam';
	}

	async function loadThreads(lessonId: string) {
		try {
			threads = await communicationsApi.getThreads(courseId, lessonId);
		} catch {
			threads = [];
		}
	}

	async function handleCreateThread() {
		if (!newThreadTitle.trim() || !newThreadContent.trim()) {
			toast.warning('Please enter both title and question details.');
			return;
		}

		isPostingThread = true;
		try {
			await communicationsApi.createThread({
				courseId,
				lessonId: activeLesson?.id || null,
				title: newThreadTitle.trim(),
				content: newThreadContent.trim()
			});
			toast.success('Discussion question posted!');
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

<div class="space-y-6 max-w-7xl mx-auto pb-16">
	<!-- Top Bar -->
	<div class="flex items-center justify-between border-b border-base-content/10 pb-4">
		<a
			href="/courses/{courseId}"
			class="btn btn-sm btn-ghost gap-2 text-base-content/70 hover:text-base-content"
		>
			<ArrowLeft class="w-4 h-4" />
			<span>Course Overview</span>
		</a>

		<div class="flex items-center gap-2">
			<span class="text-xs font-bold text-base-content">{course?.title || 'Loading...'}</span>
		</div>
	</div>

	{#if isLoading}
		<div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
			<div class="lg:col-span-2 h-[500px] bg-base-200/50 rounded-3xl animate-pulse"></div>
			<div class="h-[500px] bg-base-200/50 rounded-3xl animate-pulse"></div>
		</div>
	{:else if course}
		<div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
			<!-- Main Content Player / Viewer -->
			<div class="space-y-6 lg:col-span-2">
				{#if activeMode === 'lesson' && activeLesson}
					<LessonPlayer lesson={activeLesson} />

					<!-- Lesson Discussion Section -->
					<div class="glass-card rounded-3xl border border-base-content/10 p-6 space-y-6 shadow-xl">
						<div class="flex items-center justify-between border-b border-base-content/10 pb-3">
							<div class="flex items-center gap-2 font-bold text-base text-base-content">
								<MessageSquare class="w-4 h-4 text-primary" />
								<span>Lesson Q&A & Discussion ({threads.length})</span>
							</div>
						</div>

						<!-- New Thread Composer -->
						<div class="p-4 rounded-2xl bg-base-200/50 border border-base-content/10 space-y-3">
							<input
								type="text"
								class="input input-sm input-bordered h-10 w-full rounded-xl text-sm font-semibold bg-base-100/50"
								placeholder="Ask a question or discuss this lesson..."
								bind:value={newThreadTitle}
							/>

							<RichEditor
								bind:content={newThreadContent}
								placeholder="Describe your question or discussion in detail..."
							/>

							<div class="flex justify-end pt-1">
								<button
									type="button"
									class="btn btn-primary btn-sm rounded-xl font-semibold gap-1.5 shadow-sm"
									onclick={handleCreateThread}
									disabled={isPostingThread}
								>
									{#if isPostingThread}
										<span class="loading loading-spinner loading-xs"></span>
									{:else}
										<Send class="w-3.5 h-3.5" />
										Post Question
									{/if}
								</button>
							</div>
						</div>

						<!-- Existing Threads List -->
						<div class="space-y-3">
							{#each threads as thread (thread.id)}
								<div class="p-4 rounded-2xl border border-base-content/5 bg-base-200/30 space-y-2">
									<div class="flex items-center justify-between">
										<h4 class="text-xs font-bold text-base-content">{thread.title}</h4>
										<span class="text-[10px] text-base-content/50">
											{new Date(thread.createdAtUtc).toLocaleDateString()}
										</span>
									</div>
									<div class="text-xs text-base-content/80">
										<RichRenderer content={thread.content} />
									</div>
								</div>
							{:else}
								<div class="text-center py-6 text-xs text-base-content/50">
									No discussions for this lesson yet. Be the first to ask a question!
								</div>
							{/each}
						</div>
					</div>
				{:else if activeMode === 'exam' && activeExam}
					<!-- Course Exam Launchpad Card -->
					<div class="glass-panel rounded-3xl border border-primary/30 p-8 shadow-2xl space-y-6">
						<div class="flex items-center justify-between">
							<div class="flex items-center gap-2">
								<span class="badge badge-primary badge-sm font-bold uppercase">Course Examination</span>
								{#if activeExam.isMandatory}
									<span class="badge badge-error badge-sm text-white font-bold">Mandatory</span>
								{:else}
									<span class="badge badge-ghost badge-sm font-semibold">Optional</span>
								{/if}
							</div>
							<span class="text-xs text-base-content/60">Milestone #{activeExam.orderIndex}</span>
						</div>

						<div class="space-y-2">
							<h2 class="text-2xl sm:text-3xl font-extrabold text-base-content tracking-tight">
								{activeExam.examTitle || 'Course Assessment Examination'}
							</h2>
							<p class="text-xs text-base-content/70 max-w-xl leading-relaxed">
								This examination is linked to this course curriculum. Passing this evaluation is required for your official verified course completion certificate.
							</p>
						</div>

						<div class="grid grid-cols-1 sm:grid-cols-3 gap-4 pt-2">
							<div class="p-4 rounded-2xl bg-base-200/50 border border-base-content/10 space-y-1">
								<div class="text-[10px] font-bold uppercase tracking-wider text-base-content/60">Type</div>
								<div class="text-sm font-bold text-base-content flex items-center gap-1.5">
									<GraduationCap class="w-4 h-4 text-primary" />
									Standard Evaluation
								</div>
							</div>

							<div class="p-4 rounded-2xl bg-base-200/50 border border-base-content/10 space-y-1">
								<div class="text-[10px] font-bold uppercase tracking-wider text-base-content/60">Certification</div>
								<div class="text-sm font-bold text-success flex items-center gap-1.5">
									<Award class="w-4 h-4" />
									SHA-256 Hash
								</div>
							</div>

							<div class="p-4 rounded-2xl bg-base-200/50 border border-base-content/10 space-y-1">
								<div class="text-[10px] font-bold uppercase tracking-wider text-base-content/60">Anti-Cheat</div>
								<div class="text-sm font-bold text-warning flex items-center gap-1.5">
									<ShieldAlert class="w-4 h-4" />
									Active Monitoring
								</div>
							</div>
						</div>

						<div class="pt-4 border-t border-base-content/10 flex items-center justify-between">
							<span class="text-xs text-base-content/60">
								Ensure your webcam & mic are functional before proceeding.
							</span>

							<a
								href="/exams/{activeExam.examId}/start"
								class="btn btn-primary gradient-accent rounded-2xl font-bold text-white shadow-lg border-0 gap-2 h-11 px-6 text-sm"
							>
								Launch Examination
								<ArrowRight class="w-4 h-4" />
							</a>
						</div>
					</div>
				{:else if activeMode === 'assignment' && activeAssignment}
					<!-- Course Assignment Overview Card -->
					<div class="glass-panel rounded-3xl border border-warning/30 p-8 shadow-2xl space-y-6">
						<div class="flex items-center justify-between">
							<span class="badge badge-warning badge-sm font-bold">Course Assignment</span>
							<span class="text-xs text-base-content/60 flex items-center gap-1">
								<Clock class="w-3.5 h-3.5" />
								Deadline: {new Date(activeAssignment.deadlineUtc).toLocaleString()}
							</span>
						</div>

						<div class="space-y-2">
							<h2 class="text-2xl sm:text-3xl font-extrabold text-base-content tracking-tight">
								{activeAssignment.title}
							</h2>
							<div class="text-xs font-semibold text-warning">
								Maximum Points: {activeAssignment.maxScore} pts
							</div>
						</div>

						<div class="border-t border-base-content/10 pt-4 space-y-2">
							<h3 class="text-xs font-bold uppercase tracking-wider text-base-content/60">Instructions</h3>
							<div class="text-sm text-base-content/85 leading-relaxed bg-base-200/40 p-4 rounded-2xl border border-base-content/10">
								<RichRenderer content={activeAssignment.instruction} />
							</div>
						</div>

						<div class="pt-2 flex justify-end">
							<a
								href="/courses/{courseId}/assignments/{activeAssignment.id}"
								class="btn btn-primary gradient-accent rounded-2xl font-bold text-white shadow-lg border-0 gap-2 h-11 px-6 text-sm"
							>
								<FileCheck class="w-4 h-4" />
								Open Submission Workspace
								<ArrowRight class="w-4 h-4" />
							</a>
						</div>
					</div>
				{/if}
			</div>

			<!-- Syllabus Sidebar Navigator -->
			<div class="space-y-4">
				<div class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-base-content/60 px-1">
					<BookOpen class="w-4 h-4 text-primary" />
					<span>Course Curriculum</span>
				</div>

				<div class="sticky top-20">
					<SyllabusTree
						sections={course.sections || []}
						assignments={course.assignments || []}
						exams={course.exams || []}
						activeLessonId={activeLesson?.id}
						activeAssignmentId={activeAssignment?.id}
						activeExamId={activeExam?.examId}
						isEnrolled={true}
						courseId={courseId}
						onSelectLesson={selectLesson}
						onSelectAssignment={selectAssignment}
						onSelectExam={selectExam}
					/>
				</div>
			</div>
		</div>
	{/if}
</div>
