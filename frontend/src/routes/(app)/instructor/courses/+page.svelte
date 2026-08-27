<script lang="ts">
	import { onMount } from 'svelte';
	import { coursesApi } from '$lib/api/courses.ts';
	import type { Course } from '$lib/api/types.ts';
	import GlassCard from '$lib/components/ui/GlassCard.svelte';
	import SearchInput from '$lib/components/ui/SearchInput.svelte';
	import ConfirmModal from '$lib/components/ui/ConfirmModal.svelte';
	import { toast } from '$lib/stores/toast.svelte.ts';
	import {
		Plus,
		Edit3,
		BookOpen,
		Layers,
		CheckCircle2,
		FileText,
		ArrowRight,
		SlidersHorizontal,
		RotateCcw,
		ChevronLeft,
		ChevronRight,
		Eye,
		Send,
		Trash2,
		Sparkles,
		Users,
		DollarSign,
		Key,
		Lock,
		ExternalLink
	} from 'lucide-svelte';

	let courses = $state<Course[]>([]);
	let isLoading = $state(true);
	let searchQuery = $state('');
	let selectedStatus = $state<'All' | 'Published' | 'Draft'>('All');
	let selectedAccessType = $state('All');
	let selectedSort = $state('createdAt_desc');

	let pageIndex = $state(1);
	let pageSize = $state(9);
	let totalCount = $state(0);
	let totalPages = $state(1);

	// Delete Course State
	let isDeleteModalOpen = $state(false);
	let deletingCourseId = $state<string | null>(null);
	let deletingCourseTitle = $state('');
	let isDeleting = $state(false);

	const statusOptions = ['All', 'Published', 'Draft'] as const;
	const accessTypes = ['All', 'OpenFree', 'OpenPaid', 'PrivateWithKey'];
	const sortOptions = [
		{ value: 'createdAt_desc', label: 'Newest First' },
		{ value: 'createdAt_asc', label: 'Oldest First' },
		{ value: 'title_asc', label: 'Title: A → Z' },
		{ value: 'title_desc', label: 'Title: Z → A' },
		{ value: 'price_desc', label: 'Price: High to Low' }
	];

	onMount(async () => {
		await loadCourses();
	});

	async function loadCourses() {
		isLoading = true;
		try {
			const [sortBy, sortOrder] = selectedSort.split('_');
			const isPublished =
				selectedStatus === 'Published' ? true : selectedStatus === 'Draft' ? false : undefined;

			const res = await coursesApi.getCourses({
				accessType: selectedAccessType === 'All' ? undefined : selectedAccessType,
				searchTerm: searchQuery || undefined,
				isPublished,
				sortBy,
				sortOrder,
				pageIndex,
				pageSize
			});

			courses = res.items || [];
			totalCount = res.totalCount || 0;
			totalPages = res.totalPages || Math.ceil(totalCount / pageSize) || 1;
		} catch {
			courses = [];
			totalCount = 0;
			totalPages = 1;
		} finally {
			isLoading = false;
		}
	}

	function handleSearch(q: string) {
		searchQuery = q;
		pageIndex = 1;
		loadCourses();
	}

	function handleStatusChange(status: 'All' | 'Published' | 'Draft') {
		selectedStatus = status;
		pageIndex = 1;
		loadCourses();
	}

	function handleAccessTypeChange(type: string) {
		selectedAccessType = type;
		pageIndex = 1;
		loadCourses();
	}

	function handleSortChange() {
		pageIndex = 1;
		loadCourses();
	}

	function resetFilters() {
		searchQuery = '';
		selectedStatus = 'All';
		selectedAccessType = 'All';
		selectedSort = 'createdAt_desc';
		pageIndex = 1;
		loadCourses();
	}

	function setPage(page: number) {
		if (page < 1 || page > totalPages || page === pageIndex) return;
		pageIndex = page;
		loadCourses();
	}

	function openDeleteModal(course: Course) {
		deletingCourseId = course.id;
		deletingCourseTitle = course.title;
		isDeleteModalOpen = true;
	}

	async function handleDeleteCourse() {
		if (!deletingCourseId) return;
		isDeleting = true;
		try {
			await coursesApi.deleteCourse(deletingCourseId);
			toast.success('Course deleted successfully.');
			isDeleteModalOpen = false;
			await loadCourses();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to delete course.');
		} finally {
			isDeleting = false;
		}
	}

	function stripHtml(html: string): string {
		return html.replace(/<[^>]*>?/gm, ' ').replace(/\s+/g, ' ').trim();
	}

	function getPlainDescription(raw?: string | null): string {
		if (!raw) return 'No description provided.';
		if (raw.startsWith('{')) {
			try {
				const parsed = JSON.parse(raw);
				const extractText = (node: any): string => {
					if (node.text) return node.text;
					if (node.content && Array.isArray(node.content)) {
						return node.content.map(extractText).join(' ');
					}
					return '';
				};
				const text = extractText(parsed).trim();
				if (text) return text;
			} catch {
				// fallback
			}
		}
		if (raw.includes('<')) {
			const stripped = stripHtml(raw);
			if (stripped) return stripped;
		}
		return raw;
	}

	let hasActiveFilters = $derived(
		searchQuery.trim() !== '' ||
		selectedStatus !== 'All' ||
		selectedAccessType !== 'All' ||
		selectedSort !== 'createdAt_desc'
	);
