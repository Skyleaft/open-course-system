<script lang="ts">
	import { Edra, createEditor } from '#lib/components/edra/shadcn/index.ts';
	import '#lib/components/edra/shadcn/editor.css';
	import '#lib/components/edra/onedark.css';

	interface Props {
		content?: string;
		placeholder?: string;
		readonly?: boolean;
		minHeight?: string;
		onUpdate?: (jsonContent: string, htmlContent: string) => void;
		onFileUpload?: (file: File) => Promise<string>;
	}

	let {
		content = '',
		placeholder = 'Write something or press "/" for commands...',
		readonly = false,
		minHeight = '220px',
		onUpdate,
		onFileUpload
	}: Props = $props();

	const editor = createEditor({
		onFileUpload: (file) => (onFileUpload ? onFileUpload(file) : Promise.resolve('')),
		onUpdate: () => {
			if (onUpdate && editor) {
				const json = JSON.stringify(editor.getJSON());
				const html = editor.getHTML();
				onUpdate(json, html);
			}
		}
	});

	$effect(() => {
		if (editor && content) {
			try {
				const parsed = JSON.parse(content);
				if (JSON.stringify(editor.getJSON()) !== content) {
					editor.commands.setContent(parsed);
				}
			} catch {
				if (editor.getHTML() !== content) {
					editor.commands.setContent(content);
				}
			}
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
