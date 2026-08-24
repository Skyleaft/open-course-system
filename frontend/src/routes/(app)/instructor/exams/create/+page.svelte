<script lang="ts">
	import { goto } from '$app/navigation';
	import {
		ArrowLeft,
		Sparkles,
		Clock,
		ShieldAlert,
		CheckCircle2,
		Shuffle,
		Calendar,
		GraduationCap,
		Award,
		AlertTriangle,
		Save,
		Eye,
		HelpCircle
	} from 'lucide-svelte';
	import { examsApi } from '$lib/api/exams.ts';
	import type { QuizMode } from '$lib/api/types.ts';
	import { toast } from '$lib/stores/toast.svelte.ts';
	import GlassCard from '$lib/components/ui/GlassCard.svelte';
	import RichEditor from '$lib/components/editor/RichEditor.svelte';

	// Form State
	let title = $state('');
	let description = $state('');
	let mode = $state<QuizMode>('RealExam');
	let durationMinutes = $state(60);
	let passingScore = $state(70);
	let maxAllowedViolations = $state(3);
	let maxAttempts = $state(1);
	let availableFromLocal = $state('');
	let availableToLocal = $state('');
	let shuffleQuestions = $state(true);
	let shuffleOptions = $state(true);

	// UI State
	let isSubmitting = $state(false);

	// Helper to format ISO to Local String or null
	function toUtcIso(localDatetime: string): string | undefined {
		if (!localDatetime) return undefined;
		const d = new Date(localDatetime);
		return isNaN(d.getTime()) ? undefined : d.toISOString();
	}

	async function handleCreateExam(e: Event) {
		e.preventDefault();

		if (!title.trim()) {
			toast.warning('Please provide an examination title.');
			return;
		}

		if (durationMinutes <= 0) {
			toast.warning('Duration must be greater than 0 minutes.');
			return;
		}

		if (passingScore < 0 || passingScore > 100) {
			toast.warning('Passing score must be between 0% and 100%.');
			return;
		}

		if (availableFromLocal && availableToLocal) {
			const fromDate = new Date(availableFromLocal);
			const toDate = new Date(availableToLocal);
			if (toDate <= fromDate) {
				toast.warning('Closing date must be scheduled after opening date.');
				return;
			}
		}

		isSubmitting = true;
		try {
			const examRes = await examsApi.createExam({
				title: title.trim(),
				description: description.trim() || undefined,
				mode,
				durationMinutes: Number(durationMinutes),
				passingScore: Number(passingScore),
				maxAllowedViolations: mode === 'RealExam' ? Number(maxAllowedViolations) : 0,
				maxAttempts: Number(maxAttempts),
				availableFromUtc: toUtcIso(availableFromLocal),
				availableToUtc: toUtcIso(availableToLocal),
				shuffleQuestions,
				shuffleOptions
			});

			toast.success('Examination created! Proceeding to Question Sections Studio...');
			if (examRes?.id) {
				goto(`/instructor/exams/${examRes.id}/edit`);
			} else {
				goto('/instructor/exams');
			}
		} catch (err: any) {
			toast.error(err?.message || 'Failed to create examination.');
		} finally {
			isSubmitting = false;
		}
	}
</script>

