<script lang="ts">
	import { Edra, createEditor } from '$lib/components/edra/shadcn/index.ts';
	import '$lib/components/edra/shadcn/editor.css';
	import '$lib/components/edra/onedark.css';
	import { untrack } from 'svelte';

	interface Props {
		content?: string;
		placeholder?: string;
		readonly?: boolean;
		minHeight?: string;
		onUpdate?: (jsonContent: string, htmlContent: string) => void;
		onFileUpload?: (file: File) => Promise<string>;
	}

	let {
		content = $bindable(''),
		placeholder = 'Write something or press "/" for commands...',
		readonly = false,
		minHeight = '220px',
		onUpdate,
		onFileUpload
	}: Props = $props();

	let isUpdatingInternally = false;
	let lastEmittedContent = '';

	const editor = createEditor({
		onFileUpload: (file) => (onFileUpload ? onFileUpload(file) : Promise.resolve('')),
		onUpdate: () => {
			if (editor && !isUpdatingInternally) {
				const json = JSON.stringify(editor.getJSON());
				const html = editor.getHTML();
				const result = html || json;
				lastEmittedContent = result;
				content = result;
				if (onUpdate) {
					onUpdate(json, html);
				}
			}
		}
	});

	$effect(() => {
		// Only sync if content changed from external source (avoid circular loops)
		if (editor && content !== undefined && content !== lastEmittedContent) {
			untrack(() => {
				isUpdatingInternally = true;
				try {
					if (!content) {
						editor.commands.clearContent();
					} else {
						try {
							const parsed = JSON.parse(content);
							if (JSON.stringify(editor.getJSON()) !== content) {
								editor.commands.setContent(parsed, { emitUpdate: false });
							}
						} catch {
							if (editor.getHTML() !== content) {
								editor.commands.setContent(content, { emitUpdate: false });
							}
						}
					}
					lastEmittedContent = content;
				} finally {
					isUpdatingInternally = false;
				}
			});
		}
	});

	$effect(() => {
		if (editor) {
			editor.setEditable(!readonly);
		}
	});
</script>

<div class="glass-card relative overflow-hidden rounded-2xl border border-base-content/15 shadow-sm bg-base-100/60 transition-all focus-within:border-primary focus-within:ring-2 focus-within:ring-primary/20">
	{#if editor}
		<Edra {editor}>
			{#if !readonly}
				<div class="sticky top-0 z-10 border-b border-base-content/10 px-2 py-1.5 backdrop-blur-xl bg-base-200/60 rounded-t-2xl">
					<Edra.Toolbar />
				</div>
			{/if}

			<div class="prose dark:prose-invert max-w-none px-4 py-3 focus:outline-none text-base-content text-sm leading-relaxed" style="min-height: {minHeight};">
				<Edra.Content />
				{#if !readonly}
					<Edra.BubbleMenu />
					<Edra.DragHandle />
				{/if}
			</div>
		</Edra>
	{/if}
</div>
