<script lang="ts">
	import { coursesApi } from '#lib/api/courses.ts';
	import type { Course } from '#lib/api/types.ts';
	import CourseCard from '#lib/components/course/CourseCard.svelte';
	import SearchInput from '#lib/components/ui/SearchInput.svelte';
	import {
		BookOpen,
		Sparkles,
		Filter,
		ArrowUpDown,
		SlidersHorizontal,
		ChevronLeft,
		ChevronRight,
		RotateCcw,
		Layers
	} from '@lucide/svelte';
	import { onMount } from 'svelte';

	let courses = $state<Course[]>([]);
	let isLoading = $state(true);
	let searchQuery = $state('');
	let selectedAccessType = $state('All');
	let selectedSort = $state('createdAt_desc');
	let minPrice = $state<number | undefined>(undefined);
	let maxPrice = $state<number | undefined>(undefined);
	let showFilters = $state(false);

	let pageIndex = $state(1);
	let pageSize = $state(9);
	let totalCount = $state(0);
	let totalPages = $state(1);

	const accessTypes = ['All', 'OpenFree', 'OpenPaid', 'PrivateWithKey'];
	const sortOptions = [
		{ value: 'createdAt_desc', label: 'Newest First' },
		{ value: 'createdAt_asc', label: 'Oldest First' },
		{ value: 'title_asc', label: 'Title: A → Z' },
		{ value: 'title_desc', label: 'Title: Z → A' },
		{ value: 'price_asc', label: 'Price: Low to High' },
		{ value: 'price_desc', label: 'Price: High to Low' }
	];

	onMount(async () => {
		await loadCourses();
	});

	async function loadCourses() {
		isLoading = true;
		try {
			const [sortBy, sortOrder] = selectedSort.split('_');
			const res = await coursesApi.getCourses({
				accessType: selectedAccessType === 'All' ? undefined : selectedAccessType,
				searchTerm: searchQuery || undefined,
				minPrice: minPrice !== undefined && minPrice !== null ? minPrice : undefined,
				maxPrice: maxPrice !== undefined && maxPrice !== null ? maxPrice : undefined,
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

	function handleFilterChange(type: string) {
		selectedAccessType = type;
		pageIndex = 1;
		loadCourses();
	}

	function handleSortChange() {
		pageIndex = 1;
		loadCourses();
	}

	function handlePriceFilterApply() {
		pageIndex = 1;
		loadCourses();
	}

	function resetFilters() {
		searchQuery = '';
		selectedAccessType = 'All';
		selectedSort = 'createdAt_desc';
		minPrice = undefined;
		maxPrice = undefined;
		pageIndex = 1;
		loadCourses();
	}

	function setPage(page: number) {
		if (page < 1 || page > totalPages || page === pageIndex) return;
		pageIndex = page;
		loadCourses();
	}

	let hasActiveFilters = $derived(
		searchQuery.trim() !== '' ||
		selectedAccessType !== 'All' ||
		minPrice !== undefined ||
		maxPrice !== undefined ||
		selectedSort !== 'createdAt_desc'
	);
</script>

<div class="space-y-8">
	<!-- Header Banner -->
	<div class="glass-panel relative overflow-hidden rounded-3xl border border-white/10 p-8 shadow-2xl backdrop-blur-2xl">
		<div class="relative z-10 space-y-2">
			<div class="inline-flex items-center gap-2 rounded-lg bg-primary/10 border border-primary/20 px-3 py-1 text-xs font-semibold text-primary">
				<Sparkles class="h-3.5 w-3.5" />
				Curated Learning Paths
			</div>
			<div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
				<div>
					<h1 class="text-3xl font-extrabold tracking-tight text-base-content sm:text-4xl">
						Explore Course Catalog
					</h1>
					<p class="text-xs text-base-content/70 sm:text-sm max-w-xl mt-1">
						Master distributed engineering, computer science fundamentals, and prepare for certifications.
					</p>
				</div>
				<div class="badge badge-primary badge-outline text-xs px-3 py-3 font-semibold self-start sm:self-auto">
					{totalCount} {totalCount === 1 ? 'Course' : 'Courses'} Available
				</div>
			</div>
		</div>
	</div>

	<!-- Search & Filter Controls -->
	<div class="space-y-4">
		<div class="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
			<!-- Search Bar -->
			<div class="w-full lg:max-w-md">
				<SearchInput bind:value={searchQuery} placeholder="Search courses by title or keywords..." onInput={handleSearch} />
			</div>

			<div class="flex flex-wrap items-center gap-3">
				<!-- Access Type Pills -->
				<div class="glass-card flex items-center gap-1 rounded-2xl p-1 border border-white/5 overflow-x-auto max-w-full">
					{#each accessTypes as type}
						<button
							class="btn btn-xs rounded-xl font-medium transition-colors {selectedAccessType === type
								? 'btn-primary gradient-accent text-white font-semibold shadow-xs border-0'
								: 'btn-ghost text-base-content/70 hover:bg-base-100/40'}"
							onclick={() => handleFilterChange(type)}
						>
							{type === 'All' ? 'All Access' : type.replace('Open', '').replace('PrivateWithKey', 'Private')}
						</button>
					{/each}
				</div>

				<!-- Sort Dropdown -->
				<div class="flex items-center gap-2">
					<select
						class="select select-sm select-bordered rounded-2xl bg-base-100/50 border-white/10 text-xs text-base-content focus:border-primary"
						bind:value={selectedSort}
						onchange={handleSortChange}
					>
						{#each sortOptions as opt}
							<option value={opt.value}>{opt.label}</option>
						{/each}
					</select>

					<!-- Toggle Advanced Filters Drawer/Panel -->
					<button
						class="btn btn-sm btn-ghost glass-card border border-white/10 rounded-2xl gap-1.5 text-xs text-base-content/80 hover:bg-base-100/40"
						onclick={() => (showFilters = !showFilters)}
					>
						<SlidersHorizontal class="h-3.5 w-3.5 {showFilters ? 'text-primary' : ''}" />
						Filters
						{#if hasActiveFilters}
							<span class="badge badge-primary badge-xs">Active</span>
						{/if}
					</button>
				</div>
			</div>
		</div>

		<!-- Expandable Advanced Filter Panel -->
		{#if showFilters}
			<div class="glass-card rounded-2xl p-4 border border-white/10 space-y-4">
				<div class="flex flex-wrap items-end justify-between gap-4">
					<div class="flex flex-wrap items-center gap-4">
						<div class="space-y-1">
							<label for="course-min-price" class="text-xs font-semibold text-base-content/70">Min Price ($)</label>
							<input
								id="course-min-price"
								type="number"
								min="0"
								placeholder="0"
								class="input input-sm input-bordered rounded-xl bg-base-100/40 border-white/10 w-28 text-xs"
								bind:value={minPrice}
							/>
						</div>
						<div class="space-y-1">
							<label for="course-max-price" class="text-xs font-semibold text-base-content/70">Max Price ($)</label>
							<input
								id="course-max-price"
								type="number"
								min="0"
								placeholder="500"
								class="input input-sm input-bordered rounded-xl bg-base-100/40 border-white/10 w-28 text-xs"
								bind:value={maxPrice}
							/>
						</div>
						<button
							class="btn btn-sm btn-primary rounded-xl text-xs font-semibold self-end"
							onclick={handlePriceFilterApply}
						>
							Apply Filter
						</button>
					</div>

					{#if hasActiveFilters}
						<button
							class="btn btn-sm btn-ghost text-xs text-error hover:bg-error/10 gap-1 rounded-xl"
							onclick={resetFilters}
						>
							<RotateCcw class="h-3 w-3" />
							Reset All
						</button>
					{/if}
				</div>
			</div>
		{/if}
	</div>

	<!-- Course Grid -->
	{#if isLoading}
		<div class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
			{#each Array(6) as _}
				<div class="glass-card flex flex-col justify-between h-56 rounded-2xl p-5 border border-white/5 animate-pulse">
					<div class="space-y-3">
						<div class="h-5 w-20 bg-base-100/40 rounded-lg"></div>
						<div class="h-6 w-3/4 bg-base-100/40 rounded-lg"></div>
						<div class="h-4 w-full bg-base-100/20 rounded-lg"></div>
					</div>
					<div class="h-8 w-full bg-base-100/30 rounded-xl"></div>
				</div>
			{/each}
		</div>
	{:else if courses.length > 0}
		<div class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
			{#each courses as course (course.id)}
				<CourseCard {course} />
			{/each}
		</div>

		<!-- Pagination Controls -->
		{#if totalPages > 1}
			<div class="flex items-center justify-between pt-6 border-t border-white/10">
				<p class="text-xs text-base-content/60">
					Showing <span class="font-bold text-base-content">{courses.length}</span> of <span class="font-bold text-base-content">{totalCount}</span> courses
				</p>
				<div class="join glass-card border border-white/10 rounded-2xl p-0.5">
					<button
						class="join-item btn btn-xs btn-ghost text-base-content/70"
						disabled={pageIndex <= 1}
						onclick={() => setPage(pageIndex - 1)}
					>
						<ChevronLeft class="h-3.5 w-3.5" />
					</button>

					{#each Array(totalPages) as _, i}
						{#if i + 1 === pageIndex || i + 1 === 1 || i + 1 === totalPages || Math.abs(i + 1 - pageIndex) <= 1}
							<button
								class="join-item btn btn-xs {pageIndex === i + 1 ? 'btn-primary gradient-accent text-white font-bold' : 'btn-ghost text-base-content/70'}"
								onclick={() => setPage(i + 1)}
							>
								{i + 1}
							</button>
						{/if}
					{/each}

					<button
						class="join-item btn btn-xs btn-ghost text-base-content/70"
						disabled={pageIndex >= totalPages}
						onclick={() => setPage(pageIndex + 1)}
					>
						<ChevronRight class="h-3.5 w-3.5" />
					</button>
				</div>
			</div>
		{/if}
	{:else}
		<div class="glass-card rounded-3xl p-12 text-center border border-white/5 space-y-4">
			<div class="gradient-accent mx-auto flex h-14 w-14 items-center justify-center rounded-2xl text-white">
				<BookOpen class="h-7 w-7" />
			</div>
			<div class="space-y-1">
				<h3 class="text-lg font-bold text-base-content">No courses found</h3>
				<p class="text-xs text-base-content/60">Try adjusting your filters, price range, or search keywords.</p>
			</div>
			{#if hasActiveFilters}
				<button class="btn btn-sm btn-ghost rounded-xl border border-white/10 text-xs" onclick={resetFilters}>
					Reset Filters
				</button>
			{/if}
		</div>
	{/if}
</div>
