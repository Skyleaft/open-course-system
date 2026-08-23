<script lang="ts">
	import PageShell from '#lib/components/layout/PageShell.svelte';
	import type { Snippet } from 'svelte';

	let { children }: { children: Snippet } = $props();
</script>

<PageShell showSidebar={true}>
	{@render children()}
</PageShell>
