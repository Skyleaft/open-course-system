<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		children: Snippet;
		class?: string;
		hover?: boolean;
		glow?: boolean;
	}

	let { children, class: className = '', hover = false, glow = false }: Props = $props();
</script>

<div
	class="glass-card rounded-2xl p-6 {hover ? 'glass-card-hover cursor-pointer' : ''} {glow ? 'gradient-glow' : ''} {className}"
>
	{@render children()}
</div>
