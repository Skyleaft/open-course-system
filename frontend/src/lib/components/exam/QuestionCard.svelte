<script lang="ts">
	import type { QuizQuestion } from '#lib/api/types.ts';
	import RichRenderer from '#lib/components/editor/RichRenderer.svelte';
	import RichEditor from '#lib/components/editor/RichEditor.svelte';
	import { Flag, CheckSquare, Square, CircleDot, Circle } from '@lucide/svelte';

	interface Props {
		question: QuizQuestion;
		index: number;
		total: number;
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
</script>

<div class="glass-panel overflow-hidden rounded-3xl border border-white/10 p-6 sm:p-8 shadow-2xl space-y-6">
	<!-- Question Header -->
	<div class="flex items-center justify-between border-b border-white/10 pb-4">
		<div class="flex items-center gap-2">
			<span class="badge badge-primary badge-sm font-bold">
				Question {index + 1} of {total}
			</span>
			<span class="badge badge-ghost badge-xs uppercase font-semibold text-base-content/60">
				{question.type}
			</span>
			<span class="text-xs font-semibold text-base-content/60">
				({question.points} pts)
			</span>
		</div>

		<button
			class="btn btn-ghost btn-xs gap-1.5 rounded-xl {isFlagged ? 'text-warning bg-warning/10 font-bold' : 'text-base-content/60 hover:text-base-content'}"
			onclick={onToggleFlag}
		>
			<Flag class="h-3.5 w-3.5" />
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
				<div
					class="glass-card flex items-center gap-3.5 rounded-2xl border p-4 transition-all duration-200 cursor-pointer {isSelected
						? 'border-primary/50 bg-primary/15 shadow-md'
						: 'border-white/5 hover:border-white/15 hover:bg-base-100/40'}"
					onclick={() => onToggleOption(optId, isSingleSelection)}
					role="button"
					tabindex="0"
					onkeydown={(e) => e.key === 'Enter' && onToggleOption(optId, isSingleSelection)}
				>
					{#if isSingleSelection}
						{#if isSelected}
							<CircleDot class="h-5 w-5 text-primary shrink-0" />
						{:else}
							<Circle class="h-5 w-5 text-base-content/40 shrink-0" />
						{/if}
					{:else}
						{#if isSelected}
							<CheckSquare class="h-5 w-5 text-primary shrink-0" />
						{:else}
							<Square class="h-5 w-5 text-base-content/40 shrink-0" />
						{/if}
					{/if}

					<div class="text-sm font-medium text-base-content leading-relaxed">
						{option.text}
					</div>
				</div>
			{/each}
		{:else if question.type === 'Essay'}
			<div class="space-y-2">
				<label class="text-xs font-semibold text-base-content/70">Compose your essay response:</label>
				<RichEditor
					content={essayText}
					placeholder="Type your structured solution here..."
					minHeight="220px"
					onUpdate={(json) => onEssayChange(json)}
				/>
			</div>
		{/if}
	</div>
</div>
