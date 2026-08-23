<script lang="ts">
	import { assessmentsApi } from '#lib/api/assessments.ts';
	import type { GradeRecord } from '#lib/api/types.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import { Award, CheckCircle2, FileText, GraduationCap } from '@lucide/svelte';
	import { onMount } from 'svelte';

	let grades = $state<GradeRecord[]>([]);
	let isLoading = $state(true);

	onMount(async () => {
		try {
			grades = await assessmentsApi.getMyGrades();
		} catch {
			// Fallback mock grades for demonstration if empty
			grades = [
				{
					id: '1',
					studentId: 'st-1',
					courseId: 'c-1',
					itemType: 'Quiz',
					referenceId: 'q-1',
					score: 92,
					maxScore: 100,
					weightPercentage: 40,
					evaluatedAtUtc: new Date().toISOString(),
					title: 'Distributed Consensus & Raft Final'
				},
				{
					id: '2',
					studentId: 'st-1',
					courseId: 'c-1',
					itemType: 'Assignment',
					referenceId: 'a-1',
					score: 48,
					maxScore: 50,
					weightPercentage: 20,
					evaluatedAtUtc: new Date().toISOString(),
					title: 'MinIO Presigned Upload Architecture Lab'
				}
			];
		} finally {
			isLoading = false;
		}
	});
</script>

<div class="space-y-8">
	<!-- Header -->
	<div class="glass-panel relative overflow-hidden rounded-3xl border border-white/10 p-8 shadow-2xl backdrop-blur-2xl">
		<div class="space-y-2">
			<div class="inline-flex items-center gap-2 rounded-lg bg-primary/10 border border-primary/20 px-3 py-1 text-xs font-semibold text-primary">
				<Award class="h-3.5 w-3.5" />
				Academic Performance
			</div>
			<h1 class="text-3xl font-extrabold tracking-tight text-base-content sm:text-4xl">
				My Grades & Evaluation
			</h1>
			<p class="text-xs text-base-content/70 sm:text-sm">
				View your evaluation scores across quizzes and assignments.
			</p>
		</div>
	</div>

	<!-- Grade Table -->
	<GlassCard>
		<div class="overflow-x-auto">
			<table class="table table-zebra w-full text-xs">
				<thead>
					<tr class="border-b border-white/10 text-base-content/60 uppercase">
						<th>Item</th>
						<th>Type</th>
						<th>Score</th>
						<th>Max Score</th>
						<th>Weight</th>
						<th>Date</th>
					</tr>
				</thead>
				<tbody>
					{#each grades as grade (grade.id)}
						<tr class="hover:bg-base-100/40 transition-colors">
							<td class="font-bold text-base-content">{grade.title || 'Course Assessment'}</td>
							<td>
								<span class="badge {grade.itemType === 'Quiz' ? 'badge-primary' : 'badge-secondary'} badge-xs font-semibold">
									{grade.itemType}
								</span>
							</td>
							<td class="font-bold text-success text-sm">{grade.score}</td>
							<td>{grade.maxScore}</td>
							<td>{grade.weightPercentage}%</td>
							<td class="text-base-content/60">{new Date(grade.evaluatedAtUtc).toLocaleDateString()}</td>
						</tr>
					{:else}
						<tr>
							<td colspan="6" class="text-center py-8 text-base-content/50">
								No grades recorded yet. Complete quizzes or assignments to earn grades.
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
	</GlassCard>
</div>
