<script lang="ts">
	import type { Component } from 'svelte';

	interface Props {
		title: string;
		value: string | number;
		description?: string;
		icon?: any;
		trend?: string;
		trendUp?: boolean;
		color?: 'primary' | 'secondary' | 'accent' | 'warning' | 'success' | 'info';
	}

	let {
		title,
		value,
		description,
		icon: Icon,
		trend,
		trendUp,
		color = 'primary'
	}: Props = $props();

	const colorClasses = {
		primary: 'text-primary bg-primary/10 border-primary/20',
		secondary: 'text-secondary bg-secondary/10 border-secondary/20',
		accent: 'text-accent bg-accent/10 border-accent/20',
		warning: 'text-warning bg-warning/10 border-warning/20',
		success: 'text-success bg-success/10 border-success/20',
		info: 'text-info bg-info/10 border-info/20'
	};
</script>

<div class="glass-card flex items-center justify-between rounded-2xl p-5 border shadow-sm">
	<div class="space-y-1">
		<div class="text-xs font-medium uppercase tracking-wider text-base-content/60">{title}</div>
		<div class="text-2xl font-bold tracking-tight text-base-content">{value}</div>
		{#if description || trend}
			<div class="flex items-center gap-1.5 text-xs">
				{#if trend}
					<span class="font-semibold {trendUp ? 'text-success' : 'text-error'}">
						{trendUp ? '↑' : '↓'} {trend}
					</span>
				{/if}
				{#if description}
					<span class="text-base-content/60">{description}</span>
				{/if}
			</div>
		{/if}
	</div>

	{#if Icon}
		<div class="flex h-12 w-12 items-center justify-center rounded-2xl border {colorClasses[color]}">
			<Icon class="h-6 w-6" />
		</div>
	{/if}
</div>
