<script lang="ts">
	import { coursesApi } from '#lib/api/courses.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import RichEditor from '#lib/components/editor/RichEditor.svelte';
	import { toast } from '#lib/stores/toast.svelte.ts';
	import { goto } from '$app/navigation';
	import { ArrowLeft, Save, Sparkles } from '@lucide/svelte';

	let title = $state('');
	let description = $state('');
	let accessType = $state('OpenFree');
	let price = $state(0);
	let enrollmentKey = $state('');
	let isSubmitting = $state(false);

	async function handleCreateCourse(e: Event) {
		e.preventDefault();
		if (!title) {
			toast.warning('Please provide a course title.');
			return;
		}

		isSubmitting = true;
		try {
			const res = await coursesApi.createCourse({
				title,
				description,
				accessType,
				price: Number(price) || 0,
				enrollmentKey: accessType === 'PrivateWithKey' ? enrollmentKey : undefined
			});
			toast.success('Course created! Now configure sections and lessons.');
			goto(`/instructor/courses/${res.id}/edit`);
		} catch (err: any) {
			toast.error(err?.message || 'Failed to create course.');
		} finally {
			isSubmitting = false;
		}
	}
</script>

<div class="max-w-3xl mx-auto space-y-6">
	<a
		href="/instructor/courses"
		class="inline-flex items-center gap-1.5 text-xs font-semibold text-base-content/60 hover:text-primary transition-colors"
	>
		<ArrowLeft class="h-4 w-4" />
		Back to Courses
	</a>

	<div class="glass-panel rounded-3xl border border-white/10 p-8 shadow-2xl space-y-2">
		<div class="inline-flex items-center gap-1.5 rounded-lg bg-secondary/10 border border-secondary/20 px-3 py-1 text-xs font-semibold text-secondary">
			<Sparkles class="h-3.5 w-3.5" />
			Step 1: Course Basics
		</div>
		<h1 class="text-3xl font-extrabold text-base-content tracking-tight">
			Create New Course
		</h1>
		<p class="text-xs text-base-content/70">
			Define the primary details, access type, and rich description for your course.
		</p>
	</div>

	<GlassCard>
		<form onsubmit={handleCreateCourse} class="space-y-5">
			<div class="space-y-1.5">
				<label class="text-xs font-semibold text-base-content/80" for="c-title">Course Title</label>
				<input
					id="c-title"
					type="text"
					class="glass-input input input-sm h-11 w-full rounded-xl text-sm"
					placeholder="e.g. Advanced Distributed Systems"
					bind:value={title}
					required
				/>
			</div>

			<div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
				<div class="space-y-1.5">
					<label class="text-xs font-semibold text-base-content/80" for="c-access">Access Model</label>
					<select
						id="c-access"
						class="glass-input select select-sm h-11 w-full rounded-xl text-sm"
						bind:value={accessType}
					>
						<option value="OpenFree">Open Free (Immediate self-enrollment)</option>
						<option value="OpenPaid">Open Paid (Payment required)</option>
						<option value="PrivateWithKey">Private (Requires Secret Key)</option>
					</select>
				</div>

				{#if accessType === 'OpenPaid'}
					<div class="space-y-1.5">
						<label class="text-xs font-semibold text-base-content/80" for="c-price">Price ($ USD)</label>
						<input
							id="c-price"
							type="number"
							step="0.01"
							min="0"
							class="glass-input input input-sm h-11 w-full rounded-xl text-sm"
							bind:value={price}
						/>
					</div>
				{:else if accessType === 'PrivateWithKey'}
					<div class="space-y-1.5">
						<label class="text-xs font-semibold text-base-content/80" for="c-key">Secret Key</label>
						<input
							id="c-key"
							type="password"
							class="glass-input input input-sm h-11 w-full rounded-xl text-sm"
							placeholder="Enter secret enrollment key..."
							bind:value={enrollmentKey}
							required
						/>
					</div>
				{/if}
			</div>

			<div class="space-y-1.5">
				<label class="text-xs font-semibold text-base-content/80">Course Description & Overview (Edra Editor)</label>
				<RichEditor
					placeholder="Provide a comprehensive syllabus overview..."
					minHeight="200px"
					onUpdate={(json) => (description = json)}
				/>
			</div>

			<div class="pt-3 border-t border-white/10 flex justify-end">
				<button
					type="submit"
					class="btn btn-secondary gradient-accent rounded-xl text-white font-semibold border-0 shadow-lg gap-2 h-11 px-6"
					disabled={isSubmitting}
				>
					{#if isSubmitting}
						<span class="loading loading-spinner loading-xs"></span>
					{:else}
						<Save class="h-4 w-4" />
						Create & Continue to Curriculum
					{/if}
				</button>
			</div>
		</form>
	</GlassCard>
</div>
