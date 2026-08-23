<script lang="ts">
	import { examsApi } from '#lib/api/exams.ts';
	import type { QuizExam } from '#lib/api/types.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import ConfirmModal from '#lib/components/ui/ConfirmModal.svelte';
	import { toast } from '#lib/stores/toast.svelte.ts';
	import {
		Plus,
		Edit3,
		GraduationCap,
		ShieldAlert,
		Trash2,
		Search,
		CheckCircle2,
		Clock,
		Sparkles,
		Layers,
		ArrowRight,
		ExternalLink
	} from '@lucide/svelte';
	import { onMount } from 'svelte';

	let exams = $state<QuizExam[]>([]);
	let isLoading = $state(true);
	let searchTerm = $state('');
	let selectedMode = $state<'All' | 'RealExam' | 'Simulation'>('All');
	let isActionLoading = $state(false);

	// Delete Modal State
	let isDeleteModalOpen = $state(false);
	let deletingExam = $state<QuizExam | null>(null);

	onMount(async () => {
		await loadExams();
	});

	async function loadExams() {
		isLoading = true;
		try {
			const res = await examsApi.listExams({
				mode: selectedMode === 'All' ? undefined : selectedMode,
				search: searchTerm.trim() || undefined
			});
			exams = res.items || [];
		} catch (err: any) {
			toast.error(err?.message || 'Failed to load examinations.');
		} finally {
			isLoading = false;
		}
	}

	function openDeleteModal(exam: QuizExam) {
		deletingExam = exam;
		isDeleteModalOpen = true;
	}

	async function handleDeleteExam() {
		if (!deletingExam) return;
		isActionLoading = true;
		try {
			await examsApi.deleteExam(deletingExam.id);
			toast.success('Examination deleted successfully.');
			isDeleteModalOpen = false;
			deletingExam = null;
			await loadExams();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to delete exam.');
		} finally {
			isActionLoading = false;
		}
	}
</script>

