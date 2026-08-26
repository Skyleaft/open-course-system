<script lang="ts">
	import { customizationStore } from '#lib/stores/customization.svelte.ts';
	import { Sparkles, Shield, Heart } from '@lucide/svelte';
</script>

<footer class="glass-navbar mt-auto border-t py-8 text-center text-xs text-base-content/60">
	<div class="mx-auto flex max-w-7xl flex-col items-center justify-between gap-4 px-4 sm:flex-row sm:px-6 lg:px-8">
		<div class="flex items-center gap-2">
			<div class="gradient-accent flex h-6 w-6 items-center justify-center rounded-lg shadow-xs">
				<Sparkles class="h-3.5 w-3.5 text-white" />
			</div>
			<span class="font-semibold text-base-content">{customizationStore.data.branding.siteName}</span>
			<span>{customizationStore.data.branding.footerCopyright}</span>
		</div>
		<div class="flex items-center gap-4 text-[11px]">
			<span class="inline-flex items-center gap-1">
				<Shield class="h-3.5 w-3.5 text-success" />
				Anti-Cheat AI Protected
			</span>
			<span>&bull;</span>
			<span class="inline-flex items-center gap-1">
				Open Course Engine
			</span>
		</div>
	</div>
</footer>
