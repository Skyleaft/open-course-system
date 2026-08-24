<script lang="ts">
	import type { QuestionBank } from '#lib/api/types.ts';
	import GlassModal from '#lib/components/ui/GlassModal.svelte';
	import {
		Search,
		BookOpen,
		Layers,
		Check,
		FolderPlus,
		Tag,
		Sparkles,
		Plus
	} from '@lucide/svelte';

	interface Props {
		isOpen: boolean;
		questionBanks: QuestionBank[];
		selectedBankId?: string;
		onSelect: (bank: QuestionBank) => void;
		onClose: () => void;
		onCreateNew?: () => void;
	}

	let {
		isOpen = false,
		questionBanks = [],
		selectedBankId = '',
		onSelect,
		onClose,
		onCreateNew
	}: Props = $props();

	let searchTerm = $state('');
	let selectedCategory = $state<string>('All');

	// Extract unique categories
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

	const filteredBanks = $derived(
		questionBanks.filter((b) => {
			const matchCategory = selectedCategory === 'All' || b.category === selectedCategory;
			const search = searchTerm.trim().toLowerCase();
			const matchSearch =
				!search ||
				b.title.toLowerCase().includes(search) ||
				(b.description && b.description.toLowerCase().includes(search)) ||
				(b.tags && b.tags.some((t) => t.toLowerCase().includes(search)));
			return matchCategory && matchSearch;
		})
	);
</script>

<GlassModal
	{isOpen}
	title="Select Question Bank Package"
	maxWidth="max-w-2xl"
	{onClose}
>
	<div class="space-y-4">
		<p class="text-xs text-base-content/70">
			Link this exam section to an existing Question Bank package. Questions in the chosen pool will be automatically included in this section.
		</p>

		<!-- Search & Filter Controls -->
		<div class="flex flex-col sm:flex-row gap-2">
			<div class="relative flex-1">
				<Search class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-base-content/40" />
				<input
					type="text"
					bind:value={searchTerm}
					placeholder="Search question pool title, description, or tags..."
					class="input input-bordered input-sm w-full pl-9 bg-base-100/50"
				/>
			</div>

			<select
				bind:value={selectedCategory}
				class="select select-bordered select-sm bg-base-100/50 text-xs"
			>
				{#each categories as cat}
					<option value={cat}>{cat === 'All' ? 'All Categories' : cat}</option>
				{/each}
			</select>
		</div>

		<!-- Question Banks Grid / List -->
		<div class="max-h-72 overflow-y-auto space-y-2.5 pr-1">
			{#if filteredBanks.length === 0}
				<div class="py-8 text-center bg-base-200/40 rounded-xl border border-dashed border-base-300">
					<FolderPlus class="w-8 h-8 text-base-content/30 mx-auto mb-2" />
					<p class="text-xs font-semibold text-base-content/70">No Question Banks found</p>
					<p class="text-[11px] text-base-content/50 mt-0.5">Try a different search query or create a new question pool.</p>
					{#if onCreateNew}
						<button
							type="button"
							class="btn btn-xs btn-primary gap-1.5 mt-3"
							onclick={() => {
								onClose();
								onCreateNew?.();
							}}
						>
							<Plus class="w-3.5 h-3.5" />
							Create New Question Bank
						</button>
					{/if}
				</div>
			{:else}
				{#each filteredBanks as bank (bank.id)}
					{@const isSelected = selectedBankId === bank.id}
					<div
						role="button"
						tabindex="0"
						class="w-full text-left p-3.5 rounded-xl border transition-all cursor-pointer flex items-center justify-between gap-3 group {isSelected ? 'bg-primary/10 border-primary shadow-sm' : 'bg-base-200/50 hover:bg-base-200 border-base-content/10'}"
						onclick={() => onSelect(bank)}
						onkeydown={(e) => e.key === 'Enter' && onSelect(bank)}
					>
						<div class="min-w-0 flex-1">
							<div class="flex items-center gap-2 flex-wrap">
								<span class="font-bold text-sm text-base-content group-hover:text-primary transition-colors">
									{bank.title}
								</span>
								{#if bank.category}
									<span class="badge badge-sm badge-outline badge-primary text-[10px]">
										{bank.category}
									</span>
								{/if}
								<span class="badge badge-sm badge-ghost text-[10px]">
									{bank.questions?.length || 0} questions
								</span>
							</div>

							{#if bank.description}
								<p class="text-xs text-base-content/70 line-clamp-1 mt-1">
									{bank.description}
								</p>
							{/if}

							{#if bank.tags && bank.tags.length > 0}
								<div class="flex items-center gap-1 mt-1.5 flex-wrap">
									<Tag class="w-3 h-3 text-base-content/40" />
									{#each bank.tags.slice(0, 3) as tag}
										<span class="text-[10px] text-base-content/60 bg-base-300/60 px-1.5 py-0.5 rounded">
											#{tag}
										</span>
									{/each}
									{#if bank.tags.length > 3}
										<span class="text-[10px] text-base-content/40">+{bank.tags.length - 3}</span>
									{/if}
								</div>
							{/if}
						</div>

						<div class="flex-shrink-0">
							{#if isSelected}
								<div class="w-7 h-7 rounded-full bg-primary text-primary-content flex items-center justify-center shadow-sm">
									<Check class="w-4 h-4" />
								</div>
							{:else}
								<div class="w-7 h-7 rounded-full border border-base-content/20 flex items-center justify-center group-hover:border-primary group-hover:text-primary transition-colors">
									<Check class="w-3.5 h-3.5 opacity-0 group-hover:opacity-100" />
								</div>
							{/if}
						</div>
					</div>
				{/each}
			{/if}
		</div>

		<!-- Footer -->
		<div class="flex justify-end gap-2 pt-2 border-t border-base-content/10">
			<button type="button" class="btn btn-sm btn-ghost" onclick={onClose}>
				Cancel
			</button>
		</div>
	</div>
</GlassModal>
