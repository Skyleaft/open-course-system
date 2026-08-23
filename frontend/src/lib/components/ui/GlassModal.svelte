<script lang="ts">
	import type { Snippet } from 'svelte';
	import { X } from '@lucide/svelte';

	interface Props {
		isOpen: boolean;
		title?: string;
		onClose: () => void;
		children: Snippet;
		actions?: Snippet;
		maxWidth?: string;
	}

	let {
		isOpen,
		title,
		onClose,
		children,
		actions,
		maxWidth = 'max-w-lg'
	}: Props = $props();

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape' && isOpen) {
			onClose();
		}
	}
</script>

<svelte:window onkeydown={handleKeydown} />

{#if isOpen}
	<div
		class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-md transition-opacity duration-300 animate-in fade-in"
		role="dialog"
		aria-modal="true"
	>
		<!-- Backdrop click -->
		<div class="fixed inset-0" onclick={onClose} role="presentation"></div>

		<div class="glass-modal relative z-10 w-full {maxWidth} overflow-hidden rounded-2xl border p-6 shadow-2xl backdrop-blur-2xl">
			{#if title}
				<div class="mb-4 flex items-center justify-between border-b border-white/10 pb-3">
					<h3 class="text-lg font-bold text-base-content">{title}</h3>
					<button
						class="btn btn-ghost btn-circle btn-sm text-base-content/60 hover:text-base-content"
						onclick={onClose}
						aria-label="Close modal"
					>
						<X class="h-5 w-5" />
					</button>
				</div>
			{/if}

			<div class="space-y-4">
				{@render children()}
			</div>

			{#if actions}
				<div class="mt-6 flex items-center justify-end gap-2 border-t border-white/10 pt-4">
					{@render actions()}
				</div>
			{/if}
		</div>
	</div>
{/if}
