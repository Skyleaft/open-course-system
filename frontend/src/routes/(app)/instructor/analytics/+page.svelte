<script lang="ts">
	import { dashboardApi, type CourseAnalytics, type ExamAnalytics } from '#lib/api/dashboard.ts';
	import { coursesApi } from '#lib/api/courses.ts';
	import { examsApi } from '#lib/api/exams.ts';
	import type { Course, QuizExam } from '#lib/api/types.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import StatCard from '#lib/components/ui/StatCard.svelte';
	import ScoreHistogram from '#lib/components/ui/ScoreHistogram.svelte';
	import ItemAnalysisTable from '#lib/components/ui/ItemAnalysisTable.svelte';
	import {
		BarChart3,
		BookOpen,
		GraduationCap,
		Users,
		CheckCircle2,
		AlertTriangle,
		Layers,
		Activity,
		HelpCircle,
		Sparkles
	} from '@lucide/svelte';
	import { onMount } from 'svelte';

	let courses = $state<Course[]>([]);
	let exams = $state<QuizExam[]>([]);
	let selectedCourseId = $state<string>('');
	let selectedExamId = $state<string>('');

	let courseAnalytics = $state<CourseAnalytics | null>(null);
	let examAnalytics = $state<ExamAnalytics | null>(null);

	let isLoadingCourses = $state(true);
	let isLoadingExams = $state(true);
	let isLoadingCourseStats = $state(false);
	let isLoadingExamStats = $state(false);

	onMount(async () => {
		try {
			const [cRes, eRes] = await Promise.allSettled([
				coursesApi.getCourses({ pageSize: 50 }),
				examsApi.listExams({ pageSize: 50 })
			]);

			if (cRes.status === 'fulfilled' && cRes.value?.items && cRes.value.items.length > 0) {
				courses = cRes.value.items;
				selectedCourseId = courses[0].id;
				loadCourseAnalytics(selectedCourseId);
			}

			if (eRes.status === 'fulfilled' && eRes.value?.items && eRes.value.items.length > 0) {
				exams = eRes.value.items;
				selectedExamId = exams[0].id;
				loadExamAnalytics(selectedExamId);
			}
		} finally {
			isLoadingCourses = false;
			isLoadingExams = false;
		}
	});

	async function loadCourseAnalytics(courseId: string) {
		if (!courseId) return;
		isLoadingCourseStats = true;
		try {
			const res = await dashboardApi.getInstructorCourseAnalytics(courseId);
			if (res) {
				courseAnalytics = res;
			}
		} catch (err) {
			console.error('Failed to load course analytics:', err);
		} finally {
			isLoadingCourseStats = false;
		}
	}

	async function loadExamAnalytics(examId: string) {
		if (!examId) return;
		isLoadingExamStats = true;
		try {
			const res = await dashboardApi.getInstructorExamAnalytics(examId);
			if (res) {
				examAnalytics = res;
			}
		} catch (err) {
			console.error('Failed to load exam analytics:', err);
		} finally {
			isLoadingExamStats = false;
		}
	}
</script>

