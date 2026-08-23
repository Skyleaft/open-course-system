<script lang="ts">
	import type { Lesson } from '#lib/api/types.ts';
	import RichRenderer from '#lib/components/editor/RichRenderer.svelte';
	import {
		PlayCircle,
		FileText,
		Download,
		AlertCircle,
		ExternalLink,
		BookOpen,
		AlignLeft
	} from '@lucide/svelte';

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
			{#if lesson.contentUrl}
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
			{/if}
		</div>
	{:else}
		<!-- Default / Text Lesson: Rich Reading Surface -->
		<div class="p-8 space-y-6">
			{#if lesson.textContent}
				<div class="prose max-w-none">
					<RichRenderer content={lesson.textContent} />
				</div>
			{:else}
				<div class="flex flex-col items-center justify-center py-12 text-center space-y-2">
					<AlignLeft class="h-10 w-10 text-base-content/30" />
					<p class="text-xs text-base-content/50">No text content written for this lesson yet.</p>
				</div>
			{/if}

			{#if lesson.contentUrl}
				<div class="pt-4 border-t border-white/10 flex items-center justify-between">
					<span class="text-xs text-base-content/60">Attached Reference Resource</span>
					<a
						href={lesson.contentUrl}
						target="_blank"
						rel="noopener noreferrer"
						class="btn btn-sm btn-ghost text-xs text-primary hover:bg-primary/10 gap-1.5 rounded-xl font-semibold"
					>
						<ExternalLink class="h-3.5 w-3.5" />
						Open Resource
					</a>
				</div>
			{/if}
		</div>
	{/if}

	<!-- Lesson Title & Metadata Bar -->
	<div class="p-6 border-t border-white/10 space-y-2">
		<div class="flex items-center gap-2">
			<span class="badge badge-primary badge-xs uppercase font-bold">{lesson.type || 'Text'}</span>
			{#if lesson.durationMinutes > 0}
				<span class="text-xs text-base-content/60">{lesson.durationMinutes} minutes</span>
			{/if}
		</div>
		<h2 class="text-2xl font-extrabold text-base-content tracking-tight">{lesson.title}</h2>
	</div>
</div>
