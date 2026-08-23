<script lang="ts">
	import { page } from '$app/state';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import { AlertTriangle, Home, ArrowLeft } from '@lucide/svelte';
</script>

<div class="flex min-h-screen items-center justify-center p-4 bg-base-300">
	<div class="glass-panel max-w-md w-full rounded-3xl border border-white/10 p-8 text-center space-y-6 shadow-2xl">
		<div class="mx-auto flex h-16 w-16 items-center justify-center rounded-3xl bg-error/15 text-error border border-error/25 shadow-lg">
			<AlertTriangle class="h-8 w-8" />
		</div>

		<div class="space-y-1">
			<span class="text-xs font-mono font-bold text-error uppercase tracking-widest">
				Error {page.status}
			</span>
			<h1 class="text-2xl font-extrabold text-base-content tracking-tight">
				{page.error?.message || 'Something went wrong'}
			</h1>
			<p class="text-xs text-base-content/60 leading-relaxed pt-1">
				The page you requested might have been removed, had its name changed, or is temporarily unavailable.
			</p>
		</div>

		<div class="flex items-center justify-center gap-3 pt-2">
			<a
				href="/"
				class="btn btn-primary gradient-accent btn-sm rounded-xl text-white font-semibold border-0 shadow-md gap-1.5"
			>
				<Home class="h-4 w-4" />
				Back to Safety
			</a>
		</div>
	</div>
</div>
