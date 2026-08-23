<script lang="ts">
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { coursesApi } from '#lib/api/courses.ts';
	import type { Course, CourseSection, Lesson, LessonType } from '#lib/api/types.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import GlassModal from '#lib/components/ui/GlassModal.svelte';
	import ConfirmModal from '#lib/components/ui/ConfirmModal.svelte';
	import RichEditor from '#lib/components/editor/RichEditor.svelte';
	import { toast } from '#lib/stores/toast.svelte.ts';
	import {
		Plus,
		Check,
		PlayCircle,
		FileText,
		Download,
		Layers,
		ArrowLeft,
		Send,
		Edit3,
		Trash2,
		AlertTriangle,
		Settings,
		Save,
		DollarSign,
		Key,
		CheckCircle2,
		Lock,
		AlignLeft,
		BookOpen
	} from '@lucide/svelte';
	import { onMount } from 'svelte';

	const courseId = (page.params.id || '') as string;
	let course = $state<Course | null>(null);
	let isLoading = $state(true);
	let isActionLoading = $state(false);

	// Tabs: 'curriculum' | 'settings'
	let activeTab = $state<'curriculum' | 'settings'>('curriculum');

	// Course Settings State
	let editCourseTitle = $state('');
	let editCourseDescription = $state('');
	let editCourseAccessType = $state<'OpenFree' | 'OpenPaid' | 'PrivateWithKey'>('OpenFree');
	let editCoursePrice = $state(0);
	let editCourseEnrollmentKey = $state('');
	let isSavingSettings = $state(false);

	// Section Modals
	let isAddSectionModalOpen = $state(false);
	let newSectionTitle = $state('');

	let isEditSectionModalOpen = $state(false);
	let editingSectionId = $state<string | null>(null);
	let editingSectionTitle = $state('');

	let isDeleteSectionModalOpen = $state(false);
	let deletingSectionId = $state<string | null>(null);
	let deletingSectionTitle = $state('');

	// Lesson Modals
	let isAddLessonModalOpen = $state(false);
	let targetSectionId = $state<string | null>(null);
	let newLessonTitle = $state('');
	let newLessonType = $state<LessonType>('Text');
	let newLessonDuration = $state(10);
	let newLessonContentUrl = $state('');
	let newLessonTextContent = $state('');

	let isEditLessonModalOpen = $state(false);
	let editingLessonId = $state<string | null>(null);
	let editingLessonTitle = $state('');
	let editingLessonType = $state<LessonType>('Text');
	let editingLessonDuration = $state(10);
	let editingLessonContentUrl = $state('');
	let editingLessonTextContent = $state('');

	let isDeleteLessonModalOpen = $state(false);
	let deletingLessonId = $state<string | null>(null);
	let deletingLessonTitle = $state('');

	// Delete Course Modal
	let isDeleteCourseModalOpen = $state(false);

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

	const materialTypes: { id: LessonType; label: string; icon: any }[] = [
		{ id: 'Text', label: 'Rich Text', icon: AlignLeft },
		{ id: 'Video', label: 'Video Stream', icon: PlayCircle },
		{ id: 'PdfDocument', label: 'PDF Document', icon: FileText },
		{ id: 'DownloadableFile', label: 'Download File', icon: Download }
	];

	onMount(async () => {
		await loadCourse();
	});

	async function loadCourse() {
		isLoading = true;
		try {
			course = await coursesApi.getCourseById(courseId);
			if (course) {
				editCourseTitle = course.title;
				editCourseDescription = course.description || '';
				editCourseAccessType = (course.accessType as any) || 'OpenFree';
				editCoursePrice = course.price || 0;
				editCourseEnrollmentKey = '';
			}
		} catch (err: any) {
			toast.error(err?.message || 'Failed to load course details.');
		} finally {
			isLoading = false;
		}
	}

	// Course Settings Save Handler
	async function handleSaveCourseSettings(e: Event) {
		e.preventDefault();
		if (!editCourseTitle.trim()) {
			toast.warning('Please provide a course title.');
			return;
		}

		if (editCourseAccessType === 'OpenPaid' && Number(editCoursePrice) <= 0) {
			toast.warning('Please enter a valid course price greater than $0.');
			return;
		}

		isSavingSettings = true;
		try {
			const updated = await coursesApi.updateCourse(courseId, {
				title: editCourseTitle.trim(),
				description: editCourseDescription || undefined,
				accessType: editCourseAccessType,
				price: editCourseAccessType === 'OpenPaid' ? Number(editCoursePrice) : 0,
				enrollmentKey:
					editCourseAccessType === 'PrivateWithKey' && editCourseEnrollmentKey.trim()
						? editCourseEnrollmentKey.trim()
						: undefined
			});
			toast.success('Course settings updated successfully!');
			if (course) {
				course.title = updated.title;
				course.description = updated.description;
				course.accessType = updated.accessType;
				course.price = updated.price;
			}
		} catch (err: any) {
			toast.error(err?.message || 'Failed to update course settings.');
		} finally {
			isSavingSettings = false;
		}
	}

	// Section Handlers
	async function handleAddSection() {
		if (!newSectionTitle.trim()) return;
		isActionLoading = true;
		try {
			await coursesApi.addSection(courseId, {
				title: newSectionTitle.trim()
			});
			toast.success('Section added successfully!');
			newSectionTitle = '';
			isAddSectionModalOpen = false;
			await loadCourse();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to add section.');
		} finally {
			isActionLoading = false;
		}
	}

	function openEditSection(section: CourseSection) {
		editingSectionId = section.id;
		editingSectionTitle = section.title;
		isEditSectionModalOpen = true;
	}

	async function handleUpdateSection() {
		if (!editingSectionId || !editingSectionTitle.trim()) return;
		isActionLoading = true;
		try {
			await coursesApi.updateSection(editingSectionId, {
				title: editingSectionTitle.trim()
			});
			toast.success('Section updated successfully!');
			isEditSectionModalOpen = false;
			await loadCourse();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to update section.');
		} finally {
			isActionLoading = false;
		}
	}

	function openDeleteSection(section: CourseSection) {
		deletingSectionId = section.id;
		deletingSectionTitle = section.title;
		isDeleteSectionModalOpen = true;
	}

	async function handleDeleteSection() {
		if (!deletingSectionId) return;
		isActionLoading = true;
		try {
			await coursesApi.deleteSection(deletingSectionId);
			toast.success('Section and its lessons deleted.');
			isDeleteSectionModalOpen = false;
			await loadCourse();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to delete section.');
		} finally {
			isActionLoading = false;
		}
	}

	// Lesson Handlers
	function openAddLesson(sectionId: string) {
		targetSectionId = sectionId;
		newLessonTitle = '';
		newLessonType = 'Text';
		newLessonDuration = 10;
		newLessonContentUrl = '';
		newLessonTextContent = '';
		isAddLessonModalOpen = true;
	}

	async function handleAddLesson() {
		if (!targetSectionId || !newLessonTitle.trim()) {
			toast.warning('Please provide a lesson title.');
			return;
		}

		if (newLessonType !== 'Text' && !newLessonContentUrl.trim()) {
			toast.warning('Please provide a storage URL / MinIO path for media materials.');
			return;
		}

		isActionLoading = true;
		try {
			await coursesApi.addLesson(targetSectionId, {
				title: newLessonTitle.trim(),
				type: newLessonType,
				contentUrl: newLessonContentUrl.trim() || undefined,
				textContent: newLessonType === 'Text' ? newLessonTextContent || undefined : undefined,
				durationMinutes: Number(newLessonDuration) || 0
			});
			toast.success('Lesson created successfully!');
			isAddLessonModalOpen = false;
			await loadCourse();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to add lesson.');
		} finally {
			isActionLoading = false;
		}
	}

	function openEditLesson(lesson: Lesson) {
		editingLessonId = lesson.id;
		editingLessonTitle = lesson.title;
		editingLessonType = (lesson.type as any) || 'Text';
		editingLessonDuration = lesson.durationMinutes || 10;
		editingLessonContentUrl = lesson.contentUrl || '';
		editingLessonTextContent = lesson.textContent || '';
		isEditLessonModalOpen = true;
	}

	async function handleUpdateLesson() {
		if (!editingLessonId || !editingLessonTitle.trim()) {
			toast.warning('Please provide a lesson title.');
			return;
		}

		if (editingLessonType !== 'Text' && !editingLessonContentUrl.trim()) {
			toast.warning('Please provide a storage URL / MinIO path for media materials.');
			return;
		}

		isActionLoading = true;
		try {
			await coursesApi.updateLesson(editingLessonId, {
				title: editingLessonTitle.trim(),
				type: editingLessonType,
				contentUrl: editingLessonContentUrl.trim() || undefined,
				textContent: editingLessonType === 'Text' ? editingLessonTextContent || undefined : undefined,
				durationMinutes: Number(editingLessonDuration) || 0
			});
			toast.success('Lesson updated successfully!');
			isEditLessonModalOpen = false;
			await loadCourse();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to update lesson.');
		} finally {
			isActionLoading = false;
		}
	}

	function openDeleteLesson(lesson: Lesson) {
		deletingLessonId = lesson.id;
		deletingLessonTitle = lesson.title;
		isDeleteLessonModalOpen = true;
	}

	async function handleDeleteLesson() {
		if (!deletingLessonId) return;
		isActionLoading = true;
		try {
			await coursesApi.deleteLesson(deletingLessonId);
			toast.success('Lesson deleted.');
			isDeleteLessonModalOpen = false;
			await loadCourse();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to delete lesson.');
		} finally {
			isActionLoading = false;
		}
	}

	// Course Handlers
	async function handlePublish() {
		isActionLoading = true;
		try {
			await coursesApi.publishCourse(courseId);
			toast.success('Course published to public catalog!');
			if (course) course.isPublished = true;
		} catch (err: any) {
			toast.error(err?.message || 'Failed to publish course.');
		} finally {
			isActionLoading = false;
		}
	}

	async function handleDeleteCourse() {
		isActionLoading = true;
		try {
			await coursesApi.deleteCourse(courseId);
			toast.success('Course deleted successfully.');
			goto('/instructor/courses');
		} catch (err: any) {
			toast.error(err?.message || 'Failed to delete course.');
		} finally {
			isActionLoading = false;
			isDeleteCourseModalOpen = false;
		}
	}
