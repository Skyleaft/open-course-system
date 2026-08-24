<script lang="ts">
	import { examsApi } from '#lib/api/exams.ts';
	import { coursesApi } from '#lib/api/courses.ts';
	import type { Course } from '#lib/api/types.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import RichEditor from '#lib/components/editor/RichEditor.svelte';
	import { toast } from '#lib/stores/toast.svelte.ts';
	import { goto } from '$app/navigation';
	import {
		ArrowLeft,
		Plus,
		CheckCircle2,
		Save,
		Sparkles,
		Clock,
		ShieldAlert,
		BookOpen,
		Shuffle
	} from '@lucide/svelte';
	import { onMount } from 'svelte';

	let title = $state('');
	let description = $state('');
	let mode = $state<'RealExam' | 'Simulation'>('RealExam');
	let durationMinutes = $state(60);
	let passingScore = $state(75);
	let maxAllowedViolations = $state(3);
	let selectedCourseId = $state<string>('');
	let shuffleQuestions = $state(true);
	let shuffleOptions = $state(true);
	let isSubmitting = $state(false);

	let courses = $state<Course[]>([]);
	let isCoursesLoading = $state(true);

	onMount(async () => {
		try {
			const res = await coursesApi.getCourses({ pageSize: 50 });
			courses = res.items || [];
		} catch (err) {
			// Optional courses
		} finally {
			isCoursesLoading = false;
		}
	});

	async function handleSaveExam(e: Event) {
		e.preventDefault();
		if (!title.trim()) {
			toast.warning('Please enter an examination title.');
			return;
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
				shuffleQuestions,
				shuffleOptions
			});

			toast.success('Exam authored successfully! Opening Question Studio...');
			if (examRes?.id) {
				goto(`/instructor/exams/${examRes.id}/edit`);
			} else {
				goto('/instructor/exams');
			}
		} catch (err: any) {
			toast.error(err?.message || 'Failed to author examination.');
		} finally {
			isSubmitting = false;
		}
	}
</script>

