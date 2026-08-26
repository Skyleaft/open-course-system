<script lang="ts">
	import type { CourseExam, QuizExam } from '$lib/api/types.ts';
	import { fade, scale } from 'svelte/transition';
	import { cubicOut } from 'svelte/easing';
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
		X,
		Search,
		Award,
		Sliders
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
	let searchExamTerm = $state('');

	// Filter out already attached exams
	const availableExamsToAttach = $derived(
		allExams.filter((e) => !courseExams.some((ce) => ce.examId === e.id))
	);

	const filteredExamsToAttach = $derived(
		availableExamsToAttach.filter((e) => {
			if (!searchExamTerm.trim()) return true;
			const q = searchExamTerm.trim().toLowerCase();
			return (
				e.title.toLowerCase().includes(q) ||
				(e.description && e.description.toLowerCase().includes(q)) ||
				(e.ruleConfig?.name && e.ruleConfig.name.toLowerCase().includes(q))
			);
		})
	);

	function openModal() {
		searchExamTerm = '';
		selectedExamId = availableExamsToAttach.length > 0 ? availableExamsToAttach[0].id : '';
		isMandatory = true;
		isAttachModalOpen = true;
	}

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
			onclick={openModal}
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
				onclick={openModal}
			>
				<Plus class="w-4 h-4" />
				Attach First Exam
			</button>
		</div>
	{:else}
		<div class="space-y-2.5">
			{#each courseExams as item, idx (item.id || item.examId)}
				{@const examMeta = allExams.find((e) => e.id === item.examId)}
				{@const ruleName = examMeta?.ruleConfig?.name || examMeta?.mode || 'Standard'}
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
								<span class="badge badge-sm badge-outline text-[10px] font-semibold">
									{ruleName}
								</span>
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

<!-- Attach Exam Modal (Enhanced Picker Overlay) -->
{#if isAttachModalOpen}
	<div
		class="fixed inset-0 z-[100] flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
		role="dialog"
		aria-modal="true"
		transition:fade={{ duration: 180 }}
	>
		<div class="fixed inset-0" onclick={() => (isAttachModalOpen = false)} role="presentation"></div>

		<div
			class="relative z-10 w-full max-w-lg overflow-hidden rounded-2xl border border-base-content/10 bg-base-100/95 p-6 shadow-2xl backdrop-blur-2xl space-y-4"
			transition:scale={{ duration: 220, start: 0.94, easing: cubicOut }}
		>
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
					<div class="flex items-center justify-between mb-1.5">
						<span class="label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Select Examination <span class="text-error">*</span>
						</span>
						<a
							href="/instructor/exams/create"
							target="_blank"
							class="text-[11px] font-semibold text-primary hover:underline inline-flex items-center gap-1"
						>
							<Plus class="w-3 h-3" />
							New Exam Template
						</a>
					</div>

					{#if availableExamsToAttach.length === 0}
						<div class="p-6 bg-base-200/50 rounded-2xl text-center space-y-2.5 border border-dashed border-base-content/15">
							<AlertCircle class="w-8 h-8 text-base-content/40 mx-auto" />
							<p class="text-xs font-semibold text-base-content/80">No unlinked exams available to attach.</p>
							<p class="text-[11px] text-base-content/50">All existing exams are already linked to this course or none have been authored yet.</p>
							<a href="/instructor/exams/create" target="_blank" class="btn btn-xs btn-primary gap-1 mt-1">
								<Plus class="w-3.5 h-3.5" />
								Author New Exam
							</a>
						</div>
					{:else}
						<!-- Search Filter -->
						<div class="relative mb-2">
							<Search class="absolute left-3 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-base-content/40" />
							<input
								type="text"
								bind:value={searchExamTerm}
								placeholder="Search available exams by title or rule..."
								class="input input-bordered input-sm w-full pl-8.5 bg-base-200/50 text-xs rounded-xl"
							/>
						</div>

						<!-- Selectable Exam Cards List -->
						<div class="max-h-56 overflow-y-auto space-y-2 pr-0.5">
							{#if filteredExamsToAttach.length === 0}
								<div class="py-6 text-center text-xs text-base-content/50 italic bg-base-200/30 rounded-xl">
									No exams match your search query.
								</div>
							{:else}
								{#each filteredExamsToAttach as ex (ex.id)}
									{@const isSelected = selectedExamId === ex.id}
									{@const ruleName = ex.ruleConfig?.name || ex.mode || 'Standard Exam'}
									<button
										type="button"
										class="w-full p-3 rounded-xl border text-left transition-all flex items-center justify-between gap-3 {isSelected
											? 'border-primary bg-primary/10 ring-2 ring-primary/20 shadow-xs'
											: 'border-base-content/10 bg-base-200/40 hover:bg-base-200/70'}"
										onclick={() => (selectedExamId = ex.id)}
									>
										<div class="min-w-0 flex-1">
											<div class="flex items-center gap-2 mb-1 flex-wrap">
												<span class="font-bold text-xs text-base-content truncate">{ex.title}</span>
												<span class="badge badge-xs {isSelected ? 'badge-primary' : 'badge-ghost'} font-semibold">
													{ruleName}
												</span>
											</div>
											<div class="flex items-center gap-2.5 text-[11px] text-base-content/60 font-medium">
												<span class="inline-flex items-center gap-1">
													<Clock class="w-3 h-3 text-base-content/40" />
													{ex.durationMinutes}m
												</span>
												<span>•</span>
												<span>Pass: {ex.passingScore}%</span>
											</div>
										</div>

										<div class="flex-shrink-0">
											<div
												class="w-5 h-5 rounded-full border flex items-center justify-center transition-all {isSelected
													? 'border-primary bg-primary text-white'
													: 'border-base-content/30 bg-base-100'}"
											>
												{#if isSelected}
													<Check class="w-3 h-3 stroke-[3]" />
												{/if}
											</div>
										</div>
									</button>
								{/each}
							{/if}
						</div>
					{/if}
				</div>

				<!-- Requirement Policy Toggle Card -->
				<div class="p-3.5 rounded-2xl bg-base-200/40 border border-base-content/10 flex items-center justify-between gap-3">
					<div class="space-y-0.5">
						<div class="flex items-center gap-1.5">
							<ShieldCheck class="w-4 h-4 text-primary" />
							<span class="text-xs font-bold text-base-content">Mandatory for Completion</span>
						</div>
						<p class="text-[11px] text-base-content/60 leading-relaxed">
							Students must pass this exam before course completion and certificate issuance.
						</p>
					</div>
					<input
						type="checkbox"
						bind:checked={isMandatory}
						class="toggle toggle-primary toggle-sm shrink-0"
					/>
				</div>

				<div class="flex justify-end gap-2 pt-3 border-t border-base-content/10">
					<button
						type="button"
						class="btn btn-sm btn-ghost rounded-xl"
						onclick={() => (isAttachModalOpen = false)}
					>
						Cancel
					</button>
					<button
						type="submit"
						class="btn btn-sm btn-primary rounded-xl gap-1.5 shadow-md"
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
