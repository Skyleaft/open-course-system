<script lang="ts">
	import type { CourseExam, QuizExam } from '$lib/api/types.ts';
	import {
		FileText,
		Plus,
		Trash2,
		ShieldCheck,
		Clock,
		Check,
		AlertCircle,
		CheckCircle2,
		ExternalLink,
		X
	} from 'lucide-svelte';

	interface Props {
		courseExams: CourseExam[];
		allExams: QuizExam[];
		onAttachExam: (examId: string, isMandatory: boolean) => void;
		onDetachExam: (examId: string) => void;
		isLoading?: boolean;
	}

	let {
		courseExams = [],
		allExams = [],
		onAttachExam,
		onDetachExam,
		isLoading = false
	}: Props = $props();

	let isAttachModalOpen = $state(false);
	let selectedExamId = $state('');
	let isMandatory = $state(true);

	// Filter out already attached exams
	const availableExamsToAttach = $derived(
		allExams.filter((e) => !courseExams.some((ce) => ce.examId === e.id))
	);

	function handleAttach() {
		if (!selectedExamId) return;
		onAttachExam(selectedExamId, isMandatory);
		selectedExamId = '';
		isMandatory = true;
		isAttachModalOpen = false;
	}
</script>

<div class="space-y-4">
	<div class="flex items-center justify-between">
		<div>
			<h3 class="text-base font-bold text-base-content flex items-center gap-2">
				<FileText class="w-5 h-5 text-primary" />
				Course Examinations & Quizzes
			</h3>
			<p class="text-xs text-base-content/70">
				Attach reusable exams to this course curriculum. Students enrolled in this course will have access to take them.
			</p>
		</div>

		<button
			type="button"
			class="btn btn-sm btn-primary gap-1.5 shadow-sm"
			onclick={() => {
				selectedExamId = availableExamsToAttach.length > 0 ? availableExamsToAttach[0].id : '';
				isAttachModalOpen = true;
			}}
		>
			<Plus class="w-4 h-4" />
			Attach Exam
		</button>
	</div>

	<!-- Attached Exams List -->
	{#if courseExams.length === 0}
		<div class="py-12 text-center bg-base-200/40 rounded-2xl border border-dashed border-base-300">
			<FileText class="w-10 h-10 text-base-content/30 mx-auto mb-2.5" />
			<p class="text-sm font-semibold text-base-content/80">No examinations attached to this course</p>
			<p class="text-xs text-base-content/50 max-w-sm mx-auto mt-1">
				Exams are created independently and can be linked to one or multiple courses.
			</p>
			<button
				type="button"
				class="btn btn-sm btn-primary gap-1.5 mt-4"
				onclick={() => {
					selectedExamId = availableExamsToAttach.length > 0 ? availableExamsToAttach[0].id : '';
					isAttachModalOpen = true;
				}}
			>
				<Plus class="w-4 h-4" />
				Attach First Exam
			</button>
		</div>
	{:else}
		<div class="space-y-2.5">
			{#each courseExams as item, idx (item.id || item.examId)}
				{@const examMeta = allExams.find((e) => e.id === item.examId)}
				<div class="p-4 rounded-2xl bg-base-200/50 border border-base-content/10 flex items-center justify-between gap-3 flex-wrap">
					<div class="flex items-center gap-3 min-w-0">
						<span class="w-8 h-8 rounded-xl bg-primary/10 text-primary font-mono font-bold text-xs flex items-center justify-center flex-shrink-0">
							{idx + 1}
						</span>

						<div class="min-w-0">
							<div class="flex items-center gap-2 flex-wrap">
								<span class="font-bold text-sm text-base-content">
									{examMeta?.title || item.examTitle || 'Quiz Exam'}
								</span>
								{#if item.isMandatory}
									<span class="badge badge-sm badge-error text-white text-[10px] font-bold">
										Mandatory
									</span>
								{:else}
									<span class="badge badge-sm badge-ghost text-[10px]">
										Optional
									</span>
								{/if}
								{#if examMeta?.mode}
									<span class="badge badge-sm badge-outline text-[10px]">
										{examMeta.mode}
									</span>
								{/if}
							</div>

							<div class="flex items-center gap-3 text-xs text-base-content/60 mt-1 flex-wrap">
								<span class="flex items-center gap-1">
									<Clock class="w-3 h-3 text-base-content/40" />
									{examMeta?.durationMinutes || 60} mins
								</span>
								<span>•</span>
								<span>Passing Score: {examMeta?.passingScore || 70}%</span>
							</div>
						</div>
					</div>

					<div class="flex items-center gap-2 flex-shrink-0 ml-auto">
						<a
							href={`/instructor/exams/${item.examId}/edit`}
							target="_blank"
							class="btn btn-xs btn-ghost gap-1 text-xs"
						>
							<ExternalLink class="w-3.5 h-3.5" />
							Edit Exam
						</a>

						<button
							type="button"
							class="btn btn-xs btn-ghost btn-square text-error hover:bg-error/10"
							onclick={() => onDetachExam(item.examId)}
							title="Detach Exam from Course"
							disabled={isLoading}
						>
							<Trash2 class="w-3.5 h-3.5" />
						</button>
					</div>
				</div>
			{/each}
		</div>
	{/if}
</div>

<!-- Attach Exam Modal (Centered Glass Overlay) -->
{#if isAttachModalOpen}
	<div
		class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-md animate-in fade-in"
		role="dialog"
		aria-modal="true"
	>
		<div class="fixed inset-0" onclick={() => (isAttachModalOpen = false)} role="presentation"></div>

		<div class="relative z-10 w-full max-w-md overflow-hidden rounded-2xl border border-base-content/10 bg-base-100/95 p-6 shadow-2xl backdrop-blur-2xl space-y-4">
			<div class="flex items-center justify-between border-b border-base-content/10 pb-3">
				<h3 class="font-bold text-base text-base-content flex items-center gap-2">
					<FileText class="w-5 h-5 text-primary" />
					<span>Attach Exam to Course</span>
				</h3>
				<button
					type="button"
					class="btn btn-ghost btn-circle btn-xs text-base-content/60 hover:text-base-content"
					onclick={() => (isAttachModalOpen = false)}
					aria-label="Close modal"
				>
					<X class="w-4 h-4" />
				</button>
			</div>

			<form
				onsubmit={(e) => {
					e.preventDefault();
					handleAttach();
				}}
				class="space-y-4"
			>
				<div>
					<label for="exam-select-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Select Exam <span class="text-error">*</span>
					</label>

					{#if availableExamsToAttach.length === 0}
						<div class="p-4 bg-base-200/50 rounded-xl text-center space-y-2">
							<p class="text-xs text-base-content/70">No unlinked exams available to attach.</p>
							<a href="/instructor/exams/create" target="_blank" class="btn btn-xs btn-primary">
								Create New Exam
							</a>
						</div>
					{:else}
						<select
							id="exam-select-input"
							bind:value={selectedExamId}
							class="select select-bordered select-sm w-full bg-base-200/50"
							required
						>
							{#each availableExamsToAttach as ex (ex.id)}
								<option value={ex.id}>{ex.title} ({ex.mode}, {ex.durationMinutes}m)</option>
							{/each}
						</select>
					{/if}
				</div>

				<div class="form-control">
					<label class="label cursor-pointer justify-start gap-3">
						<input
							type="checkbox"
							bind:checked={isMandatory}
							class="checkbox checkbox-sm checkbox-primary"
						/>
						<span class="label-text text-xs font-medium text-base-content">
							Mandatory for Course Completion & Certificate
						</span>
					</label>
				</div>

				<div class="flex justify-end gap-2 pt-3 border-t border-base-content/10">
					<button
						type="button"
						class="btn btn-sm btn-ghost"
						onclick={() => (isAttachModalOpen = false)}
					>
						Cancel
					</button>
					<button
						type="submit"
						class="btn btn-sm btn-primary gap-1.5"
						disabled={!selectedExamId || availableExamsToAttach.length === 0}
					>
						<Check class="w-4 h-4" />
						Attach Exam
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}
