<script lang="ts">
	import type { CourseSection, Lesson } from '#lib/api/types.ts';
	import { PlayCircle, FileText, Download, ChevronRight, Clock } from '@lucide/svelte';

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

	const lessonIcons = {
		Video: PlayCircle,
		PdfDocument: FileText,
		DownloadableFile: Download
	};
</script>

<div class="space-y-3">
	{#each sections as section, sIdx (section.id || sIdx)}
		<div class="glass-card overflow-hidden rounded-2xl border border-white/5">
			<!-- Section Header -->
			<div class="bg-base-100/40 px-4 py-3 flex items-center justify-between border-b border-white/5">
				<div class="flex items-center gap-2">
					<span class="gradient-accent flex h-5 w-5 items-center justify-center rounded-md text-[10px] font-bold text-white">
						{sIdx + 1}
					</span>
					<span class="text-xs font-bold text-base-content">{section.title}</span>
				</div>
				<span class="text-[10px] text-base-content/50">
					{section.lessons?.length || 0} lessons
				</span>
			</div>

			<!-- Lessons List -->
			<div class="divide-y divide-white/5">
				{#each section.lessons || [] as lesson (lesson.id)}
					{@const Icon = lessonIcons[lesson.type] || PlayCircle}
					{@const isCurrent = lesson.id === activeLessonId}

					<div
						class="flex items-center justify-between px-4 py-3 text-xs transition-colors {isCurrent
							? 'bg-primary/15 text-primary font-semibold'
							: 'hover:bg-base-100/30 text-base-content/80'} {isEnrolled ? 'cursor-pointer' : ''}"
						onclick={() => isEnrolled && onSelectLesson && onSelectLesson(lesson)}
						role="button"
						tabindex="0"
						onkeydown={(e) => isEnrolled && e.key === 'Enter' && onSelectLesson && onSelectLesson(lesson)}
					>
						<div class="flex items-center gap-2.5 overflow-hidden">
							<Icon class="h-4 w-4 shrink-0 {isCurrent ? 'text-primary' : 'text-base-content/50'}" />
							<span class="truncate">{lesson.title}</span>
						</div>

						<div class="flex items-center gap-2 text-[10px] text-base-content/50 shrink-0">
							{#if lesson.durationMinutes > 0}
								<span class="flex items-center gap-1">
									<Clock class="h-3 w-3" />
									{lesson.durationMinutes}m
								</span>
							{/if}
							{#if isEnrolled}
								<ChevronRight class="h-3.5 w-3.5 {isCurrent ? 'text-primary' : 'text-base-content/40'}" />
							{/if}
						</div>
					</div>
				{/each}
			</div>
		</div>
	{:else}
		<div class="text-center py-6 text-xs text-base-content/50">
			No sections or lessons available yet.
		</div>
	{/each}
</div>
