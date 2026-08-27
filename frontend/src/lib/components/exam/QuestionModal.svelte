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
		Layers,
		Scale,
		ShieldCheck,
		Sliders,
		Percent,
		Radio
	} from 'lucide-svelte';
	import type { QuestionType, GradingMethod, QuestionOption, BankQuestion } from '$lib/api/types.ts';
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
			gradingMethod?: GradingMethod;
			points: number;
			explanation?: string;
			options: Array<{ id?: string; text: string; isCorrect: boolean; points?: number; penaltyPoints?: number }>;
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
	let questionGradingMethod = $state<GradingMethod>('AllOrNothing');
	let questionPoints = $state<number>(5);
	let questionExplanation = $state('');
	let options = $state<Array<{ id?: string; text: string; isCorrect: boolean; points?: number; penaltyPoints?: number }>>([]);

	const questionTypes: Array<{
		id: QuestionType;
		label: string;
		description: string;
		icon: typeof CheckCircle2;
	}> = [
		{
			id: 'SingleChoice',
			label: 'Single Choice',
			description: 'One option selected by candidate',
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

	const multipleChoiceStrategies: Array<{
		id: GradingMethod;
		label: string;
		tag: string;
		description: string;
		icon: typeof Scale;
	}> = [
		{
			id: 'PartialWithPenalty',
			label: 'Partial with Penalty',
			tag: 'Recommended',
			description: 'Proportional +points for correct, -points for wrong choices (prevents select-all exploit)',
			icon: ShieldCheck
		},
		{
			id: 'AllOrNothing',
			label: 'All or Nothing',
			tag: 'Strict',
			description: 'Requires selecting all correct choices with zero errors to get points',
			icon: CheckSquare
		},
		{
			id: 'PartialWithoutPenalty',
			label: 'Partial (No Penalty)',
			tag: 'Lenient',
			description: 'Proportional credit for correct choices only if no wrong choices chosen',
			icon: Percent
		},
		{
			id: 'OptionWeighted',
			label: 'Custom Option Points',
			tag: 'Weighted',
			description: 'Custom points and penalties configured individually on each option choice',
			icon: Sliders
		}
	];

	const singleChoiceStrategies: Array<{
		id: GradingMethod;
		label: string;
		tag: string;
		description: string;
		icon: typeof Scale;
	}> = [
		{
			id: 'AllOrNothing',
			label: 'Standard Scoring',
			tag: 'Default',
			description: 'Candidate receives total question points if they pick the marked correct answer choice',
			icon: CheckCircle2
		},
		{
			id: 'OptionWeighted',
			label: 'Option-Weighted Points',
			tag: 'Likert / Tiered',
			description: 'Each choice awards custom points (ideal for Likert scales, surveys, or tiered answers)',
			icon: Sliders
		}
	];

	// Derived list of strategies based on current questionType
	const currentStrategies = $derived(
		questionType === 'MultipleChoice'
			? multipleChoiceStrategies
			: questionType === 'SingleChoice'
				? singleChoiceStrategies
				: []
	);

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
					
					const rawMethod = currentQuestion.gradingMethod;
					if (rawMethod && rawMethod !== 'null' && rawMethod !== 'undefined' && ['AllOrNothing', 'PartialWithPenalty', 'PartialWithoutPenalty', 'OptionWeighted'].includes(rawMethod)) {
						questionGradingMethod = rawMethod as GradingMethod;
					} else if (questionType === 'MultipleChoice') {
						questionGradingMethod = 'PartialWithPenalty';
					} else {
						questionGradingMethod = 'AllOrNothing';
					}

					questionPoints = currentQuestion.points || 5;
					questionExplanation = currentQuestion.explanation || '';
					const loadedOpts = (currentQuestion.options || []).map((o) => ({
						id: o.id,
						text: o.text || '',
						isCorrect: Boolean(o.isCorrect),
						points: o.points ?? 0,
						penaltyPoints: o.penaltyPoints ?? 0
					}));
					if (loadedOpts.length === 0 && questionType !== 'Essay') {
						initDefaultOptions(questionType);
					} else {
						options = loadedOpts;
					}
				} else {
					questionText = '';
					questionType = 'SingleChoice';
					questionGradingMethod = 'AllOrNothing';
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
				{ text: 'True', isCorrect: true, points: 0, penaltyPoints: 0 },
				{ text: 'False', isCorrect: false, points: 0, penaltyPoints: 0 }
			];
		} else if (type === 'Essay') {
			options = [];
		} else {
			options = [
				{ text: 'Option A', isCorrect: true, points: 0, penaltyPoints: 0 },
				{ text: 'Option B', isCorrect: false, points: 0, penaltyPoints: 0 },
				{ text: 'Option C', isCorrect: false, points: 0, penaltyPoints: 0 },
				{ text: 'Option D', isCorrect: false, points: 0, penaltyPoints: 0 }
			];
		}
	}

	function handleTypeChange(newType: QuestionType) {
		if (questionType === newType) return;
		const oldType = questionType;
		questionType = newType;

		if (newType === 'MultipleChoice') {
			if (questionGradingMethod !== 'OptionWeighted') {
				questionGradingMethod = 'PartialWithPenalty';
			}
		} else if (newType === 'SingleChoice') {
			if (questionGradingMethod !== 'OptionWeighted') {
				questionGradingMethod = 'AllOrNothing';
			}
		} else {
			questionGradingMethod = 'AllOrNothing';
		}

		if (newType === 'TrueFalse') {
			options = [
				{ text: 'True', isCorrect: true, points: 0, penaltyPoints: 0 },
				{ text: 'False', isCorrect: false, points: 0, penaltyPoints: 0 }
			];
		} else if (newType === 'Essay') {
			options = [];
		} else if (options.length === 0 || oldType === 'TrueFalse') {
			options = [
				{ text: 'Option A', isCorrect: true, points: 0, penaltyPoints: 0 },
				{ text: 'Option B', isCorrect: false, points: 0, penaltyPoints: 0 },
				{ text: 'Option C', isCorrect: false, points: 0, penaltyPoints: 0 },
				{ text: 'Option D', isCorrect: false, points: 0, penaltyPoints: 0 }
			];
		} else if (newType === 'SingleChoice') {
			// Ensure only one is correct if standard scoring
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
		options = [...options, { text: `Option ${letter}`, isCorrect: false, points: 0, penaltyPoints: 0 }];
	}

	function removeOption(index: number) {
		if (options.length <= 2) {
			toast.warning('A choice question must have at least 2 options.');
			return;
		}
		const wasCorrect = options[index].isCorrect;
		options = options.filter((_, i) => i !== index);
		if (wasCorrect && !options.some((o) => o.isCorrect) && options.length > 0 && questionGradingMethod !== 'OptionWeighted') {
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
			if (!hasCorrect && questionGradingMethod !== 'OptionWeighted') {
				toast.warning('Please mark at least one option as the correct answer.');
				return;
			}
			const hasEmpty = options.some((o) => !o.text.trim());
			if (hasEmpty) {
				toast.warning('All option choices must have text.');
				return;
			}
		}

		const safeGradingMethod: GradingMethod =
			questionGradingMethod === 'OptionWeighted'
				? 'OptionWeighted'
				: questionType === 'MultipleChoice'
					? (questionGradingMethod || 'PartialWithPenalty')
					: 'AllOrNothing';

		onSave({
			questionText: questionText.trim(),
			type: questionType,
			gradingMethod: safeGradingMethod,
			points: Number(questionPoints) || 5,
			explanation: questionExplanation.trim() || undefined,
			options:
				questionType === 'Essay'
					? []
					: options.map((o) => ({
							id: o.id,
							text: o.text.trim(),
							isCorrect: o.isCorrect,
							points: Number(o.points) || 0,
							penaltyPoints: Number(o.penaltyPoints) || 0
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

				<!-- Grading Strategy Selector (For Multiple Choice or Single Choice) -->
				{#if currentStrategies.length > 0}
					<div class="space-y-2 p-3.5 rounded-2xl bg-primary/5 border border-primary/15">
						<div class="flex items-center justify-between">
							<label class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-primary">
								<Scale class="w-3.5 h-3.5" />
								<span>{questionType === 'MultipleChoice' ? 'Multiple Choice Grading Strategy' : 'Single Choice Scoring Mode'}</span>
							</label>
							<span class="badge badge-xs badge-primary badge-outline font-mono text-[9px]">
								{questionType === 'MultipleChoice' ? 'Multi-Select Engine' : 'Single-Select Engine'}
							</span>
						</div>

						<div class="grid grid-cols-1 sm:grid-cols-2 gap-2">
							{#each currentStrategies as gs}
								{@const isSelected = questionGradingMethod === gs.id}
								{@const Icon = gs.icon}
								<button
									type="button"
									class="p-2.5 rounded-xl border text-left transition-all flex items-start gap-2.5 {isSelected
										? 'border-primary bg-primary/15 text-primary shadow-xs ring-1 ring-primary/30'
										: 'border-base-content/10 bg-base-100/70 text-base-content/70 hover:bg-base-200/80 hover:text-base-content'}"
									onclick={() => (questionGradingMethod = gs.id)}
								>
									<div
										class="w-6 h-6 rounded-lg flex items-center justify-center shrink-0 mt-0.5 {isSelected
											? 'bg-primary text-white font-bold'
											: 'bg-base-200 text-base-content/60'}"
									>
										<Icon class="w-3.5 h-3.5" />
									</div>
									<div class="min-w-0 flex-1">
										<div class="flex items-center justify-between gap-1">
											<p class="font-bold text-xs leading-tight truncate {isSelected ? 'text-primary' : 'text-base-content'}">
												{gs.label}
											</p>
											<span class="badge badge-xs {isSelected ? 'badge-primary' : 'badge-ghost'} text-[9px] font-semibold">{gs.tag}</span>
										</div>
										<p class="text-[10px] text-base-content/60 mt-0.5 line-clamp-2 leading-relaxed">
											{gs.description}
										</p>
									</div>
								</button>
							{/each}
						</div>
					</div>
				{/if}

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
							<span>{questionGradingMethod === 'OptionWeighted' ? 'Max Total Points' : 'Total Points'}</span>
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
									{#if questionGradingMethod === 'OptionWeighted'}
										Specify exact reward point (+Pts) for each choice. Candidate will be awarded the points of the option they select.
									{:else}
										Click the badge pill to mark as {questionType === 'MultipleChoice' ? 'a correct answer' : 'the correct answer'}.
									{/if}
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
									class="flex items-center gap-2.5 p-2.5 sm:p-3 rounded-2xl border transition-all {opt.isCorrect
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

									<!-- Custom Option Points (when OptionWeighted) -->
									{#if questionGradingMethod === 'OptionWeighted'}
										<div class="flex items-center gap-1.5 shrink-0">
											<div class="flex items-center gap-1" title="Awarded points for choosing this option">
												<span class="text-[10px] font-bold text-success">+</span>
												<input
													type="number"
													step="0.5"
													bind:value={opt.points}
													placeholder="Pts"
													class="input input-xs w-16 bg-base-100 rounded-lg text-center font-mono text-xs border-base-content/20 text-success font-bold"
												/>
											</div>
											{#if questionType === 'MultipleChoice'}
												<div class="flex items-center gap-1" title="Penalty deducted for choosing this option">
													<span class="text-[10px] font-bold text-error">-</span>
													<input
														type="number"
														step="0.5"
														bind:value={opt.penaltyPoints}
														placeholder="Pen"
														class="input input-xs w-16 bg-base-100 rounded-lg text-center font-mono text-xs border-base-content/20 text-error font-bold"
													/>
												</div>
											{/if}
										</div>
									{/if}

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
