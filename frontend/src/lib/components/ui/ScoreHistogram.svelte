<script lang="ts">
	interface Bucket {
		rangeLabel: string;
		studentCount: number;
		minScore?: number;
		maxScore?: number;
	}

	interface Props {
		buckets: Bucket[];
		passingScore?: number;
		height?: number;
	}

	let {
		buckets = [],
		passingScore = 70,
		height = 180
	}: Props = $props();

	let maxCount = $derived(Math.max(...buckets.map(b => b.studentCount), 1));
	let totalStudents = $derived(buckets.reduce((acc, b) => acc + b.studentCount, 0));
</script>

<div class="relative w-full overflow-hidden rounded-2xl bg-base-100/30 p-4 border border-white/5 backdrop-blur-md">
	<div class="mb-3 flex items-center justify-between text-xs font-semibold text-base-content/70">
		<span>Score Distribution ({totalStudents} submissions)</span>
		<div class="flex items-center gap-2 text-[10px]">
			<span class="inline-flex items-center gap-1"><span class="h-2 w-2 rounded-full bg-error"></span> &lt; {passingScore}% (Fail)</span>
			<span class="inline-flex items-center gap-1"><span class="h-2 w-2 rounded-full bg-success"></span> &ge; {passingScore}% (Pass)</span>
		</div>
	</div>

	<div class="flex items-end gap-2.5 pt-4" style="height: {height}px;">
		{#if buckets.length === 0}
			<div class="flex h-full w-full items-center justify-center text-xs text-base-content/40">
				No score distribution data
			</div>
		{:else}
			{#each buckets as bucket}
				{@const heightPercent = Math.max(8, (bucket.studentCount / maxCount) * 100)}
				{@const isPassing = (bucket.maxScore ?? 100) >= passingScore}
				<div class="group relative flex flex-1 flex-col items-center justify-end h-full">
					<!-- Count Tooltip -->
					<div class="opacity-0 group-hover:opacity-100 transition-opacity absolute -top-7 rounded-md bg-base-300 px-1.5 py-0.5 text-[10px] font-bold text-base-content shadow border border-white/10 pointer-events-none z-10 whitespace-nowrap">
						{bucket.studentCount} students
					</div>

					<!-- Bar -->
					<div
						class="w-full rounded-t-xl transition-all duration-300 group-hover:brightness-110 {isPassing ? 'bg-gradient-to-t from-success/40 to-success text-success-content' : 'bg-gradient-to-t from-error/40 to-error text-error-content'}"
						style="height: {heightPercent}%;"
					>
						{#if bucket.studentCount > 0}
							<div class="pt-1 text-center text-[10px] font-bold opacity-90">
								{bucket.studentCount}
							</div>
						{/if}
					</div>

					<!-- Label -->
					<div class="mt-2 text-center text-[10px] font-medium text-base-content/60 truncate w-full">
						{bucket.rangeLabel}
					</div>
				</div>
			{/each}
		{/if}
	</div>
</div>
