<script lang="ts">
	import { fade, scale } from 'svelte/transition';
	import { cubicOut } from 'svelte/easing';
	import {
		X,
		Award,
		CheckCircle2,
		ShieldAlert,
		AlertTriangle,
		Sparkles,
		GraduationCap,
		FileText,
		RotateCcw,
		BookOpen,
		Calendar,
		Clock,
		Check,
		User
	} from 'lucide-svelte';
	import type { CourseStudentEnrollmentDto, CourseStudentExamProgressDto } from '$lib/api/types.ts';

	interface Props {
		isOpen: boolean;
		student: CourseStudentEnrollmentDto | null;
		onClose: () => void;
		onOpenGrading: (student: CourseStudentEnrollmentDto, exam: CourseStudentExamProgressDto) => void;
		onOpenRetake: (student: CourseStudentEnrollmentDto, exam: CourseStudentExamProgressDto) => void;
	}

	let {
		isOpen,
		student,
		onClose,
		onOpenGrading,
		onOpenRetake
	}: Props = $props();

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape' && isOpen) {
			onClose();
		}
	}
</script>

<svelte:window onkeydown={handleKeydown} />

{#if isOpen && student}
	<div
		class="fixed inset-0 z-[500] flex items-center justify-center p-3 sm:p-6 overflow-y-auto bg-black/70 backdrop-blur-md"
		role="dialog"
		aria-modal="true"
		transition:fade={{ duration: 160 }}
	>
		<!-- Backdrop click -->
		<div
			class="fixed inset-0 -z-10"
			onclick={onClose}
			role="presentation"
		></div>

		<div
			class="relative w-full max-w-2xl overflow-hidden rounded-3xl bg-base-100/95 backdrop-blur-2xl border border-base-content/10 shadow-2xl p-6 sm:p-8 space-y-6 my-auto max-h-[90vh] overflow-y-auto flex flex-col"
			transition:scale={{ duration: 200, start: 0.95, easing: cubicOut }}
		>
			<!-- Header -->
			<div class="flex items-center justify-between border-b border-base-content/10 pb-4">
				<div class="flex items-center gap-3.5">
					<div class="avatar placeholder">
						<div class="w-12 h-12 rounded-2xl bg-primary/15 text-primary font-extrabold text-base flex items-center justify-center border border-primary/20 shadow-xs">
							{student.fullName ? student.fullName.substring(0, 2).toUpperCase() : 'ST'}
						</div>
					</div>
					<div>
						<h3 class="font-extrabold text-lg text-base-content tracking-tight">{student.fullName}</h3>
						<p class="text-xs text-base-content/60 flex items-center gap-1.5 mt-0.5">
							<span>{student.email}</span>
						</p>
					</div>
				</div>

				<button
					type="button"
					class="btn btn-ghost btn-circle btn-sm text-base-content/50 hover:text-base-content"
					onclick={onClose}
					aria-label="Close modal"
				>
					<X class="w-4 h-4" />
				</button>
			</div>

			<!-- Progress Snapshot Summary Cards -->
			<div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
				<div class="bg-base-200/50 p-3.5 rounded-2xl border border-base-content/5 space-y-1">
					<span class="text-[10px] uppercase font-bold text-base-content/50 tracking-wider">Overall Progress</span>
					<div class="text-lg font-black text-primary">{student.progressPercent}%</div>
					<div class="h-1 w-full bg-base-300 rounded-full overflow-hidden">
						<div class="h-full gradient-accent" style="width: {student.progressPercent}%"></div>
					</div>
				</div>

				<div class="bg-base-200/50 p-3.5 rounded-2xl border border-base-content/5 space-y-1">
					<span class="text-[10px] uppercase font-bold text-base-content/50 tracking-wider">Lessons Done</span>
					<div class="text-lg font-black text-base-content font-mono">{student.completedLessonsCount} / {student.totalLessonsCount}</div>
					<p class="text-[10px] text-base-content/50">Completed modules</p>
				</div>

				<div class="bg-base-200/50 p-3.5 rounded-2xl border border-base-content/5 space-y-1">
					<span class="text-[10px] uppercase font-bold text-base-content/50 tracking-wider">Assignments</span>
					<div class="text-lg font-black text-base-content font-mono">{student.completedAssignmentsCount} / {student.totalAssignmentsCount}</div>
					<p class="text-[10px] text-base-content/50">Task submissions</p>
				</div>

				<div class="bg-base-200/50 p-3.5 rounded-2xl border border-base-content/5 space-y-1">
					<span class="text-[10px] uppercase font-bold text-base-content/50 tracking-wider">Enrolled Date</span>
					<div class="text-xs font-bold text-base-content truncate pt-1">{new Date(student.enrolledAtUtc).toLocaleDateString()}</div>
					<p class="text-[10px] text-base-content/50 font-mono">{new Date(student.enrolledAtUtc).toLocaleTimeString()}</p>
				</div>
			</div>

			<!-- Course Examinations Progression & Retake Controls -->
			<div class="space-y-3">
				<h4 class="text-xs font-black uppercase tracking-wider text-base-content/80 flex items-center gap-2">
					<Award class="w-4 h-4 text-primary" />
					<span>Course Examinations & Attempt Supervision</span>
				</h4>

				{#if !student.exams || student.exams.length === 0}
					<div class="p-6 rounded-2xl bg-base-200/40 text-center text-xs text-base-content/60 border border-dashed border-base-content/20 space-y-1">
						<GraduationCap class="w-8 h-8 text-base-content/30 mx-auto mb-1" />
						<p class="font-bold text-base-content/80">No examinations attached</p>
						<p class="text-[11px] text-base-content/50">This course curriculum does not currently have any attached examination modules.</p>
					</div>
				{:else}
					<div class="space-y-3">
						{#each student.exams as exam (exam.examId)}
							{@const isCompleted = exam.status === 'Completed'}
							{@const isDisqualified = exam.status === 'Disqualified'}
							{@const isTimedOut = exam.status === 'TimedOut'}
							{@const isInProgress = exam.status === 'InProgress'}
							{@const isPassed = exam.isPassed ?? ((exam.score ?? 0) >= 70)}

							<div class="p-4 rounded-2xl bg-base-200/50 hover:bg-base-200/70 border border-base-content/10 transition-all duration-200 space-y-3">
								<div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
									<!-- Left: Exam identity & Status Badges -->
									<div class="flex items-start gap-3 min-w-0">
										<div class="w-10 h-10 rounded-xl flex items-center justify-center shrink-0 {isCompleted && isPassed
											? 'bg-success/15 text-success border border-success/30'
											: isCompleted && !isPassed
												? 'bg-error/15 text-error border border-error/30'
												: isDisqualified
													? 'bg-error/15 text-error border border-error/30'
													: isTimedOut
														? 'bg-warning/15 text-warning border border-warning/30'
														: isInProgress
															? 'bg-info/15 text-info border border-info/30'
															: 'bg-base-300 text-base-content/50'}">
											{#if isCompleted && isPassed}
												<CheckCircle2 class="w-5 h-5" />
											{:else if isDisqualified}
												<ShieldAlert class="w-5 h-5" />
											{:else if isTimedOut}
												<AlertTriangle class="w-5 h-5" />
											{:else if isInProgress}
												<Sparkles class="w-5 h-5" />
											{:else}
												<GraduationCap class="w-5 h-5" />
											{/if}
										</div>

										<div class="space-y-1 min-w-0">
											<div class="flex items-center gap-2 flex-wrap">
												<h5 class="font-bold text-sm text-base-content tracking-tight">{exam.examTitle}</h5>
												
												{#if isCompleted}
													<span class="badge badge-xs font-bold text-white {isPassed ? 'badge-success' : 'badge-error'}">
														{isPassed ? 'Passed' : 'Failed'}
													</span>
													{#if exam.score !== null && exam.score !== undefined}
														<span class="badge badge-xs font-mono font-bold bg-base-100 border border-base-content/15 text-base-content">
															Score: {exam.score}%
														</span>
													{/if}
												{:else if isDisqualified}
													<span class="badge badge-error text-white badge-xs font-bold">Disqualified (Cheating Flag)</span>
												{:else if isTimedOut}
													<span class="badge badge-warning badge-xs font-bold">Timed Out</span>
												{:else if isInProgress}
													<span class="badge badge-info text-white badge-xs font-semibold animate-pulse">In Progress</span>
												{:else}
													<span class="badge badge-ghost badge-xs text-base-content/50">Not Started</span>
												{/if}
											</div>

											<div class="flex items-center gap-2 text-[11px] text-base-content/60 flex-wrap">
												{#if exam.finishedAtUtc}
													<span>Submitted: <strong>{new Date(exam.finishedAtUtc).toLocaleString()}</strong></span>
												{:else if exam.startedAtUtc}
													<span>Started: <strong>{new Date(exam.startedAtUtc).toLocaleString()}</strong></span>
												{:else}
													<span class="italic text-base-content/40">No attempt recorded yet</span>
												{/if}
											</div>
										</div>
									</div>

									<!-- Right: Interactive Action Buttons -->
									<div class="flex items-center gap-2 shrink-0 self-end sm:self-center">
										{#if exam.submissionId || isCompleted || isTimedOut}
											<button
												type="button"
												class="btn btn-primary btn-outline btn-xs sm:btn-sm rounded-xl font-bold gap-1.5 shadow-xs"
												onclick={() => onOpenGrading(student, exam)}
												title="Review student test paper and grade essay responses"
											>
												<FileText class="w-3.5 h-3.5" />
												<span>Review & Grade</span>
											</button>
										{/if}

										{#if exam.status !== 'NotStarted'}
											<button
												type="button"
												class="btn btn-ghost hover:bg-base-300 btn-xs sm:btn-sm rounded-xl text-base-content/80 font-semibold gap-1.5"
												onclick={() => onOpenRetake(student, exam)}
												title="Allow candidate to retake exam"
											>
												<RotateCcw class="w-3.5 h-3.5" />
												<span>Grant Retake</span>
											</button>
										{/if}
									</div>
								</div>
							</div>
						{/each}
					</div>
				{/if}
			</div>

			<!-- Footer -->
			<div class="flex justify-end pt-4 border-t border-base-content/10 mt-auto">
				<button
					type="button"
					class="btn btn-sm btn-ghost rounded-xl font-semibold"
					onclick={onClose}
				>
					Close
				</button>
			</div>
		</div>
	</div>
{/if}
