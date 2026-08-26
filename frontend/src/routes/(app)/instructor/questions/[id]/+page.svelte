<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import {
		BookOpen,
		Plus,
		ArrowLeft,
		Edit3,
		Trash2,
		Check,
		Layers,
		Sparkles,
		Tag,
		HelpCircle,
		Search,
		CheckSquare,
		CheckCircle2,
		FileText,
		FolderPlus,
		AlertCircle,
		Download,
		FileUp,
		ListFilter,
		ChevronDown,
		AlignLeft
	} from 'lucide-svelte';
	import { examsApi } from '$lib/api/exams.ts';
	import type { QuestionBank, BankQuestion, QuestionType, QuestionOption } from '$lib/api/types.ts';
	import { toast } from '$lib/stores/toast.svelte.ts';
	import GlassCard from '$lib/components/ui/GlassCard.svelte';
	import RichEditor from '$lib/components/editor/RichEditor.svelte';
	import RichRenderer from '$lib/components/editor/RichRenderer.svelte';

	const bankId = $derived(page.params.id);

	let bank = $state<QuestionBank | null>(null);
	let questions = $state<BankQuestion[]>([]);
	let isLoading = $state(true);
	let isActionLoading = $state(false);

	// Filters
	let searchTerm = $state('');
	let selectedType = $state<string>('All');

	// Delete Bank Modal
	let isDeleteBankModalOpen = $state(false);

	// Import Word Modal
	let isImportModalOpen = $state(false);
	let importFile = $state<File | null>(null);
	let isDownloadingTemplate = $state(false);

	// Create Question Modal
	let isCreateQuestionModalOpen = $state(false);
	let newQuestionText = $state('');
	let newQuestionType = $state<QuestionType>('SingleChoice');
	let newQuestionPoints = $state(5);
	let newQuestionExplanation = $state('');
	let newQuestionOptions = $state<Array<{ text: string; isCorrect: boolean }>>([
		{ text: 'Option A', isCorrect: true },
		{ text: 'Option B', isCorrect: false },
		{ text: 'Option C', isCorrect: false },
		{ text: 'Option D', isCorrect: false }
	]);

	// Edit Question Modal
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

	// Edit Bank Info Modal
	let isEditBankModalOpen = $state(false);
	let editBankTitle = $state('');
	let editBankCategory = $state('');
	let editBankDescription = $state('');
	let editBankTags = $state('');

	async function handleDownloadTemplate() {
		isDownloadingTemplate = true;
		try {
			const blob = await examsApi.downloadQuestionBankTemplate();
			const url = window.URL.createObjectURL(blob);
			const a = document.createElement('a');
			a.href = url;
			a.download = 'QuestionBank-Template.docx';
			document.body.appendChild(a);
			a.click();
			window.URL.revokeObjectURL(url);
			document.body.removeChild(a);
			toast.success('Question Bank Word Template downloaded.');
		} catch (err: any) {
			toast.error(err?.message || 'Failed to download Word template.');
		} finally {
			isDownloadingTemplate = false;
		}
	}

	async function handleImportQuestions(e: Event) {
		e.preventDefault();
		if (!importFile) {
			toast.warning('Please select a Word Document (.docx) file.');
			return;
		}
		if (!bankId) return;

		const formData = new FormData();
		formData.append('file', importFile);

		isActionLoading = true;
		try {
			const result = await examsApi.importQuestionBank(formData, bankId);
			toast.success(`Imported and appended ${result.totalImportedQuestions} questions into "${bank?.title || 'this bank'}"!`);
			if (result.warnings && result.warnings.length > 0) {
				toast.info(result.warnings.join(' | '));
			}
			isImportModalOpen = false;
			importFile = null;
			await loadBankDetails();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to import questions from Word document.');
		} finally {
			isActionLoading = false;
		}
	}

	const questionTypes: Array<{ id: QuestionType; label: string; icon: typeof CheckCircle2 }> = [
		{ id: 'SingleChoice', label: 'Single Choice', icon: CheckCircle2 },
		{ id: 'MultipleChoice', label: 'Multiple Choice', icon: CheckSquare },
		{ id: 'TrueFalse', label: 'True / False', icon: Sparkles },
		{ id: 'Essay', label: 'Essay Prompt', icon: FileText }
	];

	onMount(async () => {
		await loadBankDetails();
	});

	async function loadBankDetails() {
		if (!bankId) return;
		isLoading = true;
		try {
			const data = await examsApi.getQuestionBank(bankId);
			bank = data;
			questions = data.questions || [];
		} catch (err: any) {
			toast.error(err?.message || 'Failed to load Question Bank pool.');
		} finally {
			isLoading = false;
		}
	}

	// Filtered questions
	const filteredQuestions = $derived(
		questions.filter((q) => {
			const matchType = selectedType === 'All' || q.type === selectedType;
			const qText = (q.questionText || q.text || '').toLowerCase();
			const qExp = (q.explanation || '').toLowerCase();
			const matchSearch =
				!searchTerm.trim() ||
				qText.includes(searchTerm.toLowerCase().trim()) ||
				qExp.includes(searchTerm.toLowerCase().trim());
			return matchType && matchSearch;
		})
	);

	// Stats
	const totalPoints = $derived(questions.reduce((acc, q) => acc + (q.points || 0), 0));
	const singleChoiceCount = $derived(questions.filter((q) => q.type === 'SingleChoice').length);
	const multipleChoiceCount = $derived(questions.filter((q) => q.type === 'MultipleChoice').length);

	function openCreateQuestion() {
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
		isCreateQuestionModalOpen = true;
	}

	async function handleSaveNewQuestion(e: Event) {
		e.preventDefault();
		if (!newQuestionText.trim()) {
			toast.warning('Question prompt cannot be empty.');
			return;
		}

		if (newQuestionType === 'SingleChoice' || newQuestionType === 'MultipleChoice' || newQuestionType === 'TrueFalse') {
			const hasCorrect = newQuestionOptions.some((o) => o.isCorrect);
			if (!hasCorrect) {
				toast.warning('Please mark at least one option as the correct answer.');
				return;
			}
			const hasEmpty = newQuestionOptions.some((o) => !o.text.trim());
			if (hasEmpty) {
				toast.warning('All option choices must have text.');
				return;
			}
		}

		isActionLoading = true;
		try {
			await examsApi.addQuestion(bankId, {
				bankId,
				questionText: newQuestionText.trim(),
				type: newQuestionType,
				points: Number(newQuestionPoints),
				explanation: newQuestionExplanation.trim() || undefined,
				options: newQuestionOptions.map((o) => ({ text: o.text.trim(), isCorrect: o.isCorrect }))
			});

			toast.success('Question added to Question Bank pool successfully!');
			isCreateQuestionModalOpen = false;
			await loadBankDetails();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to add question.');
		} finally {
			isActionLoading = false;
		}
	}

	function openEditQuestion(question: BankQuestion) {
		editingQuestionId = question.id;
		editQuestionText = question.questionText || question.text || '';
		editQuestionType = question.type;
		editQuestionPoints = question.points || 5;
		editQuestionExplanation = question.explanation || '';
		editQuestionOptions = (question.options || []).map((o) => ({
			id: o.id,
			text: o.text,
			isCorrect: Boolean(o.isCorrect)
		}));
		isEditQuestionModalOpen = true;
	}

	async function handleUpdateQuestion(e: Event) {
		e.preventDefault();
		if (!editingQuestionId) return;
		if (!editQuestionText.trim()) {
			toast.warning('Question prompt cannot be empty.');
			return;
		}

		isActionLoading = true;
		try {
			await examsApi.updateQuestion(editingQuestionId, {
				questionText: editQuestionText.trim(),
				type: editQuestionType,
				points: Number(editQuestionPoints),
				explanation: editQuestionExplanation.trim() || undefined,
				options: editQuestionOptions.map((o) => ({
					id: o.id,
					text: o.text.trim(),
					isCorrect: o.isCorrect
				}))
			});

			toast.success('Question updated successfully!');
			isEditQuestionModalOpen = false;
			await loadBankDetails();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to update question.');
		} finally {
			isActionLoading = false;
		}
	}

	function confirmDeleteQuestion(id: string) {
		deletingQuestionId = id;
		isDeleteQuestionModalOpen = true;
	}

	async function handleDeleteQuestion() {
		if (!deletingQuestionId) return;
		isActionLoading = true;
		try {
			await examsApi.deleteQuestion(deletingQuestionId);
			toast.success('Question removed from pool successfully.');
			isDeleteQuestionModalOpen = false;
			await loadBankDetails();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to delete question.');
		} finally {
			isActionLoading = false;
		}
	}

	function openEditBankModal() {
		if (!bank) return;
		editBankTitle = bank.title;
		editBankCategory = bank.category || '';
		editBankDescription = bank.description || '';
		editBankTags = (bank.tags || []).join(', ');
		isEditBankModalOpen = true;
	}

	async function handleSaveBankInfo() {
		if (!bankId || !editBankTitle.trim()) {
			toast.warning('Pool title cannot be empty.');
			return;
		}

		const tagsList = editBankTags
			.split(',')
			.map((t) => t.trim())
			.filter(Boolean);

		isActionLoading = true;
		try {
			await examsApi.updateQuestionBank(bankId, {
				title: editBankTitle.trim(),
				category: editBankCategory.trim() || undefined,
				description: editBankDescription.trim() || undefined,
				tags: tagsList
			});
			toast.success('Question Bank pool updated successfully.');
			isEditBankModalOpen = false;
			await loadBankDetails();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to update pool details.');
		} finally {
			isActionLoading = false;
		}
	}

	async function handleDeleteBank() {
		if (!bankId) return;
		isActionLoading = true;
		try {
			await examsApi.deleteQuestionBank(bankId);
			toast.success('Question Bank pool deleted successfully.');
			isDeleteBankModalOpen = false;
			goto('/instructor/questions');
		} catch (err: any) {
			toast.error(err?.message || 'Failed to delete Question Bank pool.');
		} finally {
			isActionLoading = false;
		}
	}

	// Helper for Option management
	function addOption(target: 'new' | 'edit') {
		if (target === 'new') {
			newQuestionOptions = [...newQuestionOptions, { text: `Option ${String.fromCharCode(65 + newQuestionOptions.length)}`, isCorrect: false }];
		} else {
			editQuestionOptions = [...editQuestionOptions, { text: `Option ${String.fromCharCode(65 + editQuestionOptions.length)}`, isCorrect: false }];
		}
	}

	function removeOption(target: 'new' | 'edit', index: number) {
		if (target === 'new') {
			if (newQuestionOptions.length <= 2) return;
			newQuestionOptions = newQuestionOptions.filter((_, i) => i !== index);
		} else {
			if (editQuestionOptions.length <= 2) return;
			editQuestionOptions = editQuestionOptions.filter((_, i) => i !== index);
		}
	}

	function setCorrectOption(target: 'new' | 'edit', index: number, isMulti: boolean) {
		if (target === 'new') {
			if (isMulti) {
				newQuestionOptions[index].isCorrect = !newQuestionOptions[index].isCorrect;
			} else {
				newQuestionOptions = newQuestionOptions.map((opt, i) => ({
					...opt,
					isCorrect: i === index
				}));
			}
		} else {
			if (isMulti) {
				editQuestionOptions[index].isCorrect = !editQuestionOptions[index].isCorrect;
			} else {
				editQuestionOptions = editQuestionOptions.map((opt, i) => ({
					...opt,
					isCorrect: i === index
				}));
			}
		}
	}
