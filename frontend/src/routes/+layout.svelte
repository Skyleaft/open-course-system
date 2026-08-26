<script lang="ts">
	import '../app.css';
	import ToastContainer from '#lib/components/ui/ToastContainer.svelte';
	import { authStore } from '#lib/stores/auth.svelte.ts';
	import { customizationStore } from '#lib/stores/customization.svelte.ts';
	import { onMount } from 'svelte';

	let { children } = $props();

	onMount(() => {
		authStore.initialize();
		customizationStore.initialize();
	});
</script>

<svelte:head>
	<title>{customizationStore.data.branding.siteName} — {customizationStore.data.branding.tagline}</title>
	<meta name="description" content={customizationStore.data.branding.description} />
	{#if customizationStore.data.branding.faviconUrl}
		<link rel="icon" href={customizationStore.data.branding.faviconUrl} />
	{:else}
		<link rel="icon" href="data:image/svg+xml,<svg xmlns=%22http://www.w3.org/2000/svg%22 viewBox=%220 0 100 100%22><text y=%22.9em%22 font-size=%2290%22>⚡</text></svg>" />
	{/if}
</svelte:head>

{@render children()}
<ToastContainer />
