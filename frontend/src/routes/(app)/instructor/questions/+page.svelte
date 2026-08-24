<script lang="ts">
	import { examsApi } from '#lib/api/exams.ts';
	import type { QuestionBank, BankQuestion, QuestionType } from '#lib/api/types.ts';
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
		BookOpen,
		FolderPlus,
		Tag
	} from '@lucide/svelte';
	import { onMount } from 'svelte';

	let questionBanks = $state<QuestionBank[]>([]);
	let selectedBankId = $state<string>('All');
	let selectedType = $state<string>('All');
	let selectedCategory = $state<string>('All');
	let searchTerm = $state('');
	let isLoading = $state(true);
	let isActionLoading = $state(false);

	// All gathered questions across banks
	let allQuestions = $state<Array<BankQuestion & { bankTitle?: string; bankCategory?: string; bankId: string }>>([]);

	// Categories
	const categories = $derived([
		'All',
		...Array.from(
			new Set(
				questionBanks
					.map((b) => b.category)
					.filter((c): c is string => Boolean(c && c.trim()))
			)
		)
	]);

	// Filtered questions
	const filteredQuestions = $derived(
		allQuestions.filter((q) => {
			const matchBank = selectedBankId === 'All' || q.bankId === selectedBankId;
			const matchType = selectedType === 'All' || q.type === selectedType;
			const matchCategory = selectedCategory === 'All' || q.bankCategory === selectedCategory;
			const text = (q.questionText || q.text || '').toLowerCase();
			const matchSearch =
				!searchTerm.trim() ||
				text.includes(searchTerm.toLowerCase().trim()) ||
				(q.bankTitle && q.bankTitle.toLowerCase().includes(searchTerm.toLowerCase().trim()));
			return matchBank && matchType && matchCategory && matchSearch;
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

	// Create Question Bank Package Modal
	let isCreateBankModalOpen = $state(false);
	let newBankTitle = $state('');
	let newBankCategory = $state('');
	let newBankDescription = $state('');
	let newBankTags = $state('');

	// Create Question Modal
	let isCreateQuestionModalOpen = $state(false);
	let targetBankId = $state<string>('');
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

	onMount(async () => {
		await loadAllData();
	});

	async function loadAllData() {
		isLoading = true;
		try {
			// Fetch exams to collect linked QuestionBanks and questions
			const examListRes = await examsApi.listExams({ pageSize: 100 });
			const exams = examListRes.items || [];

			const banksMap = new Map<string, QuestionBank>();
			const questionsList: Array<BankQuestion & { bankTitle?: string; bankCategory?: string; bankId: string }> = [];

			for (const ex of exams) {
				try {
					const fullExam = await examsApi.getExamById(ex.id);
					if (fullExam && fullExam.sections) {
						for (const sec of fullExam.sections) {
							if (sec.questionBankId) {
								if (!banksMap.has(sec.questionBankId)) {
									banksMap.set(sec.questionBankId, {
										id: sec.questionBankId,
										title: sec.questionBankTitle || sec.title || 'General Question Pool',
										description: sec.description,
										createdBy: fullExam.createdBy || '',
										createdAtUtc: fullExam.createdAtUtc || '',
										questions: sec.questions || []
									});
								}
							}

							if (sec.questions) {
								for (const q of sec.questions) {
									questionsList.push({
										...q,
										bankId: sec.questionBankId || 'default',
										bankTitle: sec.questionBankTitle || sec.title || 'General Question Pool',
										bankCategory: q.category || undefined
									});
								}
							}
						}
					}
				} catch {
					// continue
				}
			}

			questionBanks = Array.from(banksMap.values());
			allQuestions = questionsList;

			if (questionBanks.length > 0 && !targetBankId) {
				targetBankId = questionBanks[0].id;
			}
		} catch (err: any) {
			toast.error(err?.message || 'Failed to load question banks.');
		} finally {
			isLoading = false;
		}
	}

	function openCreateBank() {
		newBankTitle = '';
		newBankCategory = '';
		newBankDescription = '';
		newBankTags = '';
		isCreateBankModalOpen = true;
	}

	function handleCreateBank() {
		if (!newBankTitle.trim()) {
			toast.warning('Please enter a question pool title.');
			return;
		}

		const tagsList = newBankTags
			.split(',')
			.map((t) => t.trim())
			.filter(Boolean);

		const newBank: QuestionBank = {
			id: crypto.randomUUID(),
			title: newBankTitle.trim(),
			category: newBankCategory.trim() || undefined,
			description: newBankDescription.trim() || undefined,
			tags: tagsList,
			createdBy: '',
			createdAtUtc: new Date().toISOString(),
			questions: []
		};

		questionBanks = [...questionBanks, newBank];
		targetBankId = newBank.id;
		isCreateBankModalOpen = false;
		toast.success(`Question Bank '${newBank.title}' created successfully!`);
	}

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

		// Validate options for choice types
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
			const created = await examsApi.addQuestion(undefined, {
				questionText: newQuestionText.trim(),
				type: newQuestionType,
				points: Number(newQuestionPoints),
				explanation: newQuestionExplanation.trim() || undefined,
				options: newQuestionOptions.map((o) => ({ text: o.text.trim(), isCorrect: o.isCorrect }))
			});

			toast.success('Question added to Question Bank successfully!');
			isCreateQuestionModalOpen = false;
			await loadAllData();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to save question to pool.');
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

	async function handleSaveEditQuestion(e: Event) {
		e.preventDefault();
		if (!editingQuestionId || !editQuestionText.trim()) {
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
			await loadAllData();
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
			toast.success('Question removed from bank successfully.');
			isDeleteQuestionModalOpen = false;
			await loadAllData();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to delete question from bank.');
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
	<!-- Header -->
	<div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
		<div>
			<h1 class="text-2xl sm:text-3xl font-extrabold text-base-content tracking-tight flex items-center gap-2.5">
				<BookOpen class="w-8 h-8 text-primary" />
				Question Bank Repository
			</h1>
			<p class="text-sm text-base-content/70 mt-1">
				Author, organize, and manage reusable question pools and packages across examinations and courses.
			</p>
		</div>

		<div class="flex items-center gap-2">
			<button
				type="button"
				class="btn btn-sm btn-outline btn-primary gap-1.5"
				onclick={openCreateBank}
			>
				<FolderPlus class="w-4 h-4" />
				New Question Pool
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

	<!-- Stats Overview Cards -->
	<div class="grid grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-4">
		<GlassCard class="p-4 flex items-center gap-3.5">
			<div class="w-10 h-10 rounded-xl bg-primary/10 text-primary flex items-center justify-center flex-shrink-0">
				<Layers class="w-5 h-5" />
			</div>
			<div>
				<p class="text-xs text-base-content/60 font-semibold">Total Questions</p>
				<p class="text-xl font-black text-base-content">{filteredQuestions.length}</p>
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

	<!-- Filter & Search Controls -->
	<GlassCard class="p-4">
		<div class="flex flex-col lg:flex-row gap-3 items-center justify-between">
			<div class="relative w-full lg:w-96">
				<Search class="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-base-content/40" />
				<input
					type="text"
					bind:value={searchTerm}
					placeholder="Search questions or keywords..."
					class="input input-bordered input-sm w-full pl-10 bg-base-100/50"
				/>
			</div>

			<div class="flex items-center gap-2 w-full lg:w-auto overflow-x-auto pb-1 lg:pb-0">
				<!-- Category Filter -->
				<select
					bind:value={selectedCategory}
					class="select select-bordered select-sm bg-base-100/50 text-xs font-semibold"
				>
					{#each categories as cat}
						<option value={cat}>{cat === 'All' ? 'All Categories' : cat}</option>
					{/each}
				</select>

				<!-- Type Filter -->
				<select
					bind:value={selectedType}
					class="select select-bordered select-sm bg-base-100/50 text-xs font-semibold"
				>
					<option value="All">All Types</option>
					<option value="SingleChoice">Single Choice</option>
					<option value="MultipleChoice">Multiple Choice</option>
					<option value="TrueFalse">True / False</option>
					<option value="Essay">Essay</option>
				</select>
			</div>
		</div>
	</GlassCard>

	<!-- Questions List -->
	{#if isLoading}
		<div class="py-16 text-center">
			<span class="loading loading-spinner loading-lg text-primary"></span>
			<p class="text-xs text-base-content/60 mt-3 font-semibold">Loading question repository...</p>
		</div>
	{:else if filteredQuestions.length === 0}
		<div class="py-16 text-center bg-base-200/40 rounded-3xl border border-dashed border-base-300">
			<BookOpen class="w-12 h-12 text-base-content/30 mx-auto mb-3" />
			<h3 class="text-base font-bold text-base-content">No Questions Found</h3>
			<p class="text-xs text-base-content/60 max-w-sm mx-auto mt-1">
				{searchTerm || selectedType !== 'All' ? 'Try adjusting your search criteria or filters.' : 'Add questions to the bank to start building reusable exam sections.'}
			</p>
			<button
				type="button"
				class="btn btn-sm btn-primary gap-1.5 mt-4"
				onclick={openCreateQuestion}
			>
				<Plus class="w-4 h-4" />
				Add First Question
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
									{#if q.bankTitle}
										<span class="badge badge-sm badge-ghost text-[10px] flex items-center gap-1">
											<Layers class="w-3 h-3 text-primary" />
											{q.bankTitle}
										</span>
									{/if}
									{#if q.category}
										<span class="badge badge-sm badge-secondary badge-outline text-[10px]">
											{q.category}
										</span>
									{/if}
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
</div>

<!-- Create Question Pool / Bank Modal -->
{#if isCreateBankModalOpen}
	<div class="modal modal-open z-50">
		<div class="modal-box bg-base-100/95 backdrop-blur-xl border border-base-content/10 shadow-2xl max-w-md">
			<h3 class="font-bold text-base text-base-content flex items-center gap-2">
				<FolderPlus class="w-5 h-5 text-primary" />
				Create Question Bank Pool
			</h3>

			<form
				onsubmit={(e) => {
					e.preventDefault();
					handleCreateBank();
				}}
				class="space-y-4 mt-4"
			>
				<div>
					<label for="bank-title-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Pool Title <span class="text-error">*</span>
					</label>
					<input
						id="bank-title-input"
						type="text"
						bind:value={newBankTitle}
						placeholder="e.g. C# Certification Question Pool"
						class="input input-bordered input-sm w-full bg-base-200/50"
						required
					/>
				</div>

				<div>
					<label for="bank-cat-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Category
					</label>
					<input
						id="bank-cat-input"
						type="text"
						bind:value={newBankCategory}
						placeholder="e.g. Software Engineering"
						class="input input-bordered input-sm w-full bg-base-200/50"
					/>
				</div>

				<div>
					<label for="bank-desc-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Description (Optional)
					</label>
					<textarea
						id="bank-desc-input"
						bind:value={newBankDescription}
						rows="2"
						placeholder="Purpose or syllabus coverage..."
						class="textarea textarea-bordered textarea-sm w-full bg-base-200/50"
					></textarea>
				</div>

				<div>
					<label for="bank-tags-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Tags (Comma separated)
					</label>
					<input
						id="bank-tags-input"
						type="text"
						bind:value={newBankTags}
						placeholder="e.g. csharp, dotnet, backend"
						class="input input-bordered input-sm w-full bg-base-200/50"
					/>
				</div>

				<div class="modal-action pt-2">
					<button
						type="button"
						class="btn btn-sm btn-ghost"
						onclick={() => (isCreateBankModalOpen = false)}
					>
						Cancel
					</button>
					<button
						type="submit"
						class="btn btn-sm btn-primary gap-1.5"
						disabled={!newBankTitle.trim()}
					>
						<Check class="w-4 h-4" />
						Create Pool
					</button>
				</div>
			</form>
		</div>
		<div class="modal-backdrop bg-black/40 backdrop-blur-sm" onclick={() => (isCreateBankModalOpen = false)}></div>
	</div>
{/if}

<!-- Add Question Modal -->
{#if isCreateQuestionModalOpen}
	<div class="modal modal-open z-50">
		<div class="modal-box bg-base-100/95 backdrop-blur-xl border border-base-content/10 shadow-2xl max-w-2xl">
			<h3 class="font-bold text-base text-base-content flex items-center gap-2">
				<Plus class="w-5 h-5 text-primary" />
				Add Question to Repository
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
										class="w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold transition-all {opt.isCorrect ? 'bg-success text-success-content shadow-sm' : 'bg-base-300 text-base-content/50'}"
										onclick={() => setCorrectOption('new', oIdx, newQuestionType === 'MultipleChoice')}
										title="Toggle as Correct Answer"
									>
										{#if opt.isCorrect}
											<Check class="w-3.5 h-3.5" />
										{:else}
											{String.fromCharCode(65 + oIdx)}
										{/if}
									</button>

									<input
										type="text"
										bind:value={opt.text}
										placeholder={`Choice ${String.fromCharCode(65 + oIdx)}`}
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

				<!-- Modal Actions -->
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
						class="btn btn-sm btn-primary gap-1.5 shadow-md"
						disabled={isActionLoading}
					>
						{#if isActionLoading}
							<span class="loading loading-spinner loading-xs"></span>
						{:else}
							<Check class="w-4 h-4" />
						{/if}
						Save Question
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

			<form onsubmit={handleSaveEditQuestion} class="space-y-4 mt-4">
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
						<label for="edit-q-points" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Points <span class="text-error">*</span>
						</label>
						<input
							id="edit-q-points"
							type="number"
							step="0.5"
							min="0.5"
							bind:value={editQuestionPoints}
							class="input input-bordered input-sm w-full bg-base-200/50"
							required
						/>
					</div>

					<div>
						<label for="edit-q-exp" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Explanation
						</label>
						<input
							id="edit-q-exp"
							type="text"
							bind:value={editQuestionExplanation}
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
										class="w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold transition-all {opt.isCorrect ? 'bg-success text-success-content shadow-sm' : 'bg-base-300 text-base-content/50'}"
										onclick={() => setCorrectOption('edit', oIdx, editQuestionType === 'MultipleChoice')}
										title="Toggle as Correct Answer"
									>
										{#if opt.isCorrect}
											<Check class="w-3.5 h-3.5" />
										{:else}
											{String.fromCharCode(65 + oIdx)}
										{/if}
									</button>

									<input
										type="text"
										bind:value={opt.text}
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

				<!-- Actions -->
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
						class="btn btn-sm btn-primary gap-1.5 shadow-md"
						disabled={isActionLoading}
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

<!-- Delete Question Confirm Modal -->
<ConfirmModal
	isOpen={isDeleteQuestionModalOpen}
	title="Delete Question from Repository"
	message="Are you sure you want to remove this question from the Question Bank repository? It cannot be undone."
	confirmText="Delete Question"
	isDanger={true}
	isLoading={isActionLoading}
	onConfirm={handleDeleteQuestion}
	onCancel={() => (isDeleteQuestionModalOpen = false)}
/>
