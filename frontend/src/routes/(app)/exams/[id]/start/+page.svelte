<script lang="ts">
	import { page } from '$app/state';
	import { examsApi } from '#lib/api/exams.ts';
	import type { ExamRuleConfig } from '$lib/api/types.ts';
	import PreExamChecker from '#lib/components/exam/PreExamChecker.svelte';
	import { toast } from '#lib/stores/toast.svelte.ts';
	import { goto } from '$app/navigation';
	import { onMount } from 'svelte';

	const examId = (page.params.id || '') as string;
	let examTitle = $state('Distributed Consensus Final Examination');
	let durationMinutes = $state(60);
	let mode = $state('RealExam');
	let ruleConfig = $state<ExamRuleConfig | null>(null);
	let isLoading = $state(true);

	onMount(async () => {
		try {
			const res = await examsApi.getExamById(examId);
			if (res) {
				examTitle = res.title;
				durationMinutes = res.durationMinutes;
				ruleConfig = res.ruleConfig || null;
				mode = res.ruleConfig?.name || res.mode || 'RealExam';
			}
		} catch {
			// Demo fallback
		} finally {
			isLoading = false;
		}
	});

	async function handleReadyToStart(stream: MediaStream | null) {
		try {
			const startRes = await examsApi.startExam(examId);
			if (startRes?.submissionId) {
				// Store session token in sessionStorage for submission runner
				if (startRes.activeSessionToken) {
					sessionStorage.setItem(`exam_token_${startRes.submissionId}`, startRes.activeSessionToken);
				}
				goto(`/exams/submissions/${startRes.submissionId}`);
			} else {
				toast.error('Failed to receive examination attempt session from server.');
			}
		} catch (err: any) {
			toast.error(err?.message || 'Failed to initialize examination attempt.');
		}
	}
</script>

<div class="py-6">
	{#if isLoading}
		<div class="glass-panel max-w-2xl mx-auto h-96 rounded-3xl animate-pulse"></div>
	{:else}
		<PreExamChecker
			{examTitle}
			{durationMinutes}
			{mode}
			rules={ruleConfig}
			onReadyToStart={handleReadyToStart}
		/>
	{/if}
</div>
