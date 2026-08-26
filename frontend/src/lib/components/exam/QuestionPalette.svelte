<script lang="ts">
	import type { QuizQuestion, StudentExamSectionDto } from '$lib/api/types.ts';
	import { Flag, CheckCircle2, Layers, ChevronDown, ChevronRight, Folder } from 'lucide-svelte';

	interface Props {
		questions: QuizQuestion[];
		sections?: StudentExamSectionDto[];
		currentIndex: number;
		answers: Record<string, { selectedOptionIds: string[]; essayText?: string }>;
		flaggedIds: Set<string>;
		onSelectQuestion: (index: number) => void;
	}

	let {
		questions = [],
		sections = [],
		currentIndex,
		answers,
		flaggedIds,
		onSelectQuestion
	}: Props = $props();

	let selectedSectionFilter = $state<string | 'ALL'>('ALL');

	function isAnswered(questionId: string): boolean {
		const ans = answers[questionId];
		if (!ans) return false;
		return (ans.selectedOptionIds && ans.selectedOptionIds.length > 0) || Boolean(ans.essayText?.trim());
	}

	// Group questions by section
	interface SectionGroup {
		id: string;
		title: string;
		description?: string | null;
		items: Array<{ question: QuizQuestion; globalIndex: number; sectionIndex: number }>;
		answeredCount: number;
	}

	const sectionGroups = $derived.by<SectionGroup[]>(() => {
		if (!questions || questions.length === 0) return [];

		const map = new Map<string, SectionGroup>();
		const defaultSectionId = 'default-section';

		// If sections metadata is available, register them in order
		if (sections && sections.length > 0) {
			for (const sec of sections) {
				map.set(sec.id, {
					id: sec.id,
					title: sec.title || `Section ${sec.orderIndex + 1}`,
					description: sec.description,
					items: [],
					answeredCount: 0
				});
			}
		}

		// Distribute questions into groups
		questions.forEach((q, idx) => {
			const sId = q.sectionId || defaultSectionId;
			let group = map.get(sId);
			if (!group) {
				const title = q.sectionTitle || (sections && sections.length > 0 ? 'General Section' : 'Questions');
				group = {
					id: sId,
					title,
					description: null,
					items: [],
					answeredCount: 0
				};
				map.set(sId, group);
			}

			const answered = isAnswered(q.id);
			if (answered) {
				group.answeredCount++;
			}

			group.items.push({
				question: q,
				globalIndex: idx,
				sectionIndex: group.items.length
			});
		});

		return Array.from(map.values()).filter((g) => g.items.length > 0);
	});

	const hasMultipleSections = $derived(sectionGroups.length > 1);

	// Total answered across all questions
	const totalAnswered = $derived(
		questions.reduce((acc, q) => acc + (isAnswered(q.id) ? 1 : 0), 0)
	);

	// Currently active question's section ID
	const currentSectionId = $derived.by(() => {
		const currentQ = questions[currentIndex];
		return currentQ?.sectionId || null;
	});
</script>