<div class="max-w-3xl mx-auto space-y-6">
	<a
		href="/instructor/exams"
		class="inline-flex items-center gap-1.5 text-xs font-semibold text-base-content/60 hover:text-primary transition-colors"
	>
		<ArrowLeft class="h-4 w-4" />
		Back to Exams
	</a>

	<!-- Header -->
	<div class="glass-panel rounded-3xl border border-white/10 p-8 shadow-2xl space-y-2">
		<div class="inline-flex items-center gap-1.5 rounded-lg bg-secondary/10 border border-secondary/20 px-3 py-1 text-xs font-semibold text-secondary">
			<Sparkles class="h-3.5 w-3.5" />
			Exam Authoring Studio
		</div>
		<h1 class="text-3xl font-extrabold text-base-content tracking-tight">
			Create Examination
		</h1>
		<p class="text-xs text-base-content/70">
			Configure anti-cheat proctoring parameters, grading thresholds, and associate with courses.
		</p>
	</div>

	<!-- Parameters Form -->
	<GlassCard class="p-8 space-y-6">
		<form onsubmit={handleSaveExam} class="space-y-6">
			<!-- Title -->
			<div class="space-y-2">
				<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block" for="ex-title">
					Exam Title <span class="text-error">*</span>
				</label>
				<input
					id="ex-title"
					type="text"
					class="input input-bordered w-full rounded-2xl h-12 bg-base-100/70 border-base-content/20 text-base-content font-semibold focus:border-primary"
					placeholder="e.g. Distributed Consensus & Raft Protocol Final"
					bind:value={title}
					required
				/>
			</div>

			<!-- Course Linkage -->
			<div class="space-y-2">
				<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block" for="ex-course">
					Associated Course (Optional)
				</label>
				<select
					id="ex-course"
					class="select select-bordered w-full rounded-2xl h-12 bg-base-100/70 border-base-content/20 text-sm font-medium"
					bind:value={selectedCourseId}
				>
					<option value="">-- Standalone Examination (No Course Link) --</option>
					{#each courses as course}
						<option value={course.id}>{course.title}</option>
					{/each}
				</select>
			</div>

			<!-- Mode Selection -->
			<div class="space-y-3">
				<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block">
					Examination Mode <span class="text-error">*</span>
				</label>
				<div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
					<button
						type="button"
						class="p-4 rounded-2xl border text-left transition-all {mode === 'RealExam'
							? 'border-primary bg-primary/10 ring-2 ring-primary/20 shadow-md'
							: 'border-base-content/15 bg-base-100/40 hover:bg-base-100/70'}"
						onclick={() => (mode = 'RealExam')}
					>
						<div class="flex items-center justify-between mb-2">
							<span class="badge badge-primary badge-sm font-semibold">Proctored</span>
							<ShieldAlert class="h-4 w-4 text-primary" />
						</div>
						<h4 class="font-bold text-sm text-base-content mb-1">Real Examination</h4>
						<p class="text-[11px] text-base-content/65">
							Webcam snapshots, tab-switch penalties, and strict disqualification limits.
						</p>
					</button>

					<button
						type="button"
						class="p-4 rounded-2xl border text-left transition-all {mode === 'Simulation'
							? 'border-secondary bg-secondary/10 ring-2 ring-secondary/20 shadow-md'
							: 'border-base-content/15 bg-base-100/40 hover:bg-base-100/70'}"
						onclick={() => (mode = 'Simulation')}
					>
						<div class="flex items-center justify-between mb-2">
							<span class="badge badge-secondary badge-sm font-semibold">Self-Paced</span>
							<Clock class="h-4 w-4 text-secondary" />
						</div>
						<h4 class="font-bold text-sm text-base-content mb-1">Practice Simulation</h4>
						<p class="text-[11px] text-base-content/65">
							Practice test environment for candidate learning without active proctoring.
						</p>
					</button>
				</div>
			</div>

			<!-- Numerical Parameters -->
			<div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
				<div class="space-y-1.5">
					<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block" for="ex-dur">
						Duration (Minutes) <span class="text-error">*</span>
					</label>
					<input
						id="ex-dur"
						type="number"
						min="1"
						class="input input-bordered w-full rounded-2xl h-11 bg-base-100/70 border-base-content/20 text-sm font-semibold"
						bind:value={durationMinutes}
						required
					/>
				</div>

				<div class="space-y-1.5">
					<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block" for="ex-pass">
						Passing Score (%) <span class="text-error">*</span>
					</label>
					<input
						id="ex-pass"
						type="number"
						min="0"
						max="100"
						class="input input-bordered w-full rounded-2xl h-11 bg-base-100/70 border-base-content/20 text-sm font-semibold"
						bind:value={passingScore}
						required
					/>
				</div>

				{#if mode === 'RealExam'}
					<div class="space-y-1.5">
						<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block" for="ex-viol">
							Max Violations <span class="text-error">*</span>
						</label>
						<input
							id="ex-viol"
							type="number"
							min="1"
							class="input input-bordered w-full rounded-2xl h-11 bg-base-100/70 border-base-content/20 text-sm font-semibold"
							bind:value={maxAllowedViolations}
							required
						/>
					</div>
				{/if}
			</div>

			<!-- Shuffle Toggles -->
			<div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
				<label class="flex items-center justify-between p-3 rounded-2xl bg-base-100/40 border border-white/5 cursor-pointer">
					<div class="flex items-center gap-2 text-xs font-semibold">
						<Shuffle class="h-4 w-4 text-primary" />
						<span>Shuffle Questions per Candidate</span>
					</div>
					<input type="checkbox" class="toggle toggle-primary toggle-sm" bind:checked={shuffleQuestions} />
				</label>

				<label class="flex items-center justify-between p-3 rounded-2xl bg-base-100/40 border border-white/5 cursor-pointer">
					<div class="flex items-center gap-2 text-xs font-semibold">
						<Shuffle class="h-4 w-4 text-secondary" />
						<span>Shuffle Options Choices</span>
					</div>
					<input type="checkbox" class="toggle toggle-secondary toggle-sm" bind:checked={shuffleOptions} />
				</label>
			</div>

			<!-- Description with Edra Editor -->
			<div class="space-y-2">
				<div class="flex items-center justify-between">
					<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block">
						Exam Instructions & Overview
					</label>
					<span class="badge badge-neutral badge-xs font-mono text-[10px]">Edra Editor</span>
				</div>
				<RichEditor
					content={description}
					minHeight="140px"
					placeholder="Describe guidelines, instructions, or allowed resources..."
					onUpdate={(json) => {
						description = json;
					}}
				/>
			</div>

			<!-- Submit Button -->
			<div class="pt-4 border-t border-white/10 flex justify-end">
				<button
					type="submit"
					class="btn btn-secondary gradient-accent rounded-xl text-white font-bold border-0 shadow-lg px-8 gap-2"
					disabled={isSubmitting}
				>
					{#if isSubmitting}
						<span class="loading loading-spinner loading-xs"></span>
					{:else}
						<Save class="h-4 w-4" />
					{/if}
					Create Exam & Open Question Studio
				</button>
			</div>
		</form>
	</GlassCard>
</div>
