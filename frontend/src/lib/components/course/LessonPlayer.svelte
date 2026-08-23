<script lang="ts">
	import type { Lesson } from '#lib/api/types.ts';
	import { PlayCircle, FileText, Download, AlertCircle, ExternalLink } from '@lucide/svelte';

	interface Props {
		lesson: Lesson;
	}

	let { lesson }: Props = $props();
</script>

<div class="glass-card overflow-hidden rounded-3xl border border-white/10 shadow-2xl">
	{#if lesson.type === 'Video'}
		<div class="relative aspect-video w-full bg-black">
			{#if lesson.contentUrl}
				<video
					src={lesson.contentUrl}
					controls
					class="h-full w-full object-contain"
					preload="metadata"
				>
					<track kind="captions" />
					Your browser does not support the video tag.
				</video>
			{:else}
				<div class="flex h-full items-center justify-center text-xs text-base-content/40">
					No video source provided.
				</div>
			{/if}
		</div>
	{:else if lesson.type === 'PdfDocument'}
		<div class="h-[750px] w-full bg-base-300">
			{#if lesson.contentUrl}
				<iframe
					src={lesson.contentUrl}
					title={lesson.title}
					class="h-full w-full border-0"
				></iframe>
			{:else}
				<div class="flex h-full items-center justify-center text-xs text-base-content/40">
					No PDF document available.
				</div>
			{/if}
		</div>
	{:else if lesson.type === 'DownloadableFile'}
		<div class="p-8 text-center space-y-4">
			<div class="gradient-accent mx-auto flex h-16 w-16 items-center justify-center rounded-3xl text-white shadow-xl">
				<Download class="h-8 w-8" />
			</div>
			<div class="space-y-1">
				<h3 class="text-lg font-bold text-base-content">{lesson.title}</h3>
				<p class="text-xs text-base-content/60">Download the supplementary materials for this lesson.</p>
			</div>
			<a
				href={lesson.contentUrl}
				download
				target="_blank"
				rel="noopener noreferrer"
				class="btn btn-primary gradient-accent rounded-xl text-white font-semibold border-0 shadow-md gap-2"
			>
				<Download class="h-4 w-4" />
				Download Material
			</a>
		</div>
	{/if}

	<!-- Lesson Title & Metadata Bar -->
	<div class="p-6 border-t border-white/10 space-y-2">
		<div class="flex items-center gap-2">
			<span class="badge badge-primary badge-xs uppercase font-bold">{lesson.type}</span>
			{#if lesson.durationMinutes > 0}
				<span class="text-xs text-base-content/60">{lesson.durationMinutes} minutes</span>
			{/if}
		</div>
		<h2 class="text-2xl font-extrabold text-base-content tracking-tight">{lesson.title}</h2>
	</div>
</div>