<div class="space-y-8">
	<!-- Header -->
	<div class="glass-panel flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 rounded-3xl border border-white/10 p-8 shadow-2xl backdrop-blur-2xl">
		<div class="space-y-1">
			<div class="inline-flex items-center gap-2 rounded-lg bg-secondary/10 border border-secondary/20 px-3 py-1 text-xs font-semibold text-secondary">
				<GraduationCap class="h-3.5 w-3.5" />
				Exam Studio & Assessment Engine
			</div>
			<h1 class="text-3xl font-extrabold tracking-tight text-base-content">
				Exam Authoring & Question Banks
			</h1>
			<p class="text-xs text-base-content/70">
				Manage proctored exams, author question banks with LaTeX formulas, and monitor submissions.
			</p>
		</div>

		<a
			href="/instructor/exams/create"
			class="btn btn-secondary gradient-accent rounded-xl text-xs font-bold text-white border-0 shadow-lg gap-1.5 self-start sm:self-auto"
		>
			<Plus class="h-4 w-4" />
			Author New Exam
		</a>
	</div>

	<!-- Filters & Search Bar -->
	<div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
		<div class="relative flex-1 max-w-md">
			<Search class="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-base-content/40" />
			<input
				type="text"
				class="input input-sm h-11 pl-10 w-full rounded-2xl bg-base-100/60 border-base-content/15 text-sm focus:border-primary"
				placeholder="Search exams by title or topic..."
				bind:value={searchTerm}
				onkeydown={(e) => e.key === 'Enter' && loadExams()}
			/>
		</div>

		<!-- Mode Filter Tabs -->
		<div class="glass-card flex items-center gap-1 rounded-2xl p-1 border border-white/10 self-start sm:self-auto">
			<button
				class="btn btn-xs rounded-xl font-semibold transition-all {selectedMode === 'All'
					? 'btn-secondary text-white shadow-xs'
					: 'btn-ghost text-base-content/70'}"
				onclick={() => {
					selectedMode = 'All';
					loadExams();
				}}
			>
				All Modes
			</button>
			<button
				class="btn btn-xs rounded-xl font-semibold transition-all {selectedMode === 'RealExam'
					? 'btn-secondary text-white shadow-xs'
					: 'btn-ghost text-base-content/70'}"
				onclick={() => {
					selectedMode = 'RealExam';
					loadExams();
				}}
			>
				Proctored Exams
			</button>
			<button
				class="btn btn-xs rounded-xl font-semibold transition-all {selectedMode === 'Simulation'
					? 'btn-secondary text-white shadow-xs'
					: 'btn-ghost text-base-content/70'}"
				onclick={() => {
					selectedMode = 'Simulation';
					loadExams();
				}}
			>
				Practice Simulations
			</button>
		</div>
	</div>

	<!-- Exams Grid -->
	{#if isLoading}
		<div class="grid grid-cols-1 gap-6 sm:grid-cols-2">
			<div class="glass-panel h-56 rounded-3xl animate-pulse"></div>
			<div class="glass-panel h-56 rounded-3xl animate-pulse"></div>
		</div>
	{:else if exams.length > 0}
		<div class="grid grid-cols-1 gap-6 sm:grid-cols-2">
			{#each exams as exam (exam.id)}
				{@const isReal = exam.mode === 'RealExam'}
				<GlassCard class="flex flex-col justify-between p-6 border {isReal ? 'border-primary/20' : 'border-white/10'} hover:border-primary/40 transition-all space-y-4 shadow-xl">
					<div class="space-y-3">
						<div class="flex items-center justify-between">
							<div class="flex items-center gap-2">
								<span class="badge {isReal ? 'badge-primary font-bold' : 'badge-ghost'} badge-xs uppercase">
									{exam.mode === 'RealExam' ? 'Proctored' : 'Simulation'}
								</span>
								<span class="badge {exam.isPublished ? 'badge-success text-white' : 'badge-warning'} badge-xs font-semibold">
									{exam.isPublished ? 'Published' : 'Draft'}
								</span>
							</div>

							<div class="flex items-center gap-1">
								<a
									href={`/instructor/exams/${exam.id}/edit`}
									class="btn btn-ghost btn-xs text-base-content/70 hover:text-secondary rounded-lg p-1.5"
									title="Edit Exam and Questions"
								>
									<Edit3 class="h-3.5 w-3.5" />
								</a>
								<button
									class="btn btn-ghost btn-xs text-base-content/70 hover:text-error rounded-lg p-1.5"
									title="Delete Exam"
									onclick={() => openDeleteModal(exam)}
								>
									<Trash2 class="h-3.5 w-3.5" />
								</button>
							</div>
						</div>

						<h3 class="text-base font-bold text-base-content leading-snug">{exam.title}</h3>

						{#if exam.description}
							<p class="text-xs text-base-content/65 line-clamp-2">{exam.description}</p>
						{/if}

						<div class="flex flex-wrap items-center gap-3 text-xs text-base-content/60 font-medium">
							<span class="inline-flex items-center gap-1">
								<Clock class="h-3.5 w-3.5 text-base-content/40" />
								{exam.durationMinutes} mins
							</span>
							<span>&bull;</span>
							<span>Pass: {exam.passingScore}%</span>
							{#if isReal}
								<span>&bull;</span>
								<span class="inline-flex items-center gap-1 text-warning">
									<ShieldAlert class="h-3.5 w-3.5" />
									Max {exam.maxAllowedViolations} Violations
								</span>
							{/if}
						</div>
					</div>

					<div class="pt-3 border-t border-white/10 flex items-center justify-between text-xs">
						<span class="text-base-content/60 font-semibold">{exam.questionsCount || 0} Questions</span>
						<a
							href={`/instructor/exams/${exam.id}/edit`}
							class="inline-flex items-center gap-1 text-xs font-bold text-secondary hover:underline"
						>
							Manage Question Bank
							<ArrowRight class="h-3.5 w-3.5" />
						</a>
					</div>
				</GlassCard>
			{/each}
		</div>
	{:else}
		<div class="glass-card p-14 text-center rounded-3xl border border-white/5 space-y-4">
			<GraduationCap class="h-10 w-10 text-secondary mx-auto opacity-50" />
			<h3 class="text-base font-bold">No Examinations Found</h3>
			<p class="text-xs text-base-content/60 max-w-sm mx-auto">
				You have not authored any exams yet. Create your first proctored exam or practice simulation.
			</p>
			<a
				href="/instructor/exams/create"
				class="btn btn-secondary gradient-accent btn-sm rounded-xl text-white font-bold border-0 shadow-md inline-flex items-center gap-1.5"
			>
				<Plus class="h-4 w-4" />
				Author New Exam
			</a>
		</div>
	{/if}

	<!-- Delete Exam Confirmation Modal -->
	<ConfirmModal
		isOpen={isDeleteModalOpen}
		title="Delete Examination"
		message={`Are you sure you want to permanently delete "${deletingExam?.title}"? All question sets, submissions, and proctoring snapshots will be cascade removed.`}
		confirmText="Permanently Delete Exam"
		isDanger={true}
		isLoading={isActionLoading}
		onConfirm={handleDeleteExam}
		onCancel={() => (isDeleteModalOpen = false)}
	/>
</div>
