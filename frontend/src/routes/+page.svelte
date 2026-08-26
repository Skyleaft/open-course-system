<script lang="ts">
	import PageShell from '#lib/components/layout/PageShell.svelte';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import { authStore } from '#lib/stores/auth.svelte.ts';
	import { customizationStore } from '#lib/stores/customization.svelte.ts';
	import {
		Sparkles,
		ShieldCheck,
		Zap,
		GraduationCap,
		Award,
		BookOpen,
		ArrowRight,
		CheckCircle2,
		Video,
		Clock
	} from '@lucide/svelte';
</script>

<PageShell showSidebar={false}>
	<div class="space-y-24 py-6 sm:py-12">
		<!-- Hero Section -->
		<section class="relative text-center">
			<div class="pointer-events-none absolute -top-24 left-1/2 -translate-x-1/2 h-[450px] w-[700px] rounded-full bg-primary/20 blur-[130px]"></div>

			<div class="relative z-10 mx-auto max-w-4xl space-y-6">
				<div class="inline-flex items-center gap-2 rounded-full border border-white/10 bg-base-100/50 px-4 py-1.5 backdrop-blur-xl shadow-sm">
					<span class="gradient-accent flex h-2 w-2 rounded-full animate-pulse"></span>
					<span class="text-xs font-semibold text-base-content/80">{customizationStore.data.branding.tagline}</span>
				</div>

				<h1 class="text-4xl font-extrabold tracking-tight text-base-content sm:text-6xl md:text-7xl">
					{customizationStore.data.branding.siteName} <br />
					<span class="text-gradient">Learn Without Limits.</span>
				</h1>

				<p class="mx-auto max-w-2xl text-base text-base-content/70 sm:text-lg">
					{customizationStore.data.branding.description}
				</p>

				<div class="flex flex-wrap items-center justify-center gap-4 pt-4">
					{#if authStore.isAuthenticated}
						<a
							href="/dashboard"
							class="btn btn-primary gradient-accent h-12 rounded-2xl px-6 font-semibold text-white border-0 shadow-xl"
						>
							Go to Dashboard
							<ArrowRight class="h-4 w-4 ml-1" />
						</a>
					{:else}
						<a
							href="/register"
							class="btn btn-primary gradient-accent h-12 rounded-2xl px-6 font-semibold text-white border-0 shadow-xl"
						>
							Get Started Free
							<ArrowRight class="h-4 w-4 ml-1" />
						</a>
						<a
							href="/login"
							class="btn btn-ghost glass-card h-12 rounded-2xl px-6 font-semibold border border-white/10 text-base-content hover:bg-base-100/40"
						>
							Sign In
						</a>
					{/if}
					<a
						href="/courses"
						class="btn btn-ghost h-12 rounded-2xl px-6 font-medium text-base-content/80 hover:bg-base-100/30"
					>
						<BookOpen class="h-4 w-4 mr-1" />
						Browse Courses
					</a>
				</div>
			</div>
		</section>

		<!-- Feature Highlights Grid -->
		<section class="space-y-8">
			<div class="text-center space-y-2">
				<h2 class="text-3xl font-bold tracking-tight text-base-content">Platform Highlights</h2>
				<p class="text-sm text-base-content/60">Engineered for academic integrity and maximum performance</p>
			</div>

			<div class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
				<GlassCard hover={true}>
					<div class="space-y-4">
						<div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-primary/15 text-primary border border-primary/25">
							<ShieldCheck class="h-6 w-6" />
						</div>
						<h3 class="text-lg font-bold text-base-content">Realtime Anti-Cheat Engine</h3>
						<p class="text-xs text-base-content/70 leading-relaxed">
							Multi-layered client-side interceptors for tab switches, fullscreen exit, and window blur combined with Web Worker periodic webcam snapshot captures.
						</p>
					</div>
				</GlassCard>

				<GlassCard hover={true}>
					<div class="space-y-4">
						<div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-secondary/15 text-secondary border border-secondary/25">
							<Zap class="h-6 w-6" />
						</div>
						<h3 class="text-lg font-bold text-base-content">Redis In-Memory Answer Buffer</h3>
						<p class="text-xs text-base-content/70 leading-relaxed">
							Zero database writes during exam runtime. Realtime answers are buffered in Redis cache with automatic disaster recovery upon reconnect.
						</p>
					</div>
				</GlassCard>

				<GlassCard hover={true}>
					<div class="space-y-4">
						<div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-accent/15 text-accent border border-accent/25">
							<Award class="h-6 w-6" />
						</div>
						<h3 class="text-lg font-bold text-base-content">Cryptographic Certificates</h3>
						<p class="text-xs text-base-content/70 leading-relaxed">
							Digitally signed certificates computed with SHA-256 hashes for instant tamper-proof verification by employers and institutions.
						</p>
					</div>
				</GlassCard>
			</div>
		</section>

		<!-- Dual Mode Exam Callout -->
		<section class="glass-panel relative overflow-hidden rounded-3xl border border-white/10 p-8 sm:p-12 shadow-2xl backdrop-blur-2xl">
			<div class="grid grid-cols-1 gap-8 lg:grid-cols-2 lg:items-center">
				<div class="space-y-4">
					<div class="inline-flex items-center gap-1.5 rounded-lg bg-primary/10 border border-primary/20 px-3 py-1 text-xs font-semibold text-primary">
						<Sparkles class="h-3.5 w-3.5" />
						Dual-Mode Exam Engine
					</div>
					<h2 class="text-3xl font-extrabold text-base-content tracking-tight sm:text-4xl">
						Simulation & RealExam Modes
					</h2>
					<p class="text-sm text-base-content/70 leading-relaxed">
						Practice freely with Simulation mode featuring immediate explanations and answer keys. Switch to RealExam mode for proctored examinations with time drift compensation and strict violation thresholds.
					</p>
					<ul class="space-y-2 text-xs text-base-content/80">
						<li class="flex items-center gap-2">
							<CheckCircle2 class="h-4 w-4 text-success" />
							Deterministic Fisher-Yates shuffle with fixed per-attempt seed
						</li>
						<li class="flex items-center gap-2">
							<CheckCircle2 class="h-4 w-4 text-success" />
							Direct MinIO presigned URL storage for snapshots and submissions
						</li>
						<li class="flex items-center gap-2">
							<CheckCircle2 class="h-4 w-4 text-success" />
							Async background grading via Redis Streams with Dead Letter Queue
						</li>
					</ul>
				</div>

				<div class="glass-card flex flex-col gap-4 rounded-2xl border border-white/10 p-6 shadow-inner">
					<div class="flex items-center justify-between border-b border-white/10 pb-3">
						<span class="text-xs font-bold uppercase tracking-wider text-base-content/60">Live Mode Matrix</span>
						<span class="badge badge-success badge-sm font-semibold">Active</span>
					</div>
					<div class="space-y-3">
						<div class="flex items-center justify-between rounded-xl bg-base-100/40 p-3">
							<span class="text-xs font-semibold">Simulation Mode</span>
							<span class="text-[11px] text-base-content/60">Free Practice • Instant Keys</span>
						</div>
						<div class="flex items-center justify-between rounded-xl bg-primary/10 border border-primary/20 p-3">
							<span class="text-xs font-semibold text-primary">RealExam Mode</span>
							<span class="text-[11px] text-primary/80">Proctored • Anti-Cheat • Timer</span>
						</div>
					</div>
				</div>
			</div>
		</section>
	</div>
</PageShell>
