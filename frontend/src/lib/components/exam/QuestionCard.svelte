<script lang="ts">
	import type { QuizQuestion } from '$lib/api/types.ts';
	import RichRenderer from '$lib/components/editor/RichRenderer.svelte';
	import RichEditor from '$lib/components/editor/RichEditor.svelte';
	import { Flag, Check, Folder, Sparkles } from 'lucide-svelte';

	interface Props {
		question: QuizQuestion;
		index: number;
		total: number;
		sectionIndex?: number;
		sectionTotal?: number;
		selectedOptionIds: string[];
		essayText: string;
		isFlagged: boolean;
		onToggleOption: (optionId: string, isSingle: boolean) => void;
		onEssayChange: (text: string) => void;
		onToggleFlag: () => void;
	}

	let {
		question,
		index,
		total,
		sectionIndex,
		sectionTotal,
		selectedOptionIds = [],
		essayText = '',
		isFlagged = false,
		onToggleOption,
		onEssayChange,
		onToggleFlag
	}: Props = $props();

	const isSingleSelection = $derived(
		question.type === 'SingleChoice' || question.type === 'TrueFalse'
	);

	const optionLabels = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H'];
</script>

<div class="glass-card overflow-hidden rounded-3xl border border-base-content/10 p-6 sm:p-8 shadow-xl space-y-6">
	<!-- Section Context Banner if question is part of a named section -->
	{#if question.sectionTitle}
		<div class="flex items-center justify-between p-3 rounded-2xl bg-primary/10 border border-primary/20 text-xs">
			<div class="flex items-center gap-2 text-primary font-bold">
				<Folder class="w-4 h-4" />
				<span>Section: {question.sectionTitle}</span>
			</div>
			{#if sectionIndex !== undefined && sectionTotal !== undefined}
				<span class="badge badge-primary badge-sm font-semibold">
					Question {sectionIndex + 1} of {sectionTotal}
				</span>
			{/if}
		</div>
	{/if}

	<!-- Question Header -->
	<div class="flex items-center justify-between border-b border-base-content/10 pb-4">
		<div class="flex items-center gap-2 flex-wrap">
			<span class="badge badge-primary badge-sm font-bold">
				Question {index + 1} of {total}
			</span>
			<span class="badge badge-ghost badge-xs uppercase font-semibold text-base-content/70">
				{question.type}
			</span>
			<span class="text-xs font-semibold text-base-content/70">
				({question.points} {question.points === 1 ? 'pt' : 'pts'})
			</span>
		</div>

		<button
			type="button"
			class="btn btn-ghost btn-xs gap-1.5 rounded-xl {isFlagged ? 'text-warning bg-warning/10 font-bold border border-warning/30' : 'text-base-content/70 hover:text-base-content hover:bg-base-200'}"
			onclick={onToggleFlag}
		>
			<Flag class="h-3.5 w-3.5 {isFlagged ? 'fill-warning' : ''}" />
			{isFlagged ? 'Flagged' : 'Flag Question'}
		</button>
	</div>

	<!-- Question Prompt Text (KaTeX / Markdown rendered) -->
	<div class="prose prose-invert max-w-none text-base font-medium text-base-content leading-relaxed">
		<RichRenderer content={question.text || question.questionText || ''} />
	</div>

	<!-- Options or Essay Input Area -->
	<div class="space-y-3 pt-2">
		{#if question.type === 'SingleChoice' || question.type === 'MultipleChoice' || question.type === 'TrueFalse'}
			{#each question.options || [] as option, optIdx (option.id || optIdx)}
				{@const optId = option.id || String(optIdx)}
				{@const isSelected = selectedOptionIds.includes(optId)}
				{@const optLabel = optionLabels[optIdx] || String(optIdx + 1)}

				<div
					class="group flex items-center gap-4 rounded-2xl border p-4 transition-all duration-200 cursor-pointer {isSelected
						? 'border-primary bg-primary/10 shadow-md shadow-primary/10 ring-2 ring-primary/40'
						: 'border-base-content/10 bg-base-100/60 hover:border-base-content/25 hover:bg-base-200/50'}"
					onclick={() => onToggleOption(optId, isSingleSelection)}
					role="button"
					tabindex="0"
					onkeydown={(e) => (e.key === 'Enter' || e.key === ' ') && onToggleOption(optId, isSingleSelection)}
				>
					<!-- Option Badge Letter (A, B, C...) with Selection Highlight -->
					<div class="flex h-8 w-8 shrink-0 items-center justify-center rounded-xl text-xs font-bold transition-all {isSelected
						? 'bg-primary text-primary-content shadow-sm scale-105'
						: 'bg-base-200 text-base-content/70 group-hover:bg-base-300'}">
						{#if isSelected}
							{#if isSingleSelection}
								{optLabel}
							{:else}
								<Check class="h-4 w-4 stroke-[3]" />
							{/if}
						{:else}
							{optLabel}
						{/if}
					</div>

					<!-- Option Text Content -->
					<div class="text-sm font-medium text-base-content leading-relaxed flex-1">
						{option.text}
					</div>
				</div>
			{/each}
		{:else if question.type === 'Essay'}
			<div class="space-y-2">
				<label for="essay-editor-input" class="text-xs font-semibold text-base-content/70">
					Compose your essay response:
				</label>
				<div id="essay-editor-input">
					<RichEditor
						content={essayText}
						placeholder="Type your structured solution or essay response here..."
						minHeight="240px"
						onUpdate={(json) => onEssayChange(json)}
					/>
				</div>
			</div>
		{/if}
	</div>
</div>
