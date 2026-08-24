<script lang="ts">
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { examsApi } from '#lib/api/exams.ts';
	import type { QuizExam, QuizSection, QuestionBank, QuizMode } from '#lib/api/types.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import ConfirmModal from '#lib/components/ui/ConfirmModal.svelte';
	import SectionBuilder from '#lib/components/exam/SectionBuilder.svelte';
	import { toast } from '#lib/stores/toast.svelte.ts';
	import {
		Plus,
		Check,
		Trash2,
		ArrowLeft,
		Save,
		Send,
		ShieldAlert,
		Clock,
		CheckCircle2,
		Layers,
		Settings,
		Shuffle,
		FileText,
		BookOpen
	} from '@lucide/svelte';
	import { onMount } from 'svelte';

	const examId = (page.params.id || '') as string;
	let exam = $state<QuizExam | null>(null);
	let availableBanks = $state<QuestionBank[]>([]);
	let isLoading = $state(true);
	let isActionLoading = $state(false);

	// Tabs: 'sections' | 'settings'
	let activeTab = $state<'sections' | 'settings'>('sections');

	// Exam Settings State
	let editTitle = $state('');
	let editDescription = $state('');
	let editMode = $state<QuizMode>('RealExam');
	let editDurationMinutes = $state(60);
	let editPassingScore = $state(75);
	let editMaxViolations = $state(3);
	let editShuffleQuestions = $state(true);
	let editShuffleOptions = $state(true);
	let isSavingSettings = $state(false);

	// Delete Exam Modal
	let isDeleteExamModalOpen = $state(false);

	onMount(async () => {
		await loadExamAndBanks();
	});

	async function loadExamAndBanks() {
		isLoading = true;
		try {
			// 1. Fetch current exam
			exam = await examsApi.getExamById(examId);
			if (exam) {
				editTitle = exam.title;
				editDescription = exam.description || '';
				editMode = (exam.mode as QuizMode) || 'RealExam';
				editDurationMinutes = exam.durationMinutes || 60;
				editPassingScore = exam.passingScore || 75;
				editMaxViolations = exam.maxAllowedViolations ?? 3;
				editShuffleQuestions = exam.shuffleQuestions ?? true;
				editShuffleOptions = exam.shuffleOptions ?? true;
			}

			// 2. Fetch all exams to extract available QuestionBanks
			const allExamsRes = await examsApi.listExams({ pageSize: 100 });
			const banksMap = new Map<string, QuestionBank>();

			for (const ex of allExamsRes.items || []) {
				try {
					const fullEx = await examsApi.getExamById(ex.id);
					if (fullEx && fullEx.sections) {
						for (const sec of fullEx.sections) {
							if (sec.questionBankId && !banksMap.has(sec.questionBankId)) {
								banksMap.set(sec.questionBankId, {
									id: sec.questionBankId,
									title: sec.questionBankTitle || sec.title || 'Question Pool',
									description: sec.description,
									createdBy: fullEx.createdBy || '',
									createdAtUtc: fullEx.createdAtUtc || '',
									questions: sec.questions || []
								});
							}
						}
					}
				} catch {
					// continue
				}
			}

			availableBanks = Array.from(banksMap.values());
		} catch (err: any) {
			toast.error(err?.message || 'Failed to load examination details.');
		} finally {
			isLoading = false;
		}
	}

	async function handleSaveSettings(e: Event) {
		e.preventDefault();
		if (!editTitle.trim()) {
			toast.warning('Please enter an exam title.');
			return;
		}

		isSavingSettings = true;
		try {
			const updated = await examsApi.updateExam(examId, {
				title: editTitle.trim(),
				description: editDescription.trim() || undefined,
				mode: editMode,
				durationMinutes: Number(editDurationMinutes),
				passingScore: Number(editPassingScore),
				maxAllowedViolations: editMode === 'RealExam' ? Number(editMaxViolations) : 0,
				shuffleQuestions: editShuffleQuestions,
				shuffleOptions: editShuffleOptions
			});
			toast.success('Exam settings updated successfully!');
			if (exam) {
				exam.title = updated.title;
				exam.description = updated.description;
				exam.mode = updated.mode;
				exam.durationMinutes = updated.durationMinutes;
				exam.passingScore = updated.passingScore;
			}
		} catch (err: any) {
			toast.error(err?.message || 'Failed to save settings.');
		} finally {
			isSavingSettings = false;
		}
	}

	function handleSaveSections(updatedSections: QuizSection[]) {
		if (exam) {
			exam.sections = updatedSections;
			toast.success('Sections updated.');
		}
	}

	// Exam Lifecycle Handlers
	async function handlePublish() {
		if (!exam?.sections || exam.sections.length === 0) {
			toast.warning('Cannot publish an exam without at least one section linked to a Question Bank.');
			return;
		}

		isActionLoading = true;
		try {
			await examsApi.publishExam(examId);
			toast.success('Exam published successfully!');
			if (exam) exam.isPublished = true;
		} catch (err: any) {
			toast.error(err?.message || 'Failed to publish exam.');
		} finally {
			isActionLoading = false;
		}
	}

	async function handleDeleteExam() {
		isActionLoading = true;
		try {
			await examsApi.deleteExam(examId);
			toast.success('Exam deleted successfully.');
			goto('/instructor/exams');
		} catch (err: any) {
			toast.error(err?.message || 'Failed to delete exam.');
		} finally {
			isActionLoading = false;
			isDeleteExamModalOpen = false;
		}
	}
