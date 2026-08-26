<script lang="ts">
	import type { QuizSection, QuestionBank, BankQuestion } from '$lib/api/types.ts';
	import QuestionBankPackageSelector from './QuestionBankPackageSelector.svelte';
	import RichRenderer from '$lib/components/editor/RichRenderer.svelte';
	import { fade, scale } from 'svelte/transition';
	import { cubicOut } from 'svelte/easing';
	import {
		Layers,
		Plus,
		Trash2,
		Edit3,
		Check,
		BookOpen,
		Sparkles,
		Tag,
		CheckCircle2,
		CheckSquare,
		ListFilter,
		AlignLeft,
		ChevronDown,
		ChevronUp,
		Settings,
		HelpCircle,
		X
	} from 'lucide-svelte';

	interface Props {
		sections: QuizSection[];
		availableBanks: QuestionBank[];
		onSaveSections: (sections: QuizSection[]) => void;
		onCreateNewBank?: () => void;
	}

	let {
		sections = [],
		availableBanks = [],
		onSaveSections,
		onCreateNewBank
	}: Props = $props();

	// Local mutable sections state
	let localSections = $state<QuizSection[]>([]);

	$effect(() => {
		localSections = JSON.parse(JSON.stringify(sections));
	});

	// Expanded section IDs for previewing questions
	let expandedSectionIds = $state<Record<string, boolean>>({});

	// Section Modal State (Create or Edit)
	let isModalOpen = $state(false);
	let isBankSelectorOpen = $state(false);
	let editingIndex = $state<number | null>(null);

	let modalSectionTitle = $state('');
	let modalSectionDesc = $state('');
	let modalSelectedBankId = $state('');
	let modalPointsOverride = $state<number | null>(null);
	let modalQuestionCount = $state<number | null>(null);

	const selectedBank = $derived(
		availableBanks.find((b) => b.id === modalSelectedBankId)
	);

	function toggleExpand(sectionId: string) {
		expandedSectionIds[sectionId] = !expandedSectionIds[sectionId];
	}

	function openAddModal() {
		editingIndex = null;
		modalSectionTitle = `Section ${localSections.length + 1}`;
		modalSectionDesc = '';
		modalSelectedBankId = availableBanks.length > 0 ? availableBanks[0].id : '';
		modalPointsOverride = null;
		modalQuestionCount = null;
		isModalOpen = true;
	}

	function openEditModal(index: number) {
		editingIndex = index;
		const sec = localSections[index];
		modalSectionTitle = sec.title;
		modalSectionDesc = sec.description || '';
		modalSelectedBankId = sec.questionBankId;
		modalPointsOverride = sec.pointsOverride ?? null;
		modalQuestionCount = sec.questionCount ?? null;
		isModalOpen = true;
	}

	function handleSaveSectionModal() {
		if (!modalSectionTitle.trim()) return;
		if (!modalSelectedBankId) return;

		const bank = availableBanks.find((b) => b.id === modalSelectedBankId);

		const updatedSec: QuizSection = {
			id: editingIndex !== null ? localSections[editingIndex].id : crypto.randomUUID(),
			examId: editingIndex !== null ? localSections[editingIndex].examId : '',
			questionBankId: modalSelectedBankId,
			questionBankTitle: bank?.title,
			title: modalSectionTitle.trim(),
			description: modalSectionDesc.trim() || null,
			orderIndex: editingIndex !== null ? localSections[editingIndex].orderIndex : localSections.length + 1,
			pointsOverride: modalPointsOverride !== null && modalPointsOverride > 0 ? modalPointsOverride : null,
			questionCount: modalQuestionCount !== null && modalQuestionCount > 0 ? modalQuestionCount : null,
			questions: bank?.questions || [],
			questionBank: bank
		};

		let next: QuizSection[];
		if (editingIndex !== null) {
			next = [...localSections];
			next[editingIndex] = updatedSec;
		} else {
			next = [...localSections, updatedSec];
		}

		localSections = next;
		onSaveSections(next);
		isModalOpen = false;
	}

	function handleDeleteSection(index: number) {
		const next = localSections.filter((_, i) => i !== index).map((sec, idx) => ({
			...sec,
			orderIndex: idx + 1
		}));
		localSections = next;
		onSaveSections(next);
	}

	function handleSelectBank(bank: QuestionBank) {
		modalSelectedBankId = bank.id;
		if (!modalSectionTitle || modalSectionTitle.startsWith('Section ')) {
			modalSectionTitle = bank.title;
		}
		isBankSelectorOpen = false;
	}
