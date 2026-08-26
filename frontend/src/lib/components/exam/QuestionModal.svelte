<script lang="ts">
	import { untrack } from 'svelte';
	import { fade, scale } from 'svelte/transition';
	import { cubicOut } from 'svelte/easing';
	import {
		Plus,
		Edit3,
		X,
		Check,
		CheckCircle2,
		CheckSquare,
		Sparkles,
		FileText,
		Trash2,
		HelpCircle,
		Award,
		Lightbulb,
		ListOrdered,
		Layers
	} from 'lucide-svelte';
	import type { QuestionType, QuestionOption, BankQuestion } from '$lib/api/types.ts';
	import RichEditor from '$lib/components/editor/RichEditor.svelte';
	import { toast } from '$lib/stores/toast.svelte.ts';

	interface Props {
		isOpen: boolean;
		mode?: 'create' | 'edit';
		bankTitle?: string;
		initialQuestion?: BankQuestion | null;
		isLoading?: boolean;
		onClose: () => void;
		onSave: (data: {
			questionText: string;
			type: QuestionType;
			points: number;
			explanation?: string;
			options: Array<{ id?: string; text: string; isCorrect: boolean }>;
		}) => void | Promise<void>;
	}

	let {
		isOpen,
		mode = 'create',
		bankTitle = '',
		initialQuestion = null,
		isLoading = false,
		onClose,
		onSave
	}: Props = $props();

	let questionText = $state('');
	let questionType = $state<QuestionType>('SingleChoice');
	let questionPoints = $state<number>(5);
	let questionExplanation = $state('');
	let options = $state<Array<{ id?: string; text: string; isCorrect: boolean }>>([]);

	const questionTypes: Array<{
		id: QuestionType;
		label: string;
		description: string;
		icon: typeof CheckCircle2;
	}> = [
		{
			id: 'SingleChoice',
			label: 'Single Choice',
			description: 'One correct answer choice',
			icon: CheckCircle2
		},
		{
			id: 'MultipleChoice',
			label: 'Multiple Choice',
			description: 'Multiple correct answer choices',
			icon: CheckSquare
		},
		{
			id: 'TrueFalse',
			label: 'True / False',
			description: 'Binary true or false choice',
			icon: Sparkles
		},
		{
			id: 'Essay',
			label: 'Essay Prompt',
			description: 'Open-ended text answer',
			icon: FileText
		}
	];

	// Track previous state to avoid unnecessary re-initialization
	let prevOpen = $state(false);
	let prevQuestionId = $state<string | undefined>(undefined);

	// Sync state only when modal opens or target question changes
	$effect(() => {
		const currentOpen = isOpen;
		const currentQuestion = initialQuestion;
		const currentMode = mode;

		untrack(() => {
			if (currentOpen && (!prevOpen || currentQuestion?.id !== prevQuestionId)) {
				prevOpen = true;
				prevQuestionId = currentQuestion?.id;

				if (currentMode === 'edit' && currentQuestion) {
					questionText = currentQuestion.questionText || currentQuestion.text || '';
					questionType = currentQuestion.type || 'SingleChoice';
					questionPoints = currentQuestion.points || 5;
					questionExplanation = currentQuestion.explanation || '';
					const loadedOpts = (currentQuestion.options || []).map((o) => ({
						id: o.id,
						text: o.text || '',
						isCorrect: Boolean(o.isCorrect)
					}));
					if (loadedOpts.length === 0 && questionType !== 'Essay') {
						initDefaultOptions(questionType);
					} else {
						options = loadedOpts;
					}
				} else {
					questionText = '';
					questionType = 'SingleChoice';
					questionPoints = 5;
					questionExplanation = '';
					initDefaultOptions('SingleChoice');
				}
			} else if (!currentOpen) {
				prevOpen = false;
				prevQuestionId = undefined;
			}
		});
	});

	function initDefaultOptions(type: QuestionType) {
		if (type === 'TrueFalse') {
			options = [
				{ text: 'True', isCorrect: true },
				{ text: 'False', isCorrect: false }
			];
		} else if (type === 'Essay') {
			options = [];
		} else {
			options = [
				{ text: 'Option A', isCorrect: true },
				{ text: 'Option B', isCorrect: false },
				{ text: 'Option C', isCorrect: false },
				{ text: 'Option D', isCorrect: false }
			];
		}
	}

	function handleTypeChange(newType: QuestionType) {
		if (questionType === newType) return;
		questionType = newType;

		if (newType === 'TrueFalse') {
			options = [
				{ text: 'True', isCorrect: true },
				{ text: 'False', isCorrect: false }
			];
		} else if (newType === 'Essay') {
			options = [];
		} else if (options.length === 0 || questionType === 'TrueFalse') {
			options = [
				{ text: 'Option A', isCorrect: true },
				{ text: 'Option B', isCorrect: false },
				{ text: 'Option C', isCorrect: false },
				{ text: 'Option D', isCorrect: false }
			];
		} else if (newType === 'SingleChoice') {
			// Ensure only one is correct
			let foundFirst = false;
			options = options.map((opt) => {
				if (opt.isCorrect && !foundFirst) {
					foundFirst = true;
					return { ...opt, isCorrect: true };
				}
				return { ...opt, isCorrect: false };
			});
			if (!foundFirst && options.length > 0) {
				options[0].isCorrect = true;
			}
		}
	}

	function addOption() {
		if (questionType === 'TrueFalse' || questionType === 'Essay') return;
		if (options.length >= 10) {
			toast.info('Maximum 10 options allowed per question.');
			return;
		}
		const letter = String.fromCharCode(65 + options.length);
		options = [...options, { text: `Option ${letter}`, isCorrect: false }];
	}

	function removeOption(index: number) {
		if (options.length <= 2) {
			toast.warning('A choice question must have at least 2 options.');
			return;
		}
		const wasCorrect = options[index].isCorrect;
		options = options.filter((_, i) => i !== index);
		if (wasCorrect && !options.some((o) => o.isCorrect) && options.length > 0) {
			options[0].isCorrect = true;
		}
	}

	function toggleCorrect(index: number) {
		if (questionType === 'MultipleChoice') {
			options[index].isCorrect = !options[index].isCorrect;
		} else {
			// Single choice or True/False
			options = options.map((opt, i) => ({
				...opt,
				isCorrect: i === index
			}));
		}
	}

	function handleSubmit(e: Event) {
		e.preventDefault();
		if (!questionText.trim()) {
			toast.warning('Question prompt cannot be empty.');
			return;
		}

		if (questionType !== 'Essay') {
			if (options.length < 2) {
				toast.warning('Please provide at least 2 options.');
				return;
			}
			const hasCorrect = options.some((o) => o.isCorrect);
			if (!hasCorrect) {
				toast.warning('Please mark at least one option as the correct answer.');
				return;
			}
			const hasEmpty = options.some((o) => !o.text.trim());
			if (hasEmpty) {
				toast.warning('All option choices must have text.');
				return;
			}
		}

		onSave({
			questionText: questionText.trim(),
			type: questionType,
			points: Number(questionPoints) || 5,
			explanation: questionExplanation.trim() || undefined,
			options:
				questionType === 'Essay'
					? []
					: options.map((o) => ({
							id: o.id,
							text: o.text.trim(),
							isCorrect: o.isCorrect
						}))
		});
	}

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape' && isOpen && !isLoading) {
			onClose();
		}
	}
