<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
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
		BookOpen,
		Sparkles,
		Calendar,
		AlertTriangle
	} from 'lucide-svelte';
	import { examsApi } from '$lib/api/exams.ts';
	import type { QuizExam, QuizSection, QuestionBank, QuizMode } from '$lib/api/types.ts';
	import GlassCard from '$lib/components/ui/GlassCard.svelte';
	import ConfirmModal from '$lib/components/ui/ConfirmModal.svelte';
	import SectionBuilder from '$lib/components/exam/SectionBuilder.svelte';
	import RichRenderer from '$lib/components/editor/RichRenderer.svelte';
	import RichEditor from '$lib/components/editor/RichEditor.svelte';
	import { toast } from '$lib/stores/toast.svelte.ts';

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
	let editPassingScore = $state(70);
	let editMaxViolations = $state(3);
	let editMaxAttempts = $state(1);
	let editAvailableFromLocal = $state('');
	let editAvailableToLocal = $state('');
	let editShuffleQuestions = $state(true);
	let editShuffleOptions = $state(true);
	let isSavingSettings = $state(false);

	// Delete Exam Modal
	let isDeleteExamModalOpen = $state(false);

	onMount(async () => {
		await loadExamAndBanks();
	});

	function toLocalDatetimeString(utcIso?: string | null): string {
		if (!utcIso) return '';
		const d = new Date(utcIso);
		if (isNaN(d.getTime())) return '';
		const pad = (n: number) => n.toString().padStart(2, '0');
		return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
	}

	function toUtcIso(localDatetime: string): string | undefined {
		if (!localDatetime) return undefined;
		const d = new Date(localDatetime);
		return isNaN(d.getTime()) ? undefined : d.toISOString();
	}

	async function loadExamAndBanks() {
		isLoading = true;
		try {
			const [examData, banksData] = await Promise.all([
				examsApi.getExamById(examId),
				examsApi.listQuestionBanks({ pageSize: 100 })
			]);

			exam = examData;
			availableBanks = banksData.items || [];

			if (exam) {
				editTitle = exam.title;
				editDescription = exam.description || '';
				editMode = (exam.mode as QuizMode) || 'RealExam';
				editDurationMinutes = exam.durationMinutes || 60;
				editPassingScore = exam.passingScore || 70;
				editMaxViolations = exam.maxAllowedViolations ?? 3;
				editMaxAttempts = exam.maxAttempts ?? 1;
				editAvailableFromLocal = toLocalDatetimeString(exam.availableFromUtc);
				editAvailableToLocal = toLocalDatetimeString(exam.availableToUtc);
				editShuffleQuestions = exam.shuffleQuestions ?? true;
				editShuffleOptions = exam.shuffleOptions ?? true;
			}
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

		if (editAvailableFromLocal && editAvailableToLocal) {
			const fromDate = new Date(editAvailableFromLocal);
			const toDate = new Date(editAvailableToLocal);
			if (toDate <= fromDate) {
				toast.warning('Closing date must be scheduled after opening date.');
				return;
			}
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
				maxAttempts: Number(editMaxAttempts),
				availableFromUtc: toUtcIso(editAvailableFromLocal),
				availableToUtc: toUtcIso(editAvailableToLocal),
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
			toast.error(err?.message || 'Failed to save parameters.');
		} finally {
			isSavingSettings = false;
		}
	}

	async function handleSaveDraft() {
		if (!editTitle.trim()) {
			toast.warning('Please enter an exam title.');
			return;
		}

		if (editAvailableFromLocal && editAvailableToLocal) {
			const fromDate = new Date(editAvailableFromLocal);
			const toDate = new Date(editAvailableToLocal);
			if (toDate <= fromDate) {
				toast.warning('Closing date must be scheduled after opening date.');
				return;
			}
		}

		isActionLoading = true;
		try {
			const sectionPayload = (exam?.sections || []).map((sec, idx) => ({
				id: sec.id,
				questionBankId: sec.questionBankId,
				title: sec.title,
				description: sec.description || null,
				pointsOverride: sec.pointsOverride ?? null,
				questionCount: sec.questionCount ?? null,
				orderIndex: sec.orderIndex || idx + 1
			}));

			const updated = await examsApi.updateExam(examId, {
				title: editTitle.trim(),
				description: editDescription.trim() || undefined,
				mode: editMode,
				durationMinutes: Number(editDurationMinutes),
				passingScore: Number(editPassingScore),
				maxAllowedViolations: editMode === 'RealExam' ? Number(editMaxViolations) : 0,
				maxAttempts: Number(editMaxAttempts),
				availableFromUtc: toUtcIso(editAvailableFromLocal),
				availableToUtc: toUtcIso(editAvailableToLocal),
				shuffleQuestions: editShuffleQuestions,
				shuffleOptions: editShuffleOptions,
				sections: sectionPayload
			});

			toast.success('Exam draft and sections saved successfully!');
			if (exam) {
				exam.title = updated.title;
				exam.description = updated.description;
				exam.mode = updated.mode;
				exam.durationMinutes = updated.durationMinutes;
				exam.passingScore = updated.passingScore;
			}
		} catch (err: any) {
			toast.error(err?.message || 'Failed to save exam draft.');
		} finally {
			isActionLoading = false;
		}
	}

	async function handleSaveSections(updatedSections: QuizSection[]) {
		if (!exam) return;
		exam.sections = updatedSections;

		try {
			const sectionPayload = updatedSections.map((sec, idx) => ({
				id: sec.id,
				questionBankId: sec.questionBankId,
				title: sec.title,
				description: sec.description || null,
				pointsOverride: sec.pointsOverride ?? null,
				questionCount: sec.questionCount ?? null,
				orderIndex: sec.orderIndex || idx + 1
			}));

			await examsApi.updateExam(examId, {
				title: editTitle.trim() || exam.title,
				description: editDescription.trim() || exam.description || undefined,
				mode: editMode,
				durationMinutes: Number(editDurationMinutes),
				passingScore: Number(editPassingScore),
				maxAllowedViolations: editMode === 'RealExam' ? Number(editMaxViolations) : 0,
				maxAttempts: Number(editMaxAttempts),
				availableFromUtc: toUtcIso(editAvailableFromLocal),
				availableToUtc: toUtcIso(editAvailableToLocal),
				shuffleQuestions: editShuffleQuestions,
				shuffleOptions: editShuffleOptions,
				sections: sectionPayload
			});

			toast.success('Exam sections saved.');
		} catch (err: any) {
			toast.error(err?.message || 'Failed to persist sections.');
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

<div class="space-y-6 max-w-6xl mx-auto pb-16">
	<!-- Top Navigation Bar -->
	<div class="flex flex-wrap items-center justify-between gap-3">
		<a
			href="/instructor/exams"
			class="btn btn-sm btn-ghost gap-2 text-base-content/70 hover:text-base-content"
		>
			<ArrowLeft class="w-4 h-4" />
			<span>Back to Examinations</span>
		</a>

		<div class="flex items-center gap-2">
			<button
				type="button"
				class="btn btn-primary btn-sm gap-1.5 shadow-md shadow-primary/20"
				onclick={handleSaveDraft}
				disabled={isActionLoading}
			>
				{#if isActionLoading}
					<span class="loading loading-spinner loading-xs"></span>
				{:else}
					<Save class="w-3.5 h-3.5" />
				{/if}
				Save Changes
			</button>

			{#if exam && !exam.isPublished}
				<button
					type="button"
					class="btn btn-success btn-sm text-white font-bold shadow-md gap-1.5"
					onclick={handlePublish}
					disabled={isActionLoading || (exam.sections || []).length === 0}
				>
					<Send class="w-3.5 h-3.5" />
					Publish Exam
				</button>
			{/if}

			<button
				type="button"
				class="btn btn-error btn-outline btn-sm gap-1.5"
				onclick={() => (isDeleteExamModalOpen = true)}
				disabled={isActionLoading}
			>
				<Trash2 class="w-3.5 h-3.5" />
				Delete Exam
			</button>
		</div>
	</div>

	{#if isLoading}
		<div class="h-64 rounded-3xl bg-base-200/50 animate-pulse flex items-center justify-center">
			<span class="loading loading-spinner loading-lg text-primary"></span>
		</div>
	{:else if exam}
		<!-- Exam Header Overview Banner -->
		<GlassCard class="p-6 sm:p-7 space-y-4">
			<div class="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4">
				<div class="space-y-2 flex-1 min-w-0">
					<div class="flex items-center gap-2 flex-wrap">
						<span class="badge {exam.mode === 'RealExam' ? 'badge-primary' : 'badge-ghost'} badge-sm font-bold uppercase text-[10px]">
							{exam.mode === 'RealExam' ? 'Proctored Exam' : 'Simulation Practice'}
						</span>
						<span class="badge {exam.isPublished ? 'badge-success text-white' : 'badge-warning'} badge-sm font-semibold text-[10px]">
							{exam.isPublished ? 'Published' : 'Draft'}
						</span>
						<span class="badge badge-sm badge-outline text-[10px]">
							{exam.durationMinutes} mins • {exam.passingScore}% passing
						</span>
					</div>

					<h1 class="text-2xl sm:text-3xl font-extrabold text-base-content tracking-tight">{exam.title}</h1>

					{#if exam.description}
						<div class="text-xs text-base-content/70 line-clamp-3 max-w-3xl pt-0.5">
							<RichRenderer content={exam.description} />
						</div>
					{/if}
				</div>

				<div class="flex items-center gap-2 flex-shrink-0">
					<!-- Tabs switch -->
					<div class="flex items-center gap-1 rounded-2xl p-1 bg-base-200/70 border border-base-content/10">
						<button
							type="button"
							class="btn btn-xs rounded-xl font-bold transition-all gap-1.5 {activeTab === 'sections'
								? 'btn-primary text-primary-content shadow-xs'
								: 'btn-ghost text-base-content/70'}"
							onclick={() => (activeTab = 'sections')}
						>
							<Layers class="w-3.5 h-3.5" />
							Exam Sections ({exam.sections?.length || 0})
						</button>
						<button
							type="button"
							class="btn btn-xs rounded-xl font-bold transition-all gap-1.5 {activeTab === 'settings'
								? 'btn-primary text-primary-content shadow-xs'
								: 'btn-ghost text-base-content/70'}"
							onclick={() => (activeTab = 'settings')}
						>
							<Settings class="w-3.5 h-3.5" />
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
				<form onsubmit={handleSaveSettings} class="space-y-6 max-w-3xl">
					<div>
						<label for="edit-title" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
							Exam Title <span class="text-error">*</span>
						</label>
						<input
							id="edit-title"
							type="text"
							bind:value={editTitle}
							class="input input-bordered w-full bg-base-100/50 font-semibold"
							required
						/>
					</div>

					<div class="space-y-1.5">
						<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80 block">
							Exam Description / Guidelines
						</label>
						<RichEditor
							bind:content={editDescription}
							placeholder="Exam instructions, syllabus outline, allowed reference sheets..."
						/>
					</div>

					<div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
						<div>
							<label for="edit-mode" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
								Exam Mode
							</label>
							<select id="edit-mode" bind:value={editMode} class="select select-bordered w-full bg-base-100/50 text-xs font-semibold">
								<option value="RealExam">RealExam (Proctored Anti-Cheat)</option>
								<option value="Simulation">Simulation (Practice Mode)</option>
							</select>
						</div>

						<div>
							<label for="edit-duration" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
								Duration (Minutes) <span class="text-error">*</span>
							</label>
							<input
								id="edit-duration"
								type="number"
								min="1"
								bind:value={editDurationMinutes}
								class="input input-bordered w-full bg-base-100/50 font-semibold"
								required
							/>
						</div>

						<div>
							<label for="edit-pass" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
								Passing Score (%) <span class="text-error">*</span>
							</label>
							<input
								id="edit-pass"
								type="number"
								min="0"
								max="100"
								bind:value={editPassingScore}
								class="input input-bordered w-full bg-base-100/50 font-semibold"
								required
							/>
						</div>
					</div>

					<div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
						<div>
							<label for="edit-attempts" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
								Max Retake Attempts <span class="text-error">*</span>
							</label>
							<input
								id="edit-attempts"
								type="number"
								min="1"
								max="10"
								bind:value={editMaxAttempts}
								class="input input-bordered w-full bg-base-100/50 font-semibold"
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
								max="10"
								bind:value={editMaxViolations}
								class="input input-bordered w-full bg-base-100/50 font-semibold"
								disabled={editMode !== 'RealExam'}
							/>
						</div>
					</div>

					<!-- Schedule Window -->
					<div class="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-2">
						<div>
							<label for="edit-open-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
								Opening Time (Optional)
							</label>
							<input
								id="edit-open-input"
								type="datetime-local"
								bind:value={editAvailableFromLocal}
								class="input input-bordered w-full bg-base-100/50 text-xs font-semibold"
							/>
						</div>

						<div>
							<label for="edit-close-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
								Closing / Deadline Time (Optional)
							</label>
							<input
								id="edit-close-input"
								type="datetime-local"
								bind:value={editAvailableToLocal}
								class="input input-bordered w-full bg-base-100/50 text-xs font-semibold"
							/>
						</div>
					</div>

					<!-- Randomization Toggles -->
					<div class="p-4 rounded-2xl bg-base-200/50 border border-base-content/10 space-y-3">
						<span class="text-xs font-bold uppercase tracking-wider text-base-content/80 block">
							Integrity & Randomization
						</span>

						<div class="flex items-center justify-between">
							<span class="text-xs font-medium text-base-content">Shuffle Questions per Candidate</span>
							<input
								type="checkbox"
								bind:checked={editShuffleQuestions}
								class="toggle toggle-primary toggle-sm"
							/>
						</div>

						<div class="flex items-center justify-between">
							<span class="text-xs font-medium text-base-content">Shuffle Options Choices</span>
							<input
								type="checkbox"
								bind:checked={editShuffleOptions}
								class="toggle toggle-secondary toggle-sm"
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