</script>

<div class="space-y-8 max-w-6xl mx-auto pb-16">
	<!-- Top Navigation Bar -->
	<div class="flex flex-wrap items-center justify-between gap-3">
		<a
			href="/instructor/exams"
			class="inline-flex items-center gap-1.5 text-xs font-semibold text-base-content/60 hover:text-primary transition-colors"
		>
			<ArrowLeft class="h-4 w-4" />
			Back to Exams
		</a>

		<div class="flex items-center gap-2">
			{#if exam && !exam.isPublished}
				<button
					class="btn btn-success btn-sm rounded-xl text-white font-bold border-0 shadow-md gap-1.5"
					onclick={handlePublish}
					disabled={isActionLoading || (exam.sections || []).length === 0}
				>
					<Send class="h-3.5 w-3.5" />
					Publish Exam
				</button>
			{/if}

			<button
				class="btn btn-error btn-outline btn-sm rounded-xl gap-1.5"
				onclick={() => (isDeleteExamModalOpen = true)}
				disabled={isActionLoading}
			>
				<Trash2 class="h-3.5 w-3.5" />
				Delete Exam
			</button>
		</div>
	</div>

	{#if isLoading}
		<div class="h-80 rounded-3xl bg-base-200/50 animate-pulse"></div>
	{:else if exam}
		<!-- Exam Header Overview Banner -->
		<GlassCard class="p-6 sm:p-8 space-y-4">
			<div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
				<div class="space-y-1.5">
					<div class="flex items-center gap-2">
						<span class="badge {exam.mode === 'RealExam' ? 'badge-primary' : 'badge-ghost'} badge-xs font-bold uppercase">
							{exam.mode === 'RealExam' ? 'Proctored Exam' : 'Simulation Practice'}
						</span>
						<span class="badge {exam.isPublished ? 'badge-success text-white' : 'badge-warning'} badge-xs font-semibold">
							{exam.isPublished ? 'Published' : 'Draft'}
						</span>
					</div>
					<h1 class="text-2xl sm:text-3xl font-extrabold text-base-content tracking-tight">{exam.title}</h1>
					{#if exam.description}
						<p class="text-xs text-base-content/70 line-clamp-2 max-w-2xl">{exam.description}</p>
					{/if}
				</div>

				<div class="flex items-center gap-2">
					<!-- Tabs switch -->
					<div class="flex items-center gap-1 rounded-2xl p-1 bg-base-200/70 border border-base-content/10">
						<button
							class="btn btn-xs rounded-xl font-semibold transition-all gap-1.5 {activeTab === 'sections'
								? 'btn-primary text-primary-content shadow-xs'
								: 'btn-ghost text-base-content/70'}"
							onclick={() => (activeTab = 'sections')}
						>
							<Layers class="h-3.5 w-3.5" />
							Exam Sections ({exam.sections?.length || 0})
						</button>
						<button
							class="btn btn-xs rounded-xl font-semibold transition-all gap-1.5 {activeTab === 'settings'
								? 'btn-primary text-primary-content shadow-xs'
								: 'btn-ghost text-base-content/70'}"
							onclick={() => (activeTab = 'settings')}
						>
							<Settings class="h-3.5 w-3.5" />
							Parameters
						</button>
					</div>
				</div>
			</div>
		</GlassCard>

		{#if activeTab === 'sections'}
			<!-- Tab 1: Section Builder Studio -->
			<GlassCard class="p-6">
				<SectionBuilder
					sections={exam.sections || []}
					{availableBanks}
					onSaveSections={handleSaveSections}
					onCreateNewBank={() => goto('/instructor/questions')}
				/>
			</GlassCard>
		{:else}
			<!-- Tab 2: Exam Settings Studio -->
			<GlassCard class="p-6 sm:p-8">
				<form onsubmit={handleSaveSettings} class="space-y-6 max-w-2xl">
					<div>
						<label for="edit-title" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
							Exam Title <span class="text-error">*</span>
						</label>
						<input
							id="edit-title"
							type="text"
							bind:value={editTitle}
							class="input input-bordered w-full bg-base-100/50"
							required
						/>
					</div>

					<div>
						<label for="edit-desc" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
							Exam Description / Guidelines
						</label>
						<textarea
							id="edit-desc"
							bind:value={editDescription}
							rows="3"
							class="textarea textarea-bordered w-full bg-base-100/50"
						></textarea>
					</div>

					<div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
						<div>
							<label for="edit-mode" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
								Exam Mode
							</label>
							<select id="edit-mode" bind:value={editMode} class="select select-bordered w-full bg-base-100/50">
								<option value="RealExam">RealExam (Full Anti-Cheat Proctoring)</option>
								<option value="Simulation">Simulation (Practice Mode)</option>
							</select>
						</div>

						<div>
							<label for="edit-duration" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
								Duration (Minutes)
							</label>
							<input
								id="edit-duration"
								type="number"
								min="5"
								bind:value={editDurationMinutes}
								class="input input-bordered w-full bg-base-100/50"
								required
							/>
						</div>
					</div>

					<div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
						<div>
							<label for="edit-pass" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
								Passing Score (%)
							</label>
							<input
								id="edit-pass"
								type="number"
								min="1"
								max="100"
								bind:value={editPassingScore}
								class="input input-bordered w-full bg-base-100/50"
								required
							/>
						</div>

						<div>
							<label for="edit-viol" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
								Max Violations (RealExam)
							</label>
							<input
								id="edit-viol"
								type="number"
								min="1"
								bind:value={editMaxViolations}
								class="input input-bordered w-full bg-base-100/50"
								disabled={editMode !== 'RealExam'}
							/>
						</div>
					</div>

					<!-- Randomization Toggles -->
					<div class="p-4 rounded-2xl bg-base-200/50 border border-base-content/10 space-y-3">
						<span class="text-xs font-bold uppercase tracking-wider text-base-content/80 block">
							Integrity & Randomization
						</span>

						<div class="flex items-center justify-between">
							<span class="text-xs font-medium text-base-content">Shuffle Questions (PRNG Fisher-Yates)</span>
							<input
								type="checkbox"
								bind:checked={editShuffleQuestions}
								class="toggle toggle-primary toggle-sm"
							/>
						</div>

						<div class="flex items-center justify-between">
							<span class="text-xs font-medium text-base-content">Shuffle Option Choices</span>
							<input
								type="checkbox"
								bind:checked={editShuffleOptions}
								class="toggle toggle-primary toggle-sm"
							/>
						</div>
					</div>

					<div class="pt-2">
						<button
							type="submit"
							class="btn btn-primary gap-1.5 shadow-md shadow-primary/20"
							disabled={isSavingSettings}
						>
							{#if isSavingSettings}
								<span class="loading loading-spinner loading-xs"></span>
							{:else}
								<Save class="w-4 h-4" />
							{/if}
							Save Parameters
						</button>
					</div>
				</form>
			</GlassCard>
		{/if}
	{/if}
</div>

<!-- Delete Exam Confirmation Modal -->
<ConfirmModal
	isOpen={isDeleteExamModalOpen}
	title="Delete Examination"
	message="Are you sure you want to permanently delete this exam? All candidate submission records and active sections will be deleted."
	confirmText="Delete Exam"
	isDanger={true}
	isLoading={isActionLoading}
	onConfirm={handleDeleteExam}
	onCancel={() => (isDeleteExamModalOpen = false)}
/>
