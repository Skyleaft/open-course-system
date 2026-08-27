<script lang="ts">
	import { goto } from '$app/navigation';
	import { coursesApi } from '$lib/api/courses.ts';
	import GlassCard from '$lib/components/ui/GlassCard.svelte';
	import RichEditor from '$lib/components/editor/RichEditor.svelte';
	import { toast } from '$lib/stores/toast.svelte.ts';
	import {
		ArrowLeft,
		Save,
		Sparkles,
		CheckCircle2,
		DollarSign,
		Key,
		BookOpen,
		Lock,
		Image as ImageIcon,
		UploadCloud,
		Trash2,
		RefreshCw
	} from 'lucide-svelte';

	let title = $state('');
	let description = $state('');
	let thumbnailUrl = $state('');
	let accessType = $state<'OpenFree' | 'OpenPaid' | 'PrivateWithKey'>('OpenFree');
	let price = $state(0);
	let enrollmentKey = $state('');
	let isSubmitting = $state(false);
	let isUploadingThumbnail = $state(false);
	let isThumbnailDragging = $state(false);
	let thumbnailInputRef = $state<HTMLInputElement | null>(null);

	const accessModels = [
		{
			id: 'OpenFree',
			title: 'Open Free',
			badge: 'Free',
			badgeClass: 'badge-success text-white',
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

	async function handleThumbnailUpload(files: FileList | null) {
		if (!files || files.length === 0) return;
		const file = files[0];

		if (!file.type.startsWith('image/')) {
			toast.error('Please select an image file (PNG, JPG, WebP, AVIF).');
			return;
		}

		if (file.size > 5 * 1024 * 1024) {
			toast.error('Image size must be less than 5 MB.');
			return;
		}

		isUploadingThumbnail = true;
		try {
			const uploadedUrl = await coursesApi.uploadCourseThumbnail(file);
			thumbnailUrl = uploadedUrl;
			toast.success('Course cover thumbnail uploaded to MinIO storage!');
		} catch (err: any) {
			toast.error(err?.message || 'Failed to upload course thumbnail.');
		} finally {
			isUploadingThumbnail = false;
		}
	}

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
				thumbnailUrl: thumbnailUrl.trim() || undefined,
				accessType,
				price: accessType === 'OpenPaid' ? Number(price) : 0,
				enrollmentKey: accessType === 'PrivateWithKey' ? enrollmentKey.trim() : undefined
			});
			toast.success('Course created! Now configure curriculum sections and lessons.');
			goto(`/instructor/courses/${res.id}/edit`);
		} catch (err: any) {
			toast.error(err?.message || 'Failed to create course.');
		} finally {
			isSubmitting = false;
		}
	}
</script>

