<script lang="ts">
	import { page } from '$app/state';
	import { coursesApi } from '#lib/api/courses.ts';
	import type { Assignment } from '#lib/api/types.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import FileUpload from '#lib/components/ui/FileUpload.svelte';
	import RichRenderer from '#lib/components/editor/RichRenderer.svelte';
	import { toast } from '#lib/stores/toast.svelte.ts';
	import { ArrowLeft, Clock, FileCheck, CheckCircle2 } from '@lucide/svelte';
	import { onMount } from 'svelte';

	const courseId = (page.params.id || '') as string;
	const assignmentId = (page.params.assignmentId || '') as string;

	let assignment = $state<Assignment | null>(null);
	let uploadedFileKey = $state<string | null>(null);
	let studentNotes = $state('');
	let isSubmitting = $state(false);
	let isSubmitted = $state(false);
	let isLoading = $state(true);

	onMount(async () => {
		try {
			const course = await coursesApi.getCourseById(courseId);
			const found = course.assignments?.find((a) => a.id === assignmentId);
			assignment = found || null;
		} catch (err) {
			console.error(err);
		} finally {
			isLoading = false;
		}
	});

	async function handleSubmit() {
		if (!uploadedFileKey) {
			toast.warning('Please upload your submission file first.');
			return;
		}

		isSubmitting = true;
		try {
			await coursesApi.submitAssignment(assignmentId, {
				fileAttachmentUrl: uploadedFileKey,
				studentNotes: studentNotes || undefined
			});
			isSubmitted = true;
			toast.success('Assignment submitted successfully!');
		} catch (err: any) {
			toast.error(err?.message || 'Failed to submit assignment.');
		} finally {
			isSubmitting = false;
		}
	}
</script>

<div class="max-w-4xl mx-auto space-y-6">
	<!-- Back Button -->
	<a
		href="/courses/{courseId}"
		class="inline-flex items-center gap-1.5 text-xs font-semibold text-base-content/60 hover:text-primary transition-colors"
	>
		<ArrowLeft class="h-4 w-4" />
		Back to Course
	</a>

	{#if isLoading}
		<div class="glass-panel h-96 rounded-3xl animate-pulse"></div>
	{:else if assignment}
		<!-- Assignment Overview Banner -->
		<div class="glass-panel rounded-3xl border border-white/10 p-8 shadow-2xl space-y-4">
			<div class="flex items-center justify-between">
				<div class="flex items-center gap-2">
					<span class="badge badge-primary badge-sm font-semibold">Assignment</span>
					<span class="text-xs text-base-content/60 flex items-center gap-1">
						<Clock class="h-3.5 w-3.5" />
						Deadline: {new Date(assignment.deadlineUtc).toLocaleString()}
					</span>
				</div>
				<span class="text-sm font-bold text-primary">Max Score: {assignment.maxScore} pts</span>
			</div>

			<h1 class="text-3xl font-extrabold text-base-content tracking-tight">{assignment.title}</h1>

			<div class="border-t border-white/10 pt-4">
				<h3 class="text-xs font-bold uppercase tracking-wider text-base-content/60 mb-2">Instructions</h3>
				<RichRenderer content={assignment.instruction} class="text-sm text-base-content/85" />
			</div>
		</div>

		<!-- Submission Form -->
		<GlassCard class="space-y-6">
			<div class="flex items-center justify-between border-b border-white/10 pb-3">
				<div class="flex items-center gap-2 font-bold text-base text-base-content">
					<FileCheck class="h-5 w-5 text-primary" />
					Submit Your Solution
				</div>
				{#if isSubmitted}
					<span class="badge badge-success badge-sm font-semibold gap-1 text-white">
						<CheckCircle2 class="h-3.5 w-3.5" />
						Submitted
					</span>
				{/if}
			</div>

			{#if !isSubmitted}
				<div class="space-y-4">
					<div>
						<label class="text-xs font-semibold text-base-content/80 mb-1.5 block">
							Upload Solution File (PDF, ZIP, or Code archive)
						</label>
						<FileUpload
							accept=".pdf,.zip,.tar.gz,.rar"
							maxSizeMb={50}
							onUploadComplete={(key) => (uploadedFileKey = key)}
						/>
					</div>

					<div class="space-y-1.5">
						<label class="text-xs font-semibold text-base-content/80" for="notes">Notes for Instructor (Optional)</label>
						<textarea
							id="notes"
							class="glass-input textarea h-24 w-full rounded-xl text-sm"
							placeholder="Add any remarks or context for your submission..."
							bind:value={studentNotes}
						></textarea>
					</div>

					<button
						class="btn btn-primary gradient-accent w-full rounded-xl font-semibold text-white border-0 shadow-lg h-11"
						onclick={handleSubmit}
						disabled={isSubmitting || !uploadedFileKey}
					>
						{#if isSubmitting}
							<span class="loading loading-spinner loading-sm"></span>
						{:else}
							Submit Assignment
						{/if}
					</button>
				</div>
			{:else}
				<div class="text-center py-6 space-y-2">
					<div class="gradient-accent mx-auto flex h-12 w-12 items-center justify-center rounded-2xl text-white shadow-md">
						<CheckCircle2 class="h-6 w-6" />
					</div>
					<h3 class="text-lg font-bold text-base-content">Assignment Received!</h3>
					<p class="text-xs text-base-content/60 max-w-sm mx-auto">
						Your submission has been recorded and will be evaluated by the instructor.
					</p>
				</div>
			{/if}
		</GlassCard>
	{/if}
</div>
