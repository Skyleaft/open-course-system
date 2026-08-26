<script lang="ts">
	import { fade, scale } from 'svelte/transition';
	import { cubicOut } from 'svelte/easing';
	import {
		FileUp,
		FileText,
		Download,
		X,
		UploadCloud,
		CheckCircle2,
		AlertCircle,
		BookOpen,
		Layers,
		Tag,
		AlignLeft,
		Plus,
		Check
	} from 'lucide-svelte';

	interface Props {
		isOpen: boolean;
		targetBankTitle?: string;
		isDownloadingTemplate?: boolean;
		isLoading?: boolean;
		suggestedCategories?: string[];
		onClose: () => void;
		onDownloadTemplate: () => void | Promise<void>;
		onImport: (payload: {
			file: File;
			title?: string;
			category?: string;
			description?: string;
			tags?: string[];
		}) => void | Promise<void>;
	}

	let {
		isOpen,
		targetBankTitle = '',
		isDownloadingTemplate = false,
		isLoading = false,
		suggestedCategories = [],
		onClose,
		onDownloadTemplate,
		onImport
	}: Props = $props();

	let file = $state<File | null>(null);
	let title = $state('');
	let category = $state('');
	let description = $state('');
	let tagList = $state<string[]>([]);
	let newTagInput = $state('');
	let isDragging = $state(false);
	let fileInputRef: HTMLInputElement | null = null;

	// Reset state on open
	$effect(() => {
		if (isOpen) {
			file = null;
			title = '';
			category = '';
			description = '';
			tagList = [];
			newTagInput = '';
			isDragging = false;
		}
	});

	function handleFileSelected(selectedFile: File) {
		if (
			!selectedFile.name.endsWith('.docx') &&
			selectedFile.type !==
				'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
		) {
			return;
		}
		file = selectedFile;
		if (!title.trim() && !targetBankTitle) {
			title = selectedFile.name.replace(/\.[^/.]+$/, '');
		}
	}

	function handleDragOver(e: DragEvent) {
		e.preventDefault();
		isDragging = true;
	}

	function handleDragLeave() {
		isDragging = false;
	}

	function handleDrop(e: DragEvent) {
		e.preventDefault();
		isDragging = false;
		if (e.dataTransfer?.files && e.dataTransfer.files.length > 0) {
			handleFileSelected(e.dataTransfer.files[0]);
		}
	}

	function addTag() {
		const trimmed = newTagInput.trim().replace(/^,+|,+$/g, '');
		if (!trimmed) return;
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

	function handleSubmit(e: Event) {
		e.preventDefault();
		if (!file) return;

		if (newTagInput.trim()) {
			addTag();
		}

		onImport({
			file,
			title: title.trim() || undefined,
			category: category.trim() || undefined,
			description: description.trim() || undefined,
			tags: tagList.length > 0 ? tagList : undefined
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
			class="relative w-full max-w-lg overflow-hidden rounded-3xl bg-base-100/95 backdrop-blur-2xl border border-base-content/10 shadow-2xl p-6 sm:p-7 space-y-5 my-auto"
			transition:scale={{ duration: 200, start: 0.95, easing: cubicOut }}
		>
			<!-- Modal Header -->
			<div class="flex items-start justify-between gap-4">
				<div class="flex items-center gap-3.5">
					<div
						class="w-11 h-11 rounded-2xl bg-primary/10 text-primary border border-primary/20 flex items-center justify-center shadow-xs flex-shrink-0"
					>
						<FileUp class="w-5 h-5" />
					</div>
					<div>
						<h3 class="text-lg font-extrabold text-base-content tracking-tight">
							Import Questions from Word
						</h3>
						<p class="text-xs text-base-content/60 mt-0.5">
							{#if targetBankTitle}
								Append parsed questions into <strong>{targetBankTitle}</strong>.
							{:else}
								Extract and parse questions to generate a new Question Bank pool.
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

			<!-- Template Helper Card -->
			<div
				class="p-3.5 rounded-2xl bg-primary/5 border border-primary/15 flex items-center justify-between gap-3"
			>
				<div class="flex items-center gap-2.5 min-w-0">
					<div class="w-8 h-8 rounded-xl bg-primary/10 text-primary flex items-center justify-center flex-shrink-0">
						<FileText class="w-4 h-4" />
					</div>
					<div class="min-w-0">
						<p class="text-xs font-bold text-base-content leading-tight">Word Document Template</p>
						<p class="text-[11px] text-base-content/60 truncate">
							Use standard format for single, multiple, true/false & essay questions.
						</p>
					</div>
				</div>

				<button
					type="button"
					class="btn btn-xs btn-primary btn-outline gap-1.5 rounded-xl font-bold shrink-0 hover:text-white"
					onclick={onDownloadTemplate}
					disabled={isDownloadingTemplate}
				>
					{#if isDownloadingTemplate}
						<span class="loading loading-spinner loading-xs"></span>
					{:else}
						<Download class="w-3.5 h-3.5" />
					{/if}
					<span>Template</span>
				</button>
			</div>

			<form onsubmit={handleSubmit} class="space-y-4">
				<!-- Drag & Drop Dropzone -->
				<div class="space-y-1.5">
					<span class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-base-content/80">
						<UploadCloud class="w-3.5 h-3.5 text-primary" />
						<span>Select Document (.docx)</span>
						<span class="text-error">*</span>
					</span>

					<input
						type="file"
						accept=".docx,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
						bind:this={fileInputRef}
						class="hidden"
						onchange={(e) => {
							const target = e.currentTarget;
							if (target.files && target.files.length > 0) {
								handleFileSelected(target.files[0]);
							}
						}}
					/>

					{#if !file}
						<div
							role="button"
							tabindex="0"
							class="border-2 border-dashed rounded-2xl p-6 text-center transition-all cursor-pointer flex flex-col items-center justify-center gap-2 {isDragging
								? 'border-primary bg-primary/10'
								: 'border-base-content/15 bg-base-200/40 hover:bg-base-200/70 hover:border-primary/40'}"
							ondragover={handleDragOver}
							ondragleave={handleDragLeave}
							ondrop={handleDrop}
							onclick={() => fileInputRef?.click()}
							onkeydown={(e) => {
								if (e.key === 'Enter' || e.key === ' ') {
									fileInputRef?.click();
								}
							}}
						>
							<div class="w-10 h-10 rounded-2xl bg-base-300/60 text-base-content/60 flex items-center justify-center">
								<UploadCloud class="w-5 h-5" />
							</div>
							<div>
								<p class="text-xs font-bold text-base-content">
									Drop your Word (.docx) file here or <span class="text-primary underline">browse</span>
								</p>
								<p class="text-[10px] text-base-content/50 mt-0.5">
									Supports Microsoft Word (.docx) up to 25 MB
								</p>
							</div>
						</div>
					{:else}
						<div
							class="flex items-center justify-between p-3 rounded-2xl bg-base-200/70 border border-primary/30"
						>
							<div class="flex items-center gap-3 min-w-0">
								<div class="w-10 h-10 rounded-xl bg-primary/15 text-primary flex items-center justify-center flex-shrink-0">
									<FileText class="w-5 h-5" />
								</div>
								<div class="min-w-0">
									<p class="text-xs font-bold text-base-content truncate">{file.name}</p>
									<p class="text-[10px] text-base-content/50">
										{(file.size / 1024).toFixed(1)} KB • Word Document
									</p>
								</div>
							</div>

							<div class="flex items-center gap-1.5 shrink-0">
								<button
									type="button"
									class="btn btn-xs btn-ghost text-base-content/70 hover:text-base-content"
									onclick={() => fileInputRef?.click()}
								>
									Change
								</button>
								<button
									type="button"
									class="btn btn-xs btn-circle btn-ghost text-error"
									onclick={() => {
										file = null;
										if (fileInputRef) fileInputRef.value = '';
									}}
									aria-label="Remove file"
								>
									<X class="w-3.5 h-3.5" />
								</button>
							</div>
						</div>
					{/if}
				</div>

				{#if !targetBankTitle}
					<!-- New Pool Metadata Details -->
					<div class="space-y-3.5 pt-2 border-t border-base-content/10">
						<!-- Pool Title -->
						<div class="space-y-1">
							<label
								for="import-modal-title"
								class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-base-content/80"
							>
								<BookOpen class="w-3.5 h-3.5 text-primary" />
								<span>Pool Title (Optional)</span>
							</label>
							<input
								id="import-modal-title"
								type="text"
								bind:value={title}
								placeholder="Defaults to document name"
								class="input input-bordered input-sm w-full bg-base-200/50 focus:bg-base-100 rounded-xl text-xs"
							/>
						</div>

						<!-- Category & Tags Grid -->
						<div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
							<!-- Category -->
							<div class="space-y-1">
								<label
									for="import-modal-cat"
									class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-base-content/80"
								>
									<Layers class="w-3.5 h-3.5 text-secondary" />
									<span>Category</span>
								</label>
								<input
									id="import-modal-cat"
									type="text"
									bind:value={category}
									placeholder="e.g. Computer Science"
									class="input input-bordered input-sm w-full bg-base-200/50 focus:bg-base-100 rounded-xl text-xs"
								/>
							</div>

							<!-- Tags Input -->
							<div class="space-y-1">
								<label
									for="import-modal-tag"
									class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-base-content/80"
								>
									<Tag class="w-3.5 h-3.5 text-accent" />
									<span>Tags</span>
								</label>
								<input
									id="import-modal-tag"
									type="text"
									bind:value={newTagInput}
									onkeydown={handleTagKeydown}
									placeholder="Press Enter or comma"
									class="input input-bordered input-sm w-full bg-base-200/50 focus:bg-base-100 rounded-xl text-xs font-mono"
								/>
							</div>
						</div>

						<!-- Tag Badges -->
						{#if tagList.length > 0}
							<div class="flex items-center gap-1.5 flex-wrap">
								{#each tagList as t, idx (t)}
									<span
										class="badge badge-sm badge-primary badge-outline gap-1 py-2 px-2.5 rounded-xl font-mono text-[10px]"
									>
										<span>#{t}</span>
										<button
											type="button"
											class="text-primary/70 hover:text-error transition-colors"
											onclick={() => removeTag(idx)}
										>
											<X class="w-3 h-3" />
										</button>
									</span>
								{/each}
							</div>
						{/if}

						<!-- Description -->
						<div class="space-y-1">
							<label
								for="import-modal-desc"
								class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-base-content/80"
							>
								<AlignLeft class="w-3.5 h-3.5 text-base-content/70" />
								<span>Description</span>
							</label>
							<textarea
								id="import-modal-desc"
								bind:value={description}
								rows="2"
								placeholder="Add pool context or notes..."
								class="textarea textarea-bordered textarea-sm w-full bg-base-200/50 focus:bg-base-100 rounded-xl text-xs"
							></textarea>
						</div>
					</div>
				{/if}

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
						disabled={isLoading || !file}
					>
						{#if isLoading}
							<span class="loading loading-spinner loading-xs"></span>
						{:else}
							<FileUp class="w-4 h-4" />
						{/if}
						<span>{targetBankTitle ? 'Import & Append Questions' : 'Import Questions Pool'}</span>
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}
