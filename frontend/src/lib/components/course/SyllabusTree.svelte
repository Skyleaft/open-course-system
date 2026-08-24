<script lang="ts">
	import type { CourseSection, Lesson } from '$lib/api/types.ts';
	import { PlayCircle, FileText, Download, ChevronRight, Clock, AlignLeft } from 'lucide-svelte';

	interface Props {
		sections: CourseSection[];
		activeLessonId?: string;
		isEnrolled?: boolean;
		onSelectLesson?: (lesson: Lesson) => void;
	}

	let {
		sections = [],
		activeLessonId,
		isEnrolled = false,
		onSelectLesson
	}: Props = $props();

	const lessonIcons: Record<string, any> = {
		Text: AlignLeft,
		Video: PlayCircle,
		PdfDocument: FileText,
		DownloadableFile: Download
	};
</script>

<div class="space-y-3">
	{#each sections as section, sIdx (section.id || sIdx)}
		<div class="bg-base-200/50 overflow-hidden rounded-2xl border border-base-content/10">
			<!-- Section Header -->
			<div class="bg-base-100/60 px-4 py-3 flex items-center justify-between border-b border-base-content/5">
				<div class="flex items-center gap-2.5">
					<span class="w-6 h-6 rounded-lg bg-primary/10 text-primary font-mono font-bold text-xs flex items-center justify-center">
						{sIdx + 1}
					</span>
					<span class="text-xs font-bold text-base-content">{section.title}</span>
				</div>
				<span class="text-[10px] text-base-content/50 font-semibold">
					{section.lessons?.length || 0} lessons
				</span>
			</div>

			<!-- Lessons List -->
			<div class="divide-y divide-base-content/5">
				{#each section.lessons || [] as lesson (lesson.id)}
					{@const Icon = lessonIcons[lesson.type] || PlayCircle}
					{@const isCurrent = lesson.id === activeLessonId}

					<div
						class="flex items-center justify-between px-4 py-3 text-xs transition-colors {isCurrent
							? 'bg-primary/15 text-primary font-bold'
							: 'hover:bg-base-100/40 text-base-content/80'} {isEnrolled ? 'cursor-pointer' : ''}"
						onclick={() => isEnrolled && onSelectLesson && onSelectLesson(lesson)}
						role="button"
						tabindex="0"
						onkeydown={(e) => isEnrolled && e.key === 'Enter' && onSelectLesson && onSelectLesson(lesson)}
					>
						<div class="flex items-center gap-2.5 overflow-hidden min-w-0">
							<Icon class="w-4 h-4 shrink-0 {isCurrent ? 'text-primary' : 'text-base-content/50'}" />
							<span class="truncate">{lesson.title}</span>
						</div>

						<div class="flex items-center gap-2 text-[10px] text-base-content/50 shrink-0 ml-2">
							{#if lesson.durationMinutes > 0}
								<span class="flex items-center gap-1">
									<Clock class="w-3 h-3" />
									{lesson.durationMinutes}m
								</span>
							{/if}
							{#if isEnrolled}
								<ChevronRight class="w-3.5 h-3.5 {isCurrent ? 'text-primary' : 'text-base-content/40'}" />
							{/if}
						</div>
					</div>
				{/each}
			</div>
		</div>
	{:else}
		<div class="text-center py-8 text-xs text-base-content/50 bg-base-200/30 rounded-2xl border border-dashed border-base-300">
			No curriculum sections added to this course yet.
		</div>
	{/each}
</div>
