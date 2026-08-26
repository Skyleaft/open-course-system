<script lang="ts">
	import { onMount } from 'svelte';
	import { fade, scale } from 'svelte/transition';
	import { cubicOut } from 'svelte/easing';
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import {
		Plus,
		Check,
		Trash2,
		ArrowLeft,
		Save,
		Send,
		ShieldAlert,
		Clock,
		CheckCircle2,
		Layers,
		Settings,
		Shuffle,
		FileText,
		BookOpen,
		Sparkles,
		Calendar,
		AlertTriangle,
		Users,
		RotateCcw,
		X,
		Eye,
		RefreshCw,
		Sliders,
		Camera,
		Mic,
		Maximize
	} from 'lucide-svelte';
	import { examsApi } from '$lib/api/exams.ts';
	import { examRulesApi } from '$lib/api/exam-rules.ts';
	import type { QuizExam, QuizSection, QuestionBank, QuizMode, ExamSubmissionDto, ExamRuleDto, ExamRuleConfig } from '$lib/api/types.ts';
	import GlassCard from '$lib/components/ui/GlassCard.svelte';
	import SegmentedTabs from '$lib/components/ui/SegmentedTabs.svelte';
	import ConfirmModal from '$lib/components/ui/ConfirmModal.svelte';
	import SectionBuilder from '$lib/components/exam/SectionBuilder.svelte';
	import RichRenderer from '$lib/components/editor/RichRenderer.svelte';
	import RichEditor from '$lib/components/editor/RichEditor.svelte';
	import { toast } from '$lib/stores/toast.svelte.ts';

	const examId = (page.params.id || '') as string;
	let exam = $state<QuizExam | null>(null);
	let availableBanks = $state<QuestionBank[]>([]);
	let availableRules = $state<ExamRuleDto[]>([]);
	let isLoading = $state(true);
	let isActionLoading = $state(false);

	// Tabs: 'sections' | 'submissions' | 'settings'
	let activeTab = $state<'sections' | 'submissions' | 'settings'>('sections');

	// Submissions & Proctoring State
	let submissions = $state<ExamSubmissionDto[]>([]);
	let isSubmissionsLoading = $state(false);
	let submissionsTotal = $state(0);
	let submissionFilterStatus = $state<string>('All');
	let isRetakeModalOpen = $state(false);
	let retakeCandidate = $state<ExamSubmissionDto | null>(null);
	let retakeReason = $state('');
	let isGrantingRetake = $state(false);

	// Exam Settings State
	let editTitle = $state('');
	let editDescription = $state('');
	let editDurationMinutes = $state(60);
	let editPassingScore = $state(70);
	let editMaxAttempts = $state(1);
	let editAvailableFromLocal = $state('');
	let editAvailableToLocal = $state('');
	let editShuffleQuestions = $state(true);
	let editShuffleOptions = $state(true);
	let isSavingSettings = $state(false);

	// Exam Rule State
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

	// Delete Exam Modal
	let isDeleteExamModalOpen = $state(false);

	onMount(async () => {
		await loadExamAndBanks();
	});

	function toLocalDatetimeString(utcIso?: string | null): string {
		if (!utcIso) return '';
		const d = new Date(utcIso);
		if (isNaN(d.getTime())) return '';
		const pad = (n: number) => n.toString().padStart(2, '0');
		return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
	}

	function toUtcIso(localDatetime: string): string | undefined {
		if (!localDatetime) return undefined;
		const d = new Date(localDatetime);
		return isNaN(d.getTime()) ? undefined : d.toISOString();
	}

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
		const found = availableRules.find((r) => r.id === selectedRuleId);
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

	async function loadExamAndBanks() {
		isLoading = true;
		try {
			const [examData, banksData, rulesData] = await Promise.all([
				examsApi.getExamById(examId),
				examsApi.listQuestionBanks({ pageSize: 100 }),
				examRulesApi.listRules()
			]);

			exam = examData;
			availableBanks = banksData.items || [];
			availableRules = rulesData || [];

			if (exam) {
				editTitle = exam.title;
				editDescription = exam.description || '';
				editDurationMinutes = exam.durationMinutes || 60;
				editPassingScore = exam.passingScore || 70;
				editMaxAttempts = exam.maxAttempts ?? 1;
				editAvailableFromLocal = toLocalDatetimeString(exam.availableFromUtc);
				editAvailableToLocal = toLocalDatetimeString(exam.availableToUtc);
				editShuffleQuestions = exam.shuffleQuestions ?? true;
				editShuffleOptions = exam.shuffleOptions ?? true;

				if (exam.examRuleId) {
					selectedRuleId = exam.examRuleId;
				} else if (exam.ruleConfig) {
					selectedRuleId = 'custom';
					customRule = { ...exam.ruleConfig };
				} else if (availableRules.length > 0) {
					selectedRuleId = availableRules[0].id;
				}
			}
		} catch (err: any) {
			toast.error(err?.message || 'Failed to load examination details.');
		} finally {
			isLoading = false;
		}
	}

	async function handleSaveSettings(e: Event) {
		e.preventDefault();
		if (!editTitle.trim()) {
			toast.warning('Please enter an exam title.');
			return;
		}

		if (editAvailableFromLocal && editAvailableToLocal) {
			const fromDate = new Date(editAvailableFromLocal);
			const toDate = new Date(editAvailableToLocal);
			if (toDate <= fromDate) {
				toast.warning('Closing date must be scheduled after opening date.');
				return;
			}
		}

		isSavingSettings = true;
		try {
			const updated = await examsApi.updateExam(examId, {
				title: editTitle.trim(),
				description: editDescription.trim() || undefined,
				examRuleId: selectedRuleId !== 'custom' && selectedRuleId ? selectedRuleId : undefined,
				ruleConfig: activeRuleConfig,
				durationMinutes: Number(editDurationMinutes),
				passingScore: Number(editPassingScore),
				maxAttempts: Number(editMaxAttempts),
				availableFromUtc: toUtcIso(editAvailableFromLocal),
				availableToUtc: toUtcIso(editAvailableToLocal),
				shuffleQuestions: editShuffleQuestions,
				shuffleOptions: editShuffleOptions
			});

			toast.success('Exam settings updated successfully!');
			if (exam) {
				exam.title = updated.title;
				exam.description = updated.description;
				exam.examRuleId = updated.examRuleId;
				exam.ruleConfig = updated.ruleConfig;
				exam.durationMinutes = updated.durationMinutes;
				exam.passingScore = updated.passingScore;
			}
		} catch (err: any) {
			toast.error(err?.message || 'Failed to save parameters.');
		} finally {
			isSavingSettings = false;
		}
	}

	async function handleSaveDraft() {
		if (!editTitle.trim()) {
			toast.warning('Please enter an exam title.');
			return;
		}

		if (editAvailableFromLocal && editAvailableToLocal) {
			const fromDate = new Date(editAvailableFromLocal);
			const toDate = new Date(editAvailableToLocal);
			if (toDate <= fromDate) {
				toast.warning('Closing date must be scheduled after opening date.');
				return;
			}
		}

		isActionLoading = true;
		try {
			const sectionPayload = (exam?.sections || []).map((sec, idx) => ({
				id: sec.id,
				questionBankId: sec.questionBankId,
				title: sec.title,
				description: sec.description || null,
				pointsOverride: sec.pointsOverride ?? null,
				questionCount: sec.questionCount ?? null,
				orderIndex: sec.orderIndex || idx + 1
			}));

			const updated = await examsApi.updateExam(examId, {
				title: editTitle.trim(),
				description: editDescription.trim() || undefined,
				examRuleId: selectedRuleId !== 'custom' && selectedRuleId ? selectedRuleId : undefined,
				ruleConfig: activeRuleConfig,
				durationMinutes: Number(editDurationMinutes),
				passingScore: Number(editPassingScore),
				maxAttempts: Number(editMaxAttempts),
				availableFromUtc: toUtcIso(editAvailableFromLocal),
				availableToUtc: toUtcIso(editAvailableToLocal),
				shuffleQuestions: editShuffleQuestions,
				shuffleOptions: editShuffleOptions,
				sections: sectionPayload
			});

			toast.success('Exam draft and sections saved successfully!');
			if (exam) {
				exam.title = updated.title;
				exam.description = updated.description;
				exam.examRuleId = updated.examRuleId;
				exam.ruleConfig = updated.ruleConfig;
				exam.durationMinutes = updated.durationMinutes;
				exam.passingScore = updated.passingScore;
			}
		} catch (err: any) {
			toast.error(err?.message || 'Failed to save exam draft.');
		} finally {
			isActionLoading = false;
		}
	}

	async function handleSaveSections(updatedSections: QuizSection[]) {
		if (!exam) return;
		exam.sections = updatedSections;

		try {
			const sectionPayload = updatedSections.map((sec, idx) => ({
				id: sec.id,
				questionBankId: sec.questionBankId,
				title: sec.title,
				description: sec.description || null,
				pointsOverride: sec.pointsOverride ?? null,
				questionCount: sec.questionCount ?? null,
				orderIndex: sec.orderIndex || idx + 1
			}));

			await examsApi.updateExam(examId, {
				title: editTitle.trim() || exam.title,
				description: editDescription.trim() || exam.description || undefined,
				examRuleId: selectedRuleId !== 'custom' && selectedRuleId ? selectedRuleId : undefined,
				ruleConfig: activeRuleConfig,
				durationMinutes: Number(editDurationMinutes),
				passingScore: Number(editPassingScore),
				maxAttempts: Number(editMaxAttempts),
				availableFromUtc: toUtcIso(editAvailableFromLocal),
				availableToUtc: toUtcIso(editAvailableToLocal),
				shuffleQuestions: editShuffleQuestions,
				shuffleOptions: editShuffleOptions,
				sections: sectionPayload
			});

			toast.success('Exam sections saved.');
		} catch (err: any) {
			toast.error(err?.message || 'Failed to persist sections.');
		}
	}

	// Exam Lifecycle Handlers
	async function handlePublish() {
		if (!exam?.sections || exam.sections.length === 0) {
			toast.warning('Cannot publish an exam without at least one section linked to a Question Bank.');
			return;
		}

		isActionLoading = true;
		try {
			await examsApi.publishExam(examId);
			toast.success('Exam published successfully!');
			if (exam) exam.isPublished = true;
		} catch (err: any) {
			toast.error(err?.message || 'Failed to publish exam.');
		} finally {
			isActionLoading = false;
		}
	}

	async function handleDeleteExam() {
		isActionLoading = true;
		try {
			await examsApi.deleteExam(examId);
			toast.success('Exam deleted successfully.');
			goto('/instructor/exams');
		} catch (err: any) {
			toast.error(err?.message || 'Failed to delete exam.');
		} finally {
			isActionLoading = false;
			isDeleteExamModalOpen = false;
		}
	}

	async function loadSubmissions() {
		isSubmissionsLoading = true;
		try {
			const res = await examsApi.getExamSubmissions(examId, {
				status: submissionFilterStatus !== 'All' ? submissionFilterStatus : undefined,
				pageSize: 50
			});
			submissions = res.items || [];
			submissionsTotal = res.totalCount || 0;
		} catch (err: any) {
			toast.error(err?.message || 'Failed to load submissions.');
		} finally {
			isSubmissionsLoading = false;
		}
	}

	function openRetakeModal(sub: ExamSubmissionDto) {
		retakeCandidate = sub;
		retakeReason = '';
		isRetakeModalOpen = true;
	}

	async function handleGrantRetake() {
		if (!retakeCandidate) return;
		isGrantingRetake = true;
		try {
			await examsApi.grantRetake(examId, retakeCandidate.studentId, retakeReason.trim() || undefined);
			toast.success(`Retake permission granted for ${retakeCandidate.studentName}!`);
			isRetakeModalOpen = false;
			retakeCandidate = null;
			await loadSubmissions();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to grant retake.');
		} finally {
			isGrantingRetake = false;
		}
	}
