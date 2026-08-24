<script lang="ts">
	import { page } from '$app/state';
	import { examsApi } from '#lib/api/exams.ts';
	import PreExamChecker from '#lib/components/exam/PreExamChecker.svelte';
	import { toast } from '#lib/stores/toast.svelte.ts';
	import { goto } from '$app/navigation';
	import { onMount } from 'svelte';

	const examId = (page.params.id || '') as string;
	let examTitle = $state('Distributed Consensus Final Examination');
	let durationMinutes = $state(60);
	let mode = $state('RealExam');
	let isLoading = $state(true);

	onMount(async () => {
		try {
			const res = await examsApi.getExamById(examId);
			if (res) {
				examTitle = res.title;
				durationMinutes = res.durationMinutes;
				mode = res.mode;
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
				sessionStorage.setItem(`exam_token_${startRes.submissionId}`, startRes.activeSessionToken);
				goto(`/exams/submissions/${startRes.submissionId}`);
			} else {
				// Fallback demo submission ID
				const demoSubId = 'sub-demo-123';
				sessionStorage.setItem(`exam_token_${demoSubId}`, 'token-123');
				goto(`/exams/submissions/${demoSubId}`);
			}
		} catch (err: any) {
			toast.error(err?.message || 'Failed to initialize exam session.');
			// Fallback demo redirect
			const demoSubId = 'sub-demo-123';
			sessionStorage.setItem(`exam_token_${demoSubId}`, 'token-123');
			goto(`/exams/submissions/${demoSubId}`);
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
			onReadyToStart={handleReadyToStart}
		/>
	{/if}
</div>
