<script lang="ts">
	import type { QuizQuestion } from '#lib/api/types.ts';
	import { Flag, CheckCircle2 } from '@lucide/svelte';

	interface Props {
		questions: QuizQuestion[];
		currentIndex: number;
		answers: Record<string, { selectedOptionIds: string[]; essayText?: string }>;
		flaggedIds: Set<string>;
		onSelectQuestion: (index: number) => void;
	}

	let {
		questions = [],
		currentIndex,
		answers,
		flaggedIds,
		onSelectQuestion
	}: Props = $props();

	function isAnswered(questionId: string): boolean {
		const ans = answers[questionId];
		if (!ans) return false;
		return (ans.selectedOptionIds && ans.selectedOptionIds.length > 0) || Boolean(ans.essayText?.trim());
	}
</script>

<div class="glass-card rounded-2xl p-4 border border-white/10 space-y-3">
	<div class="flex items-center justify-between border-b border-white/10 pb-2">
		<span class="text-xs font-bold uppercase tracking-wider text-base-content/60">Question Palette</span>
		<span class="text-[11px] font-semibold text-primary">{questions.length} Questions</span>
	</div>

	<!-- Numbers Grid -->
	<div class="grid grid-cols-5 gap-2 max-h-60 overflow-y-auto pr-1">
		{#each questions as q, idx (q.id)}
			{@const answered = isAnswered(q.id)}
			{@const flagged = flaggedIds.has(q.id)}
			{@const isCurrent = idx === currentIndex}

			<button
				class="relative flex h-9 w-full items-center justify-center rounded-xl text-xs font-bold transition-all {isCurrent
					? 'ring-2 ring-primary ring-offset-2 ring-offset-base-300'
					: ''} {answered
					? 'bg-success/20 text-success border border-success/40'
					: 'bg-base-100/50 text-base-content/70 border border-white/5 hover:bg-base-100/80'}"
				onclick={() => onSelectQuestion(idx)}
			>
				{idx + 1}
				{#if flagged}
					<span class="absolute -top-1 -right-1 flex h-3 w-3 items-center justify-center rounded-full bg-warning">
						<Flag class="h-2 w-2 text-warning-content" />
					</span>
				{/if}
			</button>
		{/each}
	</div>

	<!-- Legend -->
	<div class="pt-2 border-t border-white/10 grid grid-cols-2 gap-2 text-[10px] text-base-content/60">
		<div class="flex items-center gap-1.5">
			<span class="h-2.5 w-2.5 rounded-sm bg-success/40 border border-success"></span>
			Answered
		</div>
		<div class="flex items-center gap-1.5">
			<span class="h-2.5 w-2.5 rounded-sm bg-base-100/50 border border-white/10"></span>
			Unanswered
		</div>
		<div class="flex items-center gap-1.5">
			<span class="h-2.5 w-2.5 rounded-sm bg-warning"></span>
			Flagged
		</div>
		<div class="flex items-center gap-1.5">
			<span class="h-2.5 w-2.5 rounded-sm ring-1 ring-primary"></span>
			Current
		</div>
	</div>
</div>