<div class="max-w-3xl mx-auto space-y-6 pb-16">
	<!-- Navigation Back -->
	<a
		href="/instructor/courses"
		class="btn btn-sm btn-ghost gap-2 text-base-content/70 hover:text-base-content"
	>
		<ArrowLeft class="w-4 h-4" />
		<span>Back to Courses</span>
	</a>

	<!-- Header Banner -->
	<div class="glass-panel relative overflow-hidden rounded-3xl border border-base-content/10 p-6 sm:p-8 shadow-2xl space-y-2 backdrop-blur-2xl">
		<div class="inline-flex items-center gap-1.5 rounded-lg bg-primary/10 border border-primary/20 px-3 py-1 text-xs font-semibold text-primary">
			<Sparkles class="w-3.5 h-3.5" />
			<span>Step 1: Course Basics</span>
		</div>
		<h1 class="text-2xl sm:text-3xl font-extrabold text-base-content tracking-tight">
			Create New Course
		</h1>
		<p class="text-xs text-base-content/70">
			Define the primary details, cover thumbnail, access model, and rich curriculum overview for your course.
		</p>
	</div>

	<!-- Main Form -->
	<div class="glass-card rounded-3xl p-6 sm:p-8 border border-base-content/10 shadow-xl space-y-6">
		<form onsubmit={handleCreateCourse} class="space-y-6">
			<!-- Course Title Field -->
			<div class="space-y-2">
				<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80 block" for="c-title">
					Course Title <span class="text-error">*</span>
				</label>
				<input
					id="c-title"
					type="text"
					class="input input-bordered w-full rounded-2xl bg-base-100/50 text-sm font-semibold"
					placeholder="e.g. Advanced Distributed Systems & Cloud Architecture"
					bind:value={title}
					required
				/>
			</div>

			<!-- Course Thumbnail Cover Upload -->
			<div class="space-y-2">
				<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80 block">
					Course Cover Thumbnail (MinIO Storage)
				</label>

				{#if thumbnailUrl}
					<div class="glass-card relative overflow-hidden rounded-2xl border border-base-content/15 p-3 flex flex-col sm:flex-row items-center gap-4 bg-base-100/40">
						<div class="relative aspect-video w-full sm:w-48 overflow-hidden rounded-xl bg-base-200 border border-base-content/10 shadow-sm shrink-0">
							<img
								src={thumbnailUrl}
								alt="Course Cover Preview"
								class="h-full w-full object-cover"
							/>
							<div class="absolute top-2 left-2 badge badge-xs badge-success text-white font-bold">
								Uploaded
							</div>
						</div>

						<div class="flex-1 space-y-2 text-center sm:text-left w-full">
							<div class="text-xs font-bold text-base-content flex items-center justify-center sm:justify-start gap-1.5">
								<ImageIcon class="w-4 h-4 text-primary" />
								<span>Cover Image Active</span>
							</div>
							<p class="text-[11px] text-base-content/60 break-all truncate max-w-sm">
								{thumbnailUrl}
							</p>

							<div class="flex items-center justify-center sm:justify-start gap-2 pt-1">
								<button
									type="button"
									class="btn btn-xs btn-outline rounded-lg gap-1"
									onclick={() => thumbnailInputRef?.click()}
									disabled={isUploadingThumbnail}
								>
									{#if isUploadingThumbnail}
										<span class="loading loading-spinner loading-xs"></span>
									{:else}
										<RefreshCw class="w-3 h-3" />
									{/if}
									Change Image
								</button>
								<button
									type="button"
									class="btn btn-xs btn-ghost text-error hover:bg-error/10 rounded-lg gap-1"
									onclick={() => (thumbnailUrl = '')}
								>
									<Trash2 class="w-3 h-3" />
									Remove
								</button>
							</div>
						</div>
					</div>
				{:else}
					<div
						class="glass-card relative flex flex-col items-center justify-center rounded-2xl border-2 border-dashed border-base-content/20 p-6 text-center transition-all duration-200 hover:border-primary/50 hover:bg-base-100/50 cursor-pointer {isThumbnailDragging ? 'border-primary bg-primary/10' : ''}"
						ondragover={(e) => { e.preventDefault(); isThumbnailDragging = true; }}
						ondragleave={() => (isThumbnailDragging = false)}
						ondrop={(e) => { e.preventDefault(); isThumbnailDragging = false; handleThumbnailUpload(e.dataTransfer?.files || null); }}
						onclick={() => thumbnailInputRef?.click()}
						role="button"
						tabindex="0"
						onkeydown={(e) => { if (e.key === 'Enter' || e.key === ' ') thumbnailInputRef?.click(); }}
					>
						{#if isUploadingThumbnail}
							<div class="flex flex-col items-center gap-2 py-4">
								<span class="loading loading-spinner loading-md text-primary"></span>
								<span class="text-xs font-bold text-base-content">Uploading to MinIO S3...</span>
							</div>
						{:else}
							<div class="gradient-accent mb-2 flex h-10 w-10 items-center justify-center rounded-xl text-white shadow-md">
								<UploadCloud class="h-5 w-5" />
							</div>
							<div class="text-xs font-bold text-base-content">
								Click to upload course thumbnail cover or drag & drop
							</div>
							<div class="text-[10px] text-base-content/60 mt-0.5">
								PNG, JPG, WebP or AVIF (Recommended 16:9 ratio, max 5 MB)
							</div>
						{/if}
					</div>
				{/if}

				<input
					type="file"
					accept="image/png,image/jpeg,image/webp,image/avif"
					class="hidden"
					bind:this={thumbnailInputRef}
					onchange={(e) => handleThumbnailUpload((e.target as HTMLInputElement).files)}
				/>
			</div>

			<!-- Access Model Selector Cards -->
			<div class="space-y-3">
				<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80 block">
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
									<div class="w-8 h-8 rounded-xl flex items-center justify-center {accessType === model.id ? 'bg-primary text-primary-content' : 'bg-base-200 text-base-content/70'}">
										<model.icon class="w-4 h-4" />
									</div>
									<span class="badge {model.badgeClass} badge-xs font-semibold text-[9px]">
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
				<div class="p-4 rounded-2xl bg-base-200/50 border border-base-content/10 space-y-2">
					<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80 block" for="c-price">
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
							class="input input-bordered w-full rounded-xl bg-base-100/70 pl-8 pr-4 font-mono font-semibold"
							bind:value={price}
							required
						/>
					</div>
					<p class="text-[11px] text-base-content/60">Students will pay this amount via the platform checkout to enroll.</p>
				</div>
			{:else if accessType === 'PrivateWithKey'}
				<div class="p-4 rounded-2xl bg-base-200/50 border border-base-content/10 space-y-2">
					<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80 block" for="c-key">
						Secret Enrollment Passkey <span class="text-error">*</span>
					</label>
					<div class="relative">
						<span class="absolute left-3.5 top-1/2 -translate-y-1/2 text-base-content/50">
							<Lock class="w-4 h-4" />
						</span>
						<input
							id="c-key"
							type="password"
							placeholder="Enter secret passphrase (e.g. SEC-SPRING-2026)"
							class="input input-bordered w-full rounded-xl bg-base-100/70 pl-10 pr-4 font-mono"
							bind:value={enrollmentKey}
							required
						/>
					</div>
					<p class="text-[11px] text-base-content/60">Only students who supply this secret key can enroll in this course.</p>
				</div>
			{/if}

			<!-- Course Description & Overview -->
			<div class="space-y-1.5">
				<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80 block">
					Course Description & Overview
				</label>
				<RichEditor
					bind:content={description}
					placeholder="Provide a detailed syllabus overview, learning objectives, and prerequisites..."
				/>
			</div>

			<!-- Actions Footer -->
			<div class="pt-4 border-t border-base-content/10 flex items-center justify-between gap-4">
				<a
					href="/instructor/courses"
					class="btn btn-ghost btn-sm rounded-xl text-xs font-semibold text-base-content/70 hover:bg-base-100/50"
				>
					Cancel
				</a>

				<button
					type="submit"
					class="btn btn-primary btn-sm rounded-xl text-white font-bold shadow-lg gap-2 h-11 px-6"
					disabled={isSubmitting || isUploadingThumbnail}
				>
					{#if isSubmitting}
						<span class="loading loading-spinner loading-xs"></span>
						<span>Creating Course...</span>
					{:else}
						<Save class="w-4 h-4" />
						<span>Create & Continue to Curriculum</span>
					{/if}
				</button>
			</div>
		</form>
	</div>
</div>
