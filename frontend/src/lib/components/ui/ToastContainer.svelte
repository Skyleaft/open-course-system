<script lang="ts">
	import { toast } from '#lib/stores/toast.svelte.ts';
	import { Info, CheckCircle2, AlertTriangle, AlertCircle, X } from '@lucide/svelte';

	const typeConfig = {
		info: {
			icon: Info,
			borderClass: 'border-info/40 text-info',
			bgClass: 'bg-info/10'
		},
		success: {
			icon: CheckCircle2,
			borderClass: 'border-success/40 text-success',
			bgClass: 'bg-success/10'
		},
		warning: {
			icon: AlertTriangle,
			borderClass: 'border-warning/40 text-warning',
			bgClass: 'bg-warning/10'
		},
		error: {
			icon: AlertCircle,
			borderClass: 'border-error/40 text-error',
			bgClass: 'bg-error/10'
		}
	};
</script>

<div class="toast toast-end toast-top z-50 flex flex-col gap-2.5 p-4">
	{#each toast.toasts as item (item.id)}
		{@const config = typeConfig[item.type]}
		<div
			class="glass-panel flex min-w-[280px] max-w-sm items-center justify-between gap-3 rounded-xl border p-3.5 shadow-2xl backdrop-blur-xl transition-all duration-300 {config.borderClass} {config.bgClass}"
			role="alert"
		>
			<div class="flex items-center gap-2.5">
				<config.icon class="h-5 w-5 shrink-0" />
				<span class="text-sm font-medium text-base-content">{item.message}</span>
			</div>
			<button
				class="btn btn-ghost btn-xs btn-circle text-base-content/60 hover:text-base-content"
				onclick={() => toast.remove(item.id)}
				aria-label="Close"
			>
				<X class="h-4 w-4" />
			</button>
		</div>
	{/each}
</div>
