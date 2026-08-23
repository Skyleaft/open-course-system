<script lang="ts">
	import { examsApi } from '#lib/api/exams.ts';
	import type { QuizExam, QuizQuestion, QuestionType } from '#lib/api/types.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import GlassModal from '#lib/components/ui/GlassModal.svelte';
	import ConfirmModal from '#lib/components/ui/ConfirmModal.svelte';
	import RichEditor from '#lib/components/editor/RichEditor.svelte';
	import RichRenderer from '#lib/components/editor/RichRenderer.svelte';
	import { toast } from '#lib/stores/toast.svelte.ts';
	import {
		Plus,
		Search,
		HelpCircle,
		CheckCircle2,
		CheckSquare,
		ListFilter,
		AlignLeft,
		Trash2,
		Edit3,
		Copy,
		Sparkles,
		Layers,
		Check,
		Filter,
		ArrowRight,
		BookOpen
	} from '@lucide/svelte';
	import { onMount } from 'svelte';

	let exams = $state<QuizExam[]>([]);
	let selectedExamId = $state<string>('All');
	let selectedType = $state<string>('All');
	let searchTerm = $state('');
	let isLoading = $state(true);
	let isActionLoading = $state(false);

	// All gathered questions across exams
	let allQuestions = $state<Array<QuizQuestion & { examTitle?: string; examId: string }>>([]);

	// Filtered questions
	const filteredQuestions = $derived(
		allQuestions.filter((q) => {
			const matchExam = selectedExamId === 'All' || q.examId === selectedExamId;
			const matchType = selectedType === 'All' || q.type === selectedType;
			const text = (q.questionText || q.text || '').toLowerCase();
			const matchSearch = !searchTerm.trim() || text.includes(searchTerm.toLowerCase().trim());
			return matchExam && matchType && matchSearch;
		})
	);

	// Stats
	const totalPoints = $derived(filteredQuestions.reduce((acc, q) => acc + (q.points || 0), 0));
	const singleChoiceCount = $derived(filteredQuestions.filter((q) => q.type === 'SingleChoice').length);
	const multipleChoiceCount = $derived(filteredQuestions.filter((q) => q.type === 'MultipleChoice').length);
	const essayCount = $derived(filteredQuestions.filter((q) => q.type === 'Essay').length);

	// Question Types List
	const questionTypes: { id: QuestionType; label: string; icon: any }[] = [
		{ id: 'SingleChoice', label: 'Single Choice', icon: CheckCircle2 },
		{ id: 'MultipleChoice', label: 'Multiple Choice', icon: CheckSquare },
		{ id: 'TrueFalse', label: 'True / False', icon: ListFilter },
		{ id: 'Essay', label: 'Essay', icon: AlignLeft }
	];

	// Create Question Modal
	let isCreateModalOpen = $state(false);
	let targetExamId = $state<string>('');
	let newText = $state('');
	let newType = $state<QuestionType>('SingleChoice');
	let newPoints = $state(5);
	let newExplanation = $state('');
	let newOptions = $state<Array<{ text: string; isCorrect: boolean }>>([
		{ text: 'Option A', isCorrect: true },
		{ text: 'Option B', isCorrect: false },
		{ text: 'Option C', isCorrect: false },
		{ text: 'Option D', isCorrect: false }
	]);

	// Edit Question Modal
	let isEditModalOpen = $state(false);
	let editingQuestionId = $state<string | null>(null);
	let editText = $state('');
	let editType = $state<QuestionType>('SingleChoice');
	let editPoints = $state(5);
	let editExplanation = $state('');
	let editOptions = $state<Array<{ id?: string; text: string; isCorrect: boolean }>>([]);

	// Delete Question Modal
	let isDeleteModalOpen = $state(false);
	let deletingQuestionId = $state<string | null>(null);

	onMount(async () => {
		await loadAllData();
	});

	async function loadAllData() {
		isLoading = true;
		try {
			const res = await examsApi.listExams({ pageSize: 100 });
			exams = res.items || [];

			// Fetch full exam details for each to assemble full Question Bank
			const questionsList: Array<QuizQuestion & { examTitle?: string; examId: string }> = [];
			for (const exam of exams) {
				try {
					const fullExam = await examsApi.getExamById(exam.id);
					if (fullExam && fullExam.questions) {
						for (const q of fullExam.questions) {
							questionsList.push({
								...q,
								examId: exam.id,
								examTitle: exam.title
							});
						}
					}
				} catch {
					// continue
				}
			}
			allQuestions = questionsList;
			if (exams.length > 0 && !targetExamId) {
				targetExamId = exams[0].id;
			}
		} catch (err: any) {
			toast.error(err?.message || 'Failed to load question banks.');
		} finally {
			isLoading = false;
		}
	}

	function openCreateQuestion() {
		if (exams.length === 0) {
			toast.warning('Please create an examination first before adding questions.');
			return;
		}
		newText = '';
		newType = 'SingleChoice';
		newPoints = 5;
		newExplanation = '';
		newOptions = [
			{ text: 'Option A', isCorrect: true },
			{ text: 'Option B', isCorrect: false },
			{ text: 'Option C', isCorrect: false },
			{ text: 'Option D', isCorrect: false }
		];
		if (!targetExamId && exams.length > 0) {
			targetExamId = exams[0].id;
		}
		isCreateModalOpen = true;
	}

	function handleNewTypeChange(type: QuestionType) {
		newType = type;
		if (type === 'TrueFalse') {
			newOptions = [
				{ text: 'True', isCorrect: true },
				{ text: 'False', isCorrect: false }
			];
		} else if (type === 'Essay') {
			newOptions = [];
		} else if (newOptions.length === 0) {
			newOptions = [
				{ text: 'Option A', isCorrect: true },
				{ text: 'Option B', isCorrect: false }
			];
		}
	}

	function handleEditTypeChange(type: QuestionType) {
		editType = type;
		if (type === 'TrueFalse') {
			editOptions = [
				{ text: 'True', isCorrect: true },
				{ text: 'False', isCorrect: false }
			];
		} else if (type === 'Essay') {
			editOptions = [];
		} else if (editOptions.length === 0) {
			editOptions = [
				{ text: 'Option A', isCorrect: true },
				{ text: 'Option B', isCorrect: false }
			];
		}
	}

	async function handleSaveNewQuestion() {
		if (!targetExamId) {
			toast.warning('Please select a target examination.');
			return;
		}
		if (!newText.trim()) {
			toast.warning('Please enter the question text or prompt.');
			return;
		}
		if (newType !== 'Essay' && newOptions.length < 2) {
			toast.warning('Please provide at least 2 option choices.');
			return;
		}
		if (newType !== 'Essay' && !newOptions.some((o) => o.isCorrect)) {
			toast.warning('Please select at least one correct option.');
			return;
		}

		isActionLoading = true;
		try {
			await examsApi.addQuestion(targetExamId, {
				questionText: newText.trim(),
				type: newType,
				points: Number(newPoints) || 1,
				explanation: newExplanation.trim() || undefined,
				options: newType === 'Essay' ? [] : newOptions
			});
			toast.success('Question added to bank successfully!');
			isCreateModalOpen = false;
			await loadAllData();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to add question.');
		} finally {
			isActionLoading = false;
		}
	}

	function openEditQuestion(q: QuizQuestion & { examId: string }) {
		editingQuestionId = q.id;
		editText = q.questionText || q.text || '';
		editType = q.type || 'SingleChoice';
		editPoints = q.points || 5;
		editExplanation = q.explanation || '';
		editOptions = (q.options || []).map((o) => ({
			id: o.id,
			text: o.text,
			isCorrect: o.isCorrect ?? false
		}));
		isEditModalOpen = true;
	}

	async function handleUpdateQuestion() {
		if (!editingQuestionId || !editText.trim()) {
			toast.warning('Please provide the question prompt.');
			return;
		}
		if (editType !== 'Essay' && editOptions.length < 2) {
			toast.warning('Please provide at least 2 option choices.');
			return;
		}
		if (editType !== 'Essay' && !editOptions.some((o) => o.isCorrect)) {
			toast.warning('Please select at least one correct option.');
			return;
		}

		isActionLoading = true;
		try {
			await examsApi.updateQuestion(editingQuestionId, {
				questionText: editText.trim(),
				type: editType,
				points: Number(editPoints) || 1,
				explanation: editExplanation.trim() || undefined,
				options: editType === 'Essay' ? [] : editOptions
			});
			toast.success('Question updated successfully!');
			isEditModalOpen = false;
			await loadAllData();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to update question.');
		} finally {
			isActionLoading = false;
		}
	}

	function openDeleteQuestion(q: QuizQuestion) {
		deletingQuestionId = q.id;
		isDeleteModalOpen = true;
	}

	async function handleDeleteQuestion() {
		if (!deletingQuestionId) return;
		isActionLoading = true;
		try {
			await examsApi.deleteQuestion(deletingQuestionId);
			toast.success('Question removed from bank.');
			isDeleteModalOpen = false;
			await loadAllData();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to delete question.');
		} finally {
			isActionLoading = false;
		}
	}

	async function handleDuplicateQuestion(q: QuizQuestion & { examId: string }) {
		isActionLoading = true;
		try {
			await examsApi.addQuestion(q.examId, {
				questionText: `${q.questionText || q.text || ''} (Copy)`,
				type: q.type,
				points: q.points,
				explanation: q.explanation,
				options: (q.options || []).map((o) => ({ text: o.text, isCorrect: !!o.isCorrect }))
			});
			toast.success('Question duplicated!');
			await loadAllData();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to duplicate question.');
		} finally {
			isActionLoading = false;
		}
	}
