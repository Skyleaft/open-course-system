<script lang="ts">
	import { onMount } from 'svelte';
	import {
		BookOpen,
		Plus,
		FolderPlus,
		Search,
		Layers,
		Sparkles,
		Tag,
		ArrowRight,
		Edit3,
		Trash2,
		Check,
		Clock,
		AlertCircle,
		HelpCircle,
		Download,
		FileUp,
		FileText
	} from 'lucide-svelte';
	import { examsApi } from '$lib/api/exams.ts';
	import type { QuestionBank } from '$lib/api/types.ts';
	import { toast } from '$lib/stores/toast.svelte.ts';
	import GlassCard from '$lib/components/ui/GlassCard.svelte';

	let questionBanks = $state<QuestionBank[]>([]);
	let selectedCategory = $state<string>('All');
	let searchTerm = $state('');
	let isLoading = $state(true);
	let isActionLoading = $state(false);

	// Create / Edit Bank Modal
	let isBankModalOpen = $state(false);
	let modalMode = $state<'create' | 'edit'>('create');
	let editingBankId = $state<string | null>(null);
	let bankTitle = $state('');
	let bankCategory = $state('');
	let bankDescription = $state('');
	let bankTags = $state('');

	// Import Word Modal
	let isImportModalOpen = $state(false);
	let importFile = $state<File | null>(null);
	let importTitle = $state('');
	let importCategory = $state('');
	let importDescription = $state('');
	let importTags = $state('');
	let isDownloadingTemplate = $state(false);

	// Delete Bank Modal
	let isDeleteModalOpen = $state(false);
	let deletingBank = $state<QuestionBank | null>(null);

	onMount(async () => {
		await loadQuestionBanks();
	});

	async function loadQuestionBanks() {
		isLoading = true;
		try {
			const res = await examsApi.listQuestionBanks({ pageSize: 100 });
			questionBanks = res.items || [];
		} catch (err: any) {
			toast.error(err?.message || 'Failed to load question banks.');
		} finally {
			isLoading = false;
		}
	}

	// Categories derived from banks
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

	// Filtered banks
	const filteredBanks = $derived(
		questionBanks.filter((b) => {
			const matchCategory = selectedCategory === 'All' || b.category === selectedCategory;
			const matchSearch =
				!searchTerm.trim() ||
				b.title.toLowerCase().includes(searchTerm.toLowerCase().trim()) ||
				(b.description && b.description.toLowerCase().includes(searchTerm.toLowerCase().trim())) ||
				(b.tags && b.tags.some((t) => t.toLowerCase().includes(searchTerm.toLowerCase().trim())));
			return matchCategory && matchSearch;
		})
	);

	// Stats
	const totalPools = $derived(questionBanks.length);
	const totalQuestions = $derived(
		questionBanks.reduce((acc, b) => acc + (b.questionCount || (b.questions ? b.questions.length : 0)), 0)
	);
	const totalCategories = $derived(Math.max(0, categories.length - 1));

	function openCreateBank() {
		modalMode = 'create';
		editingBankId = null;
		bankTitle = '';
		bankCategory = '';
		bankDescription = '';
		bankTags = '';
		isBankModalOpen = true;
	}

	function openImportModal() {
		importFile = null;
		importTitle = '';
		importCategory = '';
		importDescription = '';
		importTags = '';
		isImportModalOpen = true;
	}

	async function handleDownloadTemplate() {
		isDownloadingTemplate = true;
		try {
			const blob = await examsApi.downloadQuestionBankTemplate();
			const url = window.URL.createObjectURL(blob);
			const a = document.createElement('a');
			a.href = url;
			a.download = 'QuestionBank-Template.docx';
			document.body.appendChild(a);
			a.click();
			window.URL.revokeObjectURL(url);
			document.body.removeChild(a);
			toast.success('Question Bank Word Template downloaded successfully.');
		} catch (err: any) {
			toast.error(err?.message || 'Failed to download Word template.');
		} finally {
			isDownloadingTemplate = false;
		}
	}

	async function handleImportWord(e: Event) {
		e.preventDefault();
		if (!importFile) {
			toast.warning('Please select a Word Document (.docx) file.');
			return;
		}

		const formData = new FormData();
		formData.append('file', importFile);
		if (importTitle.trim()) formData.append('title', importTitle.trim());
		if (importCategory.trim()) formData.append('category', importCategory.trim());
		if (importDescription.trim()) formData.append('description', importDescription.trim());
		if (importTags.trim()) formData.append('tags', importTags.trim());

		isActionLoading = true;
		try {
			const result = await examsApi.importQuestionBank(formData);
			toast.success(`Imported ${result.totalImportedQuestions} questions into "${result.bankTitle}"!`);
			if (result.warnings && result.warnings.length > 0) {
				toast.info(result.warnings.join(' | '));
			}
			isImportModalOpen = false;
			await loadQuestionBanks();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to import questions from Word document.');
		} finally {
			isActionLoading = false;
		}
	}

	function openEditBank(bank: QuestionBank) {
		modalMode = 'edit';
		editingBankId = bank.id;
		bankTitle = bank.title;
		bankCategory = bank.category || '';
		bankDescription = bank.description || '';
		bankTags = (bank.tags || []).join(', ');
		isBankModalOpen = true;
	}

	async function handleSaveBank() {
		if (!bankTitle.trim()) {
			toast.warning('Please enter a question pool title.');
			return;
		}

		const tagsList = bankTags
			.split(',')
			.map((t) => t.trim())
			.filter(Boolean);

		isActionLoading = true;
		try {
			if (modalMode === 'create') {
				await examsApi.createQuestionBank({
					title: bankTitle.trim(),
					category: bankCategory.trim() || undefined,
					description: bankDescription.trim() || undefined,
					tags: tagsList
				});
				toast.success(`Question Bank '${bankTitle.trim()}' created successfully!`);
			} else if (editingBankId) {
				await examsApi.updateQuestionBank(editingBankId, {
					title: bankTitle.trim(),
					category: bankCategory.trim() || undefined,
					description: bankDescription.trim() || undefined,
					tags: tagsList
				});
				toast.success(`Question Bank '${bankTitle.trim()}' updated successfully!`);
			}
			isBankModalOpen = false;
			await loadQuestionBanks();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to save question bank.');
		} finally {
			isActionLoading = false;
		}
	}

	function openDeleteModal(bank: QuestionBank) {
		deletingBank = bank;
		isDeleteModalOpen = true;
	}

	async function handleDeleteBank() {
		if (!deletingBank) return;
		isActionLoading = true;
		try {
			await examsApi.deleteQuestionBank(deletingBank.id);
			toast.success(`Question Bank '${deletingBank.title}' deleted successfully.`);
			isDeleteModalOpen = false;
			deletingBank = null;
			await loadQuestionBanks();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to delete question bank.');
		} finally {
			isActionLoading = false;
		}
	}
