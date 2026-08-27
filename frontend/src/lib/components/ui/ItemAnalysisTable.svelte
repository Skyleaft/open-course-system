<script lang="ts">
	interface ItemPsychometric {
		questionId: string;
		questionText: string;
		questionType: string;
		maxPoints: number;
		totalAttempts: number;
		correctCount: number;
		difficultyIndex: number;
		difficultyLabel: string;
		discriminationIndex: number;
		discriminationStatus: string;
	}

	interface Props {
		items: ItemPsychometric[];
	}

	let { items = [] }: Props = $props();
</script>

<div class="overflow-x-auto rounded-2xl border border-white/10 bg-base-100/30 backdrop-blur-md">
	<table class="table table-sm w-full">
		<thead>
			<tr class="border-b border-white/10 text-xs text-base-content/70">
				<th class="py-3 px-4">#</th>
				<th>Question</th>
				<th>Type</th>
				<th class="text-center">Attempts</th>
				<th class="text-center">Difficulty (p-value)</th>
				<th class="text-center">Discrimination (D-index)</th>
			</tr>
		</thead>
		<tbody>
			{#if items.length === 0}
				<tr>
					<td colspan="6" class="py-8 text-center text-xs text-base-content/50">
						No item analysis data available for this exam.
					</td>
				</tr>
			{:else}
				{#each items as item, i (item.questionId)}
					<tr class="border-b border-white/5 hover:bg-base-200/40 transition-colors">
						<td class="py-3 px-4 font-mono text-xs text-base-content/60">{i + 1}</td>
						<td class="max-w-xs">
							<div class="font-medium text-xs text-base-content line-clamp-2" title={item.questionText}>
								{item.questionText}
							</div>
							<div class="text-[10px] text-base-content/50">Max Points: {item.maxPoints}</div>
						</td>
						<td>
							<span class="badge badge-ghost badge-xs font-semibold text-[10px]">
								{item.questionType}
							</span>
						</td>
						<td class="text-center">
							<div class="text-xs font-bold text-base-content">{item.correctCount} / {item.totalAttempts}</div>
							<div class="text-[10px] text-base-content/50">
								{item.totalAttempts > 0 ? Math.round((item.correctCount / item.totalAttempts) * 100) : 0}% Correct
							</div>
						</td>
						<td class="text-center">
							<div class="inline-flex items-center gap-1.5">
								<span class="font-mono text-xs font-bold">{item.difficultyIndex.toFixed(2)}</span>
								{#if item.difficultyLabel === 'Easy'}
									<span class="badge badge-success badge-xs font-bold text-[9px]">Easy</span>
								{:else if item.difficultyLabel === 'Medium'}
									<span class="badge badge-info badge-xs font-bold text-[9px]">Medium</span>
								{:else}
									<span class="badge badge-warning badge-xs font-bold text-[9px]">Hard</span>
								{/if}
							</div>
						</td>
						<td class="text-center">
							<div class="inline-flex items-center gap-1.5">
								<span class="font-mono text-xs font-bold">{item.discriminationIndex.toFixed(2)}</span>
								{#if item.discriminationStatus === 'Excellent'}
									<span class="badge badge-success badge-xs font-bold text-[9px]">Excellent</span>
								{:else if item.discriminationStatus === 'Good'}
									<span class="badge badge-primary badge-xs font-bold text-[9px]">Good</span>
								{:else}
									<span class="badge badge-error badge-xs font-bold text-[9px] animate-pulse">Needs Review</span>
								{/if}
							</div>
						</td>
					</tr>
				{/each}
			{/if}
		</tbody>
	</table>
</div>