</script>

<div class="space-y-8">
	<!-- Top Banner -->
	<div class="glass-panel relative overflow-hidden rounded-3xl border border-white/10 p-8 shadow-2xl backdrop-blur-2xl">
		<div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
			<div class="space-y-2">
				<div class="inline-flex items-center gap-2 rounded-lg bg-secondary/10 border border-secondary/20 px-3 py-1 text-xs font-semibold text-secondary">
					<Sparkles class="h-3.5 w-3.5" />
					Master Question Repository
				</div>
				<h1 class="text-3xl font-extrabold tracking-tight text-base-content sm:text-4xl">
					Question Banks
				</h1>
				<p class="text-xs text-base-content/70 sm:text-sm max-w-xl">
					Author, manage, filter, and reuse questions with rich text, code formatting, and LaTeX mathematics.
				</p>
			</div>

			<button
				class="btn btn-secondary gradient-accent btn-md rounded-2xl text-white font-bold border-0 shadow-lg gap-2 self-start sm:self-auto"
				onclick={openCreateQuestion}
			>
				<Plus class="h-5 w-5" />
				Add New Question
			</button>
		</div>
	</div>

	<!-- Stats Overview Grid -->
	<div class="grid grid-cols-2 sm:grid-cols-4 gap-4">
		<GlassCard class="p-4 flex items-center gap-3">
			<div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-primary/15 text-primary">
				<Layers class="h-5 w-5" />
			</div>
			<div>
				<div class="text-lg font-black text-base-content">{filteredQuestions.length}</div>
				<div class="text-[11px] text-base-content/60 font-medium">Total Questions</div>
			</div>
		</GlassCard>

		<GlassCard class="p-4 flex items-center gap-3">
			<div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-success/15 text-success">
				<CheckCircle2 class="h-5 w-5" />
			</div>
			<div>
				<div class="text-lg font-black text-base-content">{singleChoiceCount}</div>
				<div class="text-[11px] text-base-content/60 font-medium">Single Choice</div>
			</div>
		</GlassCard>

		<GlassCard class="p-4 flex items-center gap-3">
			<div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-secondary/15 text-secondary">
				<CheckSquare class="h-5 w-5" />
			</div>
			<div>
				<div class="text-lg font-black text-base-content">{multipleChoiceCount}</div>
				<div class="text-[11px] text-base-content/60 font-medium">Multiple Choice</div>
			</div>
		</GlassCard>

		<GlassCard class="p-4 flex items-center gap-3">
			<div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-warning/15 text-warning">
				<Sparkles class="h-5 w-5" />
			</div>
			<div>
				<div class="text-lg font-black text-base-content">{totalPoints} pts</div>
				<div class="text-[11px] text-base-content/60 font-medium">Total Score Value</div>
			</div>
		</GlassCard>
	</div>

	<!-- Search & Filters Toolbar -->
	<div class="flex flex-col md:flex-row md:items-center justify-between gap-3">
		<div class="relative flex-1 max-w-md">
			<Search class="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-base-content/40" />
			<input
				type="text"
				class="input input-sm h-11 pl-10 w-full rounded-2xl bg-base-100/60 border-base-content/15 text-sm focus:border-primary"
				placeholder="Search questions by prompt or keyword..."
				bind:value={searchTerm}
			/>
		</div>

		<div class="flex flex-wrap items-center gap-2">
			<!-- Exam Filter -->
			<select
				class="select select-sm h-11 rounded-2xl bg-base-100/60 border-base-content/15 text-xs font-medium"
				bind:value={selectedExamId}
			>
				<option value="All">All Exams & Banks ({exams.length})</option>
				{#each exams as ex}
					<option value={ex.id}>{ex.title}</option>
				{/each}
			</select>

			<!-- Type Filter -->
			<select
				class="select select-sm h-11 rounded-2xl bg-base-100/60 border-base-content/15 text-xs font-medium"
				bind:value={selectedType}
			>
				<option value="All">All Question Types</option>
				<option value="SingleChoice">Single Choice</option>
				<option value="MultipleChoice">Multiple Choice</option>
				<option value="TrueFalse">True / False</option>
				<option value="Essay">Essay</option>
			</select>
		</div>
	</div>

	<!-- Questions List -->
	{#if isLoading}
		<div class="space-y-4">
			<div class="glass-panel h-36 rounded-3xl animate-pulse"></div>
			<div class="glass-panel h-36 rounded-3xl animate-pulse"></div>
		</div>
	{:else if filteredQuestions.length > 0}
		<div class="space-y-4">
			{#each filteredQuestions as q, idx (q.id || idx)}
				<GlassCard class="space-y-4 p-6 hover:border-secondary/30 transition-colors shadow-lg">
					<div class="flex flex-col sm:flex-row sm:items-center sm:justify-between border-b border-white/10 pb-3 gap-2">
						<div class="flex items-center gap-3">
							<span class="gradient-accent flex h-7 w-7 shrink-0 items-center justify-center rounded-xl text-xs font-bold text-white">
								{idx + 1}
							</span>
							<div class="flex flex-wrap items-center gap-2">
								<span class="badge badge-neutral badge-xs font-semibold uppercase">{q.type}</span>
								<span class="badge badge-ghost badge-xs font-mono">{q.points} pts</span>
								<span class="badge badge-primary/20 text-primary border-primary/30 badge-xs font-medium">
									{q.examTitle || 'Standalone Exam'}
								</span>
							</div>
						</div>

						<div class="flex items-center gap-1 self-end sm:self-auto">
							<a
								href="/instructor/exams/{q.examId}/edit"
								class="btn btn-ghost btn-xs text-base-content/70 hover:text-primary rounded-lg p-1.5"
								title="Open in Exam Studio"
							>
								<BookOpen class="h-3.5 w-3.5" />
							</a>
							<button
								class="btn btn-ghost btn-xs text-base-content/70 hover:text-secondary rounded-lg p-1.5"
								title="Duplicate Question"
								onclick={() => handleDuplicateQuestion(q)}
								disabled={isActionLoading}
							>
								<Copy class="h-3.5 w-3.5" />
							</button>
							<button
								class="btn btn-ghost btn-xs text-base-content/70 hover:text-secondary rounded-lg p-1.5"
								title="Edit Question"
								onclick={() => openEditQuestion(q)}
							>
								<Edit3 class="h-3.5 w-3.5" />
							</button>
							<button
								class="btn btn-ghost btn-xs text-base-content/70 hover:text-error rounded-lg p-1.5"
								title="Delete Question"
								onclick={() => openDeleteQuestion(q)}
							>
								<Trash2 class="h-3.5 w-3.5" />
							</button>
						</div>
					</div>

					<!-- Question Prompt with RichRenderer -->
					<div class="text-sm font-medium text-base-content leading-relaxed">
						<RichRenderer content={q.questionText || q.text || ''} />
					</div>

					<!-- Option Choices (if not essay) -->
					{#if q.type !== 'Essay'}
						<div class="grid grid-cols-1 sm:grid-cols-2 gap-2 pt-1">
							{#each q.options || [] as opt}
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
							Open-ended essay question graded via rubric.
						</div>
					{/if}

					{#if q.explanation}
						<div class="rounded-xl bg-info/10 border border-info/20 p-3 text-xs text-info flex items-start gap-2">
							<HelpCircle class="h-4 w-4 shrink-0 mt-0.5" />
							<div>
								<span class="font-bold block">Solution Explanation:</span>
								<span class="opacity-90">{q.explanation}</span>
							</div>
						</div>
					{/if}
				</GlassCard>
			{/each}
		</div>
	{:else}
		<div class="glass-card p-14 text-center rounded-3xl border border-white/5 space-y-3">
			<Layers class="h-10 w-10 text-secondary mx-auto opacity-50" />
			<h3 class="text-base font-bold">No Questions Found</h3>
			<p class="text-xs text-base-content/60 max-w-sm mx-auto">
				No questions match your current filters. Create questions with formatted LaTeX and rich options.
			</p>
			<button
				class="btn btn-secondary gradient-accent btn-sm rounded-xl text-white font-bold border-0 shadow-md gap-1.5"
				onclick={openCreateQuestion}
			>
				<Plus class="h-4 w-4" />
				Create New Question
			</button>
		</div>
	{/if}

	<!-- Create Question Modal -->
	<GlassModal
		isOpen={isCreateModalOpen}
		title="Create Question in Bank"
		onClose={() => (isCreateModalOpen = false)}
		maxWidth="max-w-2xl"
	>
		<div class="space-y-4">
			<!-- Target Exam Selector -->
			<div class="space-y-1.5">
				<label class="text-xs font-semibold text-base-content/80" for="t-exam">Target Examination</label>
				<select
					id="t-exam"
					class="select select-bordered select-sm h-10 w-full rounded-xl text-xs bg-base-100/70 border-base-content/20"
					bind:value={targetExamId}
				>
					{#each exams as ex}
						<option value={ex.id}>{ex.title}</option>
					{/each}
				</select>
			</div>

			<!-- Question Type Cards -->
			<div class="space-y-1.5">
				<label class="text-xs font-semibold text-base-content/80">Question Type</label>
				<div class="grid grid-cols-2 sm:grid-cols-4 gap-2">
					{#each questionTypes as item}
						{@const isSelected = newType === item.id}
						<button
							type="button"
							class="flex items-center gap-2 p-2.5 rounded-xl border text-xs font-semibold transition-all {isSelected
								? 'bg-secondary/15 border-secondary text-secondary shadow-xs ring-1 ring-secondary/30'
								: 'bg-base-100/50 border-base-content/15 text-base-content/70 hover:bg-base-100 hover:text-base-content'}"
							onclick={() => handleNewTypeChange(item.id)}
						>
							<item.icon class="h-4 w-4 shrink-0 {isSelected ? 'text-secondary' : 'text-base-content/50'}" />
							<span class="truncate">{item.label}</span>
						</button>
					{/each}
				</div>
			</div>

			<!-- Points -->
			<div class="space-y-1.5">
				<label class="text-xs font-semibold text-base-content/80" for="add-pts">Score Points</label>
				<input
					id="add-pts"
					type="number"
					min="1"
					class="input input-bordered input-sm h-10 w-full rounded-xl text-sm bg-base-100/70 border-base-content/20 text-base-content"
					bind:value={newPoints}
				/>
			</div>

			<!-- Question Prompt with Edra Editor -->
			<div class="space-y-1.5">
				<div class="flex items-center justify-between">
					<label class="text-xs font-semibold text-base-content/80">Question Prompt (Edra Editor / LaTeX Math)</label>
					<span class="badge badge-neutral badge-xs font-mono text-[10px]">WYSIWYG</span>
				</div>
				<RichEditor
					content={newText}
					minHeight="140px"
					placeholder="Write your question prompt with formatted code or LaTeX math..."
					onUpdate={(json) => {
						newText = json;
					}}
				/>
			</div>

			<!-- Option Choices Builder (for non-essay) -->
			{#if newType !== 'Essay'}
				<div class="space-y-2">
					<div class="flex items-center justify-between">
						<label class="text-xs font-semibold text-base-content/80">Option Choices & Answers</label>
						{#if newType !== 'TrueFalse'}
							<button
								type="button"
								class="btn btn-ghost btn-xs text-secondary font-semibold"
								onclick={() => (newOptions = [...newOptions, { text: `Option ${newOptions.length + 1}`, isCorrect: false }])}
							>
								+ Add Option
							</button>
						{/if}
					</div>

					<div class="space-y-2">
						{#each newOptions as opt, optIdx}
							<div class="flex items-center gap-2">
								{#if newType === 'SingleChoice' || newType === 'TrueFalse'}
									<input
										type="radio"
										name="create-correct-opt"
										class="radio radio-secondary radio-sm"
										checked={opt.isCorrect}
										onchange={() => {
											newOptions = newOptions.map((o, idx) => ({
												...o,
												isCorrect: idx === optIdx
											}));
										}}
									/>
								{:else}
									<input
										type="checkbox"
										class="checkbox checkbox-secondary checkbox-sm"
										bind:checked={opt.isCorrect}
									/>
								{/if}

								<input
									type="text"
									class="input input-bordered input-sm h-10 flex-1 rounded-xl text-xs bg-base-100/70 border-base-content/20"
									placeholder={`Option choice ${optIdx + 1}`}
									bind:value={opt.text}
								/>

								{#if newType !== 'TrueFalse' && newOptions.length > 2}
									<button
										type="button"
										class="btn btn-ghost btn-xs text-error p-1"
										onclick={() => (newOptions = newOptions.filter((_, i) => i !== optIdx))}
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
				<label class="text-xs font-semibold text-base-content/80" for="add-exp">
					Answer Explanation (Optional)
				</label>
				<input
					id="add-exp"
					type="text"
					class="input input-bordered input-sm h-10 w-full rounded-xl text-xs bg-base-100/70 border-base-content/20 text-base-content"
					placeholder="Solution breakdown notes..."
					bind:value={newExplanation}
				/>
			</div>
		</div>

		{#snippet actions()}
			<button class="btn btn-ghost btn-sm rounded-xl" onclick={() => (isCreateModalOpen = false)}>
				Cancel
			</button>
			<button
				class="btn btn-secondary gradient-accent btn-sm rounded-xl text-white font-semibold border-0"
				onclick={handleSaveNewQuestion}
				disabled={isActionLoading || !newText.trim()}
			>
				Save Question
			</button>
		{/snippet}
	</GlassModal>

	<!-- Edit Question Modal -->
	<GlassModal
		isOpen={isEditModalOpen}
		title="Edit Question"
		onClose={() => (isEditModalOpen = false)}
		maxWidth="max-w-2xl"
	>
		<div class="space-y-4">
			<!-- Question Type Cards -->
			<div class="space-y-1.5">
				<label class="text-xs font-semibold text-base-content/80">Question Type</label>
				<div class="grid grid-cols-2 sm:grid-cols-4 gap-2">
					{#each questionTypes as item}
						{@const isSelected = editType === item.id}
						<button
							type="button"
							class="flex items-center gap-2 p-2.5 rounded-xl border text-xs font-semibold transition-all {isSelected
								? 'bg-secondary/15 border-secondary text-secondary shadow-xs ring-1 ring-secondary/30'
								: 'bg-base-100/50 border-base-content/15 text-base-content/70 hover:bg-base-100 hover:text-base-content'}"
							onclick={() => handleEditTypeChange(item.id)}
						>
							<item.icon class="h-4 w-4 shrink-0 {isSelected ? 'text-secondary' : 'text-base-content/50'}" />
							<span class="truncate">{item.label}</span>
						</button>
					{/each}
				</div>
			</div>

			<!-- Points -->
			<div class="space-y-1.5">
				<label class="text-xs font-semibold text-base-content/80" for="edit-pts">Score Points</label>
				<input
					id="edit-pts"
					type="number"
					min="1"
					class="input input-bordered input-sm h-10 w-full rounded-xl text-sm bg-base-100/70 border-base-content/20 text-base-content"
					bind:value={editPoints}
				/>
			</div>

			<!-- Question Prompt with Edra Editor -->
			<div class="space-y-1.5">
				<div class="flex items-center justify-between">
					<label class="text-xs font-semibold text-base-content/80">Question Prompt (Edra Editor / LaTeX Math)</label>
					<span class="badge badge-neutral badge-xs font-mono text-[10px]">WYSIWYG</span>
				</div>
				<RichEditor
					content={editText}
					minHeight="140px"
					placeholder="Write your question prompt with formatted code or LaTeX math..."
					onUpdate={(json) => {
						editText = json;
					}}
				/>
			</div>

			<!-- Option Choices Builder (for non-essay) -->
			{#if editType !== 'Essay'}
				<div class="space-y-2">
					<div class="flex items-center justify-between">
						<label class="text-xs font-semibold text-base-content/80">Option Choices & Answers</label>
						{#if editType !== 'TrueFalse'}
							<button
								type="button"
								class="btn btn-ghost btn-xs text-secondary font-semibold"
								onclick={() => (editOptions = [...editOptions, { text: `Option ${editOptions.length + 1}`, isCorrect: false }])}
							>
								+ Add Option
							</button>
						{/if}
					</div>

					<div class="space-y-2">
						{#each editOptions as opt, optIdx}
							<div class="flex items-center gap-2">
								{#if editType === 'SingleChoice' || editType === 'TrueFalse'}
									<input
										type="radio"
										name="edit-bk-opt"
										class="radio radio-secondary radio-sm"
										checked={opt.isCorrect}
										onchange={() => {
											editOptions = editOptions.map((o, idx) => ({
												...o,
												isCorrect: idx === optIdx
											}));
										}}
									/>
								{:else}
									<input
										type="checkbox"
										class="checkbox checkbox-secondary checkbox-sm"
										bind:checked={opt.isCorrect}
									/>
								{/if}

								<input
									type="text"
									class="input input-bordered input-sm h-10 flex-1 rounded-xl text-xs bg-base-100/70 border-base-content/20"
									placeholder={`Option choice ${optIdx + 1}`}
									bind:value={opt.text}
								/>

								{#if editType !== 'TrueFalse' && editOptions.length > 2}
									<button
										type="button"
										class="btn btn-ghost btn-xs text-error p-1"
										onclick={() => (editOptions = editOptions.filter((_, i) => i !== optIdx))}
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
				<label class="text-xs font-semibold text-base-content/80" for="edit-bk-exp">
					Answer Explanation (Optional)
				</label>
				<input
					id="edit-bk-exp"
					type="text"
					class="input input-bordered input-sm h-10 w-full rounded-xl text-xs bg-base-100/70 border-base-content/20 text-base-content"
					placeholder="Solution breakdown notes..."
					bind:value={editExplanation}
				/>
			</div>
		</div>

		{#snippet actions()}
			<button class="btn btn-ghost btn-sm rounded-xl" onclick={() => (isEditModalOpen = false)}>
				Cancel
			</button>
			<button
				class="btn btn-secondary gradient-accent btn-sm rounded-xl text-white font-semibold border-0"
				onclick={handleUpdateQuestion}
				disabled={isActionLoading || !editText.trim()}
			>
				Update Question
			</button>
		{/snippet}
	</GlassModal>

	<!-- Delete Question Confirmation -->
	<ConfirmModal
		isOpen={isDeleteModalOpen}
		title="Delete Question from Bank"
		message="Are you sure you want to permanently delete this question from the bank?"
		confirmText="Delete Question"
		isDanger={true}
		isLoading={isActionLoading}
		onConfirm={handleDeleteQuestion}
		onCancel={() => (isDeleteModalOpen = false)}
	/>
</div>
