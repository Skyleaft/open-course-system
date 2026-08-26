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
		HelpCircle,
		Sliders,
		Camera,
		Mic,
		Maximize,
		Layers,
		Lock,
		BookOpen
	} from 'lucide-svelte';
	import { examsApi } from '$lib/api/exams.ts';
	import { examRulesApi } from '$lib/api/exam-rules.ts';
	import type { ExamRuleDto, ExamRuleConfig } from '$lib/api/types.ts';
	import { toast } from '$lib/stores/toast.svelte.ts';
	import GlassCard from '$lib/components/ui/GlassCard.svelte';
	import RichEditor from '$lib/components/editor/RichEditor.svelte';
	import { onMount } from 'svelte';

	// Form State
	let title = $state('');
	let description = $state('');
	let durationMinutes = $state(60);
	let passingScore = $state(70);
	let maxAttempts = $state(1);
	let availableFromLocal = $state('');
	let availableToLocal = $state('');
	let shuffleQuestions = $state(true);
	let shuffleOptions = $state(true);

	// Exam Rules State
	let availableRules = $state<ExamRuleDto[]>([]);
	let selectedRuleId = $state<string | 'custom'>('');
	let customRule = $state<ExamRuleConfig>({
		name: 'Custom Exam Policy',
		canTabSwitch: false,
		maxTabSwitchesAllowed: 2,
		restrictClipboardAndMouse: true,
		forceFullscreen: true,
		keyboardDetection: true,
		requireCamera: true,
		snapshotIntervalSeconds: 30,
		requireMicrophone: false,
		maxAllowedViolations: 3,
		autoDisqualifyOnExceed: true
	});

	// UI State
	let isSubmitting = $state(false);
	let isRulesLoading = $state(true);

	onMount(async () => {
		try {
			const rules = await examRulesApi.listRules();
			availableRules = rules || [];
			if (availableRules.length > 0) {
				const strictRule = availableRules.find(r => r.name.toLowerCase().includes('strict')) || availableRules[0];
				selectedRuleId = strictRule.id;
			}
		} catch (err) {
			console.warn('Failed to fetch exam rules presets:', err);
		} finally {
			isRulesLoading = false;
		}
	});

	function selectRule(rule: ExamRuleDto) {
		selectedRuleId = rule.id;
		customRule = {
			name: rule.name,
			canTabSwitch: rule.canTabSwitch,
			maxTabSwitchesAllowed: rule.maxTabSwitchesAllowed,
			restrictClipboardAndMouse: rule.restrictClipboardAndMouse,
			forceFullscreen: rule.forceFullscreen,
			keyboardDetection: rule.keyboardDetection,
			requireCamera: rule.requireCamera,
			snapshotIntervalSeconds: rule.snapshotIntervalSeconds,
			requireMicrophone: rule.requireMicrophone,
			maxAllowedViolations: rule.maxAllowedViolations,
			autoDisqualifyOnExceed: rule.autoDisqualifyOnExceed
		};
	}

	const activeRuleConfig = $derived.by<ExamRuleConfig>(() => {
		if (selectedRuleId === 'custom' || !selectedRuleId) {
			return customRule;
		}
		const found = availableRules.find(r => r.id === selectedRuleId);
		if (found) {
			return {
				name: found.name,
				canTabSwitch: found.canTabSwitch,
				maxTabSwitchesAllowed: found.maxTabSwitchesAllowed,
				restrictClipboardAndMouse: found.restrictClipboardAndMouse,
				forceFullscreen: found.forceFullscreen,
				keyboardDetection: found.keyboardDetection,
				requireCamera: found.requireCamera,
				snapshotIntervalSeconds: found.snapshotIntervalSeconds,
				requireMicrophone: found.requireMicrophone,
				maxAllowedViolations: found.maxAllowedViolations,
				autoDisqualifyOnExceed: found.autoDisqualifyOnExceed
			};
		}
		return customRule;
	});

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
				examRuleId: selectedRuleId !== 'custom' && selectedRuleId ? selectedRuleId : undefined,
				ruleConfig: activeRuleConfig,
				durationMinutes: Number(durationMinutes),
				passingScore: Number(passingScore),
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
					<h2 class="text-base font-bold text-base-content">1. Basic Information</h2>
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

			<!-- Section 2: Exam Rules & Proctoring Policy -->
			<GlassCard class="p-6 space-y-5">
				<div class="flex items-center justify-between pb-3 border-b border-base-content/10">
					<div class="flex items-center gap-2">
						<ShieldAlert class="w-5 h-5 text-primary" />
						<h2 class="text-base font-bold text-base-content">2. Security & Proctoring Rule Policy</h2>
					</div>
					<span class="badge badge-sm badge-primary font-bold">{activeRuleConfig.name}</span>
				</div>

				<!-- Presets Grid -->
				<div class="space-y-3">
					<span class="label-text text-xs font-bold uppercase tracking-wider text-base-content/70 block">
						Select Security Ruleset Preset <span class="text-error">*</span>
					</span>

					<div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
						{#each availableRules as rule (rule.id)}
							<button
								type="button"
								class="p-4 rounded-2xl border text-left transition-all {selectedRuleId === rule.id
									? 'border-primary bg-primary/10 ring-2 ring-primary/30 shadow-md'
									: 'border-base-content/10 bg-base-100/40 hover:bg-base-200/50'}"
								onclick={() => selectRule(rule)}
							>
								<div class="flex items-center justify-between mb-2">
									<span class="badge badge-sm badge-primary font-bold">{rule.name}</span>
									{#if rule.requireCamera}
										<Camera class="w-3.5 h-3.5 text-primary" />
									{:else}
										<BookOpen class="w-3.5 h-3.5 text-secondary" />
									{/if}
								</div>
								<p class="text-[11px] text-base-content/70 mt-1 leading-relaxed line-clamp-2">
									{rule.description || (rule.canTabSwitch ? 'Permits browser research.' : 'Strict proctored environment.')}
								</p>
								<div class="flex flex-wrap gap-1 mt-2 text-[10px]">
									<span class="badge badge-xs {rule.canTabSwitch ? 'badge-success' : 'badge-neutral'}">
										{rule.canTabSwitch ? 'Tabs OK' : 'No Tabs'}
									</span>
									<span class="badge badge-xs {rule.forceFullscreen ? 'badge-primary' : 'badge-ghost'}">
										{rule.forceFullscreen ? 'Fullscreen' : 'Windowed'}
									</span>
									{#if rule.requireCamera}
										<span class="badge badge-xs badge-info">Webcam</span>
									{/if}
								</div>
							</button>
						{/each}

						<!-- Custom Rule Preset Option -->
						<button
							type="button"
							class="p-4 rounded-2xl border text-left transition-all {selectedRuleId === 'custom'
								? 'border-accent bg-accent/10 ring-2 ring-accent/30 shadow-md'
								: 'border-base-content/10 bg-base-100/40 hover:bg-base-200/50'}"
							onclick={() => (selectedRuleId = 'custom')}
						>
							<div class="flex items-center justify-between mb-2">
								<span class="badge badge-sm badge-accent font-bold">Custom Policy</span>
								<Sliders class="w-3.5 h-3.5 text-accent" />
							</div>
							<p class="text-[11px] text-base-content/70 mt-1 leading-relaxed">
								Independently configure webcam intervals, tab limits, clipboard restrictions, and penalties.
							</p>
						</button>
					</div>
				</div>

				<!-- Granular Policy Parameters (Customizable) -->
				{#if selectedRuleId === 'custom'}
					<div class="p-4 rounded-2xl bg-base-200/50 border border-accent/20 space-y-4">
						<div class="flex items-center justify-between">
							<span class="text-xs font-bold text-accent uppercase tracking-wider">Custom Security Controls</span>
						</div>

						<div class="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
							<!-- Tab Switch Toggle -->
							<label class="flex items-center justify-between p-3 rounded-xl bg-base-100 border border-base-content/5 cursor-pointer">
								<div>
									<span class="font-semibold block">Allow Tab Switching</span>
									<span class="text-[10px] text-base-content/60">Permit candidate to open other browser tabs</span>
								</div>
								<input type="checkbox" class="toggle toggle-primary toggle-sm" bind:checked={customRule.canTabSwitch} />
							</label>

							<!-- Force Fullscreen Toggle -->
							<label class="flex items-center justify-between p-3 rounded-xl bg-base-100 border border-base-content/5 cursor-pointer">
								<div>
									<span class="font-semibold block">Force Fullscreen</span>
									<span class="text-[10px] text-base-content/60">Exit triggers strike violation</span>
								</div>
								<input type="checkbox" class="toggle toggle-primary toggle-sm" bind:checked={customRule.forceFullscreen} />
							</label>

							<!-- Clipboard & Mouse Lock -->
							<label class="flex items-center justify-between p-3 rounded-xl bg-base-100 border border-base-content/5 cursor-pointer">
								<div>
									<span class="font-semibold block">Restrict Clipboard & Mouse</span>
									<span class="text-[10px] text-base-content/60">Block copy, paste, and right-click menu</span>
								</div>
								<input type="checkbox" class="toggle toggle-primary toggle-sm" bind:checked={customRule.restrictClipboardAndMouse} />
							</label>

							<!-- Keyboard Detection -->
							<label class="flex items-center justify-between p-3 rounded-xl bg-base-100 border border-base-content/5 cursor-pointer">
								<div>
									<span class="font-semibold block">Detect Keyboard Shortcuts</span>
									<span class="text-[10px] text-base-content/60">Block F12, DevTools, Alt+Tab</span>
								</div>
								<input type="checkbox" class="toggle toggle-primary toggle-sm" bind:checked={customRule.keyboardDetection} />
							</label>

							<!-- Require Webcam -->
							<label class="flex items-center justify-between p-3 rounded-xl bg-base-100 border border-base-content/5 cursor-pointer">
								<div>
									<span class="font-semibold block">Require Webcam Snapshots</span>
									<span class="text-[10px] text-base-content/60">Capture periodic proctoring pictures</span>
								</div>
								<input type="checkbox" class="toggle toggle-info toggle-sm" bind:checked={customRule.requireCamera} />
							</label>

							<!-- Require Microphone -->
							<label class="flex items-center justify-between p-3 rounded-xl bg-base-100 border border-base-content/5 cursor-pointer">
								<div>
									<span class="font-semibold block">Require Microphone</span>
									<span class="text-[10px] text-base-content/60">Verify candidate audio device</span>
								</div>
								<input type="checkbox" class="toggle toggle-secondary toggle-sm" bind:checked={customRule.requireMicrophone} />
							</label>
						</div>

						<div class="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-2">
							<div>
								<label for="custom-strikes-input" class="label-text text-[11px] font-bold text-base-content/70 block mb-1">
									Max Allowed Violations (Strikes)
								</label>
								<input
									id="custom-strikes-input"
									type="number"
									min="1"
									max="10"
									bind:value={customRule.maxAllowedViolations}
									class="input input-bordered input-sm w-full bg-base-100 font-semibold"
								/>
							</div>

							{#if customRule.requireCamera}
								<div>
									<label for="custom-interval-input" class="label-text text-[11px] font-bold text-base-content/70 block mb-1">
										Webcam Snapshot Interval (Seconds)
									</label>
									<input
										id="custom-interval-input"
										type="number"
										min="10"
										max="300"
										bind:value={customRule.snapshotIntervalSeconds}
										class="input input-bordered input-sm w-full bg-base-100 font-semibold"
									/>
								</div>
							{/if}
						</div>
					</div>
				{/if}
			</GlassCard>

			<!-- Section 3: Timing & Scoring Rules -->
			<GlassCard class="p-6 space-y-5">
				<div class="flex items-center gap-2 pb-3 border-b border-base-content/10">
					<Award class="w-5 h-5 text-secondary" />
					<h2 class="text-base font-bold text-base-content">3. Timing & Scoring Thresholds</h2>
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
			</GlassCard>

			<!-- Section 4: Availability Window & Randomization -->
			<GlassCard class="p-6 space-y-5">
				<div class="flex items-center gap-2 pb-3 border-b border-base-content/10">
					<Calendar class="w-5 h-5 text-accent" />
					<h2 class="text-base font-bold text-base-content">4. Availability Windows & Randomization</h2>
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
						<span class="text-base-content/60">Ruleset Policy:</span>
						<span class="badge badge-sm badge-primary font-bold text-[11px]">
							{activeRuleConfig.name}
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

					<div class="flex items-center justify-between">
						<span class="text-base-content/60">Max Strikes:</span>
						<span class="font-bold text-error">{activeRuleConfig.maxAllowedViolations} strikes</span>
					</div>

					<div class="flex items-center justify-between">
						<span class="text-base-content/60">Webcam:</span>
						<span class="font-bold {activeRuleConfig.requireCamera ? 'text-info' : 'text-base-content/60'}">
							{activeRuleConfig.requireCamera ? `Every ${activeRuleConfig.snapshotIntervalSeconds}s` : 'Disabled'}
						</span>
					</div>

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
