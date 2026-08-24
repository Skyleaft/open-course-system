<script lang="ts">
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { coursesApi } from '#lib/api/courses.ts';
	import { examsApi } from '#lib/api/exams.ts';
	import type { Course, CourseSection, Lesson, LessonType, CourseExam, QuizExam } from '#lib/api/types.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import GlassModal from '#lib/components/ui/GlassModal.svelte';
	import ConfirmModal from '#lib/components/ui/ConfirmModal.svelte';
	import RichEditor from '#lib/components/editor/RichEditor.svelte';
	import CourseExamAttachment from '#lib/components/course/CourseExamAttachment.svelte';
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
	let allExams = $state<QuizExam[]>([]);
	let isLoading = $state(true);
	let isActionLoading = $state(false);

	// Tabs: 'curriculum' | 'exams' | 'settings'
	let activeTab = $state<'curriculum' | 'exams' | 'settings'>('curriculum');

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
		await loadAllData();
	});

	async function loadAllData() {
		isLoading = true;
		try {
			await Promise.all([loadCourse(), loadExams()]);
		} finally {
			isLoading = false;
		}
	}

	async function loadCourse() {
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
		}
	}

	async function loadExams() {
		try {
			const res = await examsApi.listExams({ pageSize: 100 });
			allExams = res.items || [];
		} catch {
			// continue
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

	async function handleEditSection() {
		if (!editingSectionId || !editingSectionTitle.trim()) return;
		isActionLoading = true;
		try {
			await coursesApi.updateSection(editingSectionId, {
				title: editingSectionTitle.trim()
			});
			toast.success('Section updated successfully.');
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
			toast.success('Section removed.');
			isDeleteSectionModalOpen = false;
			await loadCourse();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to remove section.');
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
		if (!targetSectionId || !newLessonTitle.trim()) return;
		isActionLoading = true;
		try {
			await coursesApi.addLesson(targetSectionId, {
				title: newLessonTitle.trim(),
				type: newLessonType,
				durationMinutes: Number(newLessonDuration),
				contentUrl: newLessonContentUrl.trim() || undefined,
				textContent: newLessonTextContent.trim() || undefined
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
		editingLessonType = lesson.type;
		editingLessonDuration = lesson.durationMinutes || 10;
		editingLessonContentUrl = lesson.contentUrl || '';
		editingLessonTextContent = lesson.textContent || '';
		isEditLessonModalOpen = true;
	}

	async function handleEditLesson() {
		if (!editingLessonId || !editingLessonTitle.trim()) return;
		isActionLoading = true;
		try {
			await coursesApi.updateLesson(editingLessonId, {
				title: editingLessonTitle.trim(),
				type: editingLessonType,
				durationMinutes: Number(editingLessonDuration),
				contentUrl: editingLessonContentUrl.trim() || undefined,
				textContent: editingLessonTextContent.trim() || undefined
			});
			toast.success('Lesson updated successfully.');
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

	// Exam Handlers
	async function handleAttachExam(examId: string, isMandatory: boolean) {
		isActionLoading = true;
		try {
			await coursesApi.attachExam(courseId, examId, { isMandatory });
			toast.success('Exam attached to course curriculum!');
			await loadCourse();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to attach exam.');
		} finally {
			isActionLoading = false;
		}
	}

	async function handleDetachExam(examId: string) {
		isActionLoading = true;
		try {
			await coursesApi.detachExam(courseId, examId);
			toast.success('Exam detached from course.');
			await loadCourse();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to detach exam.');
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

<div class="space-y-8 max-w-6xl mx-auto pb-16">
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
		<div class="h-80 rounded-3xl bg-base-200/50 animate-pulse"></div>
	{:else if course}
		<!-- Course Overview Banner -->
		<GlassCard class="p-6 sm:p-8 space-y-4">
			<div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
				<div class="space-y-1.5">
					<div class="flex items-center gap-2">
						<span class="badge badge-primary badge-xs font-bold uppercase">{course.accessType}</span>
						<span class="badge {course.isPublished ? 'badge-success text-white' : 'badge-warning'} badge-xs font-semibold">
							{course.isPublished ? 'Published' : 'Draft'}
						</span>
					</div>
					<h1 class="text-2xl sm:text-3xl font-extrabold text-base-content tracking-tight">{course.title}</h1>
					{#if course.description}
						<p class="text-xs text-base-content/70 line-clamp-2 max-w-2xl">{course.description}</p>
					{/if}
				</div>

				<div class="flex items-center gap-2">
					<!-- Tabs switch -->
					<div class="flex items-center gap-1 rounded-2xl p-1 bg-base-200/70 border border-base-content/10">
						<button
							class="btn btn-xs rounded-xl font-semibold transition-all gap-1.5 {activeTab === 'curriculum'
								? 'btn-primary text-primary-content shadow-xs'
								: 'btn-ghost text-base-content/70'}"
							onclick={() => (activeTab = 'curriculum')}
						>
							<Layers class="h-3.5 w-3.5" />
							Curriculum
						</button>
						<button
							class="btn btn-xs rounded-xl font-semibold transition-all gap-1.5 {activeTab === 'exams'
								? 'btn-primary text-primary-content shadow-xs'
								: 'btn-ghost text-base-content/70'}"
							onclick={() => (activeTab = 'exams')}
						>
							<FileText class="h-3.5 w-3.5" />
							Examinations ({course.exams?.length || 0})
						</button>
						<button
							class="btn btn-xs rounded-xl font-semibold transition-all gap-1.5 {activeTab === 'settings'
								? 'btn-primary text-primary-content shadow-xs'
								: 'btn-ghost text-base-content/70'}"
							onclick={() => (activeTab = 'settings')}
						>
							<Settings class="h-3.5 w-3.5" />
							Settings
						</button>
					</div>

					{#if activeTab === 'curriculum'}
						<button
							class="btn btn-primary btn-sm rounded-xl text-primary-content font-semibold border-0 shadow-md gap-1.5"
							onclick={() => (isAddSectionModalOpen = true)}
						>
							<Plus class="h-4 w-4" />
							Add Section
						</button>
					{/if}
				</div>
			</div>
		</GlassCard>

		{#if activeTab === 'curriculum'}
			<!-- Tab 1: Sections & Lessons Builder -->
			<div class="space-y-4">
				{#if (course.sections || []).length === 0}
					<div class="py-12 text-center bg-base-200/40 rounded-2xl border border-dashed border-base-300">
						<Layers class="w-10 h-10 text-base-content/30 mx-auto mb-2.5" />
						<p class="text-sm font-semibold text-base-content/80">No curriculum sections yet</p>
						<button
							type="button"
							class="btn btn-sm btn-primary gap-1.5 mt-3"
							onclick={() => (isAddSectionModalOpen = true)}
						>
							<Plus class="w-4 h-4" />
							Add Section
						</button>
					</div>
				{:else}
					{#each course.sections || [] as section, sIdx (section.id || sIdx)}
						<GlassCard class="space-y-4 p-6">
							<!-- Section Header -->
							<div class="flex items-center justify-between border-b border-base-content/10 pb-3">
								<div class="flex items-center gap-2">
									<span class="flex h-6 w-6 items-center justify-center rounded-lg bg-primary text-primary-content text-xs font-bold">
										{sIdx + 1}
									</span>
									<h3 class="text-base font-bold text-base-content">{section.title}</h3>
								</div>

								<div class="flex items-center gap-1">
									<button
										class="btn btn-ghost btn-xs text-base-content/70 hover:text-primary rounded-lg p-1.5"
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
										class="btn btn-primary btn-outline btn-xs rounded-xl gap-1 font-semibold"
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
									<div class="group flex items-center justify-between rounded-xl bg-base-200/50 p-3 text-xs border border-base-content/5 hover:border-primary/20 transition-colors">
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
													{lesson.type} • {lesson.durationMinutes || 0} mins
												</span>
											</div>
										</div>

										<div class="flex items-center gap-1 opacity-80 group-hover:opacity-100 transition-opacity">
											<button
												class="btn btn-ghost btn-xs text-base-content/70 hover:text-primary rounded-lg p-1.5"
												title="Edit Lesson"
												onclick={() => openEditLesson(lesson)}
											>
												<Edit3 class="h-3.5 w-3.5" />
											</button>
											<button
												class="btn btn-ghost btn-xs text-base-content/70 hover:text-error rounded-lg p-1.5"
												title="Delete Lesson"
												onclick={() => openDeleteLesson(lesson)}
											>
												<Trash2 class="h-3.5 w-3.5" />
											</button>
										</div>
									</div>
								{/each}
							</div>
						</GlassCard>
					{/each}
				{/if}
			</div>
		{:else if activeTab === 'exams'}
			<!-- Tab 2: Attached Course Exams Studio -->
			<GlassCard class="p-6">
				<CourseExamAttachment
					courseExams={course.exams || []}
					{allExams}
					onAttachExam={handleAttachExam}
					onDetachExam={handleDetachExam}
					isLoading={isActionLoading}
				/>
			</GlassCard>
		{:else}
			<!-- Tab 3: Course Settings -->
			<GlassCard class="p-6 sm:p-8">
				<form onsubmit={handleSaveCourseSettings} class="space-y-6 max-w-2xl">
					<div>
						<label for="course-title-edit" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
							Course Title <span class="text-error">*</span>
						</label>
						<input
							id="course-title-edit"
							type="text"
							bind:value={editCourseTitle}
							class="input input-bordered w-full bg-base-100/50"
							required
						/>
					</div>

					<div>
						<label for="course-desc-edit" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
							Course Description
						</label>
						<textarea
							id="course-desc-edit"
							bind:value={editCourseDescription}
							rows="3"
							class="textarea textarea-bordered w-full bg-base-100/50"
						></textarea>
					</div>

					<!-- Access Type Model Selection -->
					<div class="space-y-2">
						<span class="label-text text-xs font-bold uppercase tracking-wider text-base-content/80 block">
							Enrollment & Monetization Model
						</span>
						<div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
							{#each accessModels as model}
								<button
									type="button"
									class="p-4 rounded-2xl border text-left flex flex-col justify-between transition-all {editCourseAccessType === model.id ? 'border-primary bg-primary/10 text-primary shadow-sm' : 'border-base-content/10 bg-base-200/50 text-base-content/70'}"
									onclick={() => (editCourseAccessType = model.id)}
								>
									<div class="flex items-center justify-between mb-2">
										<model.icon class="w-5 h-5" />
										<span class="badge badge-xs {model.badgeClass}">{model.badge}</span>
									</div>
									<div>
										<p class="font-bold text-xs text-base-content">{model.title}</p>
										<p class="text-[10px] text-base-content/60 mt-0.5">{model.desc}</p>
									</div>
								</button>
							{/each}
						</div>
					</div>

					{#if editCourseAccessType === 'OpenPaid'}
						<div>
							<label for="course-price-edit" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
								Price (USD) <span class="text-error">*</span>
							</label>
							<input
								id="course-price-edit"
								type="number"
								min="1"
								step="0.01"
								bind:value={editCoursePrice}
								class="input input-bordered w-full bg-base-100/50"
								required
							/>
						</div>
					{:else if editCourseAccessType === 'PrivateWithKey'}
						<div>
							<label for="course-key-edit" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
								Update Enrollment Key (Leave blank to keep existing)
							</label>
							<input
								id="course-key-edit"
								type="password"
								bind:value={editCourseEnrollmentKey}
								placeholder="Enter new secret passphrase..."
								class="input input-bordered w-full bg-base-100/50"
							/>
						</div>
					{/if}

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
							Save Settings
						</button>
					</div>
				</form>
			</GlassCard>
		{/if}
	{/if}
</div>

<!-- Add Section Modal -->
{#if isAddSectionModalOpen}
	<div class="modal modal-open z-50">
		<div class="modal-box bg-base-100/95 backdrop-blur-xl border border-base-content/10 shadow-2xl max-w-md">
			<h3 class="font-bold text-base text-base-content">Add Curriculum Section</h3>
			<form
				onsubmit={(e) => {
					e.preventDefault();
					handleAddSection();
				}}
				class="space-y-4 mt-4"
			>
				<div>
					<label for="add-sec-title" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Section Title <span class="text-error">*</span>
					</label>
					<input
						id="add-sec-title"
						type="text"
						bind:value={newSectionTitle}
						placeholder="e.g. Chapter 1: Introduction"
						class="input input-bordered input-sm w-full bg-base-200/50"
						required
					/>
				</div>
				<div class="modal-action">
					<button type="button" class="btn btn-sm btn-ghost" onclick={() => (isAddSectionModalOpen = false)}>
						Cancel
					</button>
					<button type="submit" class="btn btn-sm btn-primary" disabled={isActionLoading || !newSectionTitle.trim()}>
						Add Section
					</button>
				</div>
			</form>
		</div>
		<div class="modal-backdrop bg-black/40 backdrop-blur-sm" onclick={() => (isAddSectionModalOpen = false)}></div>
	</div>
{/if}

<!-- Edit Section Modal -->
{#if isEditSectionModalOpen}
	<div class="modal modal-open z-50">
		<div class="modal-box bg-base-100/95 backdrop-blur-xl border border-base-content/10 shadow-2xl max-w-md">
			<h3 class="font-bold text-base text-base-content">Edit Section Title</h3>
			<form
				onsubmit={(e) => {
					e.preventDefault();
					handleEditSection();
				}}
				class="space-y-4 mt-4"
			>
				<div>
					<label for="edit-sec-title" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Section Title <span class="text-error">*</span>
					</label>
					<input
						id="edit-sec-title"
						type="text"
						bind:value={editingSectionTitle}
						class="input input-bordered input-sm w-full bg-base-200/50"
						required
					/>
				</div>
				<div class="modal-action">
					<button type="button" class="btn btn-sm btn-ghost" onclick={() => (isEditSectionModalOpen = false)}>
						Cancel
					</button>
					<button type="submit" class="btn btn-sm btn-primary" disabled={isActionLoading || !editingSectionTitle.trim()}>
						Save Changes
					</button>
				</div>
			</form>
		</div>
		<div class="modal-backdrop bg-black/40 backdrop-blur-sm" onclick={() => (isEditSectionModalOpen = false)}></div>
	</div>
{/if}

<!-- Delete Section Modal -->
<ConfirmModal
	isOpen={isDeleteSectionModalOpen}
	title="Remove Section"
	message={`Are you sure you want to delete "${deletingSectionTitle}" and all lessons within it?`}
	confirmText="Delete Section"
	isDanger={true}
	isLoading={isActionLoading}
	onConfirm={handleDeleteSection}
	onCancel={() => (isDeleteSectionModalOpen = false)}
/>

<!-- Add Lesson Modal -->
{#if isAddLessonModalOpen}
	<div class="modal modal-open z-50">
		<div class="modal-box bg-base-100/95 backdrop-blur-xl border border-base-content/10 shadow-2xl max-w-2xl">
			<h3 class="font-bold text-base text-base-content">Add Lesson Material</h3>
			<form
				onsubmit={(e) => {
					e.preventDefault();
					handleAddLesson();
				}}
				class="space-y-4 mt-4"
			>
				<div>
					<label for="add-les-title" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Lesson Title <span class="text-error">*</span>
					</label>
					<input
						id="add-les-title"
						type="text"
						bind:value={newLessonTitle}
						class="input input-bordered input-sm w-full bg-base-200/50"
						required
					/>
				</div>

				<div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
					<div>
						<label for="add-les-type" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Material Type
						</label>
						<select id="add-les-type" bind:value={newLessonType} class="select select-bordered select-sm w-full bg-base-200/50">
							{#each materialTypes as mt}
								<option value={mt.id}>{mt.label}</option>
							{/each}
						</select>
					</div>

					<div>
						<label for="add-les-dur" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Estimated Duration (Minutes)
						</label>
						<input
							id="add-les-dur"
							type="number"
							min="1"
							bind:value={newLessonDuration}
							class="input input-bordered input-sm w-full bg-base-200/50"
						/>
					</div>
				</div>

				{#if newLessonType === 'Video' || newLessonType === 'PdfDocument' || newLessonType === 'DownloadableFile'}
					<div>
						<label for="add-les-url" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Media / Resource URL
						</label>
						<input
							id="add-les-url"
							type="url"
							bind:value={newLessonContentUrl}
							placeholder="https://..."
							class="input input-bordered input-sm w-full bg-base-200/50"
						/>
					</div>
				{/if}

				<div>
					<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Article / Lecture Content
					</label>
					<RichEditor
						bind:content={newLessonTextContent}
						placeholder="Write lesson notes or content..."
					/>
				</div>

				<div class="modal-action">
					<button type="button" class="btn btn-sm btn-ghost" onclick={() => (isAddLessonModalOpen = false)}>
						Cancel
					</button>
					<button type="submit" class="btn btn-sm btn-primary" disabled={isActionLoading || !newLessonTitle.trim()}>
						Create Lesson
					</button>
				</div>
			</form>
		</div>
		<div class="modal-backdrop bg-black/40 backdrop-blur-sm" onclick={() => (isAddLessonModalOpen = false)}></div>
	</div>
{/if}

<!-- Edit Lesson Modal -->
{#if isEditLessonModalOpen}
	<div class="modal modal-open z-50">
		<div class="modal-box bg-base-100/95 backdrop-blur-xl border border-base-content/10 shadow-2xl max-w-2xl">
			<h3 class="font-bold text-base text-base-content">Edit Lesson Material</h3>
			<form
				onsubmit={(e) => {
					e.preventDefault();
					handleEditLesson();
				}}
				class="space-y-4 mt-4"
			>
				<div>
					<label for="edit-les-title" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Lesson Title <span class="text-error">*</span>
					</label>
					<input
						id="edit-les-title"
						type="text"
						bind:value={editingLessonTitle}
						class="input input-bordered input-sm w-full bg-base-200/50"
						required
					/>
				</div>

				<div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
					<div>
						<label for="edit-les-type" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Material Type
						</label>
						<select id="edit-les-type" bind:value={editingLessonType} class="select select-bordered select-sm w-full bg-base-200/50">
							{#each materialTypes as mt}
								<option value={mt.id}>{mt.label}</option>
							{/each}
						</select>
					</div>

					<div>
						<label for="edit-les-dur" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Estimated Duration (Minutes)
						</label>
						<input
							id="edit-les-dur"
							type="number"
							min="1"
							bind:value={editingLessonDuration}
							class="input input-bordered input-sm w-full bg-base-200/50"
						/>
					</div>
				</div>

				{#if editingLessonType === 'Video' || editingLessonType === 'PdfDocument' || editingLessonType === 'DownloadableFile'}
					<div>
						<label for="edit-les-url" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Media / Resource URL
						</label>
						<input
							id="edit-les-url"
							type="url"
							bind:value={editingLessonContentUrl}
							class="input input-bordered input-sm w-full bg-base-200/50"
						/>
					</div>
				{/if}

				<div>
					<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Article / Lecture Content
					</label>
					<RichEditor
						bind:content={editingLessonTextContent}
						placeholder="Write lesson notes or content..."
					/>
				</div>

				<div class="modal-action">
					<button type="button" class="btn btn-sm btn-ghost" onclick={() => (isEditLessonModalOpen = false)}>
						Cancel
					</button>
					<button type="submit" class="btn btn-sm btn-primary" disabled={isActionLoading || !editingLessonTitle.trim()}>
						Save Changes
					</button>
				</div>
			</form>
		</div>
		<div class="modal-backdrop bg-black/40 backdrop-blur-sm" onclick={() => (isEditLessonModalOpen = false)}></div>
	</div>
{/if}

<!-- Delete Lesson Modal -->
<ConfirmModal
	isOpen={isDeleteLessonModalOpen}
	title="Remove Lesson"
	message={`Are you sure you want to remove "${deletingLessonTitle}"?`}
	confirmText="Delete Lesson"
	isDanger={true}
	isLoading={isActionLoading}
	onConfirm={handleDeleteLesson}
	onCancel={() => (isDeleteLessonModalOpen = false)}
/>

<!-- Delete Course Modal -->
<ConfirmModal
	isOpen={isDeleteCourseModalOpen}
	title="Delete Course"
	message="Are you sure you want to permanently delete this course and all associated enrollments?"
	confirmText="Delete Course"
	isDanger={true}
	isLoading={isActionLoading}
	onConfirm={handleDeleteCourse}
	onCancel={() => (isDeleteCourseModalOpen = false)}
/>