<div class="space-y-8">
	<!-- Header -->
	<div class="glass-panel relative overflow-hidden rounded-3xl border border-white/10 p-8 shadow-2xl backdrop-blur-2xl">
		<div class="space-y-2">
			<div class="inline-flex items-center gap-2 rounded-lg bg-secondary/10 border border-secondary/20 px-3 py-1 text-xs font-semibold text-secondary">
				<Sparkles class="h-3.5 w-3.5" />
				Instructor Analytics Studio
			</div>
			<h1 class="text-3xl font-extrabold tracking-tight text-base-content sm:text-4xl">
				Course Funnel & Psychometrics
			</h1>
			<p class="text-xs text-base-content/70 sm:text-sm">
				Analyze student retention drop-offs, exam score normal curves, and Question Bank difficulty & discrimination indexes.
			</p>
		</div>
	</div>

	<!-- SECTION 1: Course Analytics -->
	<div class="space-y-4">
		<div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
			<h2 class="text-lg font-bold text-base-content flex items-center gap-2">
				<BookOpen class="h-5 w-5 text-primary" />
				Course Funnel & Retention
			</h2>

			<div class="flex items-center gap-2">
				<label for="course-select" class="text-xs text-base-content/60 font-semibold">Select Course:</label>
				<select
					id="course-select"
					class="select select-sm select-bordered rounded-xl bg-base-100/50 border-white/10 text-xs max-w-xs"
					bind:value={selectedCourseId}
					onchange={() => loadCourseAnalytics(selectedCourseId)}
					disabled={isLoadingCourses}
				>
					{#each courses as course}
						<option value={course.id}>{course.title}</option>
					{/each}
				</select>
			</div>
		</div>

		{#if isLoadingCourseStats}
			<div class="grid grid-cols-1 gap-4 sm:grid-cols-4">
				<div class="h-24 rounded-2xl bg-base-200/50 animate-pulse"></div>
				<div class="h-24 rounded-2xl bg-base-200/50 animate-pulse"></div>
				<div class="h-24 rounded-2xl bg-base-200/50 animate-pulse"></div>
				<div class="h-24 rounded-2xl bg-base-200/50 animate-pulse"></div>
			</div>
		{:else if courseAnalytics}
			<!-- Stat Cards -->
			<div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
				<StatCard
					title="Total Enrolled"
					value={String(courseAnalytics.totalEnrolled)}
					description="Active student enrollments"
					icon={Users}
					color="primary"
				/>
				<StatCard
					title="Completion Rate"
					value={`${courseAnalytics.completionRate}%`}
					description={`${courseAnalytics.completedStudentsCount} students finished all lessons`}
					icon={CheckCircle2}
					color="success"
				/>
				<StatCard
					title="Total Curriculum"
					value={`${courseAnalytics.totalSections} Sec • ${courseAnalytics.totalLessons} Les`}
					description={`${courseAnalytics.totalAssignments} graded assignments`}
					icon={Layers}
					color="accent"
				/>
				<StatCard
					title="Grading Backlog"
					value={String(courseAnalytics.pendingAssignmentReviewsCount)}
					description="Submissions pending review"
					icon={AlertTriangle}
					color={courseAnalytics.pendingAssignmentReviewsCount > 0 ? 'warning' : 'info'}
				/>
			</div>

			<!-- Section Drop-off Funnel -->
			<GlassCard>
				<div class="space-y-4">
					<div class="border-b border-white/10 pb-3">
						<h3 class="text-sm font-bold text-base-content">Curriculum Section Retention Funnel</h3>
						<p class="text-[10px] text-base-content/60">Percentage of enrolled students who completed each section</p>
					</div>

					<div class="space-y-3">
						{#each courseAnalytics.sectionDropOffs as sec}
							<div class="space-y-1">
								<div class="flex justify-between text-xs font-semibold">
									<span class="text-base-content/80">Section {sec.orderIndex}: {sec.sectionTitle}</span>
									<span class="text-primary font-mono">{sec.retentionRate}% ({sec.studentsCompletedCount} students)</span>
								</div>
								<progress
									class="progress progress-primary w-full h-2 bg-base-300/60"
									value={sec.retentionRate}
									max="100"
								></progress>
							</div>
						{/each}
					</div>
				</div>
			</GlassCard>
		{:else}
			<div class="p-8 text-center text-xs text-base-content/50 glass-card rounded-2xl">
				No course analytics available.
			</div>
		{/if}
	</div>

	<div class="divider opacity-10"></div>

	<!-- SECTION 2: Exam Analytics & Psychometrics -->
	<div class="space-y-4">
		<div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
			<h2 class="text-lg font-bold text-base-content flex items-center gap-2">
				<GraduationCap class="h-5 w-5 text-secondary" />
				Exam Score Distribution & Psychometric Health
			</h2>

			<div class="flex items-center gap-2">
				<label for="exam-select" class="text-xs text-base-content/60 font-semibold">Select Exam:</label>
				<select
					id="exam-select"
					class="select select-sm select-bordered rounded-xl bg-base-100/50 border-white/10 text-xs max-w-xs"
					bind:value={selectedExamId}
					onchange={() => loadExamAnalytics(selectedExamId)}
					disabled={isLoadingExams}
				>
					{#each exams as exam}
						<option value={exam.id}>{exam.title}</option>
					{/each}
				</select>
			</div>
		</div>

		{#if isLoadingExamStats}
			<div class="grid grid-cols-1 gap-4 sm:grid-cols-4">
				<div class="h-24 rounded-2xl bg-base-200/50 animate-pulse"></div>
				<div class="h-24 rounded-2xl bg-base-200/50 animate-pulse"></div>
				<div class="h-24 rounded-2xl bg-base-200/50 animate-pulse"></div>
				<div class="h-24 rounded-2xl bg-base-200/50 animate-pulse"></div>
			</div>
		{:else if examAnalytics}
			<!-- Stat Cards -->
			<div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
				<StatCard
					title="Submissions"
					value={String(examAnalytics.totalSubmissions)}
					description={`${examAnalytics.completedSubmissions} Completed • ${examAnalytics.disqualifiedSubmissions} Disqualified`}
					icon={Users}
					color="secondary"
				/>
				<StatCard
					title="Pass Rate"
					value={`${examAnalytics.passRate}%`}
					description={`${examAnalytics.passedCount} Passed (Passing score: ${examAnalytics.passingScore}%)`}
					icon={CheckCircle2}
					color={examAnalytics.passRate >= 70 ? 'success' : 'warning'}
				/>
				<StatCard
					title="Average Score"
					value={String(examAnalytics.averageScore)}
					description={`Median: ${examAnalytics.medianScore} • StdDev: ${examAnalytics.standardDeviation}`}
					icon={Activity}
					color="accent"
				/>
				<StatCard
					title="Score Range"
					value={`${examAnalytics.lowestScore} - ${examAnalytics.highestScore}`}
					description="Minimum & Maximum scored"
					icon={BarChart3}
					color="info"
				/>
			</div>

			<!-- Score Histogram & Item Analysis -->
			<div class="space-y-6">
				<GlassCard>
					<ScoreHistogram
						buckets={examAnalytics.scoreBuckets}
						passingScore={examAnalytics.passingScore}
						height={180}
					/>
				</GlassCard>

				<div class="space-y-3">
					<div class="flex items-center justify-between">
						<div>
							<h3 class="text-base font-bold text-base-content flex items-center gap-2">
								<HelpCircle class="h-4 w-4 text-primary" />
								Question Bank Psychometric Item Analysis
							</h3>
							<p class="text-[10px] text-base-content/60">
								Item Difficulty ($p$-value: % correct) and Item Discrimination ($D$-index: Upper 27% vs Lower 27% difference)
							</p>
						</div>
					</div>

					<ItemAnalysisTable items={examAnalytics.itemPsychometrics} />
				</div>
			</div>
		{:else}
			<div class="p-8 text-center text-xs text-base-content/50 glass-card rounded-2xl">
				No exam analytics available.
			</div>
		{/if}
	</div>
</div>
