<script lang="ts">
	import PageShell from '#lib/components/layout/PageShell.svelte';
	import { authStore } from '#lib/stores/auth.svelte.ts';
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { ShieldAlert } from '@lucide/svelte';
	import type { Snippet } from 'svelte';

	let { children }: { children: Snippet } = $props();

	function isPublicRoute(pathname: string): boolean {
		if (pathname === '/' || pathname === '/courses') return true;
		// Public course detail page /courses/[id] (but NOT /courses/[id]/learn or /courses/[id]/assignments)
		if (/^\/courses\/[^\/]+$/.test(pathname)) return true;
		// Public certificate verification /certificates/verify/[hash]
		if (/^\/certificates\/verify/.test(pathname)) return true;
		return false;
	}

	function isSidebarRoute(pathname: string): boolean {
		// Public catalog and course details use full-width layout without sidebar
		if (pathname === '/' || pathname === '/courses') return false;
		if (/^\/courses\/[^\/]+$/.test(pathname)) return false;
		if (/^\/certificates\/verify/.test(pathname)) return false;

		// Examination environment: PreExamChecker and Exam Submissions runner use focused layout without sidebar
		if (/^\/exams\/[^\/]+\/start/.test(pathname)) return false;
		if (/^\/exams\/submissions\//.test(pathname)) return false;

		// All authenticated learning, instructor, proctor, and admin portals use the sidebar
		return authStore.isAuthenticated;
	}

	const isPublic = $derived(isPublicRoute(page.url.pathname));
	const showSidebar = $derived(isSidebarRoute(page.url.pathname));

	$effect(() => {
		if (!isPublic && !authStore.isLoading && !authStore.isAuthenticated) {
			const returnUrl = encodeURIComponent(page.url.pathname + page.url.search);
			goto(`/login?returnUrl=${returnUrl}`);
		}
	});
</script>

{#if isPublic || authStore.isAuthenticated}
	<PageShell {showSidebar}>
		{@render children()}
	</PageShell>
{:else if authStore.isLoading}
	<PageShell {showSidebar}>
		<div class="space-y-6 animate-pulse p-4">
			<div class="glass-panel h-36 rounded-3xl border border-white/10"></div>
			<div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
				<div class="glass-card h-28 rounded-2xl border border-white/5"></div>
				<div class="glass-card h-28 rounded-2xl border border-white/5"></div>
				<div class="glass-card h-28 rounded-2xl border border-white/5"></div>
				<div class="glass-card h-28 rounded-2xl border border-white/5"></div>
			</div>
			<div class="glass-panel h-64 rounded-3xl border border-white/10"></div>
		</div>
	</PageShell>
{:else}
	<PageShell showSidebar={false}>
		<div class="glass-card max-w-md mx-auto my-12 p-8 text-center rounded-3xl border border-primary/30 space-y-4">
			<div class="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-primary/15 text-primary">
				<ShieldAlert class="h-7 w-7" />
			</div>
			<div class="space-y-1">
				<h2 class="text-xl font-bold text-base-content">Authentication Required</h2>
				<p class="text-xs text-base-content/60">
					Please sign in to access your portal. Redirecting...
				</p>
			</div>
			<a
				href="/login?returnUrl={encodeURIComponent(page.url.pathname + page.url.search)}"
				class="btn btn-primary gradient-accent btn-sm rounded-xl text-white font-semibold border-0 shadow-md"
			>
				Sign In
			</a>
		</div>
	</PageShell>
{/if}
