<script lang="ts">
	import { onMount } from 'svelte';
	import { fade, scale } from 'svelte/transition';
	import { cubicOut } from 'svelte/easing';
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { coursesApi } from '$lib/api/courses.ts';
	import { examsApi } from '$lib/api/exams.ts';
	import type {
		Course,
		CourseSection,
		Lesson,
		LessonType,
		CourseExam,
		QuizExam,
		CourseStudentEnrollmentDto,
		CourseStudentExamProgressDto
	} from '$lib/api/types.ts';
	import GlassCard from '$lib/components/ui/GlassCard.svelte';
	import SegmentedTabs from '$lib/components/ui/SegmentedTabs.svelte';
	import ConfirmModal from '$lib/components/ui/ConfirmModal.svelte';
	import RichEditor from '$lib/components/editor/RichEditor.svelte';
	import RichRenderer from '$lib/components/editor/RichRenderer.svelte';
	import CourseExamAttachment from '$lib/components/course/CourseExamAttachment.svelte';
	import EssayGradingModal from '$lib/components/course/EssayGradingModal.svelte';
	import StudentProgressModal from '$lib/components/course/StudentProgressModal.svelte';
	import { toast } from '$lib/stores/toast.svelte.ts';
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
		BookOpen,
		EyeOff,
		X,
		Users,
		UserPlus,
		UserMinus,
		Search,
		GraduationCap,
		RefreshCw,
		Eye,
		ShieldAlert,
		RotateCcw,
		Award,
		Zap,
		Sparkles
	} from 'lucide-svelte';

	const courseId = (page.params.id || '') as string;
	let course = $state<Course | null>(null);
	let allExams = $state<QuizExam[]>([]);
	let isLoading = $state(true);
	let isActionLoading = $state(false);

	// Tabs: 'curriculum' | 'students' | 'exams' | 'settings'
	let activeTab = $state<'curriculum' | 'students' | 'exams' | 'settings'>('curriculum');

	// Students Enrollment Management State
	let enrolledStudents = $state<CourseStudentEnrollmentDto[]>([]);
	let totalEnrollments = $state(0);
	let isEnrollmentsLoading = $state(false);
	let studentSearchTerm = $state('');
	let studentPageIndex = $state(1);
	let isEnrollModalOpen = $state(false);
	let newStudentEmail = $state('');
	let isEnrollingStudent = $state(false);
	let isUnenrollModalOpen = $state(false);
	let removingEnrollment = $state<CourseStudentEnrollmentDto | null>(null);
	let isRemovingEnrollment = $state(false);

	// Student Detail & Retake Modals
	let selectedStudentForDetail = $state<CourseStudentEnrollmentDto | null>(null);
	let isStudentDetailModalOpen = $state(false);
	let isRetakeModalOpen = $state(false);
	let retakeTarget = $state<{
		examId: string;
		examTitle: string;
		studentId: string;
		studentName: string;
	} | null>(null);
	let retakeReason = $state('');
	let isGrantingRetake = $state(false);

	// Essay Grading Modal State
	let isGradingModalOpen = $state(false);
	let gradingSubmissionId = $state<string | null>(null);
	let gradingStudentName = $state('');
	let gradingStudentEmail = $state('');
	let gradingStudentId = $state('');

	function openEssayGrading(student: CourseStudentEnrollmentDto, exam: CourseStudentExamProgressDto) {
		gradingSubmissionId = exam.submissionId || null;
		gradingStudentName = student.fullName;
		gradingStudentEmail = student.email;
		gradingStudentId = student.userId;
		isGradingModalOpen = true;
	}

	async function handleEssayGraded() {
		await loadEnrollments();
		if (selectedStudentForDetail) {
			const updatedStudent = enrolledStudents.find(s => s.userId === selectedStudentForDetail?.userId);
			if (updatedStudent) {
				selectedStudentForDetail = updatedStudent;
			}
		}
	}

	function openStudentDetail(student: CourseStudentEnrollmentDto) {
		selectedStudentForDetail = student;
		isStudentDetailModalOpen = true;
	}

	function openRetakeModal(student: CourseStudentEnrollmentDto, exam: CourseStudentExamProgressDto) {
		retakeTarget = {
			examId: exam.examId,
			examTitle: exam.examTitle,
			studentId: student.userId,
			studentName: student.fullName
		};
		retakeReason = '';
		isRetakeModalOpen = true;
	}

	async function handleGrantRetake() {
		if (!retakeTarget) return;
		isGrantingRetake = true;
		try {
			await examsApi.grantRetake(retakeTarget.examId, retakeTarget.studentId, retakeReason.trim() || undefined);
			toast.success(`Retake permission granted for ${retakeTarget.studentName} on ${retakeTarget.examTitle}!`);
			isRetakeModalOpen = false;
			retakeTarget = null;
			await loadEnrollments();
			if (selectedStudentForDetail) {
				const updatedStudent = enrolledStudents.find(s => s.userId === selectedStudentForDetail?.userId);
				if (updatedStudent) {
					selectedStudentForDetail = updatedStudent;
				}
			}
		} catch (err: any) {
			toast.error(err?.message || 'Failed to grant retake.');
		} finally {
			isGrantingRetake = false;
		}
	}

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

	// Publish / Unpublish / Delete Modals
	let isUnpublishModalOpen = $state(false);
	let isDeleteCourseModalOpen = $state(false);

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
			await Promise.all([loadCourse(), loadExams(), loadEnrollments()]);
		} finally {
			isLoading = false;
		}
	}

	async function loadEnrollments() {
		isEnrollmentsLoading = true;
		try {
			const res = await coursesApi.getCourseEnrollments(courseId, {
				pageIndex: studentPageIndex,
				search: studentSearchTerm.trim() || undefined
			});
			enrolledStudents = res.items || [];
			totalEnrollments = res.totalCount || 0;
		} catch (err: any) {
			console.error('Failed to load course enrollments:', err);
			enrolledStudents = [];
			totalEnrollments = 0;
		} finally {
			isEnrollmentsLoading = false;
		}
	}

	async function handleAdminEnrollStudent() {
		if (!newStudentEmail.trim()) {
			toast.warning('Please provide a valid email address or student identifier.');
			return;
		}

		isEnrollingStudent = true;
		try {
			const res = await coursesApi.adminEnrollStudent(courseId, {
				email: newStudentEmail.trim()
			});
			toast.success(`Successfully enrolled ${res.studentName} (${res.studentEmail})!`);
			isEnrollModalOpen = false;
			newStudentEmail = '';
			await loadEnrollments();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to enroll student.');
		} finally {
			isEnrollingStudent = false;
		}
	}

	function openUnenrollModal(student: CourseStudentEnrollmentDto) {
		removingEnrollment = student;
		isUnenrollModalOpen = true;
	}

	async function handleAdminRemoveEnrollment() {
		if (!removingEnrollment) return;
		isRemovingEnrollment = true;
		try {
			await coursesApi.adminRemoveEnrollment(courseId, removingEnrollment.enrollmentId);
			toast.success(`Removed ${removingEnrollment.fullName} from course.`);
			isUnenrollModalOpen = false;
			removingEnrollment = null;
			await loadEnrollments();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to un-enroll student.');
		} finally {
			isRemovingEnrollment = false;
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
	async function handleSaveCourseSettings(e?: Event) {
		if (e) e.preventDefault();
		if (!editCourseTitle.trim()) {
			toast.warning('Please provide a course title.');
			return;
		}

		if (editCourseAccessType === 'OpenPaid' && Number(editCoursePrice) <= 0) {
			toast.warning('Please enter a valid course price greater than $0.');
			return;
		}

		if (editCourseAccessType === 'PrivateWithKey' && !editCourseEnrollmentKey.trim() && !course?.id) {
			toast.warning('Please enter a secret enrollment key.');
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

	async function handleUnpublish() {
		isActionLoading = true;
		try {
			await coursesApi.unpublishCourse(courseId);
			toast.success('Course unpublished. Reverted to Draft status.');
			if (course) course.isPublished = false;
		} catch (err: any) {
			toast.error(err?.message || 'Failed to unpublish course.');
		} finally {
			isActionLoading = false;
			isUnpublishModalOpen = false;
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

<div class="space-y-6 max-w-6xl mx-auto pb-16">
	<!-- Header Navigation & Actions -->
	<div class="flex flex-wrap items-center justify-between gap-3">
		<a
			href="/instructor/courses"
			class="btn btn-sm btn-ghost gap-2 text-base-content/70 hover:text-base-content"
		>
			<ArrowLeft class="w-4 h-4" />
			<span>Back to Courses</span>
		</a>

		<div class="flex items-center gap-2">
			<button
				type="button"
				class="btn btn-primary btn-sm gap-1.5 shadow-md shadow-primary/20"
				onclick={() => handleSaveCourseSettings()}
				disabled={isSavingSettings}
			>
				{#if isSavingSettings}
					<span class="loading loading-spinner loading-xs"></span>
				{:else}
					<Save class="w-3.5 h-3.5" />
				{/if}
				Save Changes
			</button>

			{#if course}
				{#if course.isPublished}
					<button
						type="button"
						class="btn btn-warning btn-outline btn-sm gap-1.5"
						onclick={() => (isUnpublishModalOpen = true)}
						disabled={isActionLoading}
					>
						<EyeOff class="w-3.5 h-3.5" />
						Unpublish Course
					</button>
				{:else}
					<button
						type="button"
						class="btn btn-success btn-sm text-white font-bold shadow-md gap-1.5"
						onclick={handlePublish}
						disabled={isActionLoading}
					>
						<Send class="w-3.5 h-3.5" />
						Publish Course
					</button>
				{/if}
			{/if}

			<button
				type="button"
				class="btn btn-error btn-outline btn-sm gap-1.5"
				onclick={() => (isDeleteCourseModalOpen = true)}
				disabled={isActionLoading}
			>
				<Trash2 class="w-3.5 h-3.5" />
				Delete Course
			</button>
		</div>
	</div>

	{#if isLoading}
		<div class="h-64 rounded-3xl bg-base-200/50 animate-pulse flex items-center justify-center">
			<span class="loading loading-spinner loading-lg text-primary"></span>
		</div>
	{:else if course}
		<!-- Course Overview Banner -->
		<GlassCard class="p-6 sm:p-7 space-y-4">
			<div class="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4">
				<div class="space-y-2 flex-1 min-w-0">
					<div class="flex items-center gap-2 flex-wrap">
						<span class="badge badge-primary badge-sm font-bold uppercase text-[10px]">{course.accessType}</span>
						<span class="badge {course.isPublished ? 'badge-success text-white' : 'badge-warning'} badge-sm font-semibold text-[10px]">
							{course.isPublished ? 'Published' : 'Draft'}
						</span>
						<span class="badge badge-sm badge-outline text-[10px]">
							{course.sections?.length || 0} Sections • {course.exams?.length || 0} Exams
						</span>
					</div>

					<h1 class="text-2xl sm:text-3xl font-extrabold text-base-content tracking-tight">{course.title}</h1>

					{#if course.description}
						<div class="text-xs text-base-content/70 line-clamp-3 max-w-3xl pt-0.5">
							<RichRenderer content={course.description} />
						</div>
					{/if}
				</div>

				<div class="flex items-center gap-2 flex-shrink-0">
					<SegmentedTabs
						tabs={[
							{ id: 'curriculum', label: 'Curriculum', icon: Layers, count: course.sections?.length || 0 },
							{ id: 'students', label: 'Students', icon: Users, count: totalEnrollments },
							{ id: 'exams', label: 'Exams', icon: FileText, count: course.exams?.length || 0 },
							{ id: 'settings', label: 'Settings', icon: Settings }
						]}
						bind:active={activeTab}
						onChange={(tabId) => {
							if (tabId === 'students') loadEnrollments();
						}}
					/>
				</div>
			</div>
		</GlassCard>

		{#if activeTab === 'curriculum'}
			<!-- Tab 1: Curriculum & Lessons Studio -->
			<GlassCard class="p-6 space-y-4">
				<div class="flex items-center justify-between">
					<div>
						<h3 class="text-base font-bold text-base-content flex items-center gap-2">
							<Layers class="w-5 h-5 text-primary" />
							Curriculum Sections & Lesson Materials
						</h3>
						<p class="text-xs text-base-content/70">
							Structure learning modules into ordered sections containing text lessons, video streams, or PDF resources.
						</p>
					</div>

					<button
						type="button"
						class="btn btn-sm btn-primary gap-1.5 shadow-sm"
						onclick={() => (isAddSectionModalOpen = true)}
					>
						<Plus class="w-4 h-4" />
						Add Section
					</button>
				</div>

				{#if !course.sections || course.sections.length === 0}
					<div class="py-12 text-center bg-base-200/40 rounded-2xl border border-dashed border-base-300">
						<Layers class="w-10 h-10 text-base-content/30 mx-auto mb-2.5" />
						<p class="text-sm font-semibold text-base-content/80">No curriculum sections yet</p>
						<p class="text-xs text-base-content/50 max-w-sm mx-auto mt-1">
							Add sections to organize lessons, downloadable resources, and video lectures.
						</p>
						<button
							type="button"
							class="btn btn-sm btn-primary gap-1.5 mt-4"
							onclick={() => (isAddSectionModalOpen = true)}
						>
							<Plus class="w-4 h-4" />
							Add First Section
						</button>
					</div>
				{:else}
					<div class="space-y-4">
						{#each course.sections as section, sIdx (section.id || sIdx)}
							<div class="bg-base-200/50 rounded-2xl border border-base-content/10 overflow-hidden">
								<!-- Section Header -->
								<div class="p-4 bg-base-100/60 flex items-center justify-between gap-3 border-b border-base-content/5 flex-wrap">
									<div class="flex items-center gap-3 min-w-0">
										<span class="w-7 h-7 rounded-lg bg-primary/10 text-primary font-mono font-bold text-xs flex items-center justify-center flex-shrink-0">
											{sIdx + 1}
										</span>
										<span class="font-bold text-sm text-base-content">{section.title}</span>
										<span class="badge badge-sm badge-ghost text-[10px]">
											{section.lessons?.length || 0} lessons
										</span>
									</div>

									<div class="flex items-center gap-1.5 ml-auto">
										<button
											type="button"
											class="btn btn-xs btn-primary btn-outline gap-1 text-xs"
											onclick={() => openAddLesson(section.id)}
										>
											<Plus class="w-3.5 h-3.5" />
											Add Lesson
										</button>

										<button
											type="button"
											class="btn btn-xs btn-ghost btn-square"
											onclick={() => openEditSection(section)}
											title="Edit Section Title"
										>
											<Edit3 class="w-3.5 h-3.5 text-base-content/70" />
										</button>

										<button
											type="button"
											class="btn btn-xs btn-ghost btn-square text-error hover:bg-error/10"
											onclick={() => openDeleteSection(section)}
											title="Remove Section"
										>
											<Trash2 class="w-3.5 h-3.5" />
										</button>
									</div>
								</div>

								<!-- Lessons List -->
								<div class="p-3 divide-y divide-base-content/5 space-y-1">
									{#if !section.lessons || section.lessons.length === 0}
										<div class="py-4 text-center text-xs text-base-content/50 italic">
											No lessons in this section yet. Click "Add Lesson" to compose one.
										</div>
									{:else}
										{#each section.lessons as lesson (lesson.id)}
											<div class="p-2.5 rounded-xl hover:bg-base-100/60 flex items-center justify-between gap-3 transition-colors flex-wrap">
												<div class="flex items-center gap-2.5 min-w-0">
													<div class="w-7 h-7 rounded-lg bg-base-300/60 flex items-center justify-center text-base-content/70 flex-shrink-0">
														{#if lesson.type === 'Video'}
															<PlayCircle class="w-4 h-4 text-primary" />
														{:else if lesson.type === 'PdfDocument'}
															<FileText class="w-4 h-4 text-secondary" />
														{:else if lesson.type === 'DownloadableFile'}
															<Download class="w-4 h-4 text-accent" />
														{:else}
															<AlignLeft class="w-4 h-4 text-base-content/70" />
														{/if}
													</div>

													<div class="min-w-0">
														<p class="font-semibold text-xs text-base-content truncate">{lesson.title}</p>
														<div class="flex items-center gap-2 text-[10px] text-base-content/50 mt-0.5">
															<span class="badge badge-xs badge-outline font-mono text-[9px]">{lesson.type}</span>
															{#if lesson.durationMinutes > 0}
																<span>•</span>
																<span>{lesson.durationMinutes} mins</span>
															{/if}
														</div>
													</div>
												</div>

												<div class="flex items-center gap-1 ml-auto">
													<button
														type="button"
														class="btn btn-xs btn-ghost btn-square"
														onclick={() => openEditLesson(lesson)}
														title="Edit Lesson Content"
													>
														<Edit3 class="w-3.5 h-3.5 text-base-content/70" />
													</button>
													<button
														type="button"
														class="btn btn-xs btn-ghost btn-square text-error hover:bg-error/10"
														onclick={() => openDeleteLesson(lesson)}
														title="Delete Lesson"
													>
														<Trash2 class="w-3.5 h-3.5" />
													</button>
												</div>
											</div>
										{/each}
									{/if}
								</div>
							</div>
						{/each}
					</div>
				{/if}
			</GlassCard>
		{:else if activeTab === 'students'}
			<!-- Tab: Enrolled Students Management -->
			<GlassCard class="p-6 space-y-6">
				<div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
					<div>
						<h3 class="text-base font-bold text-base-content flex items-center gap-2">
							<Users class="w-5 h-5 text-primary" />
							Enrolled Students & Progression ({totalEnrollments})
						</h3>
						<p class="text-xs text-base-content/70">
							Track student completion rates, inspect lesson milestones, and manage enrollment roster.
						</p>
					</div>

					<div class="flex items-center gap-3">
						<div class="relative w-full sm:w-64">
							<Search class="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-base-content/50" />
							<input
								type="text"
								placeholder="Search student by name or email..."
								bind:value={studentSearchTerm}
								oninput={() => {
									studentPageIndex = 1;
									loadEnrollments();
								}}
								class="input input-sm w-full rounded-xl bg-base-200/50 pl-8 text-xs border-white/10"
							/>
						</div>

						<button
							type="button"
							class="btn btn-sm btn-primary gap-1.5 shadow-sm rounded-xl font-bold shrink-0"
							onclick={() => (isEnrollModalOpen = true)}
						>
							<UserPlus class="w-4 h-4" />
							Enroll Student
						</button>
					</div>
				</div>

				{#if isEnrollmentsLoading}
					<div class="space-y-3">
						{#each Array(3) as _}
							<div class="h-16 rounded-2xl bg-base-200/50 animate-pulse"></div>
						{/each}
					</div>
				{:else if enrolledStudents.length === 0}
					<div class="py-12 text-center bg-base-200/40 rounded-2xl border border-dashed border-base-300 space-y-3">
						<Users class="w-10 h-10 text-base-content/30 mx-auto" />
						<div class="space-y-1">
							<p class="text-sm font-semibold text-base-content/80">
								{studentSearchTerm ? 'No students match your search' : 'No students enrolled yet'}
							</p>
							<p class="text-xs text-base-content/50 max-w-sm mx-auto">
								{studentSearchTerm
									? 'Try adjusting your search criteria.'
									: 'Enrolled students will appear here with real-time course progression details.'}
							</p>
						</div>
						<button
							type="button"
							class="btn btn-sm btn-primary gap-1.5 mt-2"
							onclick={() => (isEnrollModalOpen = true)}
						>
							<UserPlus class="w-4 h-4" />
							Enroll First Student
						</button>
					</div>
				{:else}
					<div class="overflow-x-auto rounded-2xl border border-base-content/10">
						<table class="table table-sm w-full text-xs">
							<thead class="bg-base-200/70 text-base-content/70">
								<tr>
									<th>Student</th>
									<th>Enrolled Date</th>
									<th>Course Progression</th>
									<th>Lessons</th>
									<th>Assignments</th>
									<th>Examinations & Status</th>
									<th>Last Active</th>
									<th class="text-right">Actions</th>
								</tr>
							</thead>
							<tbody class="divide-y divide-base-content/5">
								{#each enrolledStudents as student (student.enrollmentId)}
									{@const isFinished = student.progressPercent >= 100}
									<tr class="hover:bg-base-100/40 transition-colors">
										<td>
											<button
												type="button"
												class="flex items-center gap-3 text-left group"
												onclick={() => openStudentDetail(student)}
											>
												<div class="avatar placeholder">
													<div class="w-8 h-8 rounded-xl bg-primary/10 text-primary font-bold text-xs flex items-center justify-center group-hover:bg-primary group-hover:text-white transition-colors">
														{student.fullName ? student.fullName.substring(0, 2).toUpperCase() : 'ST'}
													</div>
												</div>
												<div>
													<div class="font-bold text-base-content group-hover:text-primary transition-colors flex items-center gap-1">
														<span>{student.fullName}</span>
														<Eye class="w-3 h-3 opacity-0 group-hover:opacity-100 transition-opacity" />
													</div>
													<div class="text-[10px] text-base-content/50">{student.email}</div>
												</div>
											</button>
										</td>
										<td class="text-base-content/70">
											{new Date(student.enrolledAtUtc).toLocaleDateString()}
										</td>
										<td>
											<div class="space-y-1 w-28">
												<div class="flex items-center justify-between text-[10px] font-bold">
													<span class="{isFinished ? 'text-success' : 'text-primary'}">
														{student.progressPercent}%
													</span>
												</div>
												<div class="h-1.5 w-full overflow-hidden rounded-full bg-base-200">
													<div
														class="h-full transition-all duration-300 {isFinished ? 'bg-success' : 'gradient-accent'}"
														style="width: {student.progressPercent}%"
													></div>
												</div>
											</div>
										</td>
										<td>
											<span class="badge badge-ghost badge-xs font-semibold">
												{student.completedLessonsCount} / {student.totalLessonsCount}
											</span>
										</td>
										<td>
											<span class="badge badge-ghost badge-xs font-semibold">
												{student.completedAssignmentsCount} / {student.totalAssignmentsCount}
											</span>
										</td>
										<td>
											{#if !student.exams || student.exams.length === 0}
												<span class="text-base-content/40 text-[11px] italic">No exams</span>
											{:else}
												<div class="space-y-1.5 max-w-xs">
													{#each student.exams as exam}
														<div class="flex items-center justify-between gap-1.5 bg-base-200/50 p-1 rounded-lg border border-white/5">
															<span class="font-medium text-[11px] truncate max-w-[110px]" title={exam.examTitle}>
																{exam.examTitle}
															</span>

															<div class="flex items-center gap-1 shrink-0">
																{#if exam.status === 'Completed'}
																	<span class="badge badge-success text-white badge-xs font-bold">
																		{exam.score !== null && exam.score !== undefined ? `${exam.score}%` : 'Done'}
																	</span>
																	<button
																		type="button"
																		class="btn btn-ghost btn-xs h-5 px-1 text-primary hover:bg-primary/10 rounded"
																		onclick={() => openEssayGrading(student, exam)}
																		title="Review answers & grade essay questions"
																	>
																		<FileText class="w-3 h-3" />
																	</button>
																	<button
																		type="button"
																		class="btn btn-ghost btn-xs h-5 px-1 text-primary hover:bg-primary/10 rounded"
																		onclick={() => openRetakeModal(student, exam)}
																		title="Grant exam retake"
																	>
																		<RotateCcw class="w-3 h-3" />
																	</button>
																{:else if exam.status === 'Disqualified'}
																	<span class="badge badge-error text-white badge-xs font-bold">
																		Disqualified
																	</span>
																	<button
																		type="button"
																		class="btn btn-primary btn-xs h-5 px-1.5 text-white font-bold rounded shadow-xs"
																		onclick={() => openRetakeModal(student, exam)}
																		title="Allow student to retake"
																	>
																		<RotateCcw class="w-3 h-3 mr-0.5" />
																		Retake
																	</button>
																{:else if exam.status === 'TimedOut'}
																	<span class="badge badge-warning badge-xs font-bold">
																		Timed Out
																	</span>
																	<button
																		type="button"
																		class="btn btn-primary btn-xs h-5 px-1.5 text-white font-bold rounded shadow-xs"
																		onclick={() => openRetakeModal(student, exam)}
																		title="Allow student to retake"
																	>
																		<RotateCcw class="w-3 h-3 mr-0.5" />
																		Retake
																	</button>
																{:else if exam.status === 'InProgress'}
																	<span class="badge badge-info text-white badge-xs font-semibold animate-pulse">
																		In Progress
																	</span>
																{:else}
																	<span class="badge badge-ghost badge-xs text-base-content/50">
																		Not Started
																	</span>
																{/if}
															</div>
														</div>
													{/each}
												</div>
											{/if}
										</td>
										<td class="text-base-content/60 text-[11px]">
											{student.lastAccessedAtUtc
												? new Date(student.lastAccessedAtUtc).toLocaleString()
												: 'Never'}
										</td>
										<td class="text-right">
											<div class="flex items-center justify-end gap-1">
												<button
													type="button"
													class="btn btn-ghost btn-xs text-primary hover:bg-primary/10 rounded-lg gap-1"
													onclick={() => openStudentDetail(student)}
													title="Inspect Student Progress"
												>
													<Eye class="w-3.5 h-3.5" />
													Inspect
												</button>

												<button
													type="button"
													class="btn btn-ghost btn-xs text-error hover:bg-error/10 rounded-lg gap-1"
													onclick={() => openUnenrollModal(student)}
													title="Unenroll student"
												>
													<UserMinus class="w-3.5 h-3.5" />
												</button>
											</div>
										</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				{/if}
			</GlassCard>
		{:else if activeTab === 'exams'}
			<!-- Tab 2: Reusable Exams Attachment -->
			<div class="rounded-3xl bg-base-100/60 border border-base-content/10 p-6 shadow-xl space-y-4">
				<CourseExamAttachment
					courseExams={course.exams || []}
					{allExams}
					onAttachExam={handleAttachExam}
					onDetachExam={handleDetachExam}
					isLoading={isActionLoading}
				/>
			</div>
		{:else}
			<!-- Tab 3: Course Settings Studio -->
			<GlassCard class="p-6 sm:p-8">
				<form onsubmit={handleSaveCourseSettings} class="space-y-6 max-w-3xl">
					<div>
						<label for="edit-c-title" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80">
							Course Title <span class="text-error">*</span>
						</label>
						<input
							id="edit-c-title"
							type="text"
							bind:value={editCourseTitle}
							class="input input-bordered w-full bg-base-100/50 font-semibold"
							required
						/>
					</div>

					<div class="space-y-1.5">
						<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80 block">
							Course Description / Syllabus Overview
						</label>
						<RichEditor
							bind:content={editCourseDescription}
							placeholder="Provide a detailed syllabus overview, learning objectives, and prerequisites..."
						/>
					</div>

					<!-- Access Model Selector Cards -->
					<div class="space-y-3">
						<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80 block">
							Access Model
						</label>
						<div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
							{#each accessModels as model}
								<button
									type="button"
									class="text-left rounded-2xl p-4 transition-all duration-200 flex flex-col justify-between border cursor-pointer {editCourseAccessType === model.id
										? 'bg-primary/10 border-primary shadow-md ring-1 ring-primary/30'
										: 'bg-base-100/40 border-base-content/10 hover:border-base-content/30 hover:bg-base-100/70'}"
									onclick={() => (editCourseAccessType = model.id)}
								>
									<div class="space-y-2">
										<div class="flex items-center justify-between">
											<div class="w-8 h-8 rounded-xl flex items-center justify-center {editCourseAccessType === model.id ? 'bg-primary text-primary-content' : 'bg-base-200 text-base-content/70'}">
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

					{#if editCourseAccessType === 'OpenPaid'}
						<div class="p-4 rounded-2xl bg-base-200/50 border border-base-content/10 space-y-2">
							<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80 block" for="edit-c-price">
								Course Price ($ USD) <span class="text-error">*</span>
							</label>
							<div class="relative">
								<span class="absolute left-4 top-1/2 -translate-y-1/2 text-sm font-bold text-base-content/50">$</span>
								<input
									id="edit-c-price"
									type="number"
									step="0.01"
									min="0.01"
									class="input input-bordered w-full bg-base-100/70 pl-8 pr-4 font-mono font-semibold"
									bind:value={editCoursePrice}
									required
								/>
							</div>
						</div>
					{:else if editCourseAccessType === 'PrivateWithKey'}
						<div class="p-4 rounded-2xl bg-base-200/50 border border-base-content/10 space-y-2">
							<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80 block" for="edit-c-key">
								Update Secret Enrollment Passkey (Leave blank to keep existing)
							</label>
							<div class="relative">
								<span class="absolute left-3.5 top-1/2 -translate-y-1/2 text-base-content/50">
									<Lock class="w-4 h-4" />
								</span>
								<input
									id="edit-c-key"
									type="password"
									placeholder="New secret passphrase (optional)..."
									class="input input-bordered w-full bg-base-100/70 pl-10 pr-4 font-mono"
									bind:value={editCourseEnrollmentKey}
								/>
							</div>
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
							Save Parameters
						</button>
					</div>
				</form>
			</GlassCard>
		{/if}
	{/if}
</div>

<!-- Add Section Modal -->
{#if isAddSectionModalOpen}
	<div
		class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
		role="dialog"
		aria-modal="true"
		transition:fade={{ duration: 180 }}
	>
		<div class="fixed inset-0" onclick={() => (isAddSectionModalOpen = false)} role="presentation"></div>
		<div
			class="relative z-10 w-full max-w-md overflow-hidden rounded-2xl border border-base-content/10 bg-base-100/95 p-6 shadow-2xl backdrop-blur-2xl space-y-4"
			transition:scale={{ duration: 220, start: 0.94, easing: cubicOut }}
		>
			<div class="flex items-center justify-between border-b border-base-content/10 pb-3">
				<h3 class="font-bold text-base text-base-content flex items-center gap-2">
					<Layers class="w-5 h-5 text-primary" />
					<span>Add Curriculum Section</span>
				</h3>
				<button type="button" class="btn btn-ghost btn-circle btn-xs text-base-content/60" onclick={() => (isAddSectionModalOpen = false)}>
					<X class="w-4 h-4" />
				</button>
			</div>
			<form onsubmit={(e) => { e.preventDefault(); handleAddSection(); }} class="space-y-4">
				<div>
					<label for="sec-title" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Section Title <span class="text-error">*</span>
					</label>
					<input
						id="sec-title"
						type="text"
						bind:value={newSectionTitle}
						placeholder="e.g. Module 1: Architecture Overview"
						class="input input-bordered input-sm w-full bg-base-200/50"
						required
					/>
				</div>
				<div class="flex justify-end gap-2 pt-3 border-t border-base-content/10">
					<button type="button" class="btn btn-sm btn-ghost" onclick={() => (isAddSectionModalOpen = false)}>Cancel</button>
					<button type="submit" class="btn btn-sm btn-primary gap-1.5" disabled={isActionLoading || !newSectionTitle.trim()}>
						<Plus class="w-4 h-4" />
						Create Section
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}

<!-- Edit Section Modal -->
{#if isEditSectionModalOpen}
	<div
		class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
		role="dialog"
		aria-modal="true"
		transition:fade={{ duration: 180 }}
	>
		<div class="fixed inset-0" onclick={() => (isEditSectionModalOpen = false)} role="presentation"></div>
		<div
			class="relative z-10 w-full max-w-md overflow-hidden rounded-2xl border border-base-content/10 bg-base-100/95 p-6 shadow-2xl backdrop-blur-2xl space-y-4"
			transition:scale={{ duration: 220, start: 0.94, easing: cubicOut }}
		>
			<div class="flex items-center justify-between border-b border-base-content/10 pb-3">
				<h3 class="font-bold text-base text-base-content flex items-center gap-2">
					<Edit3 class="w-5 h-5 text-primary" />
					<span>Edit Section Title</span>
				</h3>
				<button type="button" class="btn btn-ghost btn-circle btn-xs text-base-content/60" onclick={() => (isEditSectionModalOpen = false)}>
					<X class="w-4 h-4" />
				</button>
			</div>
			<form onsubmit={(e) => { e.preventDefault(); handleEditSection(); }} class="space-y-4">
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
				<div class="flex justify-end gap-2 pt-3 border-t border-base-content/10">
					<button type="button" class="btn btn-sm btn-ghost" onclick={() => (isEditSectionModalOpen = false)}>Cancel</button>
					<button type="submit" class="btn btn-sm btn-primary gap-1.5" disabled={isActionLoading || !editingSectionTitle.trim()}>
						<Check class="w-4 h-4" />
						Save Title
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}

<!-- Add Lesson Modal -->
{#if isAddLessonModalOpen}
	<div
		class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
		role="dialog"
		aria-modal="true"
		transition:fade={{ duration: 180 }}
	>
		<div class="fixed inset-0" onclick={() => (isAddLessonModalOpen = false)} role="presentation"></div>
		<div
			class="relative z-10 w-full max-w-2xl overflow-hidden rounded-2xl border border-base-content/10 bg-base-100/95 p-6 shadow-2xl backdrop-blur-2xl space-y-4 max-h-[90vh] overflow-y-auto"
			transition:scale={{ duration: 220, start: 0.94, easing: cubicOut }}
		>
			<div class="flex items-center justify-between border-b border-base-content/10 pb-3">
				<h3 class="font-bold text-base text-base-content flex items-center gap-2">
					<Plus class="w-5 h-5 text-primary" />
					<span>Add New Lesson Material</span>
				</h3>
				<button type="button" class="btn btn-ghost btn-circle btn-xs text-base-content/60" onclick={() => (isAddLessonModalOpen = false)}>
					<X class="w-4 h-4" />
				</button>
			</div>
			<form onsubmit={(e) => { e.preventDefault(); handleAddLesson(); }} class="space-y-4">
				<div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
					<div>
						<label for="l-title" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Lesson Title <span class="text-error">*</span>
						</label>
						<input
							id="l-title"
							type="text"
							bind:value={newLessonTitle}
							placeholder="e.g. Understanding Event-Driven Messaging"
							class="input input-bordered input-sm w-full bg-base-200/50"
							required
						/>
					</div>
					<div>
						<label for="l-dur" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Estimated Duration (Minutes)
						</label>
						<input
							id="l-dur"
							type="number"
							min="1"
							bind:value={newLessonDuration}
							class="input input-bordered input-sm w-full bg-base-200/50"
						/>
					</div>
				</div>

				<div>
					<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70 block">
						Material Content Type
					</label>
					<div class="grid grid-cols-2 sm:grid-cols-4 gap-2">
						{#each materialTypes as m}
							<button
								type="button"
								class="p-3 rounded-xl border text-left transition-all flex flex-col items-center justify-center gap-1.5 {newLessonType === m.id ? 'bg-primary/10 border-primary shadow-xs' : 'bg-base-200/50 border-base-content/10 hover:bg-base-200'}"
								onclick={() => (newLessonType = m.id)}
							>
								<m.icon class="w-5 h-5 {newLessonType === m.id ? 'text-primary' : 'text-base-content/60'}" />
								<span class="text-[11px] font-bold text-base-content">{m.label}</span>
							</button>
						{/each}
					</div>
				</div>

				{#if newLessonType === 'Video' || newLessonType === 'PdfDocument' || newLessonType === 'DownloadableFile'}
					<div>
						<label for="l-url" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Resource / Media URL <span class="text-error">*</span>
						</label>
						<input
							id="l-url"
							type="url"
							bind:value={newLessonContentUrl}
							placeholder="https://storage.domain.com/lectures/lesson-01.mp4"
							class="input input-bordered input-sm w-full bg-base-200/50"
							required
						/>
					</div>
				{/if}

				{#if newLessonType === 'Text'}
					<div class="space-y-1.5">
						<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70 block">
							Lesson Body / Written Guide
						</label>
						<RichEditor
							bind:content={newLessonTextContent}
							placeholder="Write comprehensive markdown/rich lesson text with code blocks and formulas..."
						/>
					</div>
				{/if}

				<div class="flex justify-end gap-2 pt-3 border-t border-base-content/10">
					<button type="button" class="btn btn-sm btn-ghost" onclick={() => (isAddLessonModalOpen = false)}>Cancel</button>
					<button type="submit" class="btn btn-sm btn-primary gap-1.5" disabled={isActionLoading || !newLessonTitle.trim()}>
						<Plus class="w-4 h-4" />
						Create Lesson
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}

<!-- Edit Lesson Modal -->
{#if isEditLessonModalOpen}
	<div
		class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
		role="dialog"
		aria-modal="true"
		transition:fade={{ duration: 180 }}
	>
		<div class="fixed inset-0" onclick={() => (isEditLessonModalOpen = false)} role="presentation"></div>
		<div
			class="relative z-10 w-full max-w-2xl overflow-hidden rounded-2xl border border-base-content/10 bg-base-100/95 p-6 shadow-2xl backdrop-blur-2xl space-y-4 max-h-[90vh] overflow-y-auto"
			transition:scale={{ duration: 220, start: 0.94, easing: cubicOut }}
		>
			<div class="flex items-center justify-between border-b border-base-content/10 pb-3">
				<h3 class="font-bold text-base text-base-content flex items-center gap-2">
					<Edit3 class="w-5 h-5 text-primary" />
					<span>Edit Lesson Material</span>
				</h3>
				<button type="button" class="btn btn-ghost btn-circle btn-xs text-base-content/60" onclick={() => (isEditLessonModalOpen = false)}>
					<X class="w-4 h-4" />
				</button>
			</div>
			<form onsubmit={(e) => { e.preventDefault(); handleEditLesson(); }} class="space-y-4">
				<div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
					<div>
						<label for="edit-l-title" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Lesson Title <span class="text-error">*</span>
						</label>
						<input
							id="edit-l-title"
							type="text"
							bind:value={editingLessonTitle}
							class="input input-bordered input-sm w-full bg-base-200/50"
							required
						/>
					</div>
					<div>
						<label for="edit-l-dur" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Duration (Minutes)
						</label>
						<input
							id="edit-l-dur"
							type="number"
							min="1"
							bind:value={editingLessonDuration}
							class="input input-bordered input-sm w-full bg-base-200/50"
						/>
					</div>
				</div>

				<div>
					<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70 block">
						Material Content Type
					</label>
					<div class="grid grid-cols-2 sm:grid-cols-4 gap-2">
						{#each materialTypes as m}
							<button
								type="button"
								class="p-3 rounded-xl border text-left transition-all flex flex-col items-center justify-center gap-1.5 {editingLessonType === m.id ? 'bg-primary/10 border-primary shadow-xs' : 'bg-base-200/50 border-base-content/10 hover:bg-base-200'}"
								onclick={() => (editingLessonType = m.id)}
							>
								<m.icon class="w-5 h-5 {editingLessonType === m.id ? 'text-primary' : 'text-base-content/60'}" />
								<span class="text-[11px] font-bold text-base-content">{m.label}</span>
							</button>
						{/each}
					</div>
				</div>

				{#if editingLessonType === 'Video' || editingLessonType === 'PdfDocument' || editingLessonType === 'DownloadableFile'}
					<div>
						<label for="edit-l-url" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
							Resource / Media URL <span class="text-error">*</span>
						</label>
						<input
							id="edit-l-url"
							type="url"
							bind:value={editingLessonContentUrl}
							class="input input-bordered input-sm w-full bg-base-200/50"
							required
						/>
					</div>
				{/if}

				{#if editingLessonType === 'Text'}
					<div class="space-y-1.5">
						<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70 block">
							Lesson Body / Written Guide
						</label>
						<RichEditor
							bind:content={editingLessonTextContent}
							placeholder="Write comprehensive markdown/rich lesson text with code blocks and formulas..."
						/>
					</div>
				{/if}

				<div class="flex justify-end gap-2 pt-3 border-t border-base-content/10">
					<button type="button" class="btn btn-sm btn-ghost" onclick={() => (isEditLessonModalOpen = false)}>Cancel</button>
					<button type="submit" class="btn btn-sm btn-primary gap-1.5" disabled={isActionLoading || !editingLessonTitle.trim()}>
						<Check class="w-4 h-4" />
						Save Lesson
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}

<!-- Delete Section Confirmation Modal -->
<ConfirmModal
	isOpen={isDeleteSectionModalOpen}
	title="Remove Curriculum Section"
	message={`Are you sure you want to remove section "${deletingSectionTitle}" and all its contained lessons?`}
	confirmText="Remove Section"
	isDanger={true}
	isLoading={isActionLoading}
	onConfirm={handleDeleteSection}
	onCancel={() => (isDeleteSectionModalOpen = false)}
/>

<!-- Delete Lesson Confirmation Modal -->
<ConfirmModal
	isOpen={isDeleteLessonModalOpen}
	title="Delete Lesson Material"
	message={`Are you sure you want to delete lesson "${deletingLessonTitle}"?`}
	confirmText="Delete Lesson"
	isDanger={true}
	isLoading={isActionLoading}
	onConfirm={handleDeleteLesson}
	onCancel={() => (isDeleteLessonModalOpen = false)}
/>

<!-- Unpublish Course Confirmation Modal -->
<ConfirmModal
	isOpen={isUnpublishModalOpen}
	title="Unpublish Course"
	message="Are you sure you want to unpublish this course? It will be reverted to Draft status and hidden from the public catalog. Existing enrolled students will continue to have access."
	confirmText="Unpublish to Draft"
	isDanger={false}
	isLoading={isActionLoading}
	onConfirm={handleUnpublish}
	onCancel={() => (isUnpublishModalOpen = false)}
/>

<!-- Delete Course Confirmation Modal -->
<ConfirmModal
	isOpen={isDeleteCourseModalOpen}
	title="Delete Course"
	message="Are you sure you want to permanently delete this course? All associated curriculum, lessons, assignments, and enrollments will be deleted."
	confirmText="Delete Course"
	isDanger={true}
	isLoading={isActionLoading}
	onConfirm={handleDeleteCourse}
	onCancel={() => (isDeleteCourseModalOpen = false)}
/>

<!-- Manual Enroll Student Modal -->
{#if isEnrollModalOpen}
	<div class="modal modal-open">
		<div class="modal-box max-w-md rounded-3xl border border-white/10 bg-base-100/90 backdrop-blur-2xl p-6 space-y-4 shadow-2xl">
			<div class="flex items-center justify-between border-b border-base-content/10 pb-3">
				<h3 class="font-bold text-base text-base-content flex items-center gap-2">
					<UserPlus class="w-5 h-5 text-primary" />
					Enroll Student Manually
				</h3>
				<button type="button" class="btn btn-ghost btn-xs btn-square" onclick={() => (isEnrollModalOpen = false)}>
					<X class="w-4 h-4" />
				</button>
			</div>

			<form onsubmit={(e) => { e.preventDefault(); handleAdminEnrollStudent(); }} class="space-y-4">
				<div class="space-y-1.5">
					<label for="enroll-student-email" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/80 block">
						Student Email or User ID <span class="text-error">*</span>
					</label>
					<input
						id="enroll-student-email"
						type="text"
						bind:value={newStudentEmail}
						placeholder="e.g. student@example.com or GUID"
						class="input input-bordered w-full rounded-xl bg-base-200/50 text-sm font-semibold"
						required
					/>
					<p class="text-[11px] text-base-content/60 leading-relaxed">
						Enrolling will immediately grant the student access to all lessons, assignments, and examinations in this course.
					</p>
				</div>

				<div class="flex justify-end gap-2 pt-2 border-t border-base-content/10">
					<button type="button" class="btn btn-sm btn-ghost rounded-xl" onclick={() => (isEnrollModalOpen = false)}>Cancel</button>
					<button
						type="submit"
						class="btn btn-sm btn-primary gradient-accent text-white font-bold rounded-xl gap-1.5 border-0 shadow-md"
						disabled={isEnrollingStudent || !newStudentEmail.trim()}
					>
						{#if isEnrollingStudent}
							<span class="loading loading-spinner loading-xs"></span>
						{:else}
							<UserPlus class="w-4 h-4" />
							Enroll Student
						{/if}
					</button>
				</div>
			</form>
		</div>
		<div class="modal-backdrop bg-black/40 backdrop-blur-sm" onclick={() => (isEnrollModalOpen = false)}></div>
	</div>
{/if}

<!-- Unenroll Confirmation Modal -->
<ConfirmModal
	isOpen={isUnenrollModalOpen}
	title="Un-enroll Student"
	message={`Are you sure you want to remove student "${removingEnrollment?.fullName || 'Selected Student'}" (${removingEnrollment?.email || ''}) from this course? Their progress data for this course will be cleared.`}
	confirmText="Un-enroll Student"
	isDanger={true}
	isLoading={isRemovingEnrollment}
	onConfirm={handleAdminRemoveEnrollment}
	onCancel={() => { isUnenrollModalOpen = false; removingEnrollment = null; }}
/>

<!-- Student Detailed Progress Inspection Modal -->
<StudentProgressModal
	isOpen={isStudentDetailModalOpen}
	student={selectedStudentForDetail}
	onClose={() => (isStudentDetailModalOpen = false)}
	onOpenGrading={openEssayGrading}
	onOpenRetake={openRetakeModal}
/>

<!-- Grant Exam Retake Confirmation Modal -->
{#if isRetakeModalOpen && retakeTarget}
	<div class="modal modal-open">
		<div class="modal-box max-w-md rounded-3xl border border-white/10 bg-base-100/95 backdrop-blur-2xl p-6 space-y-4 shadow-2xl">
			<div class="flex items-center justify-between border-b border-base-content/10 pb-3">
				<h3 class="font-bold text-base text-base-content flex items-center gap-2">
					<RotateCcw class="w-5 h-5 text-primary" />
					Grant Examination Retake
				</h3>
				<button type="button" class="btn btn-ghost btn-xs btn-square" onclick={() => (isRetakeModalOpen = false)}>
					<X class="w-4 h-4" />
				</button>
			</div>

			<div class="space-y-3">
				<p class="text-xs text-base-content/80 leading-relaxed">
					Are you sure you want to allow <strong>{retakeTarget.studentName}</strong> to retake <strong>{retakeTarget.examTitle}</strong>?
				</p>

				<div class="p-3 bg-primary/10 rounded-2xl border border-primary/20 text-[11px] text-primary space-y-1">
					<p class="font-bold">What happens when you grant a retake:</p>
					<ul class="list-disc pl-4 space-y-0.5 opacity-90">
						<li>Previous blocked, timed-out, or disqualified attempt will be reset.</li>
						<li>Active Redis session locks will be flushed immediately.</li>
						<li>The student can immediately launch a fresh attempt.</li>
					</ul>
				</div>

				<div class="space-y-1">
					<label for="retake-reason" class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70">
						Reason / Note (Optional)
					</label>
					<input
						id="retake-reason"
						type="text"
						bind:value={retakeReason}
						placeholder="e.g. Technical network glitch / False positive resolved"
						class="input input-bordered input-sm w-full rounded-xl bg-base-200/50 text-xs"
					/>
				</div>
			</div>

			<div class="flex justify-end gap-2 pt-3 border-t border-base-content/10">
				<button type="button" class="btn btn-sm btn-ghost rounded-xl" onclick={() => (isRetakeModalOpen = false)}>
					Cancel
				</button>
				<button
					type="button"
					class="btn btn-sm btn-primary gradient-accent text-white font-bold rounded-xl gap-1.5 shadow-md border-0"
					onclick={handleGrantRetake}
					disabled={isGrantingRetake}
				>
					{#if isGrantingRetake}
						<span class="loading loading-spinner loading-xs"></span>
					{:else}
						<RotateCcw class="w-3.5 h-3.5" />
					{/if}
					Confirm & Unlock Retake
				</button>
			</div>
		</div>
		<div class="modal-backdrop bg-black/40 backdrop-blur-sm" onclick={() => (isRetakeModalOpen = false)}></div>
	</div>
{/if}

<!-- Essay Grading & Submission Inspection Modal -->
<EssayGradingModal
	isOpen={isGradingModalOpen}
	submissionId={gradingSubmissionId}
	courseId={courseId}
	studentName={gradingStudentName}
	studentEmail={gradingStudentEmail}
	studentId={gradingStudentId}
	onClose={() => {
		isGradingModalOpen = false;
		gradingSubmissionId = null;
	}}
	onGraded={handleEssayGraded}
/>
