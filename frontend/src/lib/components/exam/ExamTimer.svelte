<script lang="ts">
	import { Clock, AlertTriangle } from '@lucide/svelte';
	import { onMount } from 'svelte';

	interface Props {
		remainingSeconds: number;
		onTimeout?: () => void;
	}

	let { remainingSeconds = $bindable(0), onTimeout }: Props = $props();

	let isLowTime = $derived(remainingSeconds > 0 && remainingSeconds <= 300); // 5 minutes or less

	let formattedTime = $derived.by(() => {
		if (remainingSeconds <= 0) return '00:00:00';
		const hours = Math.floor(remainingSeconds / 3600);
		const minutes = Math.floor((remainingSeconds % 3600) / 60);
		const seconds = remainingSeconds % 60;
		return [
			hours.toString().padStart(2, '0'),
			minutes.toString().padStart(2, '0'),
			seconds.toString().padStart(2, '0')
		].join(':');
	});

	onMount(() => {
		const interval = setInterval(() => {
			if (remainingSeconds > 0) {
				remainingSeconds -= 1;
				if (remainingSeconds === 0 && onTimeout) {
					onTimeout();
				}
			}
		}, 1000);

		return () => clearInterval(interval);
	});
</script>

<div
	class="glass-panel flex items-center gap-2 rounded-xl px-3.5 py-1.5 border shadow-sm transition-all duration-300 {isLowTime
		? 'border-error/50 bg-error/15 text-error animate-pulse'
		: 'border-white/10 text-base-content'}"
>
	{#if isLowTime}
		<AlertTriangle class="h-4 w-4 text-error" />
	{:else}
		<Clock class="h-4 w-4 text-primary" />
	{/if}
	<span class="font-mono text-sm font-bold tracking-widest">{formattedTime}</span>
</div>
