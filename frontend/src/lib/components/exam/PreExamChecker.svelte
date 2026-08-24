<script lang="ts">
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import { Camera, Mic, Maximize, CheckCircle2, AlertCircle, Sparkles, ArrowRight } from '@lucide/svelte';
	import { onMount, onDestroy } from 'svelte';

	interface Props {
		examTitle: string;
		durationMinutes: number;
		mode: string;
		onReadyToStart: (stream: MediaStream | null) => void;
	}

	let { examTitle, durationMinutes, mode, onReadyToStart }: Props = $props();

	let videoElement: HTMLVideoElement | null = null;
	let mediaStream = $state<MediaStream | null>(null);

	let hasCamera = $state(false);
	let hasMic = $state(false);
	let hasFullscreen = $state(false);
	let isTermsAccepted = $state(false);
	let errorMessage = $state<string | null>(null);
	let isInitializing = $state(true);

	const isRealExam = $derived(mode === 'RealExam');
	const canStart = $derived(
		(!isRealExam || (hasCamera && hasMic)) && isTermsAccepted
	);

	onMount(async () => {
		try {
			if (navigator.mediaDevices?.getUserMedia) {
				const stream = await navigator.mediaDevices.getUserMedia({
					video: { width: { ideal: 640 }, height: { ideal: 480 } },
					audio: true
				});
				mediaStream = stream;
				hasCamera = stream.getVideoTracks().length > 0;
				hasMic = stream.getAudioTracks().length > 0;

				if (videoElement) {
					videoElement.srcObject = stream;
				}
			}
		} catch (err: any) {
			if (isRealExam) {
				errorMessage = 'Camera & microphone permissions are required for proctored RealExam mode.';
			}
		} finally {
			isInitializing = false;
		}
	});

	onDestroy(() => {
		// Stop media stream tracks if cancelled before start
	});

	async function handleStart() {
		// Request fullscreen
		try {
			if (document.documentElement.requestFullscreen) {
				await document.documentElement.requestFullscreen();
				hasFullscreen = true;
			}
		} catch {
			console.warn('Fullscreen prompt skipped/blocked');
		}

		onReadyToStart(mediaStream);
	}
</script>

<div class="max-w-3xl mx-auto space-y-6">
	<!-- Header -->
	<div class="glass-panel text-center rounded-3xl border border-white/10 p-8 shadow-2xl space-y-3">
		<div class="inline-flex items-center gap-1.5 rounded-lg bg-primary/10 border border-primary/20 px-3 py-1 text-xs font-semibold text-primary">
			<Sparkles class="h-3.5 w-3.5" />
			System Readiness Check
		</div>
		<h1 class="text-3xl font-extrabold text-base-content tracking-tight">{examTitle}</h1>
		<div class="flex items-center justify-center gap-3 text-xs text-base-content/70">
			<span class="badge {isRealExam ? 'badge-primary' : 'badge-ghost'} font-bold">{mode}</span>
			<span>&bull;</span>
			<span>{durationMinutes} Minutes</span>
		</div>
	</div>

	<!-- Pre-flight Checklist & Video Preview -->
	<div class="grid grid-cols-1 gap-6 sm:grid-cols-2">
		<!-- Left: Camera Preview -->
		<GlassCard class="flex flex-col items-center justify-center p-4 space-y-3 text-center">
			<div class="relative aspect-video w-full overflow-hidden rounded-2xl bg-black/60 border border-white/10 shadow-inner">
				<video
					bind:this={videoElement}
					autoplay
					muted
					playsinline
					class="h-full w-full object-cover transform scale-x-[-1]"
				></video>

				{#if isInitializing}
					<div class="absolute inset-0 flex items-center justify-center bg-base-300/80">
						<span class="loading loading-spinner loading-md text-primary"></span>
					</div>
				{:else if !hasCamera}
					<div class="absolute inset-0 flex flex-col items-center justify-center gap-2 p-4 text-xs text-base-content/50">
						<Camera class="h-8 w-8 opacity-40" />
						<span>Camera feed unavailable</span>
					</div>
				{/if}
			</div>

			<div class="text-[11px] text-base-content/60">
				📹 Live Picture-in-Picture preview. (Video is processed locally).
			</div>
		</GlassCard>

		<!-- Right: Verification Items -->
		<GlassCard class="flex flex-col justify-between p-6 space-y-4">
			<div class="space-y-3">
				<h3 class="text-sm font-bold uppercase tracking-wider text-base-content/60">Readiness Criteria</h3>

				<div class="space-y-2 text-xs">
					<div class="flex items-center justify-between rounded-xl bg-base-100/40 p-3 border border-white/5">
						<span class="flex items-center gap-2 font-semibold">
							<Camera class="h-4 w-4 text-primary" />
							Webcam Device
						</span>
						{#if hasCamera}
							<CheckCircle2 class="h-4 w-4 text-success" />
						{:else}
							<span class="badge badge-warning badge-xs font-semibold">Required</span>
						{/if}
					</div>

					<div class="flex items-center justify-between rounded-xl bg-base-100/40 p-3 border border-white/5">
						<span class="flex items-center gap-2 font-semibold">
							<Mic class="h-4 w-4 text-secondary" />
							Microphone
						</span>
						{#if hasMic}
							<CheckCircle2 class="h-4 w-4 text-success" />
						{:else}
							<span class="badge badge-warning badge-xs font-semibold">Required</span>
						{/if}
					</div>

					<div class="flex items-center justify-between rounded-xl bg-base-100/40 p-3 border border-white/5">
						<span class="flex items-center gap-2 font-semibold">
							<Maximize class="h-4 w-4 text-accent" />
							Fullscreen Support
						</span>
						<CheckCircle2 class="h-4 w-4 text-success" />
					</div>
				</div>

				<label class="flex items-start gap-2.5 pt-2 cursor-pointer">
					<input
						type="checkbox"
						class="checkbox checkbox-primary checkbox-xs mt-0.5 rounded-sm"
						bind:checked={isTermsAccepted}
					/>
					<span class="text-xs text-base-content/80 leading-tight select-none">
						I agree to uphold examination integrity and acknowledge security interceptors are active.
					</span>
				</label>
			</div>

			<button
				class="btn btn-primary gradient-accent w-full rounded-2xl font-bold text-white border-0 shadow-lg h-11"
				disabled={!canStart}
				onclick={handleStart}
			>
				Begin Examination
				<ArrowRight class="h-4 w-4 ml-1" />
			</button>
		</GlassCard>
	</div>

	{#if errorMessage}
		<div class="flex items-center gap-2 rounded-xl bg-error/15 border border-error/25 p-3.5 text-xs text-error">
			<AlertCircle class="h-4 w-4 shrink-0" />
			<span>{errorMessage}</span>
		</div>
	{/if}
</div>
