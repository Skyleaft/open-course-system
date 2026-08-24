<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { coursesApi } from '$lib/api/courses.ts';
	import { communicationsApi } from '$lib/api/communications.ts';
	import type { Course, Lesson, Assignment, CourseExam, DiscussionThread, CourseProgressDto } from '$lib/api/types.ts';
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
		Sparkles,
		Check,
		RotateCcw
	} from 'lucide-svelte';

	const courseId = (page.params.id || '') as string;
	const targetLessonId = page.url.searchParams.get('lessonId');

	let course = $state<Course | null>(null);
	let courseProgress = $state<CourseProgressDto | null>(null);

	let activeLesson = $state<Lesson | null>(null);
	let activeAssignment = $state<Assignment | null>(null);
	let activeExam = $state<CourseExam | null>(null);
	let activeMode = $state<'lesson' | 'assignment' | 'exam'>('lesson');

	let threads = $state<DiscussionThread[]>([]);
	let isLoading = $state(true);
	let isCompletingLesson = $state(false);

	let newThreadTitle = $state('');
	let newThreadContent = $state('');
	let isPostingThread = $state(false);

	const isLessonCompleted = $derived(
		activeLesson ? (courseProgress?.completedLessonIds || []).includes(activeLesson.id) : false
	);

	onMount(async () => {
		try {
			const [courseRes, progressRes] = await Promise.all([
				coursesApi.getCourseById(courseId),
				coursesApi.getCourseProgress(courseId).catch(() => null)
			]);

			course = courseRes;
			courseProgress = progressRes;

			const allLessons = (course?.sections || []).flatMap((s) => s.lessons || []);

			// If specific lessonId requested via query string
			if (targetLessonId) {
				const found = allLessons.find((l) => l.id === targetLessonId);
				if (found) {
					await selectLesson(found);
					return;
				}
			}

			// If last accessed lesson exists
			if (progressRes?.lastAccessedLessonId) {
				const found = allLessons.find((l) => l.id === progressRes.lastAccessedLessonId);
				if (found) {
					await selectLesson(found);
					return;
				}
			}

			// Default selection fallback
			if (allLessons.length > 0) {
				await selectLesson(allLessons[0]);
			} else if (course?.assignments?.[0]) {
				selectAssignment(course.assignments[0]);
			} else if (course?.exams?.[0]) {
				selectExam(course.exams[0]);
			}
		} catch (err) {
			console.error('Failed to initialize course player:', err);
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

	async function handleToggleLessonComplete() {
		if (!activeLesson) return;
		isCompletingLesson = true;
		const targetStatus = !isLessonCompleted;

		try {
			const res = await coursesApi.completeLesson(courseId, activeLesson.id, targetStatus);
			if (res) {
				if (courseProgress) {
					const set = new Set(courseProgress.completedLessonIds);
					if (res.isCompleted) {
						set.add(activeLesson.id);
						toast.success('Lesson marked as completed!');
					} else {
						set.delete(activeLesson.id);
						toast.info('Lesson marked as incomplete.');
					}
					courseProgress.completedLessonIds = Array.from(set);
					courseProgress.progressPercent = res.updatedCourseProgressPercent;
				} else {
					courseProgress = {
						courseId,
						completedLessonIds: res.isCompleted ? [activeLesson.id] : [],
						completedAssignmentIds: [],
						completedExamIds: [],
						progressPercent: res.updatedCourseProgressPercent,
						lastAccessedLessonId: activeLesson.id
					};
					toast.success(res.isCompleted ? 'Lesson marked as completed!' : 'Lesson marked as incomplete.');
				}
			}
		} catch (err: any) {
			toast.error(err?.message || 'Failed to update lesson completion status.');
		} finally {
			isCompletingLesson = false;
		}
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
	<!-- Top Navigation & Progress Bar -->
	<div class="glass-panel rounded-3xl p-4 sm:p-5 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 border border-white/10 shadow-xl">
		<div class="flex items-center gap-3">
			<a
				href="/courses/{courseId}"
				class="btn btn-sm btn-ghost rounded-2xl gap-1.5 text-base-content/70 hover:text-base-content"
			>
				<ArrowLeft class="w-4 h-4" />
				<span>Overview</span>
			</a>

			<div class="h-4 w-px bg-white/10 hidden sm:block"></div>

			<div class="space-y-0.5">
				<h1 class="text-sm sm:text-base font-bold text-base-content truncate max-w-md">
					{course?.title || 'Loading...'}
				</h1>
				<div class="text-[11px] text-base-content/60 flex items-center gap-2">
					<span>Course Progression:</span>
					<span class="font-bold text-primary">{courseProgress?.progressPercent || 0}%</span>
				</div>
			</div>
		</div>

		<!-- Progress Bar & Certificate CTA -->
		<div class="flex items-center gap-3 w-full sm:w-72">
			<div class="h-2.5 flex-1 overflow-hidden rounded-full bg-base-200/80">
				<div
					class="h-full transition-all duration-500 {(courseProgress?.progressPercent || 0) >= 100 ? 'bg-success' : 'gradient-accent'}"
					style="width: {courseProgress?.progressPercent || 0}%"
				></div>
			</div>

			{#if (courseProgress?.progressPercent || 0) >= 100}
				<a href="/certificates" class="btn btn-xs btn-success text-white font-bold rounded-xl shadow-md gap-1 shrink-0">
					<Award class="w-3.5 h-3.5" /> Certificate
				</a>
			{/if}
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

					<!-- Mark Completed Action Bar -->
					<div class="glass-card rounded-2xl p-4 border border-white/10 flex items-center justify-between shadow-md">
						<div class="flex items-center gap-2 text-xs">
							{#if isLessonCompleted}
								<span class="flex items-center gap-1.5 font-bold text-success">
									<CheckCircle2 class="w-4 h-4" /> Lesson Completed
								</span>
							{:else}
								<span class="text-base-content/60">
									Finished reviewing this lesson?
								</span>
							{/if}
						</div>

						<button
							type="button"
							class="btn btn-sm rounded-xl font-bold gap-1.5 transition-all {isLessonCompleted
								? 'btn-outline border-success text-success hover:bg-success hover:text-white'
								: 'btn-primary gradient-accent text-white border-0 shadow-md'}"
							onclick={handleToggleLessonComplete}
							disabled={isCompletingLesson}
						>
							{#if isCompletingLesson}
								<span class="loading loading-spinner loading-xs"></span>
							{:else if isLessonCompleted}
								<Check class="w-4 h-4" /> Completed
							{:else}
								<CheckCircle2 class="w-4 h-4" /> Mark as Complete
							{/if}
						</button>
					</div>

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
					{@const isExamCompleted = (courseProgress?.completedExamIds || []).includes(activeExam.examId) || (courseProgress?.completedExamIds || []).includes(activeExam.id)}
					<!-- Course Exam Launchpad Card -->
					<div class="glass-panel rounded-3xl border {isExamCompleted ? 'border-success/40 bg-success/5' : 'border-primary/30'} p-8 shadow-2xl space-y-6">
						<div class="flex items-center justify-between">
							<div class="flex items-center gap-2">
								<span class="badge {isExamCompleted ? 'badge-success text-white' : 'badge-primary'} badge-sm font-bold uppercase">
									{isExamCompleted ? 'Exam Completed' : 'Course Examination'}
								</span>
								{#if isExamCompleted}
									<span class="badge badge-success badge-sm text-white font-bold gap-1">
										<CheckCircle2 class="w-3.5 h-3.5" /> Passed
									</span>
								{:else if activeExam.isMandatory}
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
								{#if isExamCompleted}
									You have successfully completed this examination milestone. Your score and verification hash have been recorded into your course progression.
								{:else}
									This examination is linked to this course curriculum. Passing this evaluation is required for your official verified course completion certificate.
								{/if}
							</p>
						</div>

						<div class="grid grid-cols-1 sm:grid-cols-3 gap-4 pt-2">
							<div class="p-4 rounded-2xl bg-base-200/50 border border-base-content/10 space-y-1">
								<div class="text-[10px] font-bold uppercase tracking-wider text-base-content/60">Status</div>
								<div class="text-sm font-bold {isExamCompleted ? 'text-success' : 'text-base-content'} flex items-center gap-1.5">
									{#if isExamCompleted}
										<CheckCircle2 class="w-4 h-4 text-success" />
										Passed
									{:else}
										<GraduationCap class="w-4 h-4 text-primary" />
										Ready to Attempt
									{/if}
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
								{#if isExamCompleted}
									You may review your past submissions or retake if attempts remain.
								{:else}
									Ensure your webcam & mic are functional before proceeding.
								{/if}
							</span>

							<a
								href="/exams/{activeExam.examId}/start"
								class="btn btn-primary {isExamCompleted ? 'btn-outline border-success text-success hover:bg-success hover:text-white' : 'gradient-accent text-white border-0 shadow-lg'} rounded-2xl font-bold gap-2 h-11 px-6 text-sm"
							>
								{isExamCompleted ? 'Retake Examination' : 'Launch Examination'}
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
						completedLessonIds={courseProgress?.completedLessonIds || []}
						completedAssignmentIds={courseProgress?.completedAssignmentIds || []}
						completedExamIds={courseProgress?.completedExamIds || []}
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
