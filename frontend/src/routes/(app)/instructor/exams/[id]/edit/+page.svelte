<script lang="ts">
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { examsApi } from '#lib/api/exams.ts';
	import type { QuizExam, QuizQuestion, QuestionOption, QuestionType } from '#lib/api/types.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import GlassModal from '#lib/components/ui/GlassModal.svelte';
	import ConfirmModal from '#lib/components/ui/ConfirmModal.svelte';
	import RichEditor from '#lib/components/editor/RichEditor.svelte';
	import RichRenderer from '#lib/components/editor/RichRenderer.svelte';
	import { toast } from '#lib/stores/toast.svelte.ts';
	import {
		Plus,
		Check,
		Edit3,
		Trash2,
		ArrowLeft,
		Save,
		Send,
		ShieldAlert,
		Clock,
		CheckCircle2,
		Layers,
		Settings,
		HelpCircle,
		Shuffle,
		FileText,
		ListFilter,
		CheckSquare,
		AlignLeft
	} from '@lucide/svelte';
	import { onMount } from 'svelte';

	const examId = (page.params.id || '') as string;
	let exam = $state<QuizExam | null>(null);
	let isLoading = $state(true);
	let isActionLoading = $state(false);

	// Tabs: 'questions' | 'settings'
	let activeTab = $state<'questions' | 'settings'>('questions');

	// Exam Settings State
	let editTitle = $state('');
	let editDescription = $state('');
	let editMode = $state<'RealExam' | 'Simulation'>('RealExam');
	let editDurationMinutes = $state(60);
	let editPassingScore = $state(75);
	let editMaxViolations = $state(3);
	let editShuffleQuestions = $state(true);
	let editShuffleOptions = $state(true);
	let isSavingSettings = $state(false);

	// Question Types List
	const questionTypes: { id: QuestionType; label: string; icon: any; desc: string }[] = [
		{ id: 'SingleChoice', label: 'Single Choice', icon: CheckCircle2, desc: 'One correct option' },
		{ id: 'MultipleChoice', label: 'Multiple Choice', icon: CheckSquare, desc: 'One or more correct' },
		{ id: 'TrueFalse', label: 'True / False', icon: ListFilter, desc: 'Binary decision' },
		{ id: 'Essay', label: 'Essay Question', icon: AlignLeft, desc: 'Open text response' }
	];

	// Add Question Modal State
	let isAddQuestionModalOpen = $state(false);
	let newQuestionText = $state('');
	let newQuestionType = $state<QuestionType>('SingleChoice');
	let newQuestionPoints = $state(5);
	let newQuestionExplanation = $state('');
	let newQuestionOptions = $state<Array<{ text: string; isCorrect: boolean }>>([
		{ text: 'Option A', isCorrect: true },
		{ text: 'Option B', isCorrect: false }
	]);

	// Edit Question Modal State
	let isEditQuestionModalOpen = $state(false);
	let editingQuestionId = $state<string | null>(null);
	let editQuestionText = $state('');
	let editQuestionType = $state<QuestionType>('SingleChoice');
	let editQuestionPoints = $state(5);
	let editQuestionExplanation = $state('');
	let editQuestionOptions = $state<Array<{ id?: string; text: string; isCorrect: boolean }>>([]);

	// Delete Question Modal
	let isDeleteQuestionModalOpen = $state(false);
	let deletingQuestionId = $state<string | null>(null);
	let deletingQuestionText = $state('');

	// Delete Exam Modal
	let isDeleteExamModalOpen = $state(false);

	onMount(async () => {
		await loadExam();
	});

	async function loadExam() {
		isLoading = true;
		try {
			exam = await examsApi.getExamById(examId);
			if (exam) {
				editTitle = exam.title;
				editDescription = exam.description || '';
				editMode = (exam.mode as any) || 'RealExam';
				editDurationMinutes = exam.durationMinutes || 60;
				editPassingScore = exam.passingScore || 75;
				editMaxViolations = exam.maxAllowedViolations ?? 3;
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

	// Question Handlers
	function openAddQuestion() {
		newQuestionText = '';
		newQuestionType = 'SingleChoice';
		newQuestionPoints = 5;
		newQuestionExplanation = '';
		newQuestionOptions = [
			{ text: 'Option A', isCorrect: true },
			{ text: 'Option B', isCorrect: false },
			{ text: 'Option C', isCorrect: false },
			{ text: 'Option D', isCorrect: false }
		];
		isAddQuestionModalOpen = true;
	}

	function handleTypeChange(type: QuestionType) {
		newQuestionType = type;
		if (type === 'TrueFalse') {
			newQuestionOptions = [
				{ text: 'True', isCorrect: true },
				{ text: 'False', isCorrect: false }
			];
		} else if (type === 'Essay') {
			newQuestionOptions = [];
		} else if (newQuestionOptions.length === 0) {
			newQuestionOptions = [
				{ text: 'Option A', isCorrect: true },
				{ text: 'Option B', isCorrect: false }
			];
		}
	}

	function handleEditTypeChange(type: QuestionType) {
		editQuestionType = type;
		if (type === 'TrueFalse') {
			editQuestionOptions = [
				{ text: 'True', isCorrect: true },
				{ text: 'False', isCorrect: false }
			];
		} else if (type === 'Essay') {
			editQuestionOptions = [];
		} else if (editQuestionOptions.length === 0) {
			editQuestionOptions = [
				{ text: 'Option A', isCorrect: true },
				{ text: 'Option B', isCorrect: false }
			];
		}
	}

	async function handleAddQuestion() {
		if (!newQuestionText.trim()) {
			toast.warning('Please enter the question text or prompt.');
			return;
		}

		if (newQuestionType !== 'Essay' && newQuestionOptions.length < 2) {
			toast.warning('Please provide at least 2 option choices.');
			return;
		}

		if (newQuestionType !== 'Essay' && !newQuestionOptions.some((o) => o.isCorrect)) {
			toast.warning('Please select at least one correct option.');
			return;
		}

		isActionLoading = true;
		try {
			await examsApi.addQuestion(examId, {
				questionText: newQuestionText.trim(),
				type: newQuestionType,
				points: Number(newQuestionPoints) || 1,
				explanation: newQuestionExplanation.trim() || undefined,
				options: newQuestionType === 'Essay' ? [] : newQuestionOptions
			});
			toast.success('Question added to bank!');
			isAddQuestionModalOpen = false;
			await loadExam();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to add question.');
		} finally {
			isActionLoading = false;
		}
	}

	function openEditQuestion(q: QuizQuestion) {
		editingQuestionId = q.id;
		editQuestionText = q.questionText || q.text || '';
		editQuestionType = q.type || 'SingleChoice';
		editQuestionPoints = q.points || 5;
		editQuestionExplanation = q.explanation || '';
		editQuestionOptions = (q.options || []).map((o) => ({
			id: o.id,
			text: o.text,
			isCorrect: o.isCorrect ?? false
		}));
		isEditQuestionModalOpen = true;
	}

	async function handleUpdateQuestion() {
		if (!editingQuestionId || !editQuestionText.trim()) {
			toast.warning('Please provide the question prompt.');
			return;
		}

		if (editQuestionType !== 'Essay' && editQuestionOptions.length < 2) {
			toast.warning('Please provide at least 2 option choices.');
			return;
		}

		if (editQuestionType !== 'Essay' && !editQuestionOptions.some((o) => o.isCorrect)) {
			toast.warning('Please select at least one correct option.');
			return;
		}

		isActionLoading = true;
		try {
			await examsApi.updateQuestion(editingQuestionId, {
				questionText: editQuestionText.trim(),
				type: editQuestionType,
				points: Number(editQuestionPoints) || 1,
				explanation: editQuestionExplanation.trim() || undefined,
				options: editQuestionType === 'Essay' ? [] : editQuestionOptions
			});
			toast.success('Question updated successfully!');
			isEditQuestionModalOpen = false;
			await loadExam();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to update question.');
		} finally {
			isActionLoading = false;
		}
	}

	function openDeleteQuestion(q: QuizQuestion) {
		deletingQuestionId = q.id;
		deletingQuestionText = q.questionText || q.text || 'this question';
		isDeleteQuestionModalOpen = true;
	}

	async function handleDeleteQuestion() {
		if (!deletingQuestionId) return;
		isActionLoading = true;
		try {
			await examsApi.deleteQuestion(deletingQuestionId);
			toast.success('Question removed.');
			isDeleteQuestionModalOpen = false;
			await loadExam();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to delete question.');
		} finally {
			isActionLoading = false;
		}
	}

	// Exam Lifecycle Handlers
	async function handlePublish() {
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

<div class="space-y-8">
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
					disabled={isActionLoading || (exam.questions || []).length === 0}
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
		<div class="glass-panel h-80 rounded-3xl animate-pulse"></div>
	{:else if exam}
		<!-- Exam Header Overview Banner -->
		<div class="glass-panel rounded-3xl border border-white/10 p-8 shadow-2xl space-y-4">
			<div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
				<div class="space-y-1">
					<div class="flex items-center gap-2">
						<span class="badge {exam.mode === 'RealExam' ? 'badge-primary' : 'badge-ghost'} badge-xs font-bold uppercase">
							{exam.mode === 'RealExam' ? 'Proctored Exam' : 'Simulation'}
						</span>
						<span class="badge {exam.isPublished ? 'badge-success text-white' : 'badge-warning'} badge-xs font-semibold">
							{exam.isPublished ? 'Published' : 'Draft'}
						</span>
					</div>
					<h1 class="text-3xl font-extrabold text-base-content tracking-tight">{exam.title}</h1>
				</div>

				<div class="flex items-center gap-2">
					<!-- Tabs switch -->
					<div class="glass-card flex items-center gap-1 rounded-2xl p-1 border border-white/10">
						<button
							class="btn btn-xs rounded-xl font-semibold transition-all gap-1.5 {activeTab === 'questions'
								? 'btn-secondary text-white shadow-xs'
								: 'btn-ghost text-base-content/70 hover:bg-base-100/40'}"
							onclick={() => (activeTab = 'questions')}
						>
							<Layers class="h-3.5 w-3.5" />
							Question Bank ({exam.questions?.length || 0})
						</button>
						<button
							class="btn btn-xs rounded-xl font-semibold transition-all gap-1.5 {activeTab === 'settings'
								? 'btn-secondary text-white shadow-xs'
								: 'btn-ghost text-base-content/70 hover:bg-base-100/40'}"
							onclick={() => (activeTab = 'settings')}
						>
							<Settings class="h-3.5 w-3.5" />
							Exam Parameters
						</button>
					</div>

					{#if activeTab === 'questions'}
						<button
							class="btn btn-secondary gradient-accent btn-sm rounded-xl text-white font-semibold border-0 shadow-md gap-1.5"
							onclick={openAddQuestion}
						>
							<Plus class="h-4 w-4" />
							Add Question
						</button>
					{/if}
				</div>
			</div>
		</div>

		{#if activeTab === 'questions'}
			<!-- Tab 1: Question Bank Studio -->
			<div class="space-y-4">
				{#each exam.questions || [] as question, qIdx (question.id || qIdx)}
					<GlassCard class="space-y-4 p-6 hover:border-primary/30 transition-colors">
						<div class="flex items-start justify-between border-b border-white/10 pb-3 gap-4">
							<div class="flex items-center gap-3">
								<span class="gradient-accent flex h-7 w-7 shrink-0 items-center justify-center rounded-xl text-xs font-bold text-white">
									{qIdx + 1}
								</span>
								<div class="flex items-center gap-2">
									<span class="badge badge-neutral badge-xs font-semibold uppercase">{question.type}</span>
									<span class="badge badge-ghost badge-xs font-mono">{question.points} pts</span>
								</div>
							</div>

							<div class="flex items-center gap-1">
								<button
									class="btn btn-ghost btn-xs text-base-content/70 hover:text-secondary rounded-lg p-1.5"
									title="Edit Question"
									onclick={() => openEditQuestion(question)}
								>
									<Edit3 class="h-3.5 w-3.5" />
								</button>
								<button
									class="btn btn-ghost btn-xs text-base-content/70 hover:text-error rounded-lg p-1.5"
									title="Delete Question"
									onclick={() => openDeleteQuestion(question)}
								>
									<Trash2 class="h-3.5 w-3.5" />
								</button>
							</div>
						</div>

						<!-- Question Prompt with RichRenderer -->
						<div class="text-sm font-medium text-base-content leading-relaxed">
							<RichRenderer content={question.questionText || question.text || ''} />
						</div>

						<!-- Options List (if non-essay) -->
						{#if question.type !== 'Essay'}
							<div class="grid grid-cols-1 sm:grid-cols-2 gap-2 pt-2">
								{#each question.options || [] as opt}
									<div
										class="flex items-center justify-between p-2.5 rounded-xl border text-xs {opt.isCorrect
											? 'border-success/40 bg-success/10 text-success font-semibold'
											: 'border-base-content/10 bg-base-100/40 text-base-content/75'}"
									>
										<span>{opt.text}</span>
										{#if opt.isCorrect}
											<Check class="h-3.5 w-3.5 text-success shrink-0" />
										{/if}
									</div>
								{/each}
							</div>
						{:else}
							<div class="rounded-xl bg-base-100/40 border border-white/5 p-3 text-xs text-base-content/50 italic">
								Open-ended essay response graded manually or with automated rubric.
							</div>
						{/if}

						{#if question.explanation}
							<div class="rounded-xl bg-info/10 border border-info/20 p-3 text-xs text-info flex items-start gap-2">
								<HelpCircle class="h-4 w-4 shrink-0 mt-0.5" />
								<div>
									<span class="font-bold block">Explanation for Review:</span>
									<span class="opacity-90">{question.explanation}</span>
								</div>
							</div>
						{/if}
					</GlassCard>
				{:else}
					<div class="glass-card p-14 text-center rounded-3xl border border-white/5 space-y-3">
						<Layers class="h-10 w-10 text-secondary mx-auto opacity-50" />
						<h3 class="text-base font-bold">No Questions in Exam Bank</h3>
						<p class="text-xs text-base-content/60 max-w-sm mx-auto">
							Author questions with rich text formatting, LaTeX mathematical formulas, and custom point values.
						</p>
						<button
							class="btn btn-secondary gradient-accent btn-sm rounded-xl text-white font-bold border-0 shadow-md gap-1.5"
							onclick={openAddQuestion}
						>
							<Plus class="h-4 w-4" />
							Add First Question
						</button>
					</div>
				{/each}
			</div>
		{:else}
			<!-- Tab 2: Exam Parameters & Settings -->
			<GlassCard class="p-8 space-y-6">
				<form onsubmit={handleSaveSettings} class="space-y-6">
					<!-- Title -->
					<div class="space-y-2">
						<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block" for="ex-title">
							Examination Title <span class="text-error">*</span>
						</label>
						<input
							id="ex-title"
							type="text"
							class="input input-bordered w-full rounded-2xl h-12 bg-base-100/70 border-base-content/20 text-base-content font-semibold focus:border-primary"
							placeholder="e.g. Midterm Assessment: Distributed Systems"
							bind:value={editTitle}
							required
						/>
					</div>

					<!-- Description with Edra Editor -->
					<div class="space-y-2">
						<div class="flex items-center justify-between">
							<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block">
								Exam Instructions & Guidelines
							</label>
							<span class="badge badge-neutral badge-xs font-mono text-[10px]">Edra Editor</span>
						</div>
						<RichEditor
							content={editDescription}
							minHeight="140px"
							placeholder="Describe rules, allowed resources, or instructions..."
							onUpdate={(json) => {
								editDescription = json;
							}}
						/>
					</div>

					<!-- Mode Selection -->
					<div class="space-y-3">
						<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block">
							Examination Mode <span class="text-error">*</span>
						</label>
						<div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
							<button
								type="button"
								class="p-4 rounded-2xl border text-left transition-all {editMode === 'RealExam'
									? 'border-primary bg-primary/10 ring-2 ring-primary/20 shadow-md'
									: 'border-base-content/15 bg-base-100/40 hover:bg-base-100/70'}"
								onclick={() => (editMode = 'RealExam')}
							>
								<div class="flex items-center justify-between mb-2">
									<span class="badge badge-primary badge-sm font-semibold">Proctored</span>
									<ShieldAlert class="h-4 w-4 text-primary" />
								</div>
								<h4 class="font-bold text-sm text-base-content mb-1">Real Examination</h4>
								<p class="text-[11px] text-base-content/65">
									Webcam snapshot proctoring, anti-cheat detection, violation limits, and locked window.
								</p>
							</button>

							<button
								type="button"
								class="p-4 rounded-2xl border text-left transition-all {editMode === 'Simulation'
									? 'border-secondary bg-secondary/10 ring-2 ring-secondary/20 shadow-md'
									: 'border-base-content/15 bg-base-100/40 hover:bg-base-100/70'}"
								onclick={() => (editMode = 'Simulation')}
							>
								<div class="flex items-center justify-between mb-2">
									<span class="badge badge-secondary badge-sm font-semibold">Self-Paced</span>
									<Clock class="h-4 w-4 text-secondary" />
								</div>
								<h4 class="font-bold text-sm text-base-content mb-1">Practice Simulation</h4>
								<p class="text-[11px] text-base-content/65">
									Relaxed test environment for candidate preparation without active proctoring.
								</p>
							</button>
						</div>
					</div>

					<!-- Numerical Parameters Grid -->
					<div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
						<div class="space-y-1.5">
							<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block" for="ex-dur">
								Duration (Minutes) <span class="text-error">*</span>
							</label>
							<input
								id="ex-dur"
								type="number"
								min="1"
								class="input input-bordered w-full rounded-2xl h-11 bg-base-100/70 border-base-content/20 text-sm font-semibold"
								bind:value={editDurationMinutes}
								required
							/>
						</div>

						<div class="space-y-1.5">
							<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block" for="ex-pass">
								Passing Score (%) <span class="text-error">*</span>
							</label>
							<input
								id="ex-pass"
								type="number"
								min="0"
								max="100"
								class="input input-bordered w-full rounded-2xl h-11 bg-base-100/70 border-base-content/20 text-sm font-semibold"
								bind:value={editPassingScore}
								required
							/>
						</div>

						{#if editMode === 'RealExam'}
							<div class="space-y-1.5">
								<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block" for="ex-viol">
									Max Allowed Violations <span class="text-error">*</span>
								</label>
								<input
									id="ex-viol"
									type="number"
									min="1"
									class="input input-bordered w-full rounded-2xl h-11 bg-base-100/70 border-base-content/20 text-sm font-semibold"
									bind:value={editMaxViolations}
									required
								/>
							</div>
						{/if}
					</div>

					<!-- Shuffle Options -->
					<div class="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-2">
						<label class="flex items-center justify-between p-3 rounded-2xl bg-base-100/40 border border-white/5 cursor-pointer">
							<div class="flex items-center gap-2 text-xs font-semibold">
								<Shuffle class="h-4 w-4 text-primary" />
								<span>Shuffle Questions per Candidate</span>
							</div>
							<input type="checkbox" class="toggle toggle-primary toggle-sm" bind:checked={editShuffleQuestions} />
						</label>

						<label class="flex items-center justify-between p-3 rounded-2xl bg-base-100/40 border border-white/5 cursor-pointer">
							<div class="flex items-center gap-2 text-xs font-semibold">
								<Shuffle class="h-4 w-4 text-secondary" />
								<span>Shuffle Options Choices</span>
							</div>
							<input type="checkbox" class="toggle toggle-secondary toggle-sm" bind:checked={editShuffleOptions} />
						</label>
					</div>

					<!-- Save Changes Button -->
					<div class="pt-4 border-t border-white/10 flex justify-end">
						<button
							type="submit"
							class="btn btn-primary gradient-accent rounded-xl text-white font-bold border-0 shadow-lg px-8 gap-2"
							disabled={isSavingSettings}
						>
							{#if isSavingSettings}
								<span class="loading loading-spinner loading-xs"></span>
							{:else}
								<Save class="h-4 w-4" />
							{/if}
							Save Parameters
						</button>
					</div>
				</form>
			</GlassCard>
		{/if}
	{/if}

	<!-- Add Question Modal -->
	<GlassModal
		isOpen={isAddQuestionModalOpen}
		title="Add Question to Bank"
		onClose={() => (isAddQuestionModalOpen = false)}
		maxWidth="max-w-2xl"
	>
		<div class="space-y-4">
			<!-- Question Type Cards -->
			<div class="space-y-1.5">
				<label class="text-xs font-semibold text-base-content/80">Question Type</label>
				<div class="grid grid-cols-2 sm:grid-cols-4 gap-2">
					{#each questionTypes as item}
						{@const isSelected = newQuestionType === item.id}
						<button
							type="button"
							class="flex items-center gap-2 p-2.5 rounded-xl border text-xs font-semibold transition-all {isSelected
								? 'bg-primary/15 border-primary text-primary shadow-xs ring-1 ring-primary/30'
								: 'bg-base-100/50 border-base-content/15 text-base-content/70 hover:bg-base-100 hover:text-base-content'}"
							onclick={() => handleTypeChange(item.id)}
						>
							<item.icon class="h-4 w-4 shrink-0 {isSelected ? 'text-primary' : 'text-base-content/50'}" />
							<span class="truncate">{item.label}</span>
						</button>
					{/each}
				</div>
			</div>

			<!-- Points -->
			<div class="space-y-1.5">
				<label class="text-xs font-semibold text-base-content/80" for="add-q-pts">Score Points</label>
				<input
					id="add-q-pts"
					type="number"
					min="1"
					class="input input-bordered input-sm h-10 w-full rounded-xl text-sm bg-base-100/70 border-base-content/20 text-base-content"
					bind:value={newQuestionPoints}
				/>
			</div>

			<!-- Question Prompt with Edra Editor -->
			<div class="space-y-1.5">
				<div class="flex items-center justify-between">
					<label class="text-xs font-semibold text-base-content/80">Question Prompt (Edra Editor / LaTeX Math)</label>
					<span class="badge badge-neutral badge-xs font-mono text-[10px]">WYSIWYG</span>
				</div>
				<RichEditor
					content={newQuestionText}
					minHeight="140px"
					placeholder="Write your question prompt with formatted code or LaTeX math..."
					onUpdate={(json) => {
						newQuestionText = json;
					}}
				/>
			</div>

			<!-- Option Choices Builder (for non-essay) -->
			{#if newQuestionType !== 'Essay'}
				<div class="space-y-2">
					<div class="flex items-center justify-between">
						<label class="text-xs font-semibold text-base-content/80">Option Choices & Answers</label>
						{#if newQuestionType !== 'TrueFalse'}
							<button
								type="button"
								class="btn btn-ghost btn-xs text-primary font-semibold"
								onclick={() => (newQuestionOptions = [...newQuestionOptions, { text: `Option ${newQuestionOptions.length + 1}`, isCorrect: false }])}
							>
								+ Add Option
							</button>
						{/if}
					</div>

					<div class="space-y-2">
						{#each newQuestionOptions as opt, optIdx}
							<div class="flex items-center gap-2">
								{#if newQuestionType === 'SingleChoice' || newQuestionType === 'TrueFalse'}
									<input
										type="radio"
										name="new-correct-opt"
										class="radio radio-primary radio-sm"
										checked={opt.isCorrect}
										onchange={() => {
											newQuestionOptions = newQuestionOptions.map((o, idx) => ({
												...o,
												isCorrect: idx === optIdx
											}));
										}}
									/>
								{:else}
									<input
										type="checkbox"
										class="checkbox checkbox-primary checkbox-sm"
										bind:checked={opt.isCorrect}
									/>
								{/if}

								<input
									type="text"
									class="input input-bordered input-sm h-10 flex-1 rounded-xl text-xs bg-base-100/70 border-base-content/20"
									placeholder={`Option choice ${optIdx + 1}`}
									bind:value={opt.text}
								/>

								{#if newQuestionType !== 'TrueFalse' && newQuestionOptions.length > 2}
									<button
										type="button"
										class="btn btn-ghost btn-xs text-error p-1"
										onclick={() => (newQuestionOptions = newQuestionOptions.filter((_, i) => i !== optIdx))}
									>
										<Trash2 class="h-3.5 w-3.5" />
									</button>
								{/if}
							</div>
						{/each}
					</div>
				</div>
			{/if}

			<!-- Explanation -->
			<div class="space-y-1.5">
				<label class="text-xs font-semibold text-base-content/80" for="add-q-exp">
					Answer Explanation & Solution Notes (Optional)
				</label>
				<input
					id="add-q-exp"
					type="text"
					class="input input-bordered input-sm h-10 w-full rounded-xl text-xs bg-base-100/70 border-base-content/20 text-base-content"
					placeholder="Displayed during review mode..."
					bind:value={newQuestionExplanation}
				/>
			</div>
		</div>

		{#snippet actions()}
			<button class="btn btn-ghost btn-sm rounded-xl" onclick={() => (isAddQuestionModalOpen = false)}>
				Cancel
			</button>
			<button
				class="btn btn-secondary gradient-accent btn-sm rounded-xl text-white font-semibold border-0"
				onclick={handleAddQuestion}
				disabled={isActionLoading || !newQuestionText.trim()}
			>
				Save Question
			</button>
		{/snippet}
	</GlassModal>

	<!-- Edit Question Modal -->
	<GlassModal
		isOpen={isEditQuestionModalOpen}
		title="Edit Question"
		onClose={() => (isEditQuestionModalOpen = false)}
		maxWidth="max-w-2xl"
	>
		<div class="space-y-4">
			<!-- Question Type Cards -->
			<div class="space-y-1.5">
				<label class="text-xs font-semibold text-base-content/80">Question Type</label>
				<div class="grid grid-cols-2 sm:grid-cols-4 gap-2">
					{#each questionTypes as item}
						{@const isSelected = editQuestionType === item.id}
						<button
							type="button"
							class="flex items-center gap-2 p-2.5 rounded-xl border text-xs font-semibold transition-all {isSelected
								? 'bg-primary/15 border-primary text-primary shadow-xs ring-1 ring-primary/30'
								: 'bg-base-100/50 border-base-content/15 text-base-content/70 hover:bg-base-100 hover:text-base-content'}"
							onclick={() => handleEditTypeChange(item.id)}
						>
							<item.icon class="h-4 w-4 shrink-0 {isSelected ? 'text-primary' : 'text-base-content/50'}" />
							<span class="truncate">{item.label}</span>
						</button>
					{/each}
				</div>
			</div>

			<!-- Points -->
			<div class="space-y-1.5">
				<label class="text-xs font-semibold text-base-content/80" for="edit-q-pts">Score Points</label>
				<input
					id="edit-q-pts"
					type="number"
					min="1"
					class="input input-bordered input-sm h-10 w-full rounded-xl text-sm bg-base-100/70 border-base-content/20 text-base-content"
					bind:value={editQuestionPoints}
				/>
			</div>

			<!-- Question Prompt with Edra Editor -->
			<div class="space-y-1.5">
				<div class="flex items-center justify-between">
					<label class="text-xs font-semibold text-base-content/80">Question Prompt (Edra Editor / LaTeX Math)</label>
					<span class="badge badge-neutral badge-xs font-mono text-[10px]">WYSIWYG</span>
				</div>
				<RichEditor
					content={editQuestionText}
					minHeight="140px"
					placeholder="Write your question prompt with formatted code or LaTeX math..."
					onUpdate={(json) => {
						editQuestionText = json;
					}}
				/>
			</div>

			<!-- Option Choices Builder (for non-essay) -->
			{#if editQuestionType !== 'Essay'}
				<div class="space-y-2">
					<div class="flex items-center justify-between">
						<label class="text-xs font-semibold text-base-content/80">Option Choices & Answers</label>
						{#if editQuestionType !== 'TrueFalse'}
							<button
								type="button"
								class="btn btn-ghost btn-xs text-primary font-semibold"
								onclick={() => (editQuestionOptions = [...editQuestionOptions, { text: `Option ${editQuestionOptions.length + 1}`, isCorrect: false }])}
							>
								+ Add Option
							</button>
						{/if}
					</div>

					<div class="space-y-2">
						{#each editQuestionOptions as opt, optIdx}
							<div class="flex items-center gap-2">
								{#if editQuestionType === 'SingleChoice' || editQuestionType === 'TrueFalse'}
									<input
										type="radio"
										name="edit-correct-opt"
										class="radio radio-primary radio-sm"
										checked={opt.isCorrect}
										onchange={() => {
											editQuestionOptions = editQuestionOptions.map((o, idx) => ({
												...o,
												isCorrect: idx === optIdx
											}));
										}}
									/>
								{:else}
									<input
										type="checkbox"
										class="checkbox checkbox-primary checkbox-sm"
										bind:checked={opt.isCorrect}
									/>
								{/if}

								<input
									type="text"
									class="input input-bordered input-sm h-10 flex-1 rounded-xl text-xs bg-base-100/70 border-base-content/20"
									placeholder={`Option choice ${optIdx + 1}`}
									bind:value={opt.text}
								/>

								{#if editQuestionType !== 'TrueFalse' && editQuestionOptions.length > 2}
									<button
										type="button"
										class="btn btn-ghost btn-xs text-error p-1"
										onclick={() => (editQuestionOptions = editQuestionOptions.filter((_, i) => i !== optIdx))}
									>
										<Trash2 class="h-3.5 w-3.5" />
									</button>
								{/if}
							</div>
						{/each}
					</div>
				</div>
			{/if}

			<!-- Explanation -->
			<div class="space-y-1.5">
				<label class="text-xs font-semibold text-base-content/80" for="edit-q-exp">
					Answer Explanation & Solution Notes (Optional)
				</label>
				<input
					id="edit-q-exp"
					type="text"
					class="input input-bordered input-sm h-10 w-full rounded-xl text-xs bg-base-100/70 border-base-content/20 text-base-content"
					placeholder="Displayed during review mode..."
					bind:value={editQuestionExplanation}
				/>
			</div>
		</div>

		{#snippet actions()}
			<button class="btn btn-ghost btn-sm rounded-xl" onclick={() => (isEditQuestionModalOpen = false)}>
				Cancel
			</button>
			<button
				class="btn btn-secondary gradient-accent btn-sm rounded-xl text-white font-semibold border-0"
				onclick={handleUpdateQuestion}
				disabled={isActionLoading || !editQuestionText.trim()}
			>
				Update Question
			</button>
		{/snippet}
	</GlassModal>

	<!-- Delete Question Confirmation -->
	<ConfirmModal
		isOpen={isDeleteQuestionModalOpen}
		title="Delete Question"
		message={`Are you sure you want to delete this question?`}
		confirmText="Delete Question"
		isDanger={true}
		isLoading={isActionLoading}
		onConfirm={handleDeleteQuestion}
		onCancel={() => (isDeleteQuestionModalOpen = false)}
	/>

	<!-- Delete Exam Confirmation -->
	<ConfirmModal
		isOpen={isDeleteExamModalOpen}
		title="Delete Examination"
		message={`Are you sure you want to permanently delete "${exam?.title || 'this examination'}"? All questions, student submissions, and proctoring snapshots will be cascade removed.`}
		confirmText="Permanently Delete Exam"
		isDanger={true}
		isLoading={isActionLoading}
		onConfirm={handleDeleteExam}
		onCancel={() => (isDeleteExamModalOpen = false)}
	/>
</div>