</script>

<div class="space-y-6 max-w-6xl mx-auto pb-16">
	<!-- Top Navigation Bar -->
	<div class="flex flex-wrap items-center justify-between gap-3">
		<a
			href="/instructor/exams"
			class="btn btn-sm btn-ghost gap-2 text-base-content/70 hover:text-base-content"
		>
			<ArrowLeft class="w-4 h-4" />
			<span>Back to Examinations</span>
		</a>

		<div class="flex items-center gap-2">
			<button
				type="button"
				class="btn btn-primary btn-sm gap-1.5 shadow-md shadow-primary/20"
				onclick={handleSaveDraft}
				disabled={isActionLoading}
			>
				{#if isActionLoading}
					<span class="loading loading-spinner loading-xs"></span>
				{:else}
					<Save class="w-3.5 h-3.5" />
				{/if}
				Save Changes
			</button>

			{#if exam && !exam.isPublished}
				<button
					type="button"
					class="btn btn-success btn-sm text-white font-bold shadow-md gap-1.5"
					onclick={handlePublish}
					disabled={isActionLoading || (exam.sections || []).length === 0}
				>
					<Send class="w-3.5 h-3.5" />
					Publish Exam
				</button>
			{/if}

			<button
				type="button"
				class="btn btn-error btn-outline btn-sm gap-1.5"
				onclick={() => (isDeleteExamModalOpen = true)}
				disabled={isActionLoading}
			>
				<Trash2 class="w-3.5 h-3.5" />
				Delete Exam
			</button>
		</div>
	</div>

	{#if isLoading}
		<div class="h-64 rounded-3xl bg-base-200/50 animate-pulse flex items-center justify-center">
			<span class="loading loading-spinner loading-lg text-primary"></span>
		</div>
	{:else if exam}
		<!-- Exam Header Overview Banner -->
		<GlassCard class="p-6 sm:p-7 space-y-4">
			<div class="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4">
				<div class="space-y-2 flex-1 min-w-0">
					<div class="flex items-center gap-2 flex-wrap">
						<span class="badge {exam.mode === 'RealExam' ? 'badge-primary' : 'badge-ghost'} badge-sm font-bold uppercase text-[10px]">
							{exam.mode === 'RealExam' ? 'Proctored Exam' : 'Simulation Practice'}
						</span>
						<span class="badge {exam.isPublished ? 'badge-success text-white' : 'badge-warning'} badge-sm font-semibold text-[10px]">
							{exam.isPublished ? 'Published' : 'Draft'}
						</span>
						<span class="badge badge-sm badge-outline text-[10px]">
							{exam.durationMinutes} mins • {exam.passingScore}% passing
						</span>
					</div>

					<h1 class="text-2xl sm:text-3xl font-extrabold text-base-content tracking-tight">{exam.title}</h1>

					{#if exam.description}
						<div class="text-xs text-base-content/70 line-clamp-3 max-w-3xl pt-0.5">
							<RichRenderer content={exam.description} />
						</div>
					{/if}
				</div>

				<div class="flex items-center gap-2 flex-shrink-0">
					<SegmentedTabs
						tabs={[
							{ id: 'sections', label: 'Exam Sections', icon: Layers, count: exam.sections?.length || 0 },
							{ id: 'submissions', label: 'Submissions', icon: Users, count: submissionsTotal },
							{ id: 'settings', label: 'Parameters', icon: Settings }
						]}
						bind:active={activeTab}
						onChange={(tabId) => {
							if (tabId === 'submissions') loadSubmissions();
						}}
					/>
				</div>
			</div>
		</GlassCard>

		{#if activeTab === 'sections'}
			<!-- Tab 1: Section Builder Studio -->
			<div class="rounded-3xl bg-base-100/60 border border-base-content/10 p-6 shadow-xl space-y-4">
				<SectionBuilder
					sections={exam.sections || []}
					{availableBanks}
					onSaveSections={handleSaveSections}
					onCreateNewBank={() => goto('/instructor/questions')}
				/>
			</div>
		{:else if activeTab === 'submissions'}
			<!-- Tab 2: Candidate Submissions & Proctoring Audit -->
			<GlassCard class="p-6 space-y-6">
				<div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
					<div>
						<h3 class="text-base font-bold text-base-content flex items-center gap-2">
							<Users class="w-5 h-5 text-primary" />
							Candidate Submissions & Proctoring Audit ({submissionsTotal})
						</h3>
						<p class="text-xs text-base-content/70">
							Review candidate scores, integrity violation logs, snapshot counts, and manage retake permissions.
						</p>
					</div>

					<!-- Filter Pills -->
					<div class="flex items-center gap-1.5 bg-base-200/60 p-1 rounded-2xl border border-white/5">
						{#each ['All', 'Completed', 'Disqualified', 'TimedOut', 'InProgress'] as filter}
							<button
								type="button"
								class="btn btn-xs rounded-xl font-bold transition-all {submissionFilterStatus === filter
									? 'btn-primary text-white shadow-xs'
									: 'btn-ghost text-base-content/60'}"
								onclick={() => {
									submissionFilterStatus = filter;
									loadSubmissions();
								}}
							>
								{filter}
							</button>
						{/each}
					</div>
				</div>

				{#if isSubmissionsLoading}
					<div class="space-y-3">
						{#each Array(3) as _}
							<div class="h-16 rounded-2xl bg-base-200/50 animate-pulse"></div>
						{/each}
					</div>
				{:else if submissions.length === 0}
					<div class="py-12 text-center bg-base-200/40 rounded-2xl border border-dashed border-base-300 space-y-2">
						<Users class="w-10 h-10 text-base-content/30 mx-auto" />
						<p class="text-sm font-semibold text-base-content/80">No candidate submissions found</p>
						<p class="text-xs text-base-content/50 max-w-sm mx-auto">
							When enrolled students attempt this examination, their attempts, scores, and integrity logs will be recorded here.
						</p>
					</div>
				{:else}
					<div class="overflow-x-auto rounded-2xl border border-base-content/10">
						<table class="table table-sm w-full text-xs">
							<thead class="bg-base-200/70 text-base-content/70">
								<tr>
									<th>Candidate</th>
									<th>Attempt</th>
									<th>Status</th>
									<th>Score / Result</th>
									<th>Violations</th>
									<th>Started Time</th>
									<th>Submitted Time</th>
									<th class="text-right">Actions</th>
								</tr>
							</thead>
							<tbody class="divide-y divide-base-content/5">
								{#each submissions as sub (sub.id)}
									<tr class="hover:bg-base-100/40 transition-colors">
										<td>
											<div class="flex items-center gap-3">
												<div class="avatar placeholder">
													<div class="w-8 h-8 rounded-xl bg-primary/10 text-primary font-bold text-xs flex items-center justify-center">
														{sub.studentName ? sub.studentName.substring(0, 2).toUpperCase() : 'ST'}
													</div>
												</div>
												<div>
													<div class="font-bold text-base-content">{sub.studentName}</div>
													<div class="text-[10px] text-base-content/50">{sub.studentEmail}</div>
												</div>
											</div>
										</td>
										<td>
											<span class="badge badge-ghost badge-xs font-semibold">
												Attempt #{sub.attemptNumber} / {sub.maxAttempts}
											</span>
										</td>
										<td>
											{#if sub.status === 'Completed'}
												<span class="badge badge-success text-white badge-xs font-bold">
													Completed
												</span>
											{:else if sub.status === 'Disqualified'}
												<span class="badge badge-error text-white badge-xs font-bold">
													Disqualified
												</span>
											{:else if sub.status === 'TimedOut'}
												<span class="badge badge-warning badge-xs font-bold">
													Timed Out
												</span>
											{:else if sub.status === 'InProgress'}
												<span class="badge badge-info text-white badge-xs font-semibold animate-pulse">
													In Progress
												</span>
											{:else}
												<span class="badge badge-ghost badge-xs">
													{sub.status}
												</span>
											{/if}
										</td>
										<td>
											{#if sub.score !== null && sub.score !== undefined}
												<div class="flex items-center gap-1.5">
													<span class="font-bold {sub.isPassed ? 'text-success' : 'text-error'}">
														{sub.score}%
													</span>
													{#if sub.isPassed}
														<span class="badge badge-success text-white badge-xs">Passed</span>
													{:else}
														<span class="badge badge-error text-white badge-xs">Failed</span>
													{/if}
												</div>
											{:else}
												<span class="text-base-content/40 italic">—</span>
											{/if}
										</td>
										<td>
											{#if sub.violationsCount > 0}
												<div class="tooltip tooltip-bottom" data-tip={sub.violations.map(v => `${v.type}: ${v.reason}`).join(' | ')}>
													<span class="badge badge-error text-white badge-xs font-bold gap-1 cursor-pointer">
														<ShieldAlert class="w-3 h-3" />
														{sub.violationsCount} Flags
													</span>
												</div>
											{:else}
												<span class="badge badge-ghost badge-xs text-success font-semibold">0 Flags</span>
											{/if}
										</td>
										<td class="text-base-content/60 text-[11px]">
											{new Date(sub.startedAtUtc).toLocaleString()}
										</td>
										<td class="text-base-content/60 text-[11px]">
											{sub.submittedAtUtc ? new Date(sub.submittedAtUtc).toLocaleString() : 'Active session'}
										</td>
										<td class="text-right">
											<button
												type="button"
												class="btn btn-primary btn-xs rounded-lg text-white font-bold gap-1 shadow-xs"
												onclick={() => openRetakeModal(sub)}
												title="Grant candidate permission to retake exam"
											>
												<RotateCcw class="w-3 h-3" />
												Grant Retake
											</button>
										</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				{/if}
			</GlassCard>
		{:else}
			<!-- Tab 3: Exam Settings Studio -->
			<GlassCard class="p-6 sm:p-8">
				<form onsubmit={handleSaveSettings} class="space-y-6 max-w-3xl">
					<div>
						<label for="edit-title" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
							Exam Title <span class="text-error">*</span>
						</label>
						<input
							id="edit-title"
							type="text"
							bind:value={editTitle}
							class="input input-bordered w-full bg-base-100/50 font-semibold"
							required
						/>
					</div>

					<div class="space-y-1.5">
						<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80 block">
							Exam Description / Guidelines
						</label>
						<RichEditor
							bind:content={editDescription}
							placeholder="Exam instructions, syllabus outline, allowed reference sheets..."
						/>
					</div>

					<!-- Exam Rules Policy Selector -->
					<div class="space-y-3">
						<div class="flex items-center justify-between">
							<span class="label-text text-xs font-bold uppercase tracking-wider text-base-content/80 block">
								Exam Security & Proctoring Ruleset
							</span>
							<span class="badge badge-sm badge-primary font-bold">{activeRuleConfig.name}</span>
						</div>

						<div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
							{#each availableRules as rule (rule.id)}
								<button
									type="button"
									class="p-3.5 rounded-2xl border text-left transition-all {selectedRuleId === rule.id
										? 'border-primary bg-primary/10 ring-2 ring-primary/30 shadow-md'
										: 'border-base-content/10 bg-base-100/40 hover:bg-base-200/50'}"
									onclick={() => selectRule(rule)}
								>
									<div class="flex items-center justify-between mb-1.5">
										<span class="font-bold text-xs text-base-content">{rule.name}</span>
										{#if rule.requireCamera}
											<Camera class="w-3.5 h-3.5 text-primary" />
										{/if}
									</div>
									<p class="text-[11px] text-base-content/70 line-clamp-2">
										{rule.description || (rule.canTabSwitch ? 'Permits browser tabs.' : 'Strict proctored environment.')}
									</p>
								</button>
							{/each}

							<button
								type="button"
								class="p-3.5 rounded-2xl border text-left transition-all {selectedRuleId === 'custom'
									? 'border-accent bg-accent/10 ring-2 ring-accent/30 shadow-md'
									: 'border-base-content/10 bg-base-100/40 hover:bg-base-200/50'}"
								onclick={() => (selectedRuleId = 'custom')}
							>
								<div class="flex items-center justify-between mb-1.5">
									<span class="font-bold text-xs text-accent">Custom Policy</span>
									<Sliders class="w-3.5 h-3.5 text-accent" />
								</div>
								<p class="text-[11px] text-base-content/70">
									Independently configure webcam, tab limits, clipboard lock, and strikes.
								</p>
							</button>
						</div>

						{#if selectedRuleId === 'custom'}
							<div class="p-4 rounded-2xl bg-base-200/50 border border-accent/20 space-y-3">
								<span class="text-xs font-bold text-accent uppercase tracking-wider block">Custom Security Controls</span>
								<div class="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
									<label class="flex items-center justify-between p-2.5 rounded-xl bg-base-100 border border-base-content/5 cursor-pointer">
										<span class="font-semibold">Allow Tab Switching</span>
										<input type="checkbox" class="toggle toggle-primary toggle-xs" bind:checked={customRule.canTabSwitch} />
									</label>

									<label class="flex items-center justify-between p-2.5 rounded-xl bg-base-100 border border-base-content/5 cursor-pointer">
										<span class="font-semibold">Force Fullscreen</span>
										<input type="checkbox" class="toggle toggle-primary toggle-xs" bind:checked={customRule.forceFullscreen} />
									</label>

									<label class="flex items-center justify-between p-2.5 rounded-xl bg-base-100 border border-base-content/5 cursor-pointer">
										<span class="font-semibold">Restrict Clipboard</span>
										<input type="checkbox" class="toggle toggle-primary toggle-xs" bind:checked={customRule.restrictClipboardAndMouse} />
									</label>

									<label class="flex items-center justify-between p-2.5 rounded-xl bg-base-100 border border-base-content/5 cursor-pointer">
										<span class="font-semibold">Detect Keyboard Shortcuts</span>
										<input type="checkbox" class="toggle toggle-primary toggle-xs" bind:checked={customRule.keyboardDetection} />
									</label>

									<label class="flex items-center justify-between p-2.5 rounded-xl bg-base-100 border border-base-content/5 cursor-pointer">
										<span class="font-semibold">Require Camera</span>
										<input type="checkbox" class="toggle toggle-info toggle-xs" bind:checked={customRule.requireCamera} />
									</label>

									<label class="flex items-center justify-between p-2.5 rounded-xl bg-base-100 border border-base-content/5 cursor-pointer">
										<span class="font-semibold">Require Microphone</span>
										<input type="checkbox" class="toggle toggle-secondary toggle-xs" bind:checked={customRule.requireMicrophone} />
									</label>
								</div>

								<div class="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-1">
									<div>
										<label for="edit-max-viol" class="label-text text-[11px] font-bold text-base-content/70 block mb-1">
											Max Allowed Violations (Strikes)
										</label>
										<input
											id="edit-max-viol"
											type="number"
											min="1"
											max="10"
											bind:value={customRule.maxAllowedViolations}
											class="input input-bordered input-sm w-full bg-base-100 font-semibold"
										/>
									</div>

									{#if customRule.requireCamera}
										<div>
											<label for="edit-snapshot-sec" class="label-text text-[11px] font-bold text-base-content/70 block mb-1">
												Snapshot Interval (Seconds)
											</label>
											<input
												id="edit-snapshot-sec"
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
					</div>

					<div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
						<div>
							<label for="edit-duration" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
								Duration (Minutes) <span class="text-error">*</span>
							</label>
							<input
								id="edit-duration"
								type="number"
								min="1"
								bind:value={editDurationMinutes}
								class="input input-bordered w-full bg-base-100/50 font-semibold"
								required
							/>
						</div>

						<div>
							<label for="edit-pass" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
								Passing Score (%) <span class="text-error">*</span>
							</label>
							<input
								id="edit-pass"
								type="number"
								min="0"
								max="100"
								bind:value={editPassingScore}
								class="input input-bordered w-full bg-base-100/50 font-semibold"
								required
							/>
						</div>

						<div>
							<label for="edit-attempts" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
								Max Retake Attempts <span class="text-error">*</span>
							</label>
							<input
								id="edit-attempts"
								type="number"
								min="1"
								max="10"
								bind:value={editMaxAttempts}
								class="input input-bordered w-full bg-base-100/50 font-semibold"
								required
							/>
						</div>
					</div>

					<!-- Schedule Window -->
					<div class="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-2">
						<div>
							<label for="edit-open-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
								Opening Time (Optional)
							</label>
							<input
								id="edit-open-input"
								type="datetime-local"
								bind:value={editAvailableFromLocal}
								class="input input-bordered w-full bg-base-100/50 text-xs font-semibold"
							/>
						</div>

						<div>
							<label for="edit-close-input" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
								Closing / Deadline Time (Optional)
							</label>
							<input
								id="edit-close-input"
								type="datetime-local"
								bind:value={editAvailableToLocal}
								class="input input-bordered w-full bg-base-100/50 text-xs font-semibold"
							/>
						</div>
					</div>

					<!-- Randomization Toggles -->
					<div class="p-4 rounded-2xl bg-base-200/50 border border-base-content/10 space-y-3">
						<span class="text-xs font-bold uppercase tracking-wider text-base-content/80 block">
							Integrity & Randomization
						</span>

						<div class="flex items-center justify-between">
							<span class="text-xs font-medium text-base-content">Shuffle Questions per Candidate</span>
							<input
								type="checkbox"
								bind:checked={editShuffleQuestions}
								class="toggle toggle-primary toggle-sm"
							/>
						</div>

						<div class="flex items-center justify-between">
							<span class="text-xs font-medium text-base-content">Shuffle Options Choices</span>
							<input
								type="checkbox"
								bind:checked={editShuffleOptions}
								class="toggle toggle-secondary toggle-sm"
							/>
						</div>
					</div>

					<div class="pt-2">
						<button
							type="submit"
							class="btn btn-primary gap-1.5 shadow-md shadow-primary/20"
							disabled={isSavingSettings}
						>
							{#if isSavingSettings}
								<span class="loading loading-spinner loading-xs"></span>
							{:else}
								<Save class="w-4 h-4" />
							{/if}
							Save Parameters
						</button>
					</div>
				</form>
			</GlassCard>
		{/if}
	{/if}
</div>

<!-- Delete Exam Confirmation Modal -->
<ConfirmModal
	isOpen={isDeleteExamModalOpen}
	title="Delete Examination"
	message="Are you sure you want to permanently delete this exam? All candidate submission records and active sections will be deleted."
	confirmText="Delete Exam"
	isDanger={true}
	isLoading={isActionLoading}
	onConfirm={handleDeleteExam}
	onCancel={() => (isDeleteExamModalOpen = false)}
/>

<!-- Grant Exam Retake Confirmation Modal -->
{#if isRetakeModalOpen && retakeCandidate}
	<div class="modal modal-open z-[100]" transition:fade={{ duration: 180 }}>
		<div
			class="modal-box max-w-md rounded-3xl border border-white/10 bg-base-100/95 backdrop-blur-2xl p-6 space-y-4 shadow-2xl"
			transition:scale={{ duration: 220, start: 0.94, easing: cubicOut }}
		>
			<div class="flex items-center justify-between border-b border-base-content/10 pb-3">
				<h3 class="font-bold text-base text-base-content flex items-center gap-2">
					<RotateCcw class="w-5 h-5 text-primary" />
					Grant Candidate Retake
				</h3>
				<button type="button" class="btn btn-ghost btn-xs btn-square" onclick={() => (isRetakeModalOpen = false)}>
					<X class="w-4 h-4" />
				</button>
			</div>

			<div class="space-y-3">
				<p class="text-xs text-base-content/80 leading-relaxed">
					Are you sure you want to grant a retake for candidate <strong>{retakeCandidate.studentName}</strong>?
				</p>

				<div class="p-3 bg-primary/10 rounded-2xl border border-primary/20 text-[11px] text-primary space-y-1">
					<p class="font-bold">What happens when you grant a retake:</p>
					<ul class="list-disc pl-4 space-y-0.5 opacity-90">
						<li>Attempt #{retakeCandidate.attemptNumber} will be reset from the active attempt counter.</li>
						<li>Active Redis session locks will be purged.</li>
						<li>The candidate can launch a new exam attempt immediately.</li>
					</ul>
				</div>

				<div class="space-y-1">
					<label for="exam-retake-reason" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Retake Justification / Note (Optional)
					</label>
					<input
						id="exam-retake-reason"
						type="text"
						bind:value={retakeReason}
						placeholder="e.g. Approved retake / Anti-cheat false positive cleared"
						class="input input-bordered input-sm w-full rounded-xl bg-base-200/50 text-xs"
					/>
				</div>
			</div>

			<div class="flex justify-end gap-2 pt-3 border-t border-base-content/10">
				<button type="button" class="btn btn-sm btn-ghost rounded-xl" onclick={() => (isRetakeModalOpen = false)}>
					Cancel
				</button>
				<button
					type="button"
					class="btn btn-sm btn-primary gradient-accent text-white font-bold rounded-xl gap-1.5 shadow-md border-0"
					onclick={handleGrantRetake}
					disabled={isGrantingRetake}
				>
					{#if isGrantingRetake}
						<span class="loading loading-spinner loading-xs"></span>
					{:else}
						<RotateCcw class="w-3.5 h-3.5" />
					{/if}
					Unlock Retake
				</button>
			</div>
		</div>
		<div class="modal-backdrop bg-black/40 backdrop-blur-sm" onclick={() => (isRetakeModalOpen = false)}></div>
	</div>
{/if}
