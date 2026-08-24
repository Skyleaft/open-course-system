<script lang="ts">
	import type { Lesson } from '$lib/api/types.ts';
	import RichRenderer from '$lib/components/editor/RichRenderer.svelte';
	import {
		PlayCircle,
		FileText,
		Download,
		AlertCircle,
		ExternalLink,
		BookOpen,
		AlignLeft,
		Clock
	} from 'lucide-svelte';

	interface Props {
		lesson: Lesson;
	}

	let { lesson }: Props = $props();
</script>

<div class="bg-base-200/40 overflow-hidden rounded-3xl border border-base-content/10 shadow-xl">
	{#if lesson.type === 'Video'}
		<div class="relative aspect-video w-full bg-black flex items-center justify-center">
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
				<div class="flex flex-col items-center justify-center gap-2 text-xs text-white/50">
					<PlayCircle class="w-8 h-8 text-white/30" />
					<span>No video source URL configured for this lesson.</span>
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
			<div class="w-16 h-16 rounded-3xl bg-primary/10 text-primary mx-auto flex items-center justify-center shadow-md">
				<Download class="w-8 h-8" />
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
					class="btn btn-primary btn-sm rounded-xl gap-2 shadow-md"
				>
					<Download class="w-4 h-4" />
					Download Resource
				</a>
			{/if}
		</div>
	{:else}
		<!-- Default / Text Lesson: Rich Reading Surface -->
		<div class="p-6 sm:p-8 space-y-6">
			{#if lesson.textContent}
				<div class="prose max-w-none text-base-content leading-relaxed">
					<RichRenderer content={lesson.textContent} />
				</div>
			{:else}
				<div class="flex flex-col items-center justify-center py-12 text-center space-y-2">
					<AlignLeft class="w-10 h-10 text-base-content/30" />
					<p class="text-xs text-base-content/50">No text content written for this lesson yet.</p>
				</div>
			{/if}

			{#if lesson.contentUrl}
				<div class="pt-4 border-t border-base-content/10 flex items-center justify-between">
					<span class="text-xs text-base-content/60">Attached Reference Resource</span>
					<a
						href={lesson.contentUrl}
						target="_blank"
						rel="noopener noreferrer"
						class="btn btn-sm btn-ghost text-xs text-primary hover:bg-primary/10 gap-1.5 rounded-xl font-semibold"
					>
						<ExternalLink class="w-3.5 h-3.5" />
						Open Resource
					</a>
				</div>
			{/if}
		</div>
	{/if}

	<!-- Lesson Title & Metadata Bar -->
	<div class="p-6 border-t border-base-content/10 bg-base-100/50 space-y-1.5">
		<div class="flex items-center gap-2">
			<span class="badge badge-primary badge-xs uppercase font-bold text-[9px]">{lesson.type || 'Text'}</span>
			{#if lesson.durationMinutes > 0}
				<span class="text-xs text-base-content/60 flex items-center gap-1">
					<Clock class="w-3 h-3 text-base-content/40" />
					{lesson.durationMinutes} minutes
				</span>
			{/if}
		</div>
		<h2 class="text-xl sm:text-2xl font-extrabold text-base-content tracking-tight">{lesson.title}</h2>
	</div>
</div>