</script>

<div class="space-y-6 max-w-7xl mx-auto pb-12">
	<!-- Page Header -->
	<div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
		<div>
			<h1 class="text-2xl sm:text-3xl font-extrabold text-base-content tracking-tight flex items-center gap-2.5">
				<BookOpen class="w-8 h-8 text-primary" />
				Question Bank Pools
			</h1>
			<p class="text-sm text-base-content/70 mt-1">
				Author, organize, and manage reusable question pools and packages for your examinations and courses.
			</p>
		</div>

		<div class="flex items-center gap-2 flex-wrap">
			<button
				type="button"
				class="btn btn-sm btn-ghost border border-base-content/10 gap-1.5 hover:bg-base-200"
				onclick={handleDownloadTemplate}
				disabled={isDownloadingTemplate}
			>
				{#if isDownloadingTemplate}
					<span class="loading loading-spinner loading-xs"></span>
				{:else}
					<Download class="w-4 h-4 text-primary" />
				{/if}
				<span>Word Template</span>
			</button>

			<button
				type="button"
				class="btn btn-sm btn-outline btn-primary gap-1.5"
				onclick={openImportModal}
			>
				<FileUp class="w-4 h-4" />
				<span>Import Word (.docx)</span>
			</button>

			<button
				type="button"
				class="btn btn-sm btn-primary gap-1.5 shadow-md shadow-primary/20"
				onclick={openCreateBank}
			>
				<FolderPlus class="w-4 h-4" />
				<span>New Pool</span>
			</button>
		</div>
	</div>

	<!-- Stats Overview -->
	<div class="grid grid-cols-1 sm:grid-cols-3 gap-3 sm:gap-4">
		<GlassCard class="p-4 flex items-center gap-3.5">
			<div class="w-10 h-10 rounded-xl bg-primary/10 text-primary flex items-center justify-center flex-shrink-0">
				<FolderPlus class="w-5 h-5" />
			</div>
			<div>
				<p class="text-xs text-base-content/60 font-semibold">Total Question Pools</p>
				<p class="text-xl font-black text-base-content">{totalPools}</p>
			</div>
		</GlassCard>

		<GlassCard class="p-4 flex items-center gap-3.5">
			<div class="w-10 h-10 rounded-xl bg-secondary/10 text-secondary flex items-center justify-center flex-shrink-0">
				<Layers class="w-5 h-5" />
			</div>
			<div>
				<p class="text-xs text-base-content/60 font-semibold">Total Questions Available</p>
				<p class="text-xl font-black text-base-content">{totalQuestions}</p>
			</div>
		</GlassCard>

		<GlassCard class="p-4 flex items-center gap-3.5">
			<div class="w-10 h-10 rounded-xl bg-accent/10 text-accent flex items-center justify-center flex-shrink-0">
				<Sparkles class="w-5 h-5" />
			</div>
			<div>
				<p class="text-xs text-base-content/60 font-semibold">Categories Covered</p>
				<p class="text-xl font-black text-base-content">{totalCategories}</p>
			</div>
		</GlassCard>
	</div>

	<!-- Search & Filter Controls -->
	<GlassCard class="p-4">
		<div class="flex flex-col sm:flex-row gap-3 items-center justify-between">
			<div class="relative w-full sm:w-96">
				<Search class="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-base-content/40" />
				<input
					type="text"
					bind:value={searchTerm}
					placeholder="Search pools by title, description, or tag..."
					class="input input-bordered input-sm w-full pl-10 bg-base-100/50"
				/>
			</div>

			<div class="flex items-center gap-2 w-full sm:w-auto">
				<select
					bind:value={selectedCategory}
					class="select select-bordered select-sm bg-base-100/50 text-xs font-semibold w-full sm:w-auto"
				>
					{#each categories as cat}
						<option value={cat}>{cat === 'All' ? 'All Categories' : cat}</option>
					{/each}
				</select>
			</div>
		</div>
	</GlassCard>

	<!-- Question Bank Cards Grid -->
	{#if isLoading}
		<div class="py-16 text-center">
			<span class="loading loading-spinner loading-lg text-primary"></span>
			<p class="text-xs text-base-content/60 mt-3 font-semibold">Loading question pools...</p>
		</div>
	{:else if filteredBanks.length === 0}
		<div class="py-16 text-center bg-base-200/40 rounded-3xl border border-dashed border-base-300 p-8">
			<div class="w-16 h-16 rounded-2xl bg-primary/10 text-primary flex items-center justify-center mx-auto mb-4">
				<FolderPlus class="w-8 h-8" />
			</div>
			<h3 class="text-lg font-bold text-base-content">
				{questionBanks.length === 0 ? 'No Question Bank Pools Yet' : 'No Pools Match Filter'}
			</h3>
			<p class="text-xs text-base-content/70 max-w-md mx-auto mt-2 leading-relaxed">
				{questionBanks.length === 0
					? 'Create your first Question Bank pool to start organizing and storing reusable examination questions.'
					: 'Try adjusting your search query or selected category filter.'}
			</p>
			{#if questionBanks.length === 0}
				<button
					type="button"
					class="btn btn-sm btn-primary gap-1.5 mt-5 shadow-lg shadow-primary/20"
					onclick={openCreateBank}
				>
					<FolderPlus class="w-4 h-4" />
					Create Your First Pool
				</button>
			{/if}
		</div>
	{:else}
		<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 sm:gap-5">
			{#each filteredBanks as bank (bank.id)}
				<GlassCard class="p-5 h-full flex flex-col justify-between hover:border-primary/40 hover:shadow-xl hover:shadow-primary/5 transition-all duration-200">
					<div>
						<!-- Card Top Badges & Actions -->
						<div class="flex items-center justify-between gap-2 mb-3">
							{#if bank.category}
								<span class="badge badge-sm badge-primary badge-outline text-[11px] font-bold">
									{bank.category}
								</span>
							{:else}
								<span class="badge badge-sm badge-ghost text-[11px]">
									General Pool
								</span>
							{/if}

							<div class="flex items-center gap-1">
								<button
									type="button"
									class="btn btn-xs btn-ghost btn-square text-base-content/60 hover:text-base-content hover:bg-base-200"
									onclick={() => openEditBank(bank)}
									title="Edit Pool Info"
								>
									<Edit3 class="w-3.5 h-3.5" />
								</button>
								<button
									type="button"
									class="btn btn-xs btn-ghost btn-square text-error hover:bg-error/10"
									onclick={() => openDeleteModal(bank)}
									title="Delete Pool"
								>
									<Trash2 class="w-3.5 h-3.5" />
								</button>
							</div>
						</div>

						<!-- Title & Description -->
						<a
							href="/instructor/questions/{bank.id}"
							class="block group"
						>
							<h2 class="text-base font-bold text-base-content group-hover:text-primary transition-colors line-clamp-1">
								{bank.title}
							</h2>
							<p class="text-xs text-base-content/70 mt-1.5 line-clamp-2 min-h-[2rem]">
								{bank.description || 'No description provided for this question pool.'}
							</p>
						</a>

						<!-- Tags -->
						{#if bank.tags && bank.tags.length > 0}
							<div class="flex items-center gap-1.5 flex-wrap mt-3">
								{#each bank.tags.slice(0, 3) as tag}
									<span class="badge badge-xs badge-neutral text-[10px] gap-1">
										<Tag class="w-2.5 h-2.5 opacity-60" />
										{tag}
									</span>
								{/each}
								{#if bank.tags.length > 3}
									<span class="text-[10px] text-base-content/50 font-semibold">
										+{bank.tags.length - 3} more
									</span>
								{/if}
							</div>
						{/if}
					</div>

					<!-- Card Footer Meta & CTA -->
					<div class="pt-4 mt-4 border-t border-base-content/10 flex items-center justify-between text-xs">
						<div class="flex items-center gap-1.5 text-base-content/80 font-bold">
							<Layers class="w-4 h-4 text-primary" />
							<span>
								{bank.questionCount ?? (bank.questions ? bank.questions.length : 0)} questions
							</span>
						</div>

						<a
							href="/instructor/questions/{bank.id}"
							class="flex items-center gap-1 text-primary font-bold text-xs hover:underline hover:translate-x-0.5 transition-all"
						>
							<span>Manage Pool</span>
							<ArrowRight class="w-3.5 h-3.5" />
						</a>
					</div>
				</GlassCard>
			{/each}
		</div>
	{/if}
</div>

<!-- Create / Edit Question Bank Modal -->
{#if isBankModalOpen}
	<div class="modal modal-open z-50">
		<div class="modal-box bg-base-100/95 backdrop-blur-xl border border-base-content/10 shadow-2xl max-w-md">
			<h3 class="font-bold text-base text-base-content flex items-center gap-2">
				{#if modalMode === 'create'}
					<FolderPlus class="w-5 h-5 text-primary" />
					Create Question Bank Pool
				{:else}
					<Edit3 class="w-5 h-5 text-primary" />
					Edit Question Bank Pool
				{/if}
			</h3>

			<form
				onsubmit={(e) => {
					e.preventDefault();
					handleSaveBank();
				}}
				class="space-y-4 mt-4"
			>
				<div>
					<label for="bank-title-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Pool Title <span class="text-error">*</span>
					</label>
					<input
						id="bank-title-input"
						type="text"
						bind:value={bankTitle}
						placeholder="e.g. C# .NET 10 Core Certification Pool"
						class="input input-bordered input-sm w-full bg-base-200/50"
						required
					/>
				</div>

				<div>
					<label for="bank-cat-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Category
					</label>
					<input
						id="bank-cat-input"
						type="text"
						bind:value={bankCategory}
						placeholder="e.g. Software Engineering"
						class="input input-bordered input-sm w-full bg-base-200/50"
					/>
				</div>

				<div>
					<label for="bank-desc-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Description (Optional)
					</label>
					<textarea
						id="bank-desc-input"
						bind:value={bankDescription}
						rows="2"
						placeholder="Coverage, purpose, or learning objectives..."
						class="textarea textarea-bordered textarea-sm w-full bg-base-200/50"
					></textarea>
				</div>

				<div>
					<label for="bank-tags-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Tags (Comma separated)
					</label>
					<input
						id="bank-tags-input"
						type="text"
						bind:value={bankTags}
						placeholder="e.g. csharp, efcore, architecture"
						class="input input-bordered input-sm w-full bg-base-200/50"
					/>
				</div>

				<div class="modal-action pt-2">
					<button
						type="button"
						class="btn btn-sm btn-ghost"
						onclick={() => (isBankModalOpen = false)}
						disabled={isActionLoading}
					>
						Cancel
					</button>
					<button
						type="submit"
						class="btn btn-sm btn-primary gap-1.5"
						disabled={isActionLoading || !bankTitle.trim()}
					>
						{#if isActionLoading}
							<span class="loading loading-spinner loading-xs"></span>
						{:else}
							<Check class="w-4 h-4" />
						{/if}
						{modalMode === 'create' ? 'Create Pool' : 'Save Changes'}
					</button>
				</div>
			</form>
		</div>
		<div class="modal-backdrop bg-black/40 backdrop-blur-sm" onclick={() => (isBankModalOpen = false)}></div>
	</div>
{/if}

<!-- Delete Bank Confirmation Modal -->
{#if isDeleteModalOpen && deletingBank}
	<div class="modal modal-open z-50">
		<div class="modal-box bg-base-100/95 backdrop-blur-xl border border-error/20 shadow-2xl max-w-sm">
			<h3 class="font-bold text-base text-error flex items-center gap-2">
				<AlertCircle class="w-5 h-5" />
				Delete Question Bank
			</h3>
			<p class="text-xs text-base-content/80 mt-3 leading-relaxed">
				Are you sure you want to delete <strong>"{deletingBank.title}"</strong>? Questions contained within this pool will be permanently removed.
			</p>
			<div class="modal-action pt-2">
				<button
					type="button"
					class="btn btn-sm btn-ghost"
					onclick={() => (isDeleteModalOpen = false)}
					disabled={isActionLoading}
				>
					Cancel
				</button>
				<button
					type="button"
					class="btn btn-sm btn-error gap-1.5"
					onclick={handleDeleteBank}
					disabled={isActionLoading}
				>
					{#if isActionLoading}
						<span class="loading loading-spinner loading-xs"></span>
					{:else}
						<Trash2 class="w-4 h-4" />
					{/if}
					Delete Pool
				</button>
			</div>
		</div>
		<div class="modal-backdrop bg-black/40 backdrop-blur-sm" onclick={() => (isDeleteModalOpen = false)}></div>
	</div>
{/if}

<!-- Import Question Bank from Word Modal -->
{#if isImportModalOpen}
	<div class="modal modal-open z-50">
		<div class="modal-box bg-base-100/95 backdrop-blur-xl border border-base-content/10 shadow-2xl max-w-lg">
			<div class="flex items-center justify-between border-b border-base-content/10 pb-3">
				<h3 class="font-bold text-base text-base-content flex items-center gap-2">
					<FileUp class="w-5 h-5 text-primary" />
					Import Questions from Word (.docx)
				</h3>
				<button
					type="button"
					class="btn btn-xs btn-circle btn-ghost"
					onclick={() => (isImportModalOpen = false)}
				>✕</button>
			</div>

			<form onsubmit={handleImportWord} class="space-y-4 pt-3">
				<!-- Template Helper Banner -->
				<div class="p-3 rounded-xl bg-primary/10 border border-primary/20 flex items-center justify-between gap-3">
					<div class="space-y-0.5">
						<p class="text-xs font-bold text-primary flex items-center gap-1.5">
							<FileText class="w-4 h-4" />
							Need a formatted sample?
						</p>
						<p class="text-[11px] text-base-content/70 leading-tight">
							Download our pre-structured Word document template.
						</p>
					</div>
					<button
						type="button"
						class="btn btn-xs btn-primary gap-1"
						onclick={handleDownloadTemplate}
						disabled={isDownloadingTemplate}
					>
						{#if isDownloadingTemplate}
							<span class="loading loading-spinner loading-xs"></span>
						{:else}
							<Download class="w-3.5 h-3.5" />
						{/if}
						Template
					</button>
				</div>

				<!-- File Input -->
				<div>
					<label for="import-file-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Word Document (.docx) <span class="text-error">*</span>
					</label>
					<input
						id="import-file-input"
						type="file"
						accept=".docx,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
						onchange={(e) => {
							const target = e.currentTarget;
							if (target.files && target.files.length > 0) {
								importFile = target.files[0];
								if (!importTitle) {
									importTitle = target.files[0].name.replace(/\.[^/.]+$/, '');
								}
							}
						}}
						class="file-input file-input-bordered file-input-primary file-input-sm w-full bg-base-200/50"
						required
					/>
					{#if importFile}
						<p class="text-[11px] text-success font-medium mt-1">
							Selected: {importFile.name} ({(importFile.size / 1024).toFixed(1)} KB)
						</p>
					{/if}
				</div>

				<!-- Optional Bank Title -->
				<div>
					<label for="import-title-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Question Pool Title (Optional)
					</label>
					<input
						id="import-title-input"
						type="text"
						bind:value={importTitle}
						placeholder="Defaults to document title / file name"
						class="input input-bordered input-sm w-full bg-base-200/50"
					/>
				</div>

				<div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
					<div>
						<label for="import-cat-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Category (Optional)
						</label>
						<input
							id="import-cat-input"
							type="text"
							bind:value={importCategory}
							placeholder="e.g. Computer Science"
							class="input input-bordered input-sm w-full bg-base-200/50"
						/>
					</div>
					<div>
						<label for="import-tags-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Tags (Optional)
						</label>
						<input
							id="import-tags-input"
							type="text"
							bind:value={importTags}
							placeholder="e.g. intro, quiz"
							class="input input-bordered input-sm w-full bg-base-200/50"
						/>
					</div>
				</div>

				<div>
					<label for="import-desc-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Description (Optional)
					</label>
					<textarea
						id="import-desc-input"
						bind:value={importDescription}
						rows="2"
						placeholder="Add context or notes for this pool..."
						class="textarea textarea-bordered textarea-sm w-full bg-base-200/50"
					></textarea>
				</div>

				<div class="modal-action pt-2">
					<button
						type="button"
						class="btn btn-sm btn-ghost"
						onclick={() => (isImportModalOpen = false)}
						disabled={isActionLoading}
					>
						Cancel
					</button>
					<button
						type="submit"
						class="btn btn-sm btn-primary gap-1.5"
						disabled={isActionLoading || !importFile}
					>
						{#if isActionLoading}
							<span class="loading loading-spinner loading-xs"></span>
						{:else}
							<FileUp class="w-4 h-4" />
						{/if}
						Import Questions
					</button>
				</div>
			</form>
		</div>
		<div class="modal-backdrop bg-black/40 backdrop-blur-sm" onclick={() => (isImportModalOpen = false)}></div>
	</div>
{/if}
