<script lang="ts">
	import type { CourseSection, Lesson, Assignment, CourseExam } from '$lib/api/types.ts';
	import {
		PlayCircle,
		FileText,
		Download,
		ChevronRight,
		Clock,
		AlignLeft,
		FileCheck,
		GraduationCap,
		ShieldAlert,
		Sparkles,
		Award,
		Lock,
		CheckCircle2
	} from 'lucide-svelte';

	interface Props {
		sections?: CourseSection[];
		assignments?: Assignment[];
		exams?: CourseExam[];
		activeLessonId?: string;
		activeAssignmentId?: string;
		activeExamId?: string;
		isEnrolled?: boolean;
		courseId?: string;
		completedLessonIds?: string[];
		completedAssignmentIds?: string[];
		completedExamIds?: string[];
		onSelectLesson?: (lesson: Lesson) => void;
		onSelectAssignment?: (assignment: Assignment) => void;
		onSelectExam?: (exam: CourseExam) => void;
	}

	let {
		sections = [],
		assignments = [],
		exams = [],
		activeLessonId,
		activeAssignmentId,
		activeExamId,
		isEnrolled = false,
		courseId,
		completedLessonIds = [],
		completedAssignmentIds = [],
		completedExamIds = [],
		onSelectLesson,
		onSelectAssignment,
		onSelectExam
	}: Props = $props();

	const lessonIcons: Record<string, any> = {
		Text: AlignLeft,
		Video: PlayCircle,
		PdfDocument: FileText,
		DownloadableFile: Download
	};
</script>

