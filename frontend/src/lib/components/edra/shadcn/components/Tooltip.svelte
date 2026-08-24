<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		tooltip: string;
		children: Snippet<[]>;
		shortCut?: string;
	}

	const { tooltip, children, shortCut }: Props = $props();
	const titleText = $derived(`${tooltip}${shortCut ? ` (${shortCut})` : ''}`);
</script>

<span title={titleText} class="inline-flex items-center">
	{@render children()}
</span>
