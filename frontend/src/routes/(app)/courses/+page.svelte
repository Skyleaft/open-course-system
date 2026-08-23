<script lang="ts">
	import { coursesApi } from '#lib/api/courses.ts';
	import type { Course } from '#lib/api/types.ts';
	import CourseCard from '#lib/components/course/CourseCard.svelte';
	import SearchInput from '#lib/components/ui/SearchInput.svelte';
	import { BookOpen, Sparkles, Filter } from '@lucide/svelte';
	import { onMount } from 'svelte';

	let courses = $state<Course[]>([]);
	let isLoading = $state(true);
	let searchQuery = $state('');
	let selectedAccessType = $state('All');

	const accessTypes = ['All', 'OpenFree', 'OpenPaid', 'PrivateWithKey'];

	onMount(async () => {
		await loadCourses();
	});

	async function loadCourses() {
		isLoading = true;
		try {
			const res = await coursesApi.getCourses({
				accessType: selectedAccessType === 'All' ? undefined : selectedAccessType,
				search: searchQuery || undefined
			});
			courses = res.items || [];
		} catch {
			courses = [];
		} finally {
			isLoading = false;
		}
	}

	function handleSearch(q: string) {
		searchQuery = q;
		loadCourses();
	}

	function handleFilterChange(type: string) {
		selectedAccessType = type;
		loadCourses();
	}
</script>

<div class="space-y-8">
	<!-- Header Banner -->
	<div class="glass-panel relative overflow-hidden rounded-3xl border border-white/10 p-8 shadow-2xl backdrop-blur-2xl">
		<div class="relative z-10 space-y-2">
			<div class="inline-flex items-center gap-2 rounded-lg bg-primary/10 border border-primary/20 px-3 py-1 text-xs font-semibold text-primary">
				<Sparkles class="h-3.5 w-3.5" />
				Curated Learning Paths
			</div>
			<h1 class="text-3xl font-extrabold tracking-tight text-base-content sm:text-4xl">
				Explore Course Catalog
			</h1>
			<p class="text-xs text-base-content/70 sm:text-sm max-w-xl">
				Master distributed engineering, computer science fundamentals, and prepare for certifications.
			</p>
		</div>
	</div>

	<!-- Search & Filter Controls -->
	<div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
		<div class="w-full sm:max-w-xs">
			<SearchInput bind:value={searchQuery} placeholder="Search courses by title..." onInput={handleSearch} />
		</div>

		<!-- Filter Tabs -->
		<div class="glass-card flex items-center gap-1 rounded-2xl p-1 border border-white/5">
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
	{:else}
		<div class="glass-card rounded-3xl p-12 text-center border border-white/5 space-y-4">
			<div class="gradient-accent mx-auto flex h-14 w-14 items-center justify-center rounded-2xl text-white">
				<BookOpen class="h-7 w-7" />
			</div>
			<div class="space-y-1">
				<h3 class="text-lg font-bold text-base-content">No courses found</h3>
				<p class="text-xs text-base-content/60">Try adjusting your filters or search keywords.</p>
			</div>
		</div>
	{/if}
</div>
