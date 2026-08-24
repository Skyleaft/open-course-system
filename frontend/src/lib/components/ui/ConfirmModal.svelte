<script lang="ts">
	import GlassModal from './GlassModal.svelte';
	import { AlertTriangle } from 'lucide-svelte';

	interface Props {
		isOpen: boolean;
		title?: string;
		message: string;
		confirmText?: string;
		cancelText?: string;
		isDanger?: boolean;
		isLoading?: boolean;
		onConfirm: () => void;
		onCancel: () => void;
	}

	let {
		isOpen,
		title = 'Confirm Action',
		message,
		confirmText = 'Confirm',
		cancelText = 'Cancel',
		isDanger = false,
		isLoading = false,
		onConfirm,
		onCancel
	}: Props = $props();
</script>

<GlassModal {isOpen} {title} onClose={onCancel} maxWidth="max-w-md">
	<div class="flex items-start gap-3.5 py-2">
		<div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl {isDanger ? 'bg-error/15 text-error border border-error/25' : 'bg-warning/15 text-warning border border-warning/25'}">
			<AlertTriangle class="h-5 w-5" />
		</div>
		<p class="text-sm text-base-content/80 leading-relaxed">{message}</p>
	</div>

	{#snippet actions()}
		<button class="btn btn-ghost btn-sm rounded-xl" onclick={onCancel} disabled={isLoading}>
			{cancelText}
		</button>
		<button
			class="btn btn-sm rounded-xl font-semibold text-white border-0 {isDanger ? 'btn-error' : 'btn-primary gradient-accent'}"
			onclick={onConfirm}
			disabled={isLoading}
		>
			{#if isLoading}
				<span class="loading loading-spinner loading-xs"></span>
			{/if}
			{confirmText}
		</button>
	{/snippet}
</GlassModal>