</script>

<div class="space-y-4">
	<!-- Top Bar -->
	<div class="flex items-center justify-between">
		<div>
			<h3 class="text-base font-bold text-base-content flex items-center gap-2">
				<Layers class="w-5 h-5 text-primary" />
				Examination Sections
			</h3>
			<p class="text-xs text-base-content/70">
				Organize exam into sections referencing independent Question Bank packages with optional point overrides.
			</p>
		</div>

		<button
			type="button"
			class="btn btn-sm btn-primary gap-1.5 shadow-sm"
			onclick={openAddModal}
		>
			<Plus class="w-4 h-4" />
			Add Section
		</button>
	</div>

	<!-- Sections List -->
	{#if localSections.length === 0}
		<div class="py-12 text-center bg-base-200/40 rounded-2xl border border-dashed border-base-300">
			<Layers class="w-10 h-10 text-base-content/30 mx-auto mb-2.5" />
			<p class="text-sm font-semibold text-base-content/80">No sections added yet</p>
			<p class="text-xs text-base-content/50 max-w-sm mx-auto mt-1">
				Exams require at least one section linked to a Question Bank package to evaluate candidates.
			</p>
			<button
				type="button"
				class="btn btn-sm btn-primary gap-1.5 mt-4"
				onclick={openAddModal}
			>
				<Plus class="w-4 h-4" />
				Add First Section
			</button>
		</div>
	{:else}
		<div class="space-y-3">
			{#each localSections as section, idx (section.id || idx)}
				{@const bank = availableBanks.find((b) => b.id === section.questionBankId) || section.questionBank}
				{@const questions = bank?.questions || section.questions || []}
				{@const effectiveCount = section.questionCount && section.questionCount > 0 ? Math.min(section.questionCount, questions.length) : questions.length}
				{@const isExpanded = expandedSectionIds[section.id || String(idx)]}

				<div class="bg-base-200/50 rounded-2xl border border-base-content/10 overflow-hidden transition-all">
					<!-- Section Header -->
					<div class="p-4 flex items-center justify-between gap-3 bg-base-100/60 flex-wrap">
						<div class="flex items-center gap-3 min-w-0">
							<span class="w-7 h-7 rounded-lg bg-primary/10 text-primary font-mono font-bold text-xs flex items-center justify-center flex-shrink-0">
								{idx + 1}
							</span>

							<div class="min-w-0">
								<div class="flex items-center gap-2 flex-wrap">
									<span class="font-bold text-sm text-base-content">
										{section.title}
									</span>
									{#if section.pointsOverride}
										<span class="badge badge-sm badge-warning font-mono text-[10px] font-bold">
											Override: {section.pointsOverride} pts / question
										</span>
									{/if}
									{#if section.questionCount}
										<span class="badge badge-sm badge-neutral font-mono text-[10px]">
											Top {section.questionCount} Qs
										</span>
									{/if}
								</div>

								<div class="flex items-center gap-2 text-xs text-base-content/60 mt-0.5 flex-wrap">
									<span class="flex items-center gap-1">
										<BookOpen class="w-3.5 h-3.5 text-primary" />
										Bank: <span class="font-semibold text-base-content/80">{bank?.title || section.questionBankTitle || 'Custom Pool'}</span>
									</span>
									<span>•</span>
									<span>{effectiveCount} Questions active</span>
								</div>
							</div>
						</div>

						<div class="flex items-center gap-1.5 ml-auto">
							<button
								type="button"
								class="btn btn-xs btn-ghost gap-1 text-xs"
								onclick={() => toggleExpand(section.id || String(idx))}
							>
								{#if isExpanded}
									<ChevronUp class="w-3.5 h-3.5" />
									Hide Questions
								{:else}
									<ChevronDown class="w-3.5 h-3.5" />
									Preview ({effectiveCount})
								{/if}
							</button>

							<button
								type="button"
								class="btn btn-xs btn-ghost btn-square"
								onclick={() => openEditModal(idx)}
								title="Edit Section Settings"
							>
								<Edit3 class="w-3.5 h-3.5 text-base-content/70" />
							</button>

							<button
								type="button"
								class="btn btn-xs btn-ghost btn-square text-error hover:bg-error/10"
								onclick={() => handleDeleteSection(idx)}
								title="Remove Section"
							>
								<Trash2 class="w-3.5 h-3.5" />
							</button>
						</div>
					</div>

					<!-- Description if any -->
					{#if section.description}
						<div class="px-4 py-2 text-xs text-base-content/70 bg-base-100/30 border-t border-base-content/5">
							{section.description}
						</div>
					{/if}

					<!-- Questions Preview Drawer -->
					{#if isExpanded}
						<div class="p-4 border-t border-base-content/10 bg-base-200/30 space-y-2.5">
							{#if questions.length === 0}
								<p class="text-xs text-base-content/50 italic text-center py-3">
									This Question Bank pool has no questions yet. Questions added to this bank will automatically show up here.
								</p>
							{:else}
								{#each questions.slice(0, effectiveCount) as q, qIdx}
									<div class="p-3 rounded-xl bg-base-100/80 border border-base-content/5 text-xs space-y-1.5">
										<div class="flex items-center justify-between gap-2">
											<span class="font-mono font-bold text-[10px] text-base-content/60">Q{qIdx + 1}</span>
											<div class="flex items-center gap-1.5">
												<span class="badge badge-xs badge-outline badge-primary font-bold text-[9px]">{q.type}</span>
												<span class="badge badge-xs badge-neutral font-mono font-bold text-[9px]">
													{section.pointsOverride ?? q.points ?? 5} pts
												</span>
											</div>
										</div>
										<div class="text-base-content font-medium">
											<RichRenderer content={q.questionText || q.text || ''} />
										</div>
									</div>
								{/each}
								{#if questions.length > effectiveCount}
									<p class="text-[11px] text-base-content/50 text-center italic pt-1">
										+ {questions.length - effectiveCount} additional questions in pool (limited by section count of {section.questionCount})
									</p>
								{/if}
							{/if}
						</div>
					{/if}
				</div>
			{/each}
		</div>
	{/if}
</div>

<!-- Add/Edit Section Modal (Clean Centered Modal) -->
{#if isModalOpen}
	<div
		class="fixed inset-0 z-[100] flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
		role="dialog"
		aria-modal="true"
		transition:fade={{ duration: 180 }}
	>
		<!-- Backdrop click -->
		<div class="fixed inset-0" onclick={() => (isModalOpen = false)} role="presentation"></div>

		<div
			class="relative z-10 w-full max-w-lg overflow-hidden rounded-2xl border border-base-content/10 bg-base-100/95 p-6 shadow-2xl backdrop-blur-2xl space-y-4"
			transition:scale={{ duration: 220, start: 0.94, easing: cubicOut }}
		>
			<div class="flex items-center justify-between border-b border-base-content/10 pb-3">
				<h3 class="font-bold text-base text-base-content flex items-center gap-2">
					<Layers class="w-5 h-5 text-primary" />
					<span>{editingIndex !== null ? 'Edit Section Settings' : 'Add New Exam Section'}</span>
				</h3>
				<button
					type="button"
					class="btn btn-ghost btn-circle btn-xs text-base-content/60 hover:text-base-content"
					onclick={() => (isModalOpen = false)}
					aria-label="Close modal"
				>
					<X class="w-4 h-4" />
				</button>
			</div>

			<form
				onsubmit={(e) => {
					e.preventDefault();
					handleSaveSectionModal();
				}}
				class="space-y-4"
			>
				<!-- Section Title -->
				<div>
					<label for="sec-title-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Section Title <span class="text-error">*</span>
					</label>
					<input
						id="sec-title-input"
						type="text"
						bind:value={modalSectionTitle}
						placeholder="e.g. Core C# Fundamentals"
						class="input input-bordered input-sm w-full bg-base-200/50"
						required
					/>
				</div>

				<!-- Section Description -->
				<div>
					<label for="section-desc-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Section Description (Optional)
					</label>
					<textarea
						id="section-desc-input"
						bind:value={modalSectionDesc}
						rows="2"
						placeholder="Instructions or scope for this section..."
						class="textarea textarea-bordered textarea-sm w-full bg-base-200/50"
					></textarea>
				</div>

				<!-- Question Bank Picker Trigger -->
				<div>
					<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Linked Question Bank Package <span class="text-error">*</span>
					</label>
					<div class="flex items-center gap-2">
						<div class="flex-1 p-2.5 rounded-xl border border-base-content/10 bg-base-200/50 flex items-center justify-between">
							<div class="min-w-0">
								{#if selectedBank}
									<p class="font-bold text-xs text-base-content truncate">{selectedBank.title}</p>
									<p class="text-[11px] text-base-content/60">{selectedBank.questionCount ?? (selectedBank.questions?.length || 0)} questions available</p>
								{:else}
									<p class="text-xs text-base-content/40 italic">No Question Bank selected</p>
								{/if}
							</div>
						</div>

						<button
							type="button"
							class="btn btn-sm btn-outline btn-primary"
							onclick={() => (isBankSelectorOpen = true)}
						>
							Browse Banks
						</button>
					</div>
				</div>

				<!-- Section Override Settings -->
				<div class="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-2 border-t border-base-content/10">
					<div>
						<label for="pts-override-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Points Override (Optional)
						</label>
						<input
							id="pts-override-input"
							type="number"
							step="0.5"
							min="0.5"
							bind:value={modalPointsOverride}
							placeholder="Default question pts"
							class="input input-bordered input-sm w-full bg-base-200/50"
						/>
						<span class="text-[10px] text-base-content/50 mt-1 block">Override point value for all Qs in this section</span>
					</div>

					<div>
						<label for="q-count-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Question Count Limit
						</label>
						<input
							id="q-count-input"
							type="number"
							min="1"
							bind:value={modalQuestionCount}
							placeholder="Take all questions"
							class="input input-bordered input-sm w-full bg-base-200/50"
						/>
						<span class="text-[10px] text-base-content/50 mt-1 block">Limit number of questions pulled from pool</span>
					</div>
				</div>

				<!-- Actions -->
				<div class="flex justify-end gap-2 pt-3 border-t border-base-content/10">
					<button
						type="button"
						class="btn btn-sm btn-ghost"
						onclick={() => (isModalOpen = false)}
					>
						Cancel
					</button>
					<button
						type="submit"
						class="btn btn-sm btn-primary gap-1.5"
						disabled={!modalSectionTitle.trim() || !modalSelectedBankId}
					>
						<Check class="w-4 h-4" />
						{editingIndex !== null ? 'Save Changes' : 'Add Section'}
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}

<!-- Question Bank Package Selector Modal -->
<QuestionBankPackageSelector
	isOpen={isBankSelectorOpen}
	questionBanks={availableBanks}
	selectedBankId={modalSelectedBankId}
	onSelect={handleSelectBank}
	onClose={() => (isBankSelectorOpen = false)}
	onCreateNew={onCreateNewBank}
/>