<div class="max-w-7xl mx-auto space-y-6 pb-16">
	<!-- Navigation -->
	<div class="flex items-center gap-2">
		<a
			href="/instructor/exams"
			class="btn btn-sm btn-ghost gap-2 text-base-content/70 hover:text-base-content"
		>
			<ArrowLeft class="w-4 h-4" />
			<span>Back to Examinations</span>
		</a>
	</div>

	<!-- Page Banner -->
	<GlassCard class="p-6 sm:p-8">
		<div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
			<div class="space-y-1.5">
				<div class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-primary/10 text-primary border border-primary/20 text-xs font-bold">
					<Sparkles class="w-3.5 h-3.5" />
					<span>Exam Authoring Studio</span>
				</div>
				<h1 class="text-2xl sm:text-3xl font-black text-base-content tracking-tight">
					Create New Examination
				</h1>
				<p class="text-xs sm:text-sm text-base-content/70 max-w-2xl">
					Configure proctoring policies, passing criteria, schedule windows, and attach to courses.
				</p>
			</div>
		</div>
	</GlassCard>

	<!-- Main Form & Live Preview Grid -->
	<form onsubmit={handleCreateExam} class="grid grid-cols-1 lg:grid-cols-3 gap-6 items-start">
		<!-- Left: Form Controls (2 Cols) -->
		<div class="lg:col-span-2 space-y-6">
			<!-- Section 1: Basic Information -->
			<GlassCard class="p-6 space-y-5">
				<div class="flex items-center gap-2 pb-3 border-b border-base-content/10">
					<GraduationCap class="w-5 h-5 text-primary" />
					<h2 class="text-base font-bold text-base-content">1. Basic Information & Mode</h2>
				</div>

				<div class="space-y-4">
					<!-- Title -->
					<div>
						<label for="exam-title-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Examination Title <span class="text-error">*</span>
						</label>
						<input
							id="exam-title-input"
							type="text"
							bind:value={title}
							placeholder="e.g. Advanced Distributed Systems & Cloud Architecture Certification"
							class="input input-bordered w-full bg-base-100/50 font-semibold focus:border-primary"
							required
						/>
					</div>

					<!-- Exam Mode Selector Cards -->
					<div class="space-y-2">
						<span class="label-text text-xs font-bold uppercase tracking-wider text-base-content/70 block">
							Examination Mode <span class="text-error">*</span>
						</span>
						<div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
							<!-- Real Exam Card -->
							<button
								type="button"
								class="p-4 rounded-2xl border text-left transition-all {mode === 'RealExam'
									? 'border-primary bg-primary/10 ring-2 ring-primary/30 shadow-md'
									: 'border-base-content/10 bg-base-100/40 hover:bg-base-200/50'}"
								onclick={() => (mode = 'RealExam')}
							>
								<div class="flex items-center justify-between mb-2">
									<span class="badge badge-sm badge-primary font-bold">Proctored Real Exam</span>
									<ShieldAlert class="w-4 h-4 text-primary" />
								</div>
								<h3 class="font-bold text-sm text-base-content">Strict Examination</h3>
								<p class="text-[11px] text-base-content/70 mt-1 leading-relaxed">
									Webcam verification snapshots, tab-switch detection, and automatic disqualification upon violation limits.
								</p>
							</button>

							<!-- Simulation Card -->
							<button
								type="button"
								class="p-4 rounded-2xl border text-left transition-all {mode === 'Simulation'
									? 'border-secondary bg-secondary/10 ring-2 ring-secondary/30 shadow-md'
									: 'border-base-content/10 bg-base-100/40 hover:bg-base-200/50'}"
								onclick={() => (mode = 'Simulation')}
							>
								<div class="flex items-center justify-between mb-2">
									<span class="badge badge-sm badge-secondary font-bold">Self-Paced Practice</span>
									<Clock class="w-4 h-4 text-secondary" />
								</div>
								<h3 class="font-bold text-sm text-base-content">Practice Simulation</h3>
								<p class="text-[11px] text-base-content/70 mt-1 leading-relaxed">
									Ungraded or relaxed learning mode without active telemetry proctoring or penalty enforcement.
								</p>
							</button>
						</div>
					</div>

					<!-- Candidate Instructions / Overview -->
					<div class="space-y-1.5">
						<div class="flex items-center justify-between">
							<span class="label-text text-xs font-bold uppercase tracking-wider text-base-content/70 block">
								Exam Instructions & Syllabus Overview
							</span>
							<span class="badge badge-xs badge-neutral font-mono text-[10px]">Rich Text</span>
						</div>
						<RichEditor
							bind:content={description}
							placeholder="Provide guidelines, allowed materials, calculator policy, or topic breakdowns for candidates..."
						/>
					</div>
				</div>
			</GlassCard>

			<!-- Section 2: Timing, Scoring & Proctoring Rules -->
			<GlassCard class="p-6 space-y-5">
				<div class="flex items-center gap-2 pb-3 border-b border-base-content/10">
					<Award class="w-5 h-5 text-secondary" />
					<h2 class="text-base font-bold text-base-content">2. Scoring & Security Rules</h2>
				</div>

				<div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
					<!-- Duration -->
					<div>
						<label for="exam-duration-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Duration (Minutes) <span class="text-error">*</span>
						</label>
						<input
							id="exam-duration-input"
							type="number"
							min="1"
							max="720"
							bind:value={durationMinutes}
							class="input input-bordered w-full bg-base-100/50 font-semibold"
							required
						/>
						<span class="text-[10px] text-base-content/50 mt-1 block">Countdown timer length</span>
					</div>

					<!-- Passing Score -->
					<div>
						<label for="exam-pass-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Passing Score (%) <span class="text-error">*</span>
						</label>
						<input
							id="exam-pass-input"
							type="number"
							min="0"
							max="100"
							step="1"
							bind:value={passingScore}
							class="input input-bordered w-full bg-base-100/50 font-semibold"
							required
						/>
						<span class="text-[10px] text-base-content/50 mt-1 block">Score to receive passing badge</span>
					</div>

					<!-- Max Attempts -->
					<div>
						<label for="exam-attempts-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Max Retake Attempts <span class="text-error">*</span>
						</label>
						<input
							id="exam-attempts-input"
							type="number"
							min="1"
							max="10"
							bind:value={maxAttempts}
							class="input input-bordered w-full bg-base-100/50 font-semibold"
							required
						/>
						<span class="text-[10px] text-base-content/50 mt-1 block">Allowed submissions</span>
					</div>
				</div>

				<!-- Anti-cheat violations if RealExam -->
				{#if mode === 'RealExam'}
					<div class="p-4 rounded-2xl bg-error/5 border border-error/20 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
						<div class="space-y-1">
							<div class="flex items-center gap-2 text-error font-bold text-xs">
								<ShieldAlert class="w-4 h-4" />
								<span>Proctoring Disqualification Threshold</span>
							</div>
							<p class="text-[11px] text-base-content/70 max-w-md">
								Number of suspicious tab-outs, window blurs, or missing webcam frames before automatic disqualification.
							</p>
						</div>

						<div class="w-full sm:w-36 flex-shrink-0">
							<input
								type="number"
								min="1"
								max="10"
								bind:value={maxAllowedViolations}
								class="input input-bordered input-sm w-full bg-base-100 text-center font-bold text-error"
								required
							/>
							<span class="text-[10px] text-base-content/50 text-center block mt-1">Allowed strikes</span>
						</div>
					</div>
				{/if}
			</GlassCard>

			<!-- Section 3: Availability Window & Randomization -->
			<GlassCard class="p-6 space-y-5">
				<div class="flex items-center gap-2 pb-3 border-b border-base-content/10">
					<Calendar class="w-5 h-5 text-accent" />
					<h2 class="text-base font-bold text-base-content">3. Availability Windows & Randomization</h2>
				</div>

				<!-- Schedule Windows -->
				<div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
					<div>
						<label for="exam-open-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Opening Time (Optional)
						</label>
						<input
							id="exam-open-input"
							type="datetime-local"
							bind:value={availableFromLocal}
							class="input input-bordered w-full bg-base-100/50 text-xs font-semibold"
						/>
						<span class="text-[10px] text-base-content/50 mt-1 block">Earliest candidate start time</span>
					</div>

					<div>
						<label for="exam-close-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Closing / Deadline Time (Optional)
						</label>
						<input
							id="exam-close-input"
							type="datetime-local"
							bind:value={availableToLocal}
							class="input input-bordered w-full bg-base-100/50 text-xs font-semibold"
						/>
						<span class="text-[10px] text-base-content/50 mt-1 block">Submissions locked after this time</span>
					</div>
				</div>

				<!-- Randomization Toggles -->
				<div class="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-2">
					<label class="flex items-center justify-between p-3.5 rounded-2xl bg-base-200/50 border border-base-content/5 cursor-pointer hover:bg-base-200 transition-colors">
						<div class="flex items-center gap-2.5">
							<div class="w-8 h-8 rounded-xl bg-primary/10 text-primary flex items-center justify-center">
								<Shuffle class="w-4 h-4" />
							</div>
							<div>
								<span class="text-xs font-bold text-base-content block">Shuffle Questions</span>
								<span class="text-[10px] text-base-content/60">Randomize question order</span>
							</div>
						</div>
						<input type="checkbox" class="toggle toggle-primary toggle-sm" bind:checked={shuffleQuestions} />
					</label>

					<label class="flex items-center justify-between p-3.5 rounded-2xl bg-base-200/50 border border-base-content/5 cursor-pointer hover:bg-base-200 transition-colors">
						<div class="flex items-center gap-2.5">
							<div class="w-8 h-8 rounded-xl bg-secondary/10 text-secondary flex items-center justify-center">
								<Shuffle class="w-4 h-4" />
							</div>
							<div>
								<span class="text-xs font-bold text-base-content block">Shuffle Option Choices</span>
								<span class="text-[10px] text-base-content/60">Randomize choice letters</span>
							</div>
						</div>
						<input type="checkbox" class="toggle toggle-secondary toggle-sm" bind:checked={shuffleOptions} />
					</label>
				</div>
			</GlassCard>
		</div>

		<!-- Right: Live Summary & Action Box (1 Col) -->
		<div class="space-y-6 lg:sticky lg:top-6">
			<GlassCard class="p-6 space-y-5 border-primary/20">
				<div class="flex items-center justify-between pb-3 border-b border-base-content/10">
					<h3 class="text-sm font-bold text-base-content flex items-center gap-2">
						<Eye class="w-4 h-4 text-primary" />
						Configuration Summary
					</h3>
					<span class="badge badge-sm badge-outline text-[10px] font-mono">Draft</span>
				</div>

				<!-- Key parameters preview -->
				<div class="space-y-3 text-xs">
					<div class="flex items-center justify-between">
						<span class="text-base-content/60">Mode:</span>
						<span class="badge badge-sm {mode === 'RealExam' ? 'badge-primary' : 'badge-secondary'} font-bold text-[11px]">
							{mode === 'RealExam' ? 'Proctored Exam' : 'Practice Test'}
						</span>
					</div>

					<div class="flex items-center justify-between">
						<span class="text-base-content/60">Duration:</span>
						<span class="font-bold text-base-content">{durationMinutes} minutes</span>
					</div>

					<div class="flex items-center justify-between">
						<span class="text-base-content/60">Passing Threshold:</span>
						<span class="font-bold text-success">{passingScore}%</span>
					</div>

					<div class="flex items-center justify-between">
						<span class="text-base-content/60">Allowed Attempts:</span>
						<span class="font-bold text-base-content">{maxAttempts} attempt(s)</span>
					</div>

					{#if mode === 'RealExam'}
						<div class="flex items-center justify-between">
							<span class="text-base-content/60">Max Violations:</span>
							<span class="font-bold text-error">{maxAllowedViolations} strikes</span>
						</div>
					{/if}

					<div class="flex items-center justify-between">
						<span class="text-base-content/60">Randomization:</span>
						<span class="font-bold text-base-content">
							{shuffleQuestions ? 'Q' : ''}{shuffleQuestions && shuffleOptions ? ' + ' : ''}{shuffleOptions ? 'Choices' : ''}{!shuffleQuestions && !shuffleOptions ? 'None' : ''}
						</span>
					</div>

					<div class="flex items-center justify-between">
						<span class="text-base-content/60">Schedule:</span>
						<span class="font-bold text-base-content truncate max-w-[150px]">
							{availableFromLocal || availableToLocal ? 'Window Active' : 'Always Open'}
						</span>
					</div>
				</div>

				<!-- Call to Action Buttons -->
				<div class="pt-4 border-t border-base-content/10 space-y-2.5">
					<button
						type="submit"
						class="btn btn-primary btn-block shadow-lg shadow-primary/20 gap-2 text-xs font-bold"
						disabled={isSubmitting || !title.trim()}
					>
						{#if isSubmitting}
							<span class="loading loading-spinner loading-xs"></span>
							<span>Creating Exam...</span>
						{:else}
							<Save class="w-4 h-4" />
							<span>Create Exam & Configure Sections</span>
						{/if}
					</button>

					<a
						href="/instructor/exams"
						class="btn btn-ghost btn-sm btn-block text-xs"
					>
						Cancel
					</a>
				</div>
			</GlassCard>

			<!-- Quick Tip Card -->
			<div class="p-4 rounded-2xl bg-base-200/40 border border-base-content/5 text-xs text-base-content/70 space-y-1.5">
				<div class="flex items-center gap-1.5 font-bold text-base-content">
					<HelpCircle class="w-4 h-4 text-primary" />
					<span>What happens next?</span>
				</div>
				<p class="text-[11px] leading-relaxed">
					After creating this exam, you'll be directed to the <strong>Question Sections Studio</strong> where you can link reusable <strong>Question Bank Pools</strong>, customize section points, and publish to students.
				</p>
			</div>
		</div>
	</form>
</div>
