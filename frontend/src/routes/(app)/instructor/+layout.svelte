<script lang="ts">
	import { authStore } from '#lib/stores/auth.svelte.ts';
	import { ShieldAlert, ArrowLeft } from '@lucide/svelte';
	import type { Snippet } from 'svelte';

	let { children }: { children: Snippet } = $props();
</script>

{#if authStore.isLoading}
	<div class="glass-panel h-96 rounded-3xl animate-pulse"></div>
{:else if authStore.isInstructor || authStore.isAdmin}
	{@render children()}
{:else}
	<div class="glass-card max-w-lg mx-auto p-12 text-center rounded-3xl border border-warning/30 space-y-4 my-8">
		<div class="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-warning/15 text-warning">
			<ShieldAlert class="h-7 w-7" />
		</div>
		<div class="space-y-1">
			<h2 class="text-xl font-bold text-base-content">Instructor Studio Restricted</h2>
			<p class="text-xs text-base-content/60">
				You need Instructor or Administrator credentials to access course and exam authoring tools.
			</p>
		</div>
		<a href="/dashboard" class="btn btn-ghost glass-card btn-sm rounded-xl border border-white/10">
			<ArrowLeft class="h-4 w-4 mr-1" />
			Back to Dashboard
		</a>
	</div>
{/if}
