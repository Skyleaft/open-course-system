<script lang="ts">
	import { goto } from '$app/navigation';
	import { onMount } from 'svelte';

	onMount(() => {
		goto('/courses', { replaceState: true });
	});
</script>

<div class="p-8 text-center space-y-3">
	<span class="loading loading-spinner loading-md text-primary"></span>
	<p class="text-xs text-base-content/60">Redirecting to course catalog...</p>
</div>
