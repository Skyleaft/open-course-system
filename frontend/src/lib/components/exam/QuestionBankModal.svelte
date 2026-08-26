<script lang="ts">
	import { fade, scale } from 'svelte/transition';
	import { cubicOut } from 'svelte/easing';
	import {
		FolderPlus,
		Edit3,
		X,
		BookOpen,
		Layers,
		AlignLeft,
		Tag,
		Plus,
		Check,
		Sparkles
	} from 'lucide-svelte';

	interface Props {
		isOpen: boolean;
		mode?: 'create' | 'edit';
		title?: string;
		category?: string;
		description?: string;
		tags?: string[] | string;
		suggestedCategories?: string[];
		isLoading?: boolean;
		onClose: () => void;
		onSave: (data: {
			title: string;
			category: string;
			description: string;
			tags: string[];
		}) => void | Promise<void>;
	}

	let {
		isOpen,
		mode = 'create',
		title = '',
		category = '',
		description = '',
		tags = [],
		suggestedCategories = [],
		isLoading = false,
		onClose,
		onSave
	}: Props = $props();

	let formTitle = $state('');
	let formCategory = $state('');
	let formDescription = $state('');
	let tagList = $state<string[]>([]);
	let newTagInput = $state('');

	// Sync initial state when modal opens
	$effect(() => {
		if (isOpen) {
			formTitle = title || '';
			formCategory = category || '';
			formDescription = description || '';

			if (Array.isArray(tags)) {
				tagList = [...tags];
			} else if (typeof tags === 'string') {
				tagList = tags
					.split(',')
					.map((t) => t.trim())
					.filter(Boolean);
			} else {
				tagList = [];
			}
			newTagInput = '';
		}
	});

	function addTag() {
		const trimmed = newTagInput.trim().replace(/^,+|,+$/g, '');
		if (!trimmed) return;

		// Handle comma separated input
		const parts = trimmed.split(',').map((p) => p.trim()).filter(Boolean);
		for (const part of parts) {
			if (!tagList.includes(part)) {
				tagList = [...tagList, part];
			}
		}
		newTagInput = '';
	}

	function handleTagKeydown(e: KeyboardEvent) {
		if (e.key === 'Enter' || e.key === ',') {
			e.preventDefault();
			addTag();
		} else if (e.key === 'Backspace' && !newTagInput && tagList.length > 0) {
			tagList = tagList.slice(0, -1);
		}
	}

	function removeTag(index: number) {
		tagList = tagList.filter((_, i) => i !== index);
	}

	function selectCategory(cat: string) {
		formCategory = cat;
	}

	function handleSubmit(e: Event) {
		e.preventDefault();
		if (!formTitle.trim()) return;

		if (newTagInput.trim()) {
			addTag();
		}

		onSave({
			title: formTitle.trim(),
			category: formCategory.trim(),
			description: formDescription.trim(),
			tags: tagList
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
		class="fixed inset-0 z-50 flex items-center justify-center p-4 sm:p-6 overflow-y-auto bg-black/60 backdrop-blur-sm"
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
			class="relative w-full max-w-lg overflow-hidden rounded-3xl bg-base-100/95 backdrop-blur-2xl border border-base-content/10 shadow-2xl p-6 sm:p-7 space-y-6"
			transition:scale={{ duration: 200, start: 0.95, easing: cubicOut }}
		>
			<!-- Modal Header -->
			<div class="flex items-start justify-between gap-4">
				<div class="flex items-center gap-3.5">
					<div
						class="w-11 h-11 rounded-2xl bg-primary/10 text-primary border border-primary/20 flex items-center justify-center shadow-xs flex-shrink-0"
					>
						{#if mode === 'create'}
							<FolderPlus class="w-5 h-5" />
						{:else}
							<Edit3 class="w-5 h-5" />
						{/if}
					</div>
					<div>
						<h3 class="text-lg font-extrabold text-base-content tracking-tight">
							{mode === 'create' ? 'Create Question Bank Pool' : 'Edit Question Bank Pool'}
						</h3>
						<p class="text-xs text-base-content/60 mt-0.5">
							Configure pool taxonomy, categories, and question collection settings.
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
			<form onsubmit={handleSubmit} class="space-y-4">
				<!-- Pool Title -->
				<div class="space-y-1.5">
					<label
						for="qb-pool-title"
						class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-base-content/80"
					>
						<BookOpen class="w-3.5 h-3.5 text-primary" />
						<span>Pool Title</span>
						<span class="text-error">*</span>
					</label>
					<input
						id="qb-pool-title"
						type="text"
						bind:value={formTitle}
						placeholder="e.g. C# .NET 10 Core Certification Pool"
						class="input input-bordered w-full bg-base-200/50 focus:bg-base-100 rounded-xl text-sm font-semibold transition-all"
						required
					/>
				</div>

				<!-- Category -->
				<div class="space-y-1.5">
					<label
						for="qb-pool-cat"
						class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-base-content/80"
					>
						<Layers class="w-3.5 h-3.5 text-secondary" />
						<span>Category</span>
					</label>
					<input
						id="qb-pool-cat"
						type="text"
						bind:value={formCategory}
						placeholder="e.g. Software Engineering"
						class="input input-bordered w-full bg-base-200/50 focus:bg-base-100 rounded-xl text-sm transition-all"
					/>

					{#if suggestedCategories && suggestedCategories.length > 0}
						<div class="flex items-center gap-1.5 flex-wrap pt-1">
							<span class="text-[10px] text-base-content/50 font-medium mr-0.5">Quick picks:</span>
							{#each suggestedCategories.slice(0, 5) as cat}
								<button
									type="button"
									class="badge badge-sm rounded-lg transition-all cursor-pointer {formCategory === cat
										? 'badge-primary font-bold shadow-xs'
										: 'badge-ghost text-base-content/70 hover:bg-base-300'}"
									onclick={() => selectCategory(cat)}
								>
									{cat}
								</button>
							{/each}
						</div>
					{/if}
				</div>

				<!-- Description -->
				<div class="space-y-1.5">
					<label
						for="qb-pool-desc"
						class="flex items-center justify-between text-xs font-bold uppercase tracking-wider text-base-content/80"
					>
						<span class="flex items-center gap-1.5">
							<AlignLeft class="w-3.5 h-3.5 text-accent" />
							<span>Description</span>
						</span>
						<span class="badge badge-xs badge-ghost text-[9px] uppercase font-mono">Optional</span>
					</label>
					<textarea
						id="qb-pool-desc"
						bind:value={formDescription}
						rows="3"
						placeholder="Coverage, purpose, target skills, or learning objectives..."
						class="textarea textarea-bordered w-full bg-base-200/50 focus:bg-base-100 rounded-xl text-xs transition-all leading-relaxed"
					></textarea>
				</div>

				<!-- Tags (Interactive Tag Manager) -->
				<div class="space-y-2">
					<label
						for="qb-pool-tag-input"
						class="flex items-center justify-between text-xs font-bold uppercase tracking-wider text-base-content/80"
					>
						<span class="flex items-center gap-1.5">
							<Tag class="w-3.5 h-3.5 text-primary" />
							<span>Tags & Keywords</span>
						</span>
						<span class="text-[10px] text-base-content/50 font-normal lowercase">Press Enter or comma</span>
					</label>

					<div
						class="p-2.5 rounded-2xl bg-base-200/50 border border-base-content/10 focus-within:border-primary/40 focus-within:bg-base-100 transition-all space-y-2"
					>
						{#if tagList.length > 0}
							<div class="flex items-center gap-1.5 flex-wrap">
								{#each tagList as t, idx (t)}
									<span
										class="badge badge-sm badge-primary badge-outline gap-1.5 py-2.5 px-2.5 rounded-xl font-mono text-[11px] font-semibold transition-all group"
									>
										<span>#{t}</span>
										<button
											type="button"
											class="text-primary/70 hover:text-error hover:scale-110 transition-all ml-0.5"
											onclick={() => removeTag(idx)}
											title="Remove tag"
										>
											<X class="w-3 h-3" />
										</button>
									</span>
								{/each}
							</div>
						{/if}

						<div class="flex items-center gap-1.5">
							<input
								id="qb-pool-tag-input"
								type="text"
								bind:value={newTagInput}
								onkeydown={handleTagKeydown}
								placeholder={tagList.length === 0 ? "Add tag and press Enter (e.g. csharp, efcore)" : "Add another tag..."}
								class="input input-xs bg-transparent border-0 focus:outline-none w-full text-xs font-mono"
							/>
							{#if newTagInput.trim()}
								<button
									type="button"
									class="btn btn-xs btn-primary rounded-lg font-bold shrink-0 gap-1"
									onclick={addTag}
								>
									<Plus class="w-3 h-3" />
									Add
								</button>
							{/if}
						</div>
					</div>
				</div>

				<!-- Action Buttons -->
				<div class="flex items-center justify-end gap-2 pt-3 border-t border-base-content/10">
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
						class="btn btn-sm btn-primary rounded-xl font-bold shadow-md gap-1.5 px-5"
						disabled={isLoading || !formTitle.trim()}
					>
						{#if isLoading}
							<span class="loading loading-spinner loading-xs"></span>
						{:else}
							<Check class="w-4 h-4" />
						{/if}
						<span>{mode === 'create' ? 'Create Pool' : 'Save Changes'}</span>
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}