</script>

<div class="space-y-6 max-w-7xl mx-auto pb-12">
	<!-- Navigation Breadcrumb -->
	<div class="flex items-center gap-2">
		<a
			href="/instructor/questions"
			class="btn btn-sm btn-ghost gap-2 text-base-content/70 hover:text-base-content"
		>
			<ArrowLeft class="w-4 h-4" />
			<span>Back to Question Pools</span>
		</a>
	</div>

	<!-- Pool Detail Banner -->
	{#if isLoading}
		<div class="py-16 text-center">
			<span class="loading loading-spinner loading-lg text-primary"></span>
			<p class="text-xs text-base-content/60 mt-3 font-semibold">Loading question pool details...</p>
		</div>
	{:else if bank}
		<GlassCard class="p-6 sm:p-7">
			<div class="flex flex-col lg:flex-row lg:items-center justify-between gap-6">
				<div class="space-y-2">
					<div class="flex items-center gap-2 flex-wrap">
						{#if bank.category}
							<span class="badge badge-sm badge-primary font-bold text-[11px]">
								{bank.category}
							</span>
						{/if}
						<span class="badge badge-sm badge-outline text-[11px]">
							{questions.length} questions
						</span>
					</div>

					<h1 class="text-2xl sm:text-3xl font-extrabold text-base-content tracking-tight">
						{bank.title}
					</h1>

					<p class="text-sm text-base-content/70 max-w-2xl">
						{bank.description || 'No description provided for this question pool.'}
					</p>

					{#if bank.tags && bank.tags.length > 0}
						<div class="flex items-center gap-1.5 flex-wrap pt-1">
							{#each bank.tags as tag}
								<span class="badge badge-xs badge-neutral text-[10px] gap-1">
									<Tag class="w-2.5 h-2.5 opacity-60" />
									{tag}
								</span>
							{/each}
						</div>
					{/if}
				</div>

				<div class="flex items-center gap-2 flex-wrap flex-shrink-0">
					<button
						type="button"
						class="btn btn-sm btn-ghost border border-base-content/10 gap-1.5 hover:bg-base-200"
						onclick={handleDownloadTemplate}
						disabled={isDownloadingTemplate}
					>
						{#if isDownloadingTemplate}
							<span class="loading loading-spinner loading-xs"></span>
						{:else}
							<Download class="w-4 h-4 text-primary" />
						{/if}
						<span>Word Template</span>
					</button>

					<button
						type="button"
						class="btn btn-sm btn-outline btn-primary gap-1.5"
						onclick={() => {
							importFile = null;
							isImportModalOpen = true;
						}}
					>
						<FileUp class="w-4 h-4" />
						<span>Import Word (.docx)</span>
					</button>

					<button
						type="button"
						class="btn btn-sm btn-outline btn-neutral gap-1.5"
						onclick={openEditBankModal}
					>
						<Edit3 class="w-4 h-4" />
						Edit Pool
					</button>

					<button
						type="button"
						class="btn btn-sm btn-ghost text-error hover:bg-error/10 gap-1.5"
						onclick={() => (isDeleteBankModalOpen = true)}
					>
						<Trash2 class="w-4 h-4" />
						Delete
					</button>

					<button
						type="button"
						class="btn btn-sm btn-primary gap-1.5 shadow-md shadow-primary/20"
						onclick={openCreateQuestion}
					>
						<Plus class="w-4 h-4" />
						Add Question
					</button>
				</div>
			</div>
		</GlassCard>

		<!-- Stats Overview Cards -->
		<div class="grid grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-4">
			<GlassCard class="p-4 flex items-center gap-3.5">
				<div class="w-10 h-10 rounded-xl bg-primary/10 text-primary flex items-center justify-center flex-shrink-0">
					<Layers class="w-5 h-5" />
				</div>
				<div>
					<p class="text-xs text-base-content/60 font-semibold">Total Questions</p>
					<p class="text-xl font-black text-base-content">{questions.length}</p>
				</div>
			</GlassCard>

			<GlassCard class="p-4 flex items-center gap-3.5">
				<div class="w-10 h-10 rounded-xl bg-secondary/10 text-secondary flex items-center justify-center flex-shrink-0">
					<Sparkles class="w-5 h-5" />
				</div>
				<div>
					<p class="text-xs text-base-content/60 font-semibold">Total Pool Points</p>
					<p class="text-xl font-black text-base-content">{totalPoints} pts</p>
				</div>
			</GlassCard>

			<GlassCard class="p-4 flex items-center gap-3.5">
				<div class="w-10 h-10 rounded-xl bg-success/10 text-success flex items-center justify-center flex-shrink-0">
					<CheckCircle2 class="w-5 h-5" />
				</div>
				<div>
					<p class="text-xs text-base-content/60 font-semibold">Single Choice</p>
					<p class="text-xl font-black text-base-content">{singleChoiceCount}</p>
				</div>
			</GlassCard>

			<GlassCard class="p-4 flex items-center gap-3.5">
				<div class="w-10 h-10 rounded-xl bg-accent/10 text-accent flex items-center justify-center flex-shrink-0">
					<CheckSquare class="w-5 h-5" />
				</div>
				<div>
					<p class="text-xs text-base-content/60 font-semibold">Multiple Choice</p>
					<p class="text-xl font-black text-base-content">{multipleChoiceCount}</p>
				</div>
			</GlassCard>
		</div>

		<!-- Search & Filter Controls -->
		<GlassCard class="p-4 relative z-30 overflow-visible">
			<div class="flex flex-col sm:flex-row gap-3 items-center justify-between">
				<div class="relative w-full sm:w-96">
					<Search class="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-base-content/40" />
					<input
						type="text"
						bind:value={searchTerm}
						placeholder="Search questions in this pool..."
						class="input input-bordered input-sm w-full pl-10 bg-base-100/50"
					/>
				</div>

				<!-- Type Filter Combobox / Dropdown with Blur -->
				<div class="dropdown dropdown-end w-full sm:w-auto z-50">
					<div
						tabindex="0"
						role="button"
						class="btn btn-sm btn-outline border-base-content/20 bg-base-100/70 backdrop-blur-md rounded-xl text-xs font-semibold flex items-center justify-between gap-2 w-full sm:w-44 shadow-xs hover:bg-base-100/90"
					>
						<span class="flex items-center gap-1.5 truncate">
							{#if selectedType === 'All'}
								<ListFilter class="w-3.5 h-3.5 text-base-content/60" />
								<span>All Types</span>
							{:else if selectedType === 'SingleChoice'}
								<CheckCircle2 class="w-3.5 h-3.5 text-primary" />
								<span>Single Choice</span>
							{:else if selectedType === 'MultipleChoice'}
								<CheckSquare class="w-3.5 h-3.5 text-info" />
								<span>Multiple Choice</span>
							{:else if selectedType === 'TrueFalse'}
								<Tag class="w-3.5 h-3.5 text-warning" />
								<span>True / False</span>
							{:else if selectedType === 'Essay'}
								<AlignLeft class="w-3.5 h-3.5 text-secondary" />
								<span>Essay</span>
							{/if}
						</span>
						<ChevronDown class="w-3.5 h-3.5 text-base-content/50 shrink-0" />
					</div>
					<ul
						tabindex="0"
						class="dropdown-content menu p-1.5 shadow-2xl bg-base-100/95 backdrop-blur-2xl border border-base-content/10 rounded-2xl w-52 z-50 mt-1.5 space-y-0.5 text-xs font-medium"
					>
						<li>
							<button
								type="button"
								class="rounded-xl flex items-center justify-between {selectedType === 'All' ? 'active bg-primary text-white font-bold' : ''}"
								onclick={() => {
									selectedType = 'All';
									(document.activeElement as HTMLElement)?.blur?.();
								}}
							>
								<span class="flex items-center gap-2">
									<ListFilter class="w-3.5 h-3.5" />
									All Types
								</span>
								{#if selectedType === 'All'}
									<Check class="w-3.5 h-3.5" />
								{/if}
							</button>
						</li>
						<li>
							<button
								type="button"
								class="rounded-xl flex items-center justify-between {selectedType === 'SingleChoice' ? 'active bg-primary text-white font-bold' : ''}"
								onclick={() => {
									selectedType = 'SingleChoice';
									(document.activeElement as HTMLElement)?.blur?.();
								}}
							>
								<span class="flex items-center gap-2">
									<CheckCircle2 class="w-3.5 h-3.5 text-primary {selectedType === 'SingleChoice' ? 'text-white' : ''}" />
									Single Choice
								</span>
								{#if selectedType === 'SingleChoice'}
									<Check class="w-3.5 h-3.5" />
								{/if}
							</button>
						</li>
						<li>
							<button
								type="button"
								class="rounded-xl flex items-center justify-between {selectedType === 'MultipleChoice' ? 'active bg-primary text-white font-bold' : ''}"
								onclick={() => {
									selectedType = 'MultipleChoice';
									(document.activeElement as HTMLElement)?.blur?.();
								}}
							>
								<span class="flex items-center gap-2">
									<CheckSquare class="w-3.5 h-3.5 text-info {selectedType === 'MultipleChoice' ? 'text-white' : ''}" />
									Multiple Choice
								</span>
								{#if selectedType === 'MultipleChoice'}
									<Check class="w-3.5 h-3.5" />
								{/if}
							</button>
						</li>
						<li>
							<button
								type="button"
								class="rounded-xl flex items-center justify-between {selectedType === 'TrueFalse' ? 'active bg-primary text-white font-bold' : ''}"
								onclick={() => {
									selectedType = 'TrueFalse';
									(document.activeElement as HTMLElement)?.blur?.();
								}}
							>
								<span class="flex items-center gap-2">
									<Tag class="w-3.5 h-3.5 text-warning {selectedType === 'TrueFalse' ? 'text-white' : ''}" />
									True / False
								</span>
								{#if selectedType === 'TrueFalse'}
									<Check class="w-3.5 h-3.5" />
								{/if}
							</button>
						</li>
						<li>
							<button
								type="button"
								class="rounded-xl flex items-center justify-between {selectedType === 'Essay' ? 'active bg-primary text-white font-bold' : ''}"
								onclick={() => {
									selectedType = 'Essay';
									(document.activeElement as HTMLElement)?.blur?.();
								}}
							>
								<span class="flex items-center gap-2">
									<AlignLeft class="w-3.5 h-3.5 text-secondary {selectedType === 'Essay' ? 'text-white' : ''}" />
									Essay
								</span>
								{#if selectedType === 'Essay'}
									<Check class="w-3.5 h-3.5" />
								{/if}
							</button>
						</li>
					</ul>
				</div>
			</div>
		</GlassCard>

		<!-- Questions List -->
		{#if filteredQuestions.length === 0}
			<div class="py-16 text-center bg-base-200/40 rounded-3xl border border-dashed border-base-300 p-8">
				<BookOpen class="w-12 h-12 text-base-content/30 mx-auto mb-3" />
				<h3 class="text-base font-bold text-base-content">
					{questions.length === 0 ? 'No Questions in Pool' : 'No Questions Match Filter'}
				</h3>
				<p class="text-xs text-base-content/60 max-w-sm mx-auto mt-1">
					{questions.length === 0
						? 'Start adding questions to this Question Bank package to make them available across exams.'
						: 'Try adjusting your search query or question type filter.'}
				</p>
				<button
					type="button"
					class="btn btn-sm btn-primary gap-1.5 mt-4"
					onclick={openCreateQuestion}
				>
					<Plus class="w-4 h-4" />
					Add Question
				</button>
			</div>
		{:else}
			<div class="space-y-3.5">
				{#each filteredQuestions as q, idx (q.id || idx)}
					<GlassCard class="p-4 sm:p-5 hover:border-primary/30 transition-all">
						<div class="flex items-start justify-between gap-4">
							<div class="flex items-start gap-3 min-w-0 flex-1">
								<span class="w-7 h-7 rounded-xl bg-base-200 text-base-content/70 font-mono font-bold text-xs flex items-center justify-center flex-shrink-0 mt-0.5">
									{idx + 1}
								</span>

								<div class="min-w-0 flex-1">
									<div class="flex items-center gap-2 flex-wrap mb-2">
										<span class="badge badge-sm badge-outline badge-primary font-bold text-[10px]">
											{q.type}
										</span>
										<span class="badge badge-sm badge-neutral font-mono text-[10px] font-bold">
											{q.points} pts
										</span>
									</div>

									<!-- Question Prompt -->
									<div class="text-sm font-semibold text-base-content">
										<RichRenderer content={q.questionText || q.text || ''} />
									</div>

									<!-- Options Preview for Choice Types -->
									{#if q.options && q.options.length > 0}
										<div class="grid grid-cols-1 sm:grid-cols-2 gap-2 mt-3 pt-3 border-t border-base-content/10">
											{#each q.options as opt, optIdx}
												<div class="p-2 rounded-lg text-xs flex items-center gap-2 {opt.isCorrect ? 'bg-success/10 border border-success/30 text-success font-semibold' : 'bg-base-200/40 text-base-content/70'}">
													<span class="w-4 h-4 rounded-full flex items-center justify-center text-[10px] font-bold {opt.isCorrect ? 'bg-success text-success-content' : 'bg-base-300 text-base-content/60'}">
														{String.fromCharCode(65 + optIdx)}
													</span>
													<span class="truncate">{opt.text}</span>
													{#if opt.isCorrect}
														<Check class="w-3.5 h-3.5 ml-auto text-success" />
													{/if}
												</div>
											{/each}
										</div>
									{/if}

									<!-- Explanation if available -->
									{#if q.explanation}
										<div class="mt-2.5 p-2 rounded-lg bg-base-200/40 text-xs text-base-content/70 flex items-start gap-1.5">
											<HelpCircle class="w-3.5 h-3.5 text-primary flex-shrink-0 mt-0.5" />
											<span class="italic"><strong class="not-italic">Explanation:</strong> {q.explanation}</span>
										</div>
									{/if}
								</div>
							</div>

							<!-- Actions -->
							<div class="flex items-center gap-1 flex-shrink-0">
								<button
									type="button"
									class="btn btn-xs btn-ghost btn-square"
									onclick={() => openEditQuestion(q)}
									title="Edit Question"
								>
									<Edit3 class="w-3.5 h-3.5" />
								</button>

								<button
									type="button"
									class="btn btn-xs btn-ghost btn-square text-error hover:bg-error/10"
									onclick={() => confirmDeleteQuestion(q.id)}
									title="Delete Question"
								>
									<Trash2 class="w-3.5 h-3.5" />
								</button>
							</div>
						</div>
					</GlassCard>
				{/each}
			</div>
		{/if}
	{/if}
</div>

<!-- Add Question Modal -->
{#if isCreateQuestionModalOpen}
	<div class="modal modal-open z-50">
		<div class="modal-box bg-base-100/95 backdrop-blur-xl border border-base-content/10 shadow-2xl max-w-2xl">
			<h3 class="font-bold text-base text-base-content flex items-center gap-2">
				<Plus class="w-5 h-5 text-primary" />
				Add Question to Pool: {bank?.title}
			</h3>

			<form onsubmit={handleSaveNewQuestion} class="space-y-4 mt-4">
				<!-- Question Type Selector -->
				<div>
					<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Question Type
					</label>
					<div class="grid grid-cols-2 sm:grid-cols-4 gap-2">
						{#each questionTypes as qt}
							<button
								type="button"
								class="p-2.5 rounded-xl border text-left flex items-center gap-2 text-xs font-bold transition-all {newQuestionType === qt.id ? 'border-primary bg-primary/10 text-primary shadow-sm' : 'border-base-content/10 bg-base-200/50 text-base-content/70'}"
								onclick={() => (newQuestionType = qt.id)}
							>
								<qt.icon class="w-4 h-4" />
								<span>{qt.label}</span>
							</button>
						{/each}
					</div>
				</div>

				<!-- Prompt Editor -->
				<div>
					<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Question Prompt <span class="text-error">*</span>
					</label>
					<RichEditor
						bind:content={newQuestionText}
						placeholder="Write question statement, scenario, or mathematical equation..."
					/>
				</div>

				<!-- Points -->
				<div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
					<div>
						<label for="q-points-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Default Points <span class="text-error">*</span>
						</label>
						<input
							id="q-points-input"
							type="number"
							step="0.5"
							min="0.5"
							bind:value={newQuestionPoints}
							class="input input-bordered input-sm w-full bg-base-200/50"
							required
						/>
					</div>

					<div>
						<label for="q-exp-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Explanation / Key Note
						</label>
						<input
							id="q-exp-input"
							type="text"
							bind:value={newQuestionExplanation}
							placeholder="Feedback shown to candidate..."
							class="input input-bordered input-sm w-full bg-base-200/50"
						/>
					</div>
				</div>

				<!-- Options for Choices -->
				{#if newQuestionType !== 'Essay'}
					<div class="space-y-2 pt-2 border-t border-base-content/10">
						<div class="flex items-center justify-between">
							<span class="text-xs font-bold uppercase tracking-wider text-base-content/70">
								Options & Answers
							</span>
							{#if newQuestionType !== 'TrueFalse'}
								<button
									type="button"
									class="btn btn-xs btn-ghost gap-1 text-primary"
									onclick={() => addOption('new')}
								>
									<Plus class="w-3.5 h-3.5" />
									Add Option
								</button>
							{/if}
						</div>

						<div class="space-y-2">
							{#each newQuestionOptions as opt, oIdx}
								<div class="flex items-center gap-2 p-2 rounded-xl bg-base-200/50 border border-base-content/5">
									<button
										type="button"
										class="btn btn-xs btn-square {opt.isCorrect ? 'btn-success text-success-content' : 'btn-ghost'}"
										onclick={() => setCorrectOption('new', oIdx, newQuestionType === 'MultipleChoice')}
										title={opt.isCorrect ? 'Correct Answer' : 'Mark as Correct'}
									>
										{#if opt.isCorrect}
											<Check class="w-3.5 h-3.5" />
										{:else}
											<span class="text-[10px] font-bold">{String.fromCharCode(65 + oIdx)}</span>
										{/if}
									</button>

									<input
										type="text"
										bind:value={opt.text}
										placeholder="Choice statement..."
										class="input input-bordered input-xs flex-1 bg-base-100"
										required
									/>

									{#if newQuestionType !== 'TrueFalse' && newQuestionOptions.length > 2}
										<button
											type="button"
											class="btn btn-xs btn-ghost btn-square text-error"
											onclick={() => removeOption('new', oIdx)}
										>
											<Trash2 class="w-3.5 h-3.5" />
										</button>
									{/if}
								</div>
							{/each}
						</div>
					</div>
				{/if}

				<div class="modal-action pt-3 border-t border-base-content/10">
					<button
						type="button"
						class="btn btn-sm btn-ghost"
						onclick={() => (isCreateQuestionModalOpen = false)}
						disabled={isActionLoading}
					>
						Cancel
					</button>
					<button
						type="submit"
						class="btn btn-sm btn-primary gap-1.5"
						disabled={isActionLoading || !newQuestionText.trim()}
					>
						{#if isActionLoading}
							<span class="loading loading-spinner loading-xs"></span>
						{:else}
							<Check class="w-4 h-4" />
						{/if}
						Add Question
					</button>
				</div>
			</form>
		</div>
		<div class="modal-backdrop bg-black/40 backdrop-blur-sm" onclick={() => (isCreateQuestionModalOpen = false)}></div>
	</div>
{/if}

<!-- Edit Question Modal -->
{#if isEditQuestionModalOpen}
	<div class="modal modal-open z-50">
		<div class="modal-box bg-base-100/95 backdrop-blur-xl border border-base-content/10 shadow-2xl max-w-2xl">
			<h3 class="font-bold text-base text-base-content flex items-center gap-2">
				<Edit3 class="w-5 h-5 text-primary" />
				Edit Question
			</h3>

			<form onsubmit={handleUpdateQuestion} class="space-y-4 mt-4">
				<!-- Prompt Editor -->
				<div>
					<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Question Prompt <span class="text-error">*</span>
					</label>
					<RichEditor
						bind:content={editQuestionText}
						placeholder="Write question statement, scenario, or mathematical equation..."
					/>
				</div>

				<!-- Points -->
				<div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
					<div>
						<label for="edit-q-points-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Points <span class="text-error">*</span>
						</label>
						<input
							id="edit-q-points-input"
							type="number"
							step="0.5"
							min="0.5"
							bind:value={editQuestionPoints}
							class="input input-bordered input-sm w-full bg-base-200/50"
							required
						/>
					</div>

					<div>
						<label for="edit-q-exp-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Explanation / Key Note
						</label>
						<input
							id="edit-q-exp-input"
							type="text"
							bind:value={editQuestionExplanation}
							placeholder="Feedback shown to candidate..."
							class="input input-bordered input-sm w-full bg-base-200/50"
						/>
					</div>
				</div>

				<!-- Options for Choices -->
				{#if editQuestionType !== 'Essay'}
					<div class="space-y-2 pt-2 border-t border-base-content/10">
						<div class="flex items-center justify-between">
							<span class="text-xs font-bold uppercase tracking-wider text-base-content/70">
								Options & Answers
							</span>
							{#if editQuestionType !== 'TrueFalse'}
								<button
									type="button"
									class="btn btn-xs btn-ghost gap-1 text-primary"
									onclick={() => addOption('edit')}
								>
									<Plus class="w-3.5 h-3.5" />
									Add Option
								</button>
							{/if}
						</div>

						<div class="space-y-2">
							{#each editQuestionOptions as opt, oIdx}
								<div class="flex items-center gap-2 p-2 rounded-xl bg-base-200/50 border border-base-content/5">
									<button
										type="button"
										class="btn btn-xs btn-square {opt.isCorrect ? 'btn-success text-success-content' : 'btn-ghost'}"
										onclick={() => setCorrectOption('edit', oIdx, editQuestionType === 'MultipleChoice')}
										title={opt.isCorrect ? 'Correct Answer' : 'Mark as Correct'}
									>
										{#if opt.isCorrect}
											<Check class="w-3.5 h-3.5" />
										{:else}
											<span class="text-[10px] font-bold">{String.fromCharCode(65 + oIdx)}</span>
										{/if}
									</button>

									<input
										type="text"
										bind:value={opt.text}
										placeholder="Choice statement..."
										class="input input-bordered input-xs flex-1 bg-base-100"
										required
									/>

									{#if editQuestionType !== 'TrueFalse' && editQuestionOptions.length > 2}
										<button
											type="button"
											class="btn btn-xs btn-ghost btn-square text-error"
											onclick={() => removeOption('edit', oIdx)}
										>
											<Trash2 class="w-3.5 h-3.5" />
										</button>
									{/if}
								</div>
							{/each}
						</div>
					</div>
				{/if}

				<div class="modal-action pt-3 border-t border-base-content/10">
					<button
						type="button"
						class="btn btn-sm btn-ghost"
						onclick={() => (isEditQuestionModalOpen = false)}
						disabled={isActionLoading}
					>
						Cancel
					</button>
					<button
						type="submit"
						class="btn btn-sm btn-primary gap-1.5"
						disabled={isActionLoading || !editQuestionText.trim()}
					>
						{#if isActionLoading}
							<span class="loading loading-spinner loading-xs"></span>
						{:else}
							<Check class="w-4 h-4" />
						{/if}
						Save Changes
					</button>
				</div>
			</form>
		</div>
		<div class="modal-backdrop bg-black/40 backdrop-blur-sm" onclick={() => (isEditQuestionModalOpen = false)}></div>
	</div>
{/if}

<!-- Edit Bank Details Modal -->
{#if isEditBankModalOpen}
	<div class="modal modal-open z-50">
		<div class="modal-box bg-base-100/95 backdrop-blur-xl border border-base-content/10 shadow-2xl max-w-md">
			<h3 class="font-bold text-base text-base-content flex items-center gap-2">
				<Edit3 class="w-5 h-5 text-primary" />
				Edit Question Bank Pool Info
			</h3>

			<form
				onsubmit={(e) => {
					e.preventDefault();
					handleSaveBankInfo();
				}}
				class="space-y-4 mt-4"
			>
				<div>
					<label for="edit-bank-title" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Pool Title <span class="text-error">*</span>
					</label>
					<input
						id="edit-bank-title"
						type="text"
						bind:value={editBankTitle}
						class="input input-bordered input-sm w-full bg-base-200/50"
						required
					/>
				</div>

				<div>
					<label for="edit-bank-cat" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Category
					</label>
					<input
						id="edit-bank-cat"
						type="text"
						bind:value={editBankCategory}
						class="input input-bordered input-sm w-full bg-base-200/50"
					/>
				</div>

				<div>
					<label for="edit-bank-desc" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Description
					</label>
					<textarea
						id="edit-bank-desc"
						bind:value={editBankDescription}
						rows="2"
						class="textarea textarea-bordered textarea-sm w-full bg-base-200/50"
					></textarea>
				</div>

				<div>
					<label for="edit-bank-tags" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Tags (Comma separated)
					</label>
					<input
						id="edit-bank-tags"
						type="text"
						bind:value={editBankTags}
						class="input input-bordered input-sm w-full bg-base-200/50"
					/>
				</div>

				<div class="modal-action pt-2">
					<button
						type="button"
						class="btn btn-sm btn-ghost"
						onclick={() => (isEditBankModalOpen = false)}
						disabled={isActionLoading}
					>
						Cancel
					</button>
					<button
						type="submit"
						class="btn btn-sm btn-primary gap-1.5"
						disabled={isActionLoading || !editBankTitle.trim()}
					>
						{#if isActionLoading}
							<span class="loading loading-spinner loading-xs"></span>
						{:else}
							<Check class="w-4 h-4" />
						{/if}
						Save Info
					</button>
				</div>
			</form>
		</div>
		<div class="modal-backdrop bg-black/40 backdrop-blur-sm" onclick={() => (isEditBankModalOpen = false)}></div>
	</div>
{/if}

<!-- Delete Question Confirmation Modal -->
{#if isDeleteQuestionModalOpen}
	<div class="modal modal-open z-50">
		<div class="modal-box bg-base-100/95 backdrop-blur-xl border border-error/20 shadow-2xl max-w-sm">
			<h3 class="font-bold text-base text-error flex items-center gap-2">
				<Trash2 class="w-5 h-5" />
				Remove Question
			</h3>
			<p class="text-xs text-base-content/80 mt-3 leading-relaxed">
				Are you sure you want to remove this question from <strong>"{bank?.title}"</strong>?
			</p>
			<div class="modal-action pt-2">
				<button
					type="button"
					class="btn btn-sm btn-ghost"
					onclick={() => (isDeleteQuestionModalOpen = false)}
					disabled={isActionLoading}
				>
					Cancel
				</button>
				<button
					type="button"
					class="btn btn-sm btn-error gap-1.5"
					onclick={handleDeleteQuestion}
					disabled={isActionLoading}
				>
					{#if isActionLoading}
						<span class="loading loading-spinner loading-xs"></span>
					{:else}
						<Trash2 class="w-4 h-4" />
					{/if}
					Delete
				</button>
			</div>
		</div>
		<div class="modal-backdrop bg-black/40 backdrop-blur-sm" onclick={() => (isDeleteQuestionModalOpen = false)}></div>
	</div>
{/if}

<!-- Delete Bank Confirmation Modal -->
{#if isDeleteBankModalOpen && bank}
	<div class="modal modal-open z-50">
		<div class="modal-box bg-base-100/95 backdrop-blur-xl border border-error/20 shadow-2xl max-w-sm">
			<h3 class="font-bold text-base text-error flex items-center gap-2">
				<AlertCircle class="w-5 h-5" />
				Delete Question Bank
			</h3>
			<p class="text-xs text-base-content/80 mt-3 leading-relaxed">
				Are you sure you want to delete <strong>"{bank.title}"</strong>? All questions contained in this pool will be permanently deleted.
			</p>
			<div class="modal-action pt-2">
				<button
					type="button"
					class="btn btn-sm btn-ghost"
					onclick={() => (isDeleteBankModalOpen = false)}
					disabled={isActionLoading}
				>
					Cancel
				</button>
				<button
					type="button"
					class="btn btn-sm btn-error gap-1.5"
					onclick={handleDeleteBank}
					disabled={isActionLoading}
				>
					{#if isActionLoading}
						<span class="loading loading-spinner loading-xs"></span>
					{:else}
						<Trash2 class="w-4 h-4" />
					{/if}
					Delete Pool
				</button>
			</div>
		</div>
		<div class="modal-backdrop bg-black/40 backdrop-blur-sm" onclick={() => (isDeleteBankModalOpen = false)}></div>
	</div>
{/if}

<!-- Import Questions from Word Modal -->
{#if isImportModalOpen}
	<div class="modal modal-open z-50">
		<div class="modal-box bg-base-100/95 backdrop-blur-xl border border-base-content/10 shadow-2xl max-w-lg">
			<div class="flex items-center justify-between border-b border-base-content/10 pb-3">
				<h3 class="font-bold text-base text-base-content flex items-center gap-2">
					<FileUp class="w-5 h-5 text-primary" />
					Import Questions into "{bank?.title || 'Bank'}"
				</h3>
				<button
					type="button"
					class="btn btn-xs btn-circle btn-ghost"
					onclick={() => (isImportModalOpen = false)}
				>✕</button>
			</div>

			<form onsubmit={handleImportQuestions} class="space-y-4 pt-3">
				<!-- Template Helper Banner -->
				<div class="p-3 rounded-xl bg-primary/10 border border-primary/20 flex items-center justify-between gap-3">
					<div class="space-y-0.5">
						<p class="text-xs font-bold text-primary flex items-center gap-1.5">
							<FileText class="w-4 h-4" />
							Formatted Question Template
						</p>
						<p class="text-[11px] text-base-content/70 leading-tight">
							Ensure your questions follow the standard template format.
						</p>
					</div>
					<button
						type="button"
						class="btn btn-xs btn-primary gap-1"
						onclick={handleDownloadTemplate}
						disabled={isDownloadingTemplate}
					>
						{#if isDownloadingTemplate}
							<span class="loading loading-spinner loading-xs"></span>
						{:else}
							<Download class="w-3.5 h-3.5" />
						{/if}
						Template
					</button>
				</div>

				<!-- File Input -->
				<div>
					<label for="detail-import-file-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Word Document (.docx) <span class="text-error">*</span>
					</label>
					<input
						id="detail-import-file-input"
						type="file"
						accept=".docx,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
						onchange={(e) => {
							const target = e.currentTarget;
							if (target.files && target.files.length > 0) {
								importFile = target.files[0];
							}
						}}
						class="file-input file-input-bordered file-input-primary file-input-sm w-full bg-base-200/50"
						required
					/>
					{#if importFile}
						<p class="text-[11px] text-success font-medium mt-1">
							Selected: {importFile.name} ({(importFile.size / 1024).toFixed(1)} KB)
						</p>
					{/if}
				</div>

				<div class="modal-action pt-2">
					<button
						type="button"
						class="btn btn-sm btn-ghost"
						onclick={() => (isImportModalOpen = false)}
						disabled={isActionLoading}
					>
						Cancel
					</button>
					<button
						type="submit"
						class="btn btn-sm btn-primary gap-1.5"
						disabled={isActionLoading || !importFile}
					>
						{#if isActionLoading}
							<span class="loading loading-spinner loading-xs"></span>
						{:else}
							<FileUp class="w-4 h-4" />
						{/if}
						Import & Append Questions
					</button>
				</div>
			</form>
		</div>
		<div class="modal-backdrop bg-black/40 backdrop-blur-sm" onclick={() => (isImportModalOpen = false)}></div>
	</div>
{/if}
