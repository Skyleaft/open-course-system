<script lang="ts">
	import { coursesApi } from '#lib/api/courses.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import RichEditor from '#lib/components/editor/RichEditor.svelte';
	import { toast } from '#lib/stores/toast.svelte.ts';
	import { goto } from '$app/navigation';
	import {
		ArrowLeft,
		Save,
		Sparkles,
		CheckCircle2,
		DollarSign,
		Key,
		BookOpen,
		Lock
	} from '@lucide/svelte';

	let title = $state('');
	let description = $state('');
	let accessType = $state<'OpenFree' | 'OpenPaid' | 'PrivateWithKey'>('OpenFree');
	let price = $state(0);
	let enrollmentKey = $state('');
	let isSubmitting = $state(false);

	const accessModels = [
		{
			id: 'OpenFree',
			title: 'Open Free',
			badge: 'Free',
			badgeClass: 'badge-success text-success-content',
			desc: 'Immediate self-enrollment without payment.',
			icon: CheckCircle2
		},
		{
			id: 'OpenPaid',
			title: 'Open Paid',
			badge: 'Paid',
			badgeClass: 'badge-primary text-primary-content',
			desc: 'Charge students before granting curriculum access.',
			icon: DollarSign
		},
		{
			id: 'PrivateWithKey',
			title: 'Private Key',
			badge: 'Private',
			badgeClass: 'badge-warning text-warning-content',
			desc: 'Invite-only enrollment using a secret passkey.',
			icon: Key
		}
	] as const;

	async function handleCreateCourse(e: Event) {
		e.preventDefault();
		if (!title.trim()) {
			toast.warning('Please provide a course title.');
			return;
		}

		if (accessType === 'OpenPaid' && Number(price) <= 0) {
			toast.warning('Please enter a valid course price greater than $0.');
			return;
		}

		if (accessType === 'PrivateWithKey' && !enrollmentKey.trim()) {
			toast.warning('Please enter a secret enrollment key.');
			return;
		}

		isSubmitting = true;
		try {
			const res = await coursesApi.createCourse({
				title: title.trim(),
				description: description || undefined,
				accessType,
				price: accessType === 'OpenPaid' ? Number(price) : 0,
				enrollmentKey: accessType === 'PrivateWithKey' ? enrollmentKey.trim() : undefined
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
	<!-- Navigation Back -->
	<a
		href="/instructor/courses"
		class="inline-flex items-center gap-2 text-xs font-semibold text-base-content/70 hover:text-primary transition-colors"
	>
		<ArrowLeft class="h-4 w-4" />
		Back to Courses
	</a>

	<!-- Header Banner -->
	<div class="glass-panel relative overflow-hidden rounded-3xl border border-base-content/10 p-8 shadow-2xl space-y-2 backdrop-blur-2xl">
		<div class="inline-flex items-center gap-1.5 rounded-lg bg-secondary/10 border border-secondary/20 px-3 py-1 text-xs font-semibold text-secondary">
			<Sparkles class="h-3.5 w-3.5" />
			Step 1: Course Basics
		</div>
		<h1 class="text-3xl font-extrabold text-base-content tracking-tight sm:text-4xl">
			Create New Course
		</h1>
		<p class="text-xs text-base-content/70 sm:text-sm">
			Define the primary details, access model, and rich curriculum overview for your course.
		</p>
	</div>

	<!-- Main Form -->
	<div class="glass-card rounded-3xl p-6 sm:p-8 border border-base-content/10 shadow-xl space-y-6">
		<form onsubmit={handleCreateCourse} class="space-y-6">
			<!-- Course Title Field -->
			<div class="space-y-2">
				<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block" for="c-title">
					Course Title <span class="text-error">*</span>
				</label>
				<input
					id="c-title"
					type="text"
					class="input input-bordered w-full rounded-2xl bg-base-100/50 border-base-content/20 text-sm text-base-content placeholder:text-base-content/40 focus:border-primary focus:outline-none transition-all px-4 py-3 h-12"
					placeholder="e.g. Advanced Distributed Systems & Cloud Architecture"
					bind:value={title}
					required
				/>
			</div>

			<!-- Access Model Selector Cards -->
			<div class="space-y-3">
				<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block">
					Access Model <span class="text-error">*</span>
				</label>
				<div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
					{#each accessModels as model}
						<button
							type="button"
							class="text-left rounded-2xl p-4 transition-all duration-200 flex flex-col justify-between border cursor-pointer {accessType === model.id
								? 'bg-primary/10 border-primary shadow-md ring-1 ring-primary/30'
								: 'bg-base-100/40 border-base-content/10 hover:border-base-content/30 hover:bg-base-100/70'}"
							onclick={() => (accessType = model.id)}
						>
							<div class="space-y-2">
								<div class="flex items-center justify-between">
									<div class="flex h-8 w-8 items-center justify-center rounded-xl {accessType === model.id ? 'bg-primary text-white' : 'bg-base-200 text-base-content/70'}">
										<model.icon class="h-4 w-4" />
									</div>
									<span class="badge {model.badgeClass} badge-xs font-semibold">
										{model.badge}
									</span>
								</div>
								<div class="font-bold text-sm text-base-content">{model.title}</div>
								<p class="text-xs text-base-content/65 leading-relaxed">{model.desc}</p>
							</div>
						</button>
					{/each}
				</div>
			</div>

			<!-- Conditional Inputs (Price or Secret Key) -->
			{#if accessType === 'OpenPaid'}
				<div class="p-4 rounded-2xl bg-base-100/50 border border-base-content/10 space-y-2 animate-fadeIn">
					<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block" for="c-price">
						Course Price ($ USD) <span class="text-error">*</span>
					</label>
					<div class="relative">
						<span class="absolute left-4 top-1/2 -translate-y-1/2 text-sm font-bold text-base-content/50">$</span>
						<input
							id="c-price"
							type="number"
							step="0.01"
							min="0.01"
							placeholder="49.99"
							class="input input-bordered w-full rounded-xl bg-base-100/70 border-base-content/20 text-sm text-base-content pl-8 pr-4 py-2.5 h-11 focus:border-primary focus:outline-none"
							bind:value={price}
							required
						/>
					</div>
					<p class="text-[11px] text-base-content/60">Students will pay this amount via the platform checkout to enroll.</p>
				</div>
			{:else if accessType === 'PrivateWithKey'}
				<div class="p-4 rounded-2xl bg-base-100/50 border border-base-content/10 space-y-2 animate-fadeIn">
					<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block" for="c-key">
						Secret Enrollment Passkey <span class="text-error">*</span>
					</label>
					<div class="relative">
						<span class="absolute left-4 top-1/2 -translate-y-1/2 text-base-content/50">
							<Lock class="h-4 w-4" />
						</span>
						<input
							id="c-key"
							type="password"
							placeholder="Enter secret passphrase (e.g. SEC-SPRING-2026)"
							class="input input-bordered w-full rounded-xl bg-base-100/70 border-base-content/20 text-sm text-base-content pl-10 pr-4 py-2.5 h-11 focus:border-primary focus:outline-none"
							bind:value={enrollmentKey}
							required
						/>
					</div>
					<p class="text-[11px] text-base-content/60">Only students who supply this secret key can enroll in this course.</p>
				</div>
			{/if}

			<!-- Course Description & Overview (Edra) -->
			<div class="space-y-2">
				<div class="flex items-center justify-between">
					<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block">
						Course Description & Overview
					</label>
					<span class="badge badge-neutral badge-xs font-mono text-[10px]">Edra Editor</span>
				</div>
				<RichEditor
					placeholder="Provide a detailed syllabus overview, learning objectives, and prerequisites..."
					minHeight="220px"
					onUpdate={(json) => (description = json)}
				/>
			</div>

			<!-- Actions Footer -->
			<div class="pt-4 border-t border-base-content/10 flex items-center justify-between gap-4">
				<a
					href="/instructor/courses"
					class="btn btn-ghost rounded-xl text-xs font-semibold text-base-content/70 hover:bg-base-100/50"
				>
					Cancel
				</a>

				<button
					type="submit"
					class="btn btn-primary gradient-accent rounded-xl text-white font-bold border-0 shadow-lg hover:shadow-xl gap-2 h-12 px-6 transition-all"
					disabled={isSubmitting}
				>
					{#if isSubmitting}
						<span class="loading loading-spinner loading-xs"></span>
						<span>Creating Course...</span>
					{:else}
						<Save class="h-4 w-4" />
						<span>Create & Continue to Curriculum</span>
					{/if}
				</button>
			</div>
		</form>
	</div>
</div>