</script>

<div class="space-y-8">
	<!-- Header Navigation & Actions -->
	<div class="flex flex-wrap items-center justify-between gap-3">
		<a
			href="/instructor/courses"
			class="inline-flex items-center gap-1.5 text-xs font-semibold text-base-content/60 hover:text-primary transition-colors"
		>
			<ArrowLeft class="h-4 w-4" />
			Back to Courses
		</a>

		<div class="flex items-center gap-2">
			{#if course && !course.isPublished}
				<button
					class="btn btn-success btn-sm rounded-xl text-white font-bold border-0 shadow-md gap-1.5"
					onclick={handlePublish}
					disabled={isActionLoading}
				>
					<Send class="h-3.5 w-3.5" />
					Publish Course
				</button>
			{/if}

			<button
				class="btn btn-error btn-outline btn-sm rounded-xl gap-1.5"
				onclick={() => (isDeleteCourseModalOpen = true)}
				disabled={isActionLoading}
			>
				<Trash2 class="h-3.5 w-3.5" />
				Delete Course
			</button>
		</div>
	</div>

	{#if isLoading}
		<div class="glass-panel h-80 rounded-3xl animate-pulse"></div>
	{:else if course}
		<!-- Course Overview Banner -->
		<div class="glass-panel rounded-3xl border border-white/10 p-8 shadow-2xl space-y-4">
			<div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
				<div class="space-y-1">
					<div class="flex items-center gap-2">
						<span class="badge badge-primary badge-xs font-bold uppercase">{course.accessType}</span>
						<span class="badge {course.isPublished ? 'badge-success text-white' : 'badge-warning'} badge-xs font-semibold">
							{course.isPublished ? 'Published' : 'Draft'}
						</span>
					</div>
					<h1 class="text-3xl font-extrabold text-base-content tracking-tight">{course.title}</h1>
				</div>

				<div class="flex items-center gap-2">
					<!-- Tabs switch -->
					<div class="glass-card flex items-center gap-1 rounded-2xl p-1 border border-white/10">
						<button
							class="btn btn-xs rounded-xl font-semibold transition-all gap-1.5 {activeTab === 'curriculum'
								? 'btn-secondary text-white shadow-xs'
								: 'btn-ghost text-base-content/70 hover:bg-base-100/40'}"
							onclick={() => (activeTab = 'curriculum')}
						>
							<Layers class="h-3.5 w-3.5" />
							Curriculum
						</button>
						<button
							class="btn btn-xs rounded-xl font-semibold transition-all gap-1.5 {activeTab === 'settings'
								? 'btn-secondary text-white shadow-xs'
								: 'btn-ghost text-base-content/70 hover:bg-base-100/40'}"
							onclick={() => (activeTab = 'settings')}
						>
							<Settings class="h-3.5 w-3.5" />
							Course Settings
						</button>
					</div>

					{#if activeTab === 'curriculum'}
						<button
							class="btn btn-secondary gradient-accent btn-sm rounded-xl text-white font-semibold border-0 shadow-md gap-1.5"
							onclick={() => (isAddSectionModalOpen = true)}
						>
							<Plus class="h-4 w-4" />
							Add Section
						</button>
					{/if}
				</div>
			</div>
		</div>

		{#if activeTab === 'curriculum'}
			<!-- Tab 1: Sections & Lessons Builder -->
			<div class="space-y-4">
				{#each course.sections || [] as section, sIdx (section.id || sIdx)}
					<GlassCard class="space-y-4 p-6">
						<!-- Section Header -->
						<div class="flex items-center justify-between border-b border-white/10 pb-3">
							<div class="flex items-center gap-2">
								<span class="gradient-accent flex h-6 w-6 items-center justify-center rounded-lg text-xs font-bold text-white">
									{sIdx + 1}
								</span>
								<h3 class="text-base font-bold text-base-content">{section.title}</h3>
							</div>

							<div class="flex items-center gap-1">
								<button
									class="btn btn-ghost btn-xs text-base-content/70 hover:text-secondary rounded-lg p-1.5"
									title="Edit section title"
									onclick={() => openEditSection(section)}
								>
									<Edit3 class="h-3.5 w-3.5" />
								</button>

								<button
									class="btn btn-ghost btn-xs text-base-content/70 hover:text-error rounded-lg p-1.5"
									title="Delete section"
									onclick={() => openDeleteSection(section)}
								>
									<Trash2 class="h-3.5 w-3.5" />
								</button>

								<div class="h-4 w-px bg-base-content/15 mx-1"></div>

								<button
									class="btn btn-secondary btn-outline btn-xs rounded-xl gap-1 font-semibold"
									onclick={() => openAddLesson(section.id)}
								>
									<Plus class="h-3.5 w-3.5" />
									Add Lesson
								</button>
							</div>
						</div>

						<!-- Lessons List -->
						<div class="space-y-2">
							{#each section.lessons || [] as lesson (lesson.id)}
								<div class="group flex items-center justify-between rounded-xl bg-base-100/40 p-3 text-xs border border-white/5 hover:border-primary/20 transition-colors">
									<div class="flex items-center gap-2.5">
										{#if lesson.type === 'Video'}
											<PlayCircle class="h-4 w-4 text-primary" />
										{:else if lesson.type === 'PdfDocument'}
											<FileText class="h-4 w-4 text-secondary" />
										{:else if lesson.type === 'DownloadableFile'}
											<Download class="h-4 w-4 text-accent" />
										{:else}
											<AlignLeft class="h-4 w-4 text-primary" />
										{/if}
										<div>
											<span class="font-semibold text-base-content block">{lesson.title}</span>
											<span class="text-[10px] text-base-content/50 font-mono">
												{lesson.type || 'Text'} &bull; {lesson.durationMinutes}m
											</span>
										</div>
									</div>

									<div class="flex items-center gap-1 opacity-80 group-hover:opacity-100 transition-opacity">
										<button
											class="btn btn-ghost btn-xs rounded-lg text-base-content/70 hover:text-secondary p-1"
											title="Edit lesson"
											onclick={() => openEditLesson(lesson)}
										>
											<Edit3 class="h-3.5 w-3.5" />
										</button>
										<button
											class="btn btn-ghost btn-xs rounded-lg text-base-content/70 hover:text-error p-1"
											title="Delete lesson"
											onclick={() => openDeleteLesson(lesson)}
										>
											<Trash2 class="h-3.5 w-3.5" />
										</button>
									</div>
								</div>
							{:else}
								<div class="text-center py-4 text-xs text-base-content/50">
									No lessons in this section yet. Click "Add Lesson" to add educational content.
								</div>
							{/each}
						</div>
					</GlassCard>
				{:else}
					<div class="glass-card p-12 text-center rounded-3xl border border-white/5 space-y-3">
						<Layers class="h-8 w-8 text-secondary mx-auto opacity-50" />
						<h3 class="text-base font-bold">Curriculum is empty</h3>
						<p class="text-xs text-base-content/60">Create your first section to organize lessons and modules.</p>
					</div>
				{/each}
			</div>
		{:else}
			<!-- Tab 2: Course Settings & Overview Form -->
			<GlassCard class="p-8 space-y-6">
				<form onsubmit={handleSaveCourseSettings} class="space-y-6">
					<!-- Title -->
					<div class="space-y-2">
						<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block" for="c-title">
							Course Title <span class="text-error">*</span>
						</label>
						<input
							id="c-title"
							type="text"
							class="input input-bordered w-full rounded-2xl h-12 bg-base-100/70 border-base-content/20 text-base-content font-semibold focus:border-primary"
							placeholder="e.g. Distributed Systems & High-Scale Architecture"
							bind:value={editCourseTitle}
							required
						/>
					</div>

					<!-- Access Model Selection -->
					<div class="space-y-3">
						<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block">
							Access Model <span class="text-error">*</span>
						</label>
						<div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
							{#each accessModels as model}
								{@const isSelected = editCourseAccessType === model.id}
								<button
									type="button"
									class="relative flex flex-col items-start p-4 rounded-2xl border text-left transition-all {isSelected
										? 'border-primary bg-primary/10 ring-2 ring-primary/20 shadow-md'
										: 'border-base-content/15 bg-base-100/40 hover:bg-base-100/70 hover:border-base-content/30'}"
									onclick={() => (editCourseAccessType = model.id)}
								>
									<div class="flex items-center justify-between w-full mb-2">
										<span class="badge {model.badgeClass} badge-sm font-semibold">{model.badge}</span>
										{#if isSelected}
											<div class="h-2 w-2 rounded-full bg-primary animate-ping"></div>
										{/if}
									</div>
									<h4 class="font-bold text-sm text-base-content mb-1">{model.title}</h4>
									<p class="text-[11px] text-base-content/65 leading-relaxed">{model.desc}</p>
								</button>
							{/each}
						</div>
					</div>

					<!-- Dynamic Pricing or Passkey input -->
					{#if editCourseAccessType === 'OpenPaid'}
						<div class="space-y-2 animate-in fade-in slide-in-from-top-2 duration-200">
							<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block" for="c-price">
								Price ($ USD) <span class="text-error">*</span>
							</label>
							<div class="relative">
								<DollarSign class="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-base-content/50" />
								<input
									id="c-price"
									type="number"
									step="0.01"
									min="0.01"
									class="input input-bordered w-full pl-10 rounded-2xl h-12 bg-base-100/70 border-base-content/20 text-base-content font-bold focus:border-primary"
									placeholder="29.99"
									bind:value={editCoursePrice}
									required
								/>
							</div>
						</div>
					{:else if editCourseAccessType === 'PrivateWithKey'}
						<div class="space-y-2 animate-in fade-in slide-in-from-top-2 duration-200">
							<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block" for="c-key">
								Change Secret Passkey (Optional - leave blank to keep current)
							</label>
							<div class="relative">
								<Key class="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-base-content/50" />
								<input
									id="c-key"
									type="password"
									class="input input-bordered w-full pl-10 rounded-2xl h-12 bg-base-100/70 border-base-content/20 text-base-content font-mono focus:border-primary"
									placeholder="Enter new passkey..."
									bind:value={editCourseEnrollmentKey}
								/>
							</div>
						</div>
					{/if}

					<!-- Description with Edra Editor -->
					<div class="space-y-2">
						<div class="flex items-center justify-between">
							<label class="text-xs font-bold uppercase tracking-wider text-base-content/80 block">
								Course Description & Overview
							</label>
							<span class="badge badge-neutral badge-xs font-mono text-[10px]">Edra Editor</span>
						</div>
						<RichEditor
							content={editCourseDescription}
							minHeight="220px"
							onUpdate={(json) => {
								editCourseDescription = json;
							}}
						/>
					</div>

					<!-- Save Changes Button -->
					<div class="pt-4 border-t border-white/10 flex justify-end">
						<button
							type="submit"
							class="btn btn-primary gradient-accent rounded-xl text-white font-bold border-0 shadow-lg px-8 gap-2"
							disabled={isSavingSettings}
						>
							{#if isSavingSettings}
								<span class="loading loading-spinner loading-xs"></span>
							{:else}
								<Save class="h-4 w-4" />
							{/if}
							Save Changes
						</button>
					</div>
				</form>
			</GlassCard>
		{/if}
	{/if}

	<!-- Add Section Modal -->
	<GlassModal
		isOpen={isAddSectionModalOpen}
		title="Create Section"
		onClose={() => (isAddSectionModalOpen = false)}
	>
		<div class="space-y-3">
			<label class="text-xs font-semibold text-base-content/80" for="s-title">Section Title</label>
			<input
				id="s-title"
				type="text"
				class="input input-bordered input-sm h-11 w-full rounded-xl text-sm bg-base-100/70 border-base-content/20 text-base-content"
				placeholder="e.g. Module 1: Core Fundamentals"
				bind:value={newSectionTitle}
			/>
		</div>

		{#snippet actions()}
			<button class="btn btn-ghost btn-sm rounded-xl" onclick={() => (isAddSectionModalOpen = false)}>
				Cancel
			</button>
			<button
				class="btn btn-secondary gradient-accent btn-sm rounded-xl text-white font-semibold border-0"
				onclick={handleAddSection}
				disabled={isActionLoading || !newSectionTitle.trim()}
			>
				Save Section
			</button>
		{/snippet}
	</GlassModal>

	<!-- Edit Section Modal -->
	<GlassModal
		isOpen={isEditSectionModalOpen}
		title="Edit Section Title"
		onClose={() => (isEditSectionModalOpen = false)}
	>
		<div class="space-y-3">
			<label class="text-xs font-semibold text-base-content/80" for="edit-s-title">Section Title</label>
			<input
				id="edit-s-title"
				type="text"
				class="input input-bordered input-sm h-11 w-full rounded-xl text-sm bg-base-100/70 border-base-content/20 text-base-content"
				placeholder="Section Title"
				bind:value={editingSectionTitle}
			/>
		</div>

		{#snippet actions()}
			<button class="btn btn-ghost btn-sm rounded-xl" onclick={() => (isEditSectionModalOpen = false)}>
				Cancel
			</button>
			<button
				class="btn btn-secondary gradient-accent btn-sm rounded-xl text-white font-semibold border-0"
				onclick={handleUpdateSection}
				disabled={isActionLoading || !editingSectionTitle.trim()}
			>
				Update Section
			</button>
		{/snippet}
	</GlassModal>

	<!-- Delete Section Confirmation -->
	<ConfirmModal
		isOpen={isDeleteSectionModalOpen}
		title="Delete Curriculum Section"
		message={`Are you sure you want to delete "${deletingSectionTitle}"? All lessons inside this section will also be permanently removed.`}
		confirmText="Delete Section"
		isDanger={true}
		isLoading={isActionLoading}
		onConfirm={handleDeleteSection}
		onCancel={() => (isDeleteSectionModalOpen = false)}
	/>

	<!-- Add Lesson Modal -->
	<GlassModal
		isOpen={isAddLessonModalOpen}
		title="Add Lesson to Section"
		onClose={() => (isAddLessonModalOpen = false)}
		maxWidth="max-w-2xl"
	>
		<div class="space-y-4">
			<div class="space-y-1.5">
				<label class="text-xs font-semibold text-base-content/80" for="l-title">Lesson Title</label>
				<input
					id="l-title"
					type="text"
					class="input input-bordered input-sm h-11 w-full rounded-xl text-sm bg-base-100/70 border-base-content/20 text-base-content"
					placeholder="e.g. Overview of Event Sourcing"
					bind:value={newLessonTitle}
				/>
			</div>

			<!-- Material Type Cards Selection -->
			<div class="space-y-1.5">
				<label class="text-xs font-semibold text-base-content/80">Material Type</label>
				<div class="grid grid-cols-2 sm:grid-cols-4 gap-2">
					{#each materialTypes as item}
						{@const isSelected = newLessonType === item.id}
						<button
							type="button"
							class="flex items-center gap-2 p-2.5 rounded-xl border text-xs font-semibold transition-all {isSelected
								? 'bg-primary/15 border-primary text-primary shadow-xs ring-1 ring-primary/30'
								: 'bg-base-100/50 border-base-content/15 text-base-content/70 hover:bg-base-100 hover:text-base-content hover:border-base-content/30'}"
							onclick={() => (newLessonType = item.id)}
						>
							<item.icon class="h-4 w-4 shrink-0 {isSelected ? 'text-primary' : 'text-base-content/50'}" />
							<span class="truncate">{item.label}</span>
						</button>
					{/each}
				</div>
			</div>

			<div class="space-y-1.5">
				<label class="text-xs font-semibold text-base-content/80" for="l-dur">Estimated Duration (mins)</label>
				<input
					id="l-dur"
					type="number"
					class="input input-bordered input-sm h-11 w-full rounded-xl text-sm bg-base-100/70 border-base-content/20 text-base-content"
					bind:value={newLessonDuration}
				/>
			</div>

			{#if newLessonType === 'Text'}
				<div class="space-y-1.5">
					<div class="flex items-center justify-between">
						<label class="text-xs font-semibold text-base-content/80">Lesson Content (Edra Editor)</label>
						<span class="badge badge-neutral badge-xs font-mono text-[10px]">WYSIWYG</span>
					</div>
					<RichEditor
						content={newLessonTextContent}
						minHeight="180px"
						placeholder="Write rich text content, formulas, or code snippets for this lesson..."
						onUpdate={(json) => {
							newLessonTextContent = json;
						}}
					/>
				</div>
			{/if}

			<div class="space-y-1.5">
				<label class="text-xs font-semibold text-base-content/80" for="l-url">
					{newLessonType === 'Text' ? 'Optional Reference / Storage URL' : 'Storage URL / MinIO Object Path *'}
				</label>
				<input
					id="l-url"
					type="text"
					class="input input-bordered input-sm h-11 w-full rounded-xl text-sm font-mono text-xs bg-base-100/70 border-base-content/20 text-base-content"
					placeholder={newLessonType === 'Text' ? 'https://docs... (Optional)' : 'https://... or minio/course-materials/lesson1.mp4'}
					bind:value={newLessonContentUrl}
				/>
			</div>
		</div>

		{#snippet actions()}
			<button class="btn btn-ghost btn-sm rounded-xl" onclick={() => (isAddLessonModalOpen = false)}>
				Cancel
			</button>
			<button
				class="btn btn-secondary gradient-accent btn-sm rounded-xl text-white font-semibold border-0"
				onclick={handleAddLesson}
				disabled={isActionLoading || !newLessonTitle.trim() || (newLessonType !== 'Text' && !newLessonContentUrl.trim())}
			>
				Create Lesson
			</button>
		{/snippet}
	</GlassModal>

	<!-- Edit Lesson Modal -->
	<GlassModal
		isOpen={isEditLessonModalOpen}
		title="Edit Lesson"
		onClose={() => (isEditLessonModalOpen = false)}
		maxWidth="max-w-2xl"
	>
		<div class="space-y-4">
			<div class="space-y-1.5">
				<label class="text-xs font-semibold text-base-content/80" for="edit-l-title">Lesson Title</label>
				<input
					id="edit-l-title"
					type="text"
					class="input input-bordered input-sm h-11 w-full rounded-xl text-sm bg-base-100/70 border-base-content/20 text-base-content"
					placeholder="Lesson Title"
					bind:value={editingLessonTitle}
				/>
			</div>

			<!-- Material Type Cards Selection -->
			<div class="space-y-1.5">
				<label class="text-xs font-semibold text-base-content/80">Material Type</label>
				<div class="grid grid-cols-2 sm:grid-cols-4 gap-2">
					{#each materialTypes as item}
						{@const isSelected = editingLessonType === item.id}
						<button
							type="button"
							class="flex items-center gap-2 p-2.5 rounded-xl border text-xs font-semibold transition-all {isSelected
								? 'bg-primary/15 border-primary text-primary shadow-xs ring-1 ring-primary/30'
								: 'bg-base-100/50 border-base-content/15 text-base-content/70 hover:bg-base-100 hover:text-base-content hover:border-base-content/30'}"
							onclick={() => (editingLessonType = item.id)}
						>
							<item.icon class="h-4 w-4 shrink-0 {isSelected ? 'text-primary' : 'text-base-content/50'}" />
							<span class="truncate">{item.label}</span>
						</button>
					{/each}
				</div>
			</div>

			<div class="space-y-1.5">
				<label class="text-xs font-semibold text-base-content/80" for="edit-l-dur">Estimated Duration (mins)</label>
				<input
					id="edit-l-dur"
					type="number"
					class="input input-bordered input-sm h-11 w-full rounded-xl text-sm bg-base-100/70 border-base-content/20 text-base-content"
					bind:value={editingLessonDuration}
				/>
			</div>

			{#if editingLessonType === 'Text'}
				<div class="space-y-1.5">
					<div class="flex items-center justify-between">
						<label class="text-xs font-semibold text-base-content/80">Lesson Content (Edra Editor)</label>
						<span class="badge badge-neutral badge-xs font-mono text-[10px]">WYSIWYG</span>
					</div>
					<RichEditor
						content={editingLessonTextContent}
						minHeight="180px"
						placeholder="Write rich text content, formulas, or code snippets for this lesson..."
						onUpdate={(json) => {
							editingLessonTextContent = json;
						}}
					/>
				</div>
			{/if}

			<div class="space-y-1.5">
				<label class="text-xs font-semibold text-base-content/80" for="edit-l-url">
					{editingLessonType === 'Text' ? 'Optional Reference / Storage URL' : 'Storage URL / MinIO Object Path *'}
				</label>
				<input
					id="edit-l-url"
					type="text"
					class="input input-bordered input-sm h-11 w-full rounded-xl text-sm font-mono text-xs bg-base-100/70 border-base-content/20 text-base-content"
					placeholder={editingLessonType === 'Text' ? 'https://docs... (Optional)' : 'https://... or minio/course-materials/lesson1.mp4'}
					bind:value={editingLessonContentUrl}
				/>
			</div>
		</div>

		{#snippet actions()}
			<button class="btn btn-ghost btn-sm rounded-xl" onclick={() => (isEditLessonModalOpen = false)}>
				Cancel
			</button>
			<button
				class="btn btn-secondary gradient-accent btn-sm rounded-xl text-white font-semibold border-0"
				onclick={handleUpdateLesson}
				disabled={isActionLoading || !editingLessonTitle.trim() || (editingLessonType !== 'Text' && !editingLessonContentUrl.trim())}
			>
				Update Lesson
			</button>
		{/snippet}
	</GlassModal>

	<!-- Delete Lesson Confirmation -->
	<ConfirmModal
		isOpen={isDeleteLessonModalOpen}
		title="Delete Lesson"
		message={`Are you sure you want to delete "${deletingLessonTitle}"?`}
		confirmText="Delete Lesson"
		isDanger={true}
		isLoading={isActionLoading}
		onConfirm={handleDeleteLesson}
		onCancel={() => (isDeleteLessonModalOpen = false)}
	/>

	<!-- Delete Course Confirmation -->
	<ConfirmModal
		isOpen={isDeleteCourseModalOpen}
		title="Delete Course"
		message={`Are you sure you want to permanently delete "${course?.title}"? All sections, lessons, assignments, and associated data will be removed.`}
		confirmText="Permanently Delete Course"
		isDanger={true}
		isLoading={isActionLoading}
		onConfirm={handleDeleteCourse}
		onCancel={() => (isDeleteCourseModalOpen = false)}
	/>
</div>