</script>

<svelte:window onkeydown={handleKeydown} />

{#if isOpen}
	<div
		class="fixed inset-0 z-50 flex items-center justify-center p-3 sm:p-6 overflow-y-auto bg-black/60 backdrop-blur-sm"
		role="dialog"
		aria-modal="true"
		transition:fade={{ duration: 160 }}
	>
		<!-- Backdrop click -->
		<div
			class="fixed inset-0 -z-10"
			onclick={() => {
				if (!isLoading) onClose();
			}}
			role="presentation"
		></div>

		<div
			class="relative w-full max-w-3xl overflow-hidden rounded-3xl bg-base-100/95 backdrop-blur-2xl border border-base-content/10 shadow-2xl p-6 sm:p-8 space-y-6 my-auto max-h-[92vh] overflow-y-auto"
			transition:scale={{ duration: 200, start: 0.95, easing: cubicOut }}
		>
			<!-- Modal Header -->
			<div class="flex items-start justify-between gap-4">
				<div class="flex items-center gap-3.5">
					<div
						class="w-11 h-11 rounded-2xl bg-primary/10 text-primary border border-primary/20 flex items-center justify-center shadow-xs flex-shrink-0"
					>
						{#if mode === 'create'}
							<Plus class="w-5 h-5" />
						{:else}
							<Edit3 class="w-5 h-5" />
						{/if}
					</div>
					<div>
						<h3 class="text-lg font-extrabold text-base-content tracking-tight">
							{mode === 'create' ? 'Add Question to Pool' : 'Edit Question'}
						</h3>
						<p class="text-xs text-base-content/60 mt-0.5">
							{#if bankTitle}
								Target Pool: <strong class="text-base-content/80">{bankTitle}</strong>
							{:else}
								Configure question statement, evaluation choices, and scoring rules.
							{/if}
						</p>
					</div>
				</div>

				<button
					type="button"
					class="btn btn-ghost btn-circle btn-sm text-base-content/50 hover:text-base-content"
					onclick={onClose}
					disabled={isLoading}
					aria-label="Close"
				>
					<X class="w-4 h-4" />
				</button>
			</div>

			<!-- Modal Form -->
			<form onsubmit={handleSubmit} class="space-y-6">
				<!-- Question Type Selector -->
				<div class="space-y-2">
					<label
						class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-base-content/80"
					>
						<Layers class="w-3.5 h-3.5 text-primary" />
						<span>Question Evaluation Type</span>
					</label>

					<div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-2.5">
						{#each questionTypes as qt}
							{@const isSelected = questionType === qt.id}
							{@const Icon = qt.icon}
							<button
								type="button"
								class="p-3 rounded-2xl border text-left transition-all relative flex flex-col justify-between gap-2.5 {isSelected
									? 'border-primary bg-primary/10 text-primary shadow-xs ring-2 ring-primary/20'
									: 'border-base-content/10 bg-base-200/40 text-base-content/70 hover:bg-base-200/80 hover:text-base-content'}"
								onclick={() => handleTypeChange(qt.id)}
							>
								<div class="flex items-center justify-between w-full">
									<div
										class="w-8 h-8 rounded-xl flex items-center justify-center {isSelected
											? 'bg-primary text-primary-content font-bold'
											: 'bg-base-300/60 text-base-content/60'}"
									>
										<Icon class="w-4 h-4" />
									</div>
									{#if isSelected}
										<span class="badge badge-xs badge-primary font-bold">Active</span>
									{/if}
								</div>
								<div>
									<p class="font-bold text-xs leading-tight {isSelected ? 'text-primary' : 'text-base-content'}">
										{qt.label}
									</p>
									<p class="text-[10px] text-base-content/50 mt-0.5 leading-snug">
										{qt.description}
									</p>
								</div>
							</button>
						{/each}
					</div>
				</div>

				<!-- Question Prompt Editor -->
				<div class="space-y-1.5">
					<label
						class="flex items-center justify-between text-xs font-bold uppercase tracking-wider text-base-content/80"
					>
						<span class="flex items-center gap-1.5">
							<HelpCircle class="w-3.5 h-3.5 text-primary" />
							<span>Question Prompt</span>
							<span class="text-error">*</span>
						</span>
						<span class="text-[10px] text-base-content/50 font-normal lowercase">Rich text, code & formulas supported</span>
					</label>
					<div class="rounded-2xl border border-base-content/10 overflow-hidden shadow-xs focus-within:border-primary/40 transition-colors">
						<RichEditor
							bind:content={questionText}
							placeholder="Compose question statement, problem scenario, or code snippet..."
						/>
					</div>
				</div>

				<!-- Points & Explanation Row -->
				<div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
					<!-- Points -->
					<div class="space-y-1.5">
						<label
							for="modal-q-points"
							class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-base-content/80"
						>
							<Award class="w-3.5 h-3.5 text-warning" />
							<span>Points / Score</span>
							<span class="text-error">*</span>
						</label>
						<input
							id="modal-q-points"
							type="number"
							step="0.5"
							min="0.5"
							bind:value={questionPoints}
							class="input input-bordered w-full bg-base-200/50 focus:bg-base-100 rounded-xl text-sm font-semibold font-mono"
							required
						/>
					</div>

					<!-- Explanation -->
					<div class="sm:col-span-2 space-y-1.5">
						<label
							for="modal-q-exp"
							class="flex items-center justify-between text-xs font-bold uppercase tracking-wider text-base-content/80"
						>
							<span class="flex items-center gap-1.5">
								<Lightbulb class="w-3.5 h-3.5 text-accent" />
								<span>Explanation / Feedback</span>
							</span>
							<span class="badge badge-xs badge-ghost text-[9px] uppercase font-mono">Optional</span>
						</label>
						<input
							id="modal-q-exp"
							type="text"
							bind:value={questionExplanation}
							placeholder="Explain the rationale behind the correct solution..."
							class="input input-bordered w-full bg-base-200/50 focus:bg-base-100 rounded-xl text-xs"
						/>
					</div>
				</div>

				<!-- Options / Answers Section -->
				{#if questionType !== 'Essay'}
					<div class="space-y-3 pt-3 border-t border-base-content/10">
						<div class="flex items-center justify-between">
							<div>
								<span class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-base-content/80">
									<ListOrdered class="w-3.5 h-3.5 text-primary" />
									<span>Answer Choices</span>
								</span>
								<p class="text-[11px] text-base-content/50 mt-0.5">
									Click the badge pill to mark as {questionType === 'MultipleChoice' ? 'a correct answer' : 'the correct answer'}.
								</p>
							</div>

							{#if questionType !== 'TrueFalse'}
								<button
									type="button"
									class="btn btn-xs btn-primary btn-outline gap-1.5 rounded-xl font-bold hover:text-white"
									onclick={addOption}
								>
									<Plus class="w-3.5 h-3.5" />
									<span>Add Option</span>
								</button>
							{/if}
						</div>

						<div class="space-y-2.5">
							{#each options as opt, idx (opt.id || idx)}
								{@const letter = String.fromCharCode(65 + idx)}
								<div
									class="flex items-center gap-3 p-2.5 sm:p-3 rounded-2xl border transition-all {opt.isCorrect
										? 'border-success/40 bg-success/5 shadow-xs'
										: 'border-base-content/10 bg-base-200/40 hover:bg-base-200/70'}"
								>
									<!-- Correct Answer Toggle Button -->
									<button
										type="button"
										class="w-8 h-8 rounded-xl font-mono text-xs font-bold flex items-center justify-center transition-all shrink-0 cursor-pointer {opt.isCorrect
											? 'bg-success text-success-content shadow-xs scale-105'
											: 'bg-base-300/70 text-base-content/70 hover:bg-base-content/20'}"
										onclick={() => toggleCorrect(idx)}
										title={opt.isCorrect ? 'Correct Answer' : 'Click to mark as correct'}
									>
										{#if opt.isCorrect}
											<Check class="w-4 h-4" />
										{:else}
											<span>{letter}</span>
										{/if}
									</button>

									<!-- Option Text Input -->
									<input
										type="text"
										bind:value={opt.text}
										disabled={questionType === 'TrueFalse'}
										placeholder="Choice statement..."
										class="input input-sm flex-1 bg-base-100/80 rounded-xl text-xs font-medium focus:bg-base-100 border-base-content/15"
										required
									/>

									<!-- Remove Option Button -->
									{#if questionType !== 'TrueFalse' && options.length > 2}
										<button
											type="button"
											class="btn btn-xs btn-circle btn-ghost text-base-content/40 hover:text-error hover:bg-error/10 shrink-0"
											onclick={() => removeOption(idx)}
											title="Delete option"
											aria-label="Delete option"
										>
											<Trash2 class="w-3.5 h-3.5" />
										</button>
									{/if}
								</div>
							{/each}
						</div>
					</div>
				{:else}
					<!-- Essay Info Card -->
					<div class="p-4 rounded-2xl bg-info/10 border border-info/20 flex items-start gap-3">
						<FileText class="w-5 h-5 text-info shrink-0 mt-0.5" />
						<div class="text-xs space-y-1">
							<p class="font-bold text-info">Essay Response Prompt</p>
							<p class="text-base-content/70 leading-relaxed">
								Candidates will provide written open-ended essay answers during examinations. Responses can be graded automatically or manually with rubric feedback.
							</p>
						</div>
					</div>
				{/if}

				<!-- Action Buttons -->
				<div class="flex items-center justify-end gap-2 pt-4 border-t border-base-content/10">
					<button
						type="button"
						class="btn btn-sm btn-ghost rounded-xl font-semibold"
						onclick={onClose}
						disabled={isLoading}
					>
						Cancel
					</button>
					<button
						type="submit"
						class="btn btn-sm btn-primary rounded-xl font-bold shadow-md gap-1.5 px-6"
						disabled={isLoading || !questionText.trim()}
					>
						{#if isLoading}
							<span class="loading loading-spinner loading-xs"></span>
						{:else}
							<Check class="w-4 h-4" />
						{/if}
						<span>{mode === 'create' ? 'Add Question' : 'Save Changes'}</span>
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}
