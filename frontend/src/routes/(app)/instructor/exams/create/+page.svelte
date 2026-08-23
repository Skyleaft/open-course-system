<script lang="ts">
	import { examsApi } from '#lib/api/exams.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import RichEditor from '#lib/components/editor/RichEditor.svelte';
	import { toast } from '#lib/stores/toast.svelte.ts';
	import { goto } from '$app/navigation';
	import { ArrowLeft, Plus, Trash2, CheckCircle2, Save, Sparkles } from '@lucide/svelte';

	let title = $state('');
	let mode = $state('RealExam');
	let durationMinutes = $state(60);
	let passingScore = $state(75);
	let maxAllowedViolations = $state(3);
	let isSubmitting = $state(false);

	// Questions Bank
	interface DraftQuestion {
		text: string;
		type: 'SingleChoice' | 'MultipleChoice' | 'TrueFalse' | 'Essay';
		points: number;
		options: Array<{ text: string; isCorrect: boolean }>;
		explanation: string;
	}

	let questions = $state<DraftQuestion[]>([
		{
			text: '',
			type: 'SingleChoice',
			points: 5,
			options: [
				{ text: 'Option A', isCorrect: true },
				{ text: 'Option B', isCorrect: false },
				{ text: 'Option C', isCorrect: false },
				{ text: 'Option D', isCorrect: false }
			],
			explanation: ''
		}
	]);

	function addQuestion() {
		questions = [
			...questions,
			{
				text: '',
				type: 'SingleChoice',
				points: 5,
				options: [
					{ text: 'Option 1', isCorrect: true },
					{ text: 'Option 2', isCorrect: false }
				],
				explanation: ''
			}
		];
	}

	function removeQuestion(index: number) {
		questions = questions.filter((_, i) => i !== index);
	}

	function addOption(qIdx: number) {
		questions[qIdx].options = [
			...questions[qIdx].options,
			{ text: `Option ${questions[qIdx].options.length + 1}`, isCorrect: false }
		];
	}

	function removeOption(qIdx: number, optIdx: number) {
		questions[qIdx].options = questions[qIdx].options.filter((_, i) => i !== optIdx);
	}

	async function handleSaveExam(e: Event) {
		e.preventDefault();
		if (!title) {
			toast.warning('Please enter an exam title.');
			return;
		}

		isSubmitting = true;
		try {
			// 1. Create exam entity
			const examRes = await examsApi.createExam({
				courseId: 'c-1',
				title,
				mode,
				durationMinutes: Number(durationMinutes),
				passingScore: Number(passingScore),
				maxAllowedViolations: mode === 'RealExam' ? Number(maxAllowedViolations) : 0
			});

			// 2. Add question bank
			if (examRes?.id && questions.length > 0) {
				await examsApi.addQuestions(
					examRes.id,
					questions.map((q, idx) => ({
						text: q.text || 'Sample Question Prompt',
						type: q.type,
						points: Number(q.points),
						orderIndex: idx + 1,
						options: q.options,
						explanation: q.explanation || undefined
					}))
				);
			}

			toast.success('Exam and question bank authored successfully!');
			goto('/instructor/exams');
		} catch (err: any) {
			toast.error(err?.message || 'Failed to author exam.');
		} finally {
			isSubmitting = false;
		}
	}
</script>

