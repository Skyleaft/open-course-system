<script lang="ts">
	import type { Snippet } from 'svelte';
	import { Sparkles } from '@lucide/svelte';

	let { children }: { children: Snippet } = $props();
</script>

<div class="relative flex min-h-screen items-center justify-center overflow-hidden bg-base-300 p-4">
	<!-- Background Glowing Ambient Orbs -->
	<div class="pointer-events-none absolute -top-40 -left-40 h-96 w-96 rounded-full bg-primary/20 blur-3xl"></div>
	<div class="pointer-events-none absolute -bottom-40 -right-40 h-96 w-96 rounded-full bg-secondary/20 blur-3xl"></div>
	<div class="pointer-events-none absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 h-[500px] w-[500px] rounded-full bg-accent/10 blur-[120px]"></div>

	<div class="relative z-10 w-full max-w-md">
		<!-- Brand Logo -->
		<div class="mb-6 text-center">
			<a href="/" class="inline-flex items-center gap-2.5 transition-transform hover:scale-105">
				<div class="gradient-accent flex h-10 w-10 items-center justify-center rounded-2xl shadow-lg">
					<Sparkles class="h-6 w-6 text-white" />
				</div>
				<div class="flex flex-col text-left">
					<span class="text-gradient font-extrabold text-2xl tracking-tight">Open Course System</span>
					<span class="text-[10px] uppercase tracking-widest text-base-content/60 font-semibold">LMS & Exams</span>
				</div>
			</a>
		</div>

		<!-- Card Container -->
		<div class="glass-panel overflow-hidden rounded-3xl border border-white/10 p-8 shadow-2xl backdrop-blur-2xl">
			{@render children()}
		</div>
	</div>
</div>