<div class="space-y-4">
	<!-- Sections & Lessons -->
	{#if sections.length > 0}
		<div class="space-y-3">
			{#each sections as section, sIdx (section.id || sIdx)}
				<div class="bg-base-200/50 overflow-hidden rounded-2xl border border-base-content/10">
					<!-- Section Header -->
					<div class="bg-base-100/60 px-4 py-3 flex items-center justify-between border-b border-base-content/5">
						<div class="flex items-center gap-2.5">
							<span class="w-6 h-6 rounded-lg bg-primary/10 text-primary font-mono font-bold text-xs flex items-center justify-center">
								{sIdx + 1}
							</span>
							<span class="text-xs font-bold text-base-content">{section.title}</span>
						</div>
						<span class="text-[10px] text-base-content/50 font-semibold">
							{section.lessons?.length || 0} lessons
						</span>
					</div>

					<!-- Lessons List -->
					<div class="divide-y divide-base-content/5">
						{#each section.lessons || [] as lesson (lesson.id)}
							{@const Icon = lessonIcons[lesson.type] || PlayCircle}
							{@const isCurrent = lesson.id === activeLessonId}
							{@const isCompleted = completedLessonIds.includes(lesson.id)}

							<div
								class="flex items-center justify-between px-4 py-3 text-xs transition-colors {isCurrent
									? 'bg-primary/15 text-primary font-bold'
									: 'hover:bg-base-100/40 text-base-content/80'} {isEnrolled ? 'cursor-pointer' : ''}"
								onclick={() => isEnrolled && onSelectLesson && onSelectLesson(lesson)}
								role="button"
								tabindex="0"
								onkeydown={(e) => isEnrolled && e.key === 'Enter' && onSelectLesson && onSelectLesson(lesson)}
							>
								<div class="flex items-center gap-2.5 overflow-hidden min-w-0">
									{#if isCompleted}
										<CheckCircle2 class="w-4 h-4 shrink-0 text-success" />
									{:else}
										<Icon class="w-4 h-4 shrink-0 {isCurrent ? 'text-primary' : 'text-base-content/50'}" />
									{/if}
									<span class="truncate {isCompleted ? 'text-base-content/90 font-medium' : ''}">{lesson.title}</span>
								</div>

								<div class="flex items-center gap-2 text-[10px] text-base-content/50 shrink-0 ml-2">
									{#if lesson.durationMinutes > 0}
										<span class="flex items-center gap-1">
											<Clock class="w-3 h-3" />
											{lesson.durationMinutes}m
										</span>
									{/if}
									{#if isEnrolled}
										{#if isCompleted}
											<span class="badge badge-success badge-xs text-white font-semibold">Done</span>
										{:else}
											<ChevronRight class="w-3.5 h-3.5 {isCurrent ? 'text-primary' : 'text-base-content/40'}" />
										{/if}
									{:else}
										<Lock class="w-3.5 h-3.5 text-base-content/30" />
									{/if}
								</div>
							</div>
						{/each}
					</div>
				</div>
			{/each}
		</div>
	{/if}

	<!-- Course Assignments (if any) -->
	{#if assignments.length > 0}
		<div class="bg-base-200/50 overflow-hidden rounded-2xl border border-base-content/10">
			<div class="bg-base-100/60 px-4 py-3 flex items-center justify-between border-b border-base-content/5">
				<div class="flex items-center gap-2 text-xs font-bold text-base-content">
					<FileCheck class="w-4 h-4 text-warning" />
					<span>Assignments & Projects ({assignments.length})</span>
				</div>
			</div>

			<div class="divide-y divide-base-content/5">
				{#each assignments as assignment (assignment.id)}
					{@const isCurrent = assignment.id === activeAssignmentId}
					{@const isCompleted = completedAssignmentIds.includes(assignment.id)}
					<div
						class="flex items-center justify-between px-4 py-3 text-xs transition-colors {isCurrent
							? 'bg-warning/15 text-warning font-bold'
							: 'hover:bg-base-100/40 text-base-content/80'} {isEnrolled ? 'cursor-pointer' : ''}"
						onclick={() => {
							if (isEnrolled) {
								if (onSelectAssignment) {
									onSelectAssignment(assignment);
								} else if (courseId) {
									window.location.href = `/courses/${courseId}/assignments/${assignment.id}`;
								}
							}
						}}
						role="button"
						tabindex="0"
						onkeydown={(e) => {
							if (isEnrolled && e.key === 'Enter') {
								if (onSelectAssignment) {
									onSelectAssignment(assignment);
								} else if (courseId) {
									window.location.href = `/courses/${courseId}/assignments/${assignment.id}`;
								}
							}
						}}
					>
						<div class="flex items-center gap-2.5 overflow-hidden min-w-0">
							{#if isCompleted}
								<CheckCircle2 class="w-4 h-4 shrink-0 text-success" />
							{:else}
								<FileCheck class="w-4 h-4 shrink-0 {isCurrent ? 'text-warning' : 'text-base-content/50'}" />
							{/if}
							<span class="truncate {isCompleted ? 'text-base-content/90 font-medium' : ''}">{assignment.title}</span>
						</div>

						<div class="flex items-center gap-2 text-[10px] text-base-content/50 shrink-0 ml-2">
							<span class="badge badge-xs badge-ghost font-semibold">{assignment.maxScore} pts</span>
							{#if isEnrolled}
								{#if isCompleted}
									<span class="badge badge-success badge-xs text-white font-semibold">Submitted</span>
								{:else}
									<ChevronRight class="w-3.5 h-3.5 {isCurrent ? 'text-warning' : 'text-base-content/40'}" />
								{/if}
							{:else}
								<Lock class="w-3.5 h-3.5 text-base-content/30" />
							{/if}
						</div>
					</div>
				{/each}
			</div>
		</div>
	{/if}

	<!-- Course Attached Examinations (CourseExam) -->
	{#if exams.length > 0}
		<div class="bg-base-200/50 overflow-hidden rounded-2xl border border-primary/20 shadow-md">
			<div class="bg-primary/10 px-4 py-3 flex items-center justify-between border-b border-primary/15">
				<div class="flex items-center gap-2 text-xs font-bold text-primary">
					<GraduationCap class="w-4 h-4" />
					<span>Course Examinations ({exams.length})</span>
				</div>
				<span class="badge badge-primary badge-xs uppercase font-bold">Certification</span>
			</div>

			<div class="divide-y divide-base-content/5">
				{#each exams as exam (exam.id || exam.examId)}
					{@const isCurrent = exam.examId === activeExamId || exam.id === activeExamId}
					{@const isCompleted = completedExamIds.includes(exam.examId) || completedExamIds.includes(exam.id)}
					<div
						class="flex items-center justify-between px-4 py-3 text-xs transition-colors {isCurrent
							? 'bg-primary/15 text-primary font-bold'
							: 'hover:bg-base-100/40 text-base-content/80'} {onSelectExam ? 'cursor-pointer' : ''}"
						onclick={() => {
							if (onSelectExam) {
								onSelectExam(exam);
							}
						}}
						role={onSelectExam ? 'button' : undefined}
						tabindex={onSelectExam ? 0 : undefined}
						onkeydown={(e) => {
							if (onSelectExam && e.key === 'Enter') {
								onSelectExam(exam);
							}
						}}
					>
						<div class="flex items-center gap-2.5 overflow-hidden min-w-0">
							{#if isCompleted}
								<CheckCircle2 class="w-4 h-4 shrink-0 text-success" />
							{:else}
								<GraduationCap class="w-4 h-4 shrink-0 {isCurrent ? 'text-primary' : 'text-primary/70'}" />
							{/if}
							<div class="truncate">
								<span class="{isCompleted ? 'text-base-content/90 font-medium' : ''} font-medium">{exam.examTitle || 'Course Final Examination'}</span>
							</div>
						</div>

						<div class="flex items-center gap-2 text-[10px] shrink-0 ml-2">
							{#if isCompleted}
								<span class="badge badge-success badge-xs text-white font-semibold">Completed</span>
							{:else if exam.isMandatory}
								<span class="badge badge-error badge-xs text-white font-bold">Mandatory</span>
							{:else}
								<span class="badge badge-ghost badge-xs font-semibold">Optional</span>
							{/if}
							{#if onSelectExam}
								{#if isCompleted}
									<!-- Already completed -->
								{:else}
									<ChevronRight class="w-3.5 h-3.5 {isCurrent ? 'text-primary' : 'text-base-content/40'}" />
								{/if}
							{:else if !isEnrolled}
								<Lock class="w-3.5 h-3.5 text-base-content/30" />
							{/if}
						</div>
					</div>
				{/each}
			</div>
		</div>
	{/if}

	{#if sections.length === 0 && assignments.length === 0 && exams.length === 0}
		<div class="text-center py-8 text-xs text-base-content/50 bg-base-200/30 rounded-2xl border border-dashed border-base-300">
			No curriculum materials added to this course yet.
		</div>
	{/if}
</div>