</script>

<div class="space-y-8 max-w-7xl mx-auto pb-16">
	<!-- Header -->
	<div class="glass-panel flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 rounded-3xl border border-base-content/10 p-6 sm:p-8 shadow-2xl backdrop-blur-2xl">
		<div class="space-y-1">
			<div class="inline-flex items-center gap-1.5 rounded-lg bg-primary/10 border border-primary/20 px-3 py-1 text-xs font-semibold text-primary">
				<Layers class="w-3.5 h-3.5" />
				<span>Instructor Studio</span>
			</div>
			<h1 class="text-2xl sm:text-3xl font-extrabold tracking-tight text-base-content">
				Course Authoring & Management
			</h1>
			<p class="text-xs text-base-content/70">
				Build curriculum, manage publish status, and configure student assignments.
			</p>
		</div>

		<a
			href="/instructor/courses/create"
			class="btn btn-primary btn-sm rounded-xl text-xs font-bold shadow-lg gap-1.5 self-start sm:self-auto"
		>
			<Plus class="w-4 h-4" />
			<span>Create New Course</span>
		</a>
	</div>

	<!-- Controls & Filters -->
	<div class="space-y-4">
		<div class="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
			<!-- Search Bar -->
			<div class="w-full lg:max-w-md">
				<SearchInput
					bind:value={searchQuery}
					placeholder="Search instructor courses by title..."
					onInput={handleSearch}
				/>
			</div>

			<div class="flex flex-wrap items-center gap-3">
				<!-- Status Filter (Published / Drafts / All) -->
				<div class="glass-card flex items-center gap-1 rounded-2xl p-1 border border-base-content/10">
					{#each statusOptions as status}
						<button
							type="button"
							class="btn btn-xs rounded-xl font-semibold transition-all {selectedStatus === status
								? 'btn-primary text-primary-content shadow-xs'
								: 'btn-ghost text-base-content/70 hover:bg-base-100/40'}"
							onclick={() => handleStatusChange(status)}
						>
							{status === 'All' ? 'All Status' : status}
						</button>
					{/each}
				</div>

				<!-- Access Type Pills -->
				<div class="glass-card hidden sm:flex items-center gap-1 rounded-2xl p-1 border border-base-content/10">
					{#each accessTypes as type}
						<button
							type="button"
							class="btn btn-xs rounded-xl font-semibold transition-all {selectedAccessType === type
								? 'btn-primary text-primary-content shadow-xs'
								: 'btn-ghost text-base-content/70 hover:bg-base-100/40'}"
							onclick={() => handleAccessTypeChange(type)}
						>
							{type === 'All' ? 'All Access' : type.replace('Open', '').replace('PrivateWithKey', 'Private')}
						</button>
					{/each}
				</div>

				<!-- Sort Dropdown -->
				<select
					class="select select-sm select-bordered rounded-2xl bg-base-100/50 border-base-content/10 text-xs text-base-content focus:border-primary"
					bind:value={selectedSort}
					onchange={handleSortChange}
				>
					{#each sortOptions as opt}
						<option value={opt.value}>{opt.label}</option>
					{/each}
				</select>

				{#if hasActiveFilters}
					<button
						type="button"
						class="btn btn-sm btn-ghost text-xs text-error hover:bg-error/10 gap-1 rounded-xl"
						onclick={resetFilters}
					>
						<RotateCcw class="w-3 h-3" />
						Reset
					</button>
				{/if}
			</div>
		</div>
	</div>

	<!-- Course List -->
	{#if isLoading}
		<div class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
			{#each Array(6) as _}
				<div class="glass-card flex flex-col justify-between overflow-hidden rounded-3xl p-5 border border-base-content/10 space-y-4 animate-pulse">
					<div class="aspect-video w-full rounded-2xl bg-base-100/50"></div>
					<div class="space-y-2.5">
						<div class="h-5 w-3/4 bg-base-100/50 rounded-lg"></div>
						<div class="h-3.5 w-full bg-base-100/30 rounded-lg"></div>
						<div class="h-3.5 w-2/3 bg-base-100/30 rounded-lg"></div>
					</div>
					<div class="pt-3 border-t border-base-content/5 flex items-center justify-between">
						<div class="h-8 w-28 bg-base-100/40 rounded-xl"></div>
						<div class="h-8 w-8 bg-base-100/40 rounded-xl"></div>
					</div>
				</div>
			{/each}
		</div>
	{:else if courses.length > 0}
		<div class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
			{#each courses as course (course.id)}
				<GlassCard class="flex flex-col justify-between overflow-hidden rounded-3xl p-4 sm:p-5 border border-base-content/10 hover:border-primary/30 transition-all duration-300 hover:shadow-2xl group">
					<div class="space-y-3.5">
						<!-- 16:9 Thumbnail Cover / Fallback Header with Floating Badges -->
						<div class="relative aspect-video w-full overflow-hidden rounded-2xl bg-base-200/60 border border-base-content/10 shadow-inner group/thumb">
							{#if course.thumbnailUrl}
								<img
									src={course.thumbnailUrl}
									alt={course.title}
									class="h-full w-full object-cover transition-transform duration-500 group-hover:scale-105"
									loading="lazy"
								/>
							{:else}
								<div class="h-full w-full relative flex flex-col items-center justify-center p-4 bg-gradient-to-br from-primary/15 via-base-200/70 to-secondary/15 transition-all duration-500 group-hover:from-primary/25 group-hover:to-secondary/25 overflow-hidden">
									<!-- Ambient Glow Orbs -->
									<div class="absolute -top-6 -right-6 w-24 h-24 rounded-full bg-primary/20 blur-xl pointer-events-none"></div>
									<div class="absolute -bottom-6 -left-6 w-24 h-24 rounded-full bg-secondary/20 blur-xl pointer-events-none"></div>

									<!-- Center Icon & Brand -->
									<div class="relative z-10 flex flex-col items-center justify-center gap-1.5 text-center">
										<div class="h-10 w-10 rounded-2xl bg-base-100/80 border border-white/10 shadow-lg flex items-center justify-center text-primary group-hover:scale-110 transition-all duration-300 backdrop-blur-md">
											<BookOpen class="w-5 h-5" />
										</div>
										<span class="text-[10px] font-bold text-base-content/60">Curriculum Studio</span>
									</div>
								</div>
							{/if}

							<!-- Floating Access Badge (Top-Left) -->
							<div class="absolute top-2.5 left-2.5 z-10">
								{#if course.accessType === 'OpenPaid'}
									<span class="badge badge-primary font-mono font-extrabold text-[10px] shadow-md shadow-black/30">
										${Number(course.price || 0).toFixed(2)}
									</span>
								{:else if course.accessType === 'PrivateWithKey'}
									<span class="badge badge-warning text-warning-content font-bold text-[10px] gap-1 shadow-md shadow-black/30">
										<Key class="w-2.5 h-2.5" />
										Passkey
									</span>
								{:else}
									<span class="badge badge-success text-white font-bold text-[10px] shadow-md shadow-black/30">
										Free
									</span>
								{/if}
							</div>

							<!-- Floating Publish Status Badge (Top-Right) -->
							<div class="absolute top-2.5 right-2.5 z-10">
								<span
									class="badge {course.isPublished ? 'badge-success text-white' : 'badge-warning text-warning-content'} text-[10px] font-bold shadow-md shadow-black/30 gap-1 backdrop-blur-md"
								>
									<span class="w-1.5 h-1.5 rounded-full {course.isPublished ? 'bg-white animate-pulse' : 'bg-current'}"></span>
									{course.isPublished ? 'Published' : 'Draft'}
								</span>
							</div>
						</div>

						<!-- Title & Description -->
						<div class="space-y-1.5 text-left">
							<a href="/instructor/courses/{course.id}/edit" class="block group/link">
								<h3 class="text-base font-extrabold text-base-content line-clamp-1 group-hover/link:text-primary transition-colors tracking-tight">
									{course.title}
								</h3>
							</a>
							<p class="text-xs text-base-content/65 line-clamp-2 leading-relaxed">
								{getPlainDescription(course.description)}
							</p>
						</div>

						<!-- Stat Badges Bar -->
						<div class="flex items-center gap-3 pt-1 text-[11px] text-base-content/60 flex-wrap">
							<span class="flex items-center gap-1">
								<Layers class="w-3.5 h-3.5 text-primary" />
								<span>{course.sections?.length || 0} Sections</span>
							</span>
							<span class="flex items-center gap-1">
								<Users class="w-3.5 h-3.5 text-secondary" />
								<span>{course.enrolledStudentsCount || 0} Enrolled</span>
							</span>
							{#if course.exams && course.exams.length > 0}
								<span class="flex items-center gap-1">
									<FileText class="w-3.5 h-3.5 text-accent" />
									<span>{course.exams.length} Exams</span>
								</span>
							{/if}
						</div>
					</div>

					<!-- Card Action Buttons Footer -->
					<div class="pt-3 mt-3 border-t border-base-content/10 flex items-center justify-between gap-2">
						<div class="flex items-center gap-1.5">
							<a
								href="/instructor/courses/{course.id}/edit"
								class="btn btn-primary gradient-accent btn-xs rounded-xl text-white font-bold border-0 shadow-xs gap-1 h-7 px-2.5"
							>
								<Edit3 class="w-3 h-3" />
								<span>Manage</span>
							</a>
							<a
								href="/courses/{course.id}"
								class="btn btn-ghost btn-xs rounded-xl text-base-content/70 hover:text-base-content hover:bg-base-100/60 gap-1 h-7 px-2"
								title="View public student page"
							>
								<Eye class="w-3 h-3" />
								<span>Preview</span>
							</a>
						</div>

						<button
							type="button"
							class="btn btn-ghost btn-xs text-error/70 hover:text-error hover:bg-error/10 h-7 w-7 p-0 rounded-xl"
							title="Delete course"
							onclick={() => openDeleteModal(course)}
						>
							<Trash2 class="w-3.5 h-3.5" />
						</button>
					</div>
				</GlassCard>
			{/each}
		</div>

		<!-- Pagination -->
		{#if totalPages > 1}
			<div class="flex items-center justify-center gap-2 pt-6">
				<button
					type="button"
					class="btn btn-sm btn-ghost rounded-xl"
					disabled={pageIndex <= 1}
					onclick={() => setPage(pageIndex - 1)}
				>
					<ChevronLeft class="w-4 h-4" />
					Prev
				</button>
				<span class="text-xs font-medium text-base-content/70">
					Page {pageIndex} of {totalPages}
				</span>
				<button
					type="button"
					class="btn btn-sm btn-ghost rounded-xl"
					disabled={pageIndex >= totalPages}
					onclick={() => setPage(pageIndex + 1)}
				>
					Next
					<ChevronRight class="w-4 h-4" />
				</button>
			</div>
		{/if}
	{:else}
		<div class="glass-card flex flex-col items-center justify-center p-12 text-center rounded-3xl border border-base-content/10 space-y-4">
			<Layers class="w-12 h-12 text-primary/40" />
			<div class="space-y-1">
				<h3 class="text-lg font-bold text-base-content">No courses found</h3>
				<p class="text-xs text-base-content/60 max-w-sm">
					{hasActiveFilters
						? 'No courses matched your current filter criteria. Try resetting filters.'
						: 'You have not created any courses yet. Get started by creating your first course!'}
				</p>
			</div>
			{#if hasActiveFilters}
				<button type="button" class="btn btn-sm btn-outline rounded-xl text-xs gap-1.5" onclick={resetFilters}>
					<RotateCcw class="w-3.5 h-3.5" />
					Reset All Filters
				</button>
			{:else}
				<a
					href="/instructor/courses/create"
					class="btn btn-primary rounded-xl text-xs font-bold shadow-lg gap-1.5"
				>
					<Plus class="w-4 h-4" />
					Create Your First Course
				</a>
			{/if}
		</div>
	{/if}

	<!-- Delete Course Modal -->
	<ConfirmModal
		isOpen={isDeleteModalOpen}
		title="Delete Course"
		message={`Are you sure you want to permanently delete "${deletingCourseTitle}"? This will remove all curriculum sections, lessons, assignments, and student enrollments associated with this course.`}
		confirmText="Delete Course"
		isDanger={true}
		isLoading={isDeleting}
		onConfirm={handleDeleteCourse}
		onCancel={() => (isDeleteModalOpen = false)}
	/>
</div>