<div class="glass-card rounded-2xl p-4 border border-base-content/10 space-y-4 shadow-xl">
	<!-- Palette Header -->
	<div class="flex items-center justify-between border-b border-base-content/10 pb-3">
		<div class="flex items-center gap-2">
			<Layers class="w-4 h-4 text-primary" />
			<span class="text-xs font-bold uppercase tracking-wider text-base-content/80">Question Palette</span>
		</div>
		<div class="flex items-center gap-1.5">
			<span class="badge badge-sm badge-primary font-bold">{totalAnswered}/{questions.length} Answered</span>
		</div>
	</div>

	<!-- Section Filter Tabs if multiple sections exist -->
	{#if hasMultipleSections}
		<div class="space-y-1.5">
			<div class="flex items-center justify-between text-[11px] text-base-content/60 font-semibold px-0.5">
				<span>Filter by Section:</span>
				<span class="text-[10px] text-primary">{sectionGroups.length} Sections</span>
			</div>
			<div class="flex gap-1.5 overflow-x-auto pb-1 no-scrollbar">
				<button
					type="button"
					class="btn btn-xs rounded-lg whitespace-nowrap transition-all {selectedSectionFilter === 'ALL'
						? 'btn-primary shadow-sm'
						: 'btn-ghost border border-base-content/10 text-base-content/70'}"
					onclick={() => (selectedSectionFilter = 'ALL')}
				>
					All ({questions.length})
				</button>
				{#each sectionGroups as group, idx}
					{@const isCurrentSection = currentSectionId === group.id}
					{@const isAllAnswered = group.answeredCount === group.items.length && group.items.length > 0}
					<button
						type="button"
						class="btn btn-xs rounded-lg whitespace-nowrap gap-1 transition-all {selectedSectionFilter === group.id
							? 'btn-primary shadow-sm'
							: isCurrentSection
							? 'btn-outline btn-primary'
							: 'btn-ghost border border-base-content/10 text-base-content/70'}"
						onclick={() => (selectedSectionFilter = group.id)}
					>
						<span>Sec {idx + 1}</span>
						<span class="text-[10px] opacity-80">({group.answeredCount}/{group.items.length})</span>
						{#if isAllAnswered}
							<CheckCircle2 class="w-3 h-3 text-success" />
						{/if}
					</button>
				{/each}
			</div>
		</div>
	{/if}

	<!-- Question Sections Grid -->
	<div class="space-y-4 max-h-[420px] overflow-y-auto pr-1">
		{#each sectionGroups as group, groupIdx (group.id)}
			{#if selectedSectionFilter === 'ALL' || selectedSectionFilter === group.id}
				{@const isCurrentSec = currentSectionId === group.id}
				{@const allDone = group.answeredCount === group.items.length}

				<div class="space-y-2 {hasMultipleSections ? 'p-2.5 rounded-xl bg-base-200/40 border border-base-content/5' : ''}">
					{#if hasMultipleSections}
						<div class="flex items-center justify-between px-0.5">
							<div class="flex items-center gap-1.5 min-w-0">
								<Folder class="w-3.5 h-3.5 {isCurrentSec ? 'text-primary' : 'text-base-content/50'} flex-shrink-0" />
								<span class="text-xs font-bold text-base-content truncate" title={group.title}>
									{group.title}
								</span>
							</div>
							<span class="badge {allDone ? 'badge-success' : 'badge-neutral'} badge-xs text-[10px] font-semibold flex-shrink-0">
								{group.answeredCount}/{group.items.length}
							</span>
						</div>
					{/if}

					<!-- Numbers Grid -->
					<div class="grid grid-cols-5 gap-1.5">
						{#each group.items as item (item.question.id)}
							{@const answered = isAnswered(item.question.id)}
							{@const flagged = flaggedIds.has(item.question.id)}
							{@const isCurrent = item.globalIndex === currentIndex}

							<button
								type="button"
								class="relative flex h-8 sm:h-9 w-full items-center justify-center rounded-xl text-xs font-bold transition-all {isCurrent
									? 'ring-2 ring-primary ring-offset-2 ring-offset-base-300 scale-105 shadow-md font-extrabold z-10'
									: ''} {answered
									? 'bg-success/20 text-success border border-success/40 hover:bg-success/30'
									: 'bg-base-100/60 text-base-content/70 border border-base-content/10 hover:bg-base-100 hover:text-base-content'}"
								onclick={() => onSelectQuestion(item.globalIndex)}
								title="Question {item.globalIndex + 1}{answered ? ' (Answered)' : ' (Unanswered)'}{flagged ? ' [Flagged]' : ''}"
							>
								{item.globalIndex + 1}
								{#if flagged}
									<span class="absolute -top-1 -right-1 flex h-3.5 w-3.5 items-center justify-center rounded-full bg-warning shadow-sm">
										<Flag class="h-2 w-2 text-warning-content" />
									</span>
								{/if}
							</button>
						{/each}
					</div>
				</div>
			{/if}
		{/each}
	</div>

	<!-- Legend -->
	<div class="pt-3 border-t border-base-content/10 grid grid-cols-2 gap-2 text-[10px] text-base-content/70">
		<div class="flex items-center gap-1.5">
			<span class="h-2.5 w-2.5 rounded-sm bg-success/40 border border-success"></span>
			Answered ({totalAnswered})
		</div>
		<div class="flex items-center gap-1.5">
			<span class="h-2.5 w-2.5 rounded-sm bg-base-100 border border-base-content/20"></span>
			Unanswered ({questions.length - totalAnswered})
		</div>
		<div class="flex items-center gap-1.5">
			<span class="h-2.5 w-2.5 rounded-sm bg-warning"></span>
			Flagged ({flaggedIds.size})
		</div>
		<div class="flex items-center gap-1.5">
			<span class="h-2.5 w-2.5 rounded-sm ring-2 ring-primary bg-primary/20"></span>
			Current #{currentIndex + 1}
		</div>
	</div>
</div>