<div class="max-w-4xl mx-auto space-y-6">
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
			Exam & Question Authoring
		</div>
		<h1 class="text-3xl font-extrabold text-base-content tracking-tight">
			Create Examination
		</h1>
		<p class="text-xs text-base-content/70">
			Configure examination parameters and build questions using the Edra rich editor.
		</p>
	</div>

	<!-- Parameters Form -->
	<GlassCard>
		<form onsubmit={handleSaveExam} class="space-y-6">
			<div class="space-y-4">
				<h3 class="text-sm font-bold uppercase tracking-wider text-base-content/60 border-b border-white/10 pb-2">
					1. Exam Parameters
				</h3>

				<div class="space-y-1.5">
					<label class="text-xs font-semibold" for="ex-title">Exam Title</label>
					<input
						id="ex-title"
						type="text"
						class="glass-input input input-sm h-11 w-full rounded-xl text-sm"
						placeholder="e.g. Distributed Consensus Final Examination"
						bind:value={title}
						required
					/>
				</div>

				<div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
					<div class="space-y-1.5">
						<label class="text-xs font-semibold" for="ex-mode">Mode</label>
						<select id="ex-mode" class="glass-input select select-sm h-11 w-full rounded-xl text-sm" bind:value={mode}>
							<option value="RealExam">RealExam (Proctored & Strict)</option>
							<option value="Simulation">Simulation (Practice & Keys)</option>
						</select>
					</div>

					<div class="space-y-1.5">
						<label class="text-xs font-semibold" for="ex-dur">Duration (mins)</label>
						<input
							id="ex-dur"
							type="number"
							min="5"
							class="glass-input input input-sm h-11 w-full rounded-xl text-sm"
							bind:value={durationMinutes}
						/>
					</div>

					<div class="space-y-1.5">
						<label class="text-xs font-semibold" for="ex-pass">Passing Score (%)</label>
						<input
							id="ex-pass"
							type="number"
							min="1"
							max="100"
							class="glass-input input input-sm h-11 w-full rounded-xl text-sm"
							bind:value={passingScore}
						/>
					</div>
				</div>

				{#if mode === 'RealExam'}
					<div class="space-y-1.5 sm:w-1/3">
						<label class="text-xs font-semibold" for="ex-viol">Max Allowed Violations</label>
						<input
							id="ex-viol"
							type="number"
							min="1"
							class="glass-input input input-sm h-11 w-full rounded-xl text-sm"
							bind:value={maxAllowedViolations}
						/>
					</div>
				{/if}
			</div>

			<!-- Question Bank Builder -->
			<div class="space-y-6 pt-4 border-t border-white/10">
				<div class="flex items-center justify-between">
					<h3 class="text-sm font-bold uppercase tracking-wider text-base-content/60">
						2. Question Bank ({questions.length})
					</h3>
					<button
						type="button"
						class="btn btn-secondary gradient-accent btn-xs rounded-xl text-white font-semibold border-0 gap-1"
						onclick={addQuestion}
					>
						<Plus class="h-3.5 w-3.5" />
						Add Question
					</button>
				</div>

				{#each questions as q, qIdx (qIdx)}
					<div class="glass-card rounded-2xl border border-white/10 p-6 space-y-4">
						<div class="flex items-center justify-between border-b border-white/10 pb-3">
							<div class="flex items-center gap-2">
								<span class="gradient-accent flex h-6 w-6 items-center justify-center rounded-lg text-xs font-bold text-white">
									{qIdx + 1}
								</span>
								<select
									class="glass-input select select-xs rounded-lg text-xs font-semibold"
									bind:value={q.type}
								>
									<option value="SingleChoice">Single Choice</option>
									<option value="MultipleChoice">Multiple Choice</option>
									<option value="TrueFalse">True / False</option>
									<option value="Essay">Essay</option>
								</select>
							</div>

							<div class="flex items-center gap-3">
								<div class="flex items-center gap-1 text-xs">
									<span class="text-base-content/60">Points:</span>
									<input
										type="number"
										min="1"
										class="glass-input input input-xs w-14 rounded-lg text-center text-xs"
										bind:value={q.points}
									/>
								</div>

								{#if questions.length > 1}
									<button
										type="button"
										class="btn btn-ghost btn-circle btn-xs text-error hover:bg-error/10"
										onclick={() => removeQuestion(qIdx)}
									>
										<Trash2 class="h-3.5 w-3.5" />
									</button>
								{/if}
							</div>
						</div>

						<!-- Question Prompt with Edra -->
						<div class="space-y-1.5">
							<label class="text-xs font-semibold text-base-content/70">Question Prompt (Edra Editor / LaTeX Math)</label>
							<RichEditor
								placeholder="Enter question text with LaTeX formulas or code blocks..."
								minHeight="140px"
								onUpdate={(json) => (q.text = json)}
							/>
						</div>

						<!-- Options Editor (for choice questions) -->
						{#if q.type !== 'Essay'}
							<div class="space-y-2 pt-2">
								<div class="flex items-center justify-between text-xs font-semibold text-base-content/70">
									<span>Answer Options (Mark correct answers)</span>
									<button
										type="button"
										class="text-xs text-primary hover:underline"
										onclick={() => addOption(qIdx)}
									>
										+ Add Option
									</button>
								</div>

								{#each q.options as opt, oIdx (oIdx)}
									<div class="flex items-center gap-2">
										<input
											type="checkbox"
											class="checkbox checkbox-success checkbox-xs rounded-sm"
											bind:checked={opt.isCorrect}
											title="Mark as correct answer"
										/>
										<input
											type="text"
											class="glass-input input input-xs h-8 flex-1 rounded-lg text-xs"
											placeholder="Option text..."
											bind:value={opt.text}
										/>
										{#if q.options.length > 2}
											<button
												type="button"
												class="btn btn-ghost btn-xs text-base-content/40 hover:text-error"
												onclick={() => removeOption(qIdx, oIdx)}
											>
												&times;
											</button>
										{/if}
									</div>
								{/each}
							</div>
						{/if}

						<!-- Explanation -->
						<div class="space-y-1">
							<label class="text-xs font-semibold text-base-content/60">Explanation for Review (Optional)</label>
							<input
								type="text"
								class="glass-input input input-xs h-8 w-full rounded-lg text-xs"
								placeholder="Explain why this answer is correct..."
								bind:value={q.explanation}
							/>
						</div>
					</div>
				{/each}
			</div>

			<div class="pt-4 border-t border-white/10 flex justify-end">
				<button
					type="submit"
					class="btn btn-secondary gradient-accent rounded-xl text-white font-bold border-0 shadow-lg gap-2 h-11 px-6"
					disabled={isSubmitting}
				>
					{#if isSubmitting}
						<span class="loading loading-spinner loading-xs"></span>
					{:else}
						<Save class="h-4 w-4" />
						Save & Publish Exam
					{/if}
				</button>
			</div>
		</form>
	</GlassCard>
</div>
