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

<div class="glass-card relative overflow-hidden rounded-xl border border-white/10 p-2 shadow-inner" style="min-height: {minHeight};">
	{#if editor}
		<Edra {editor}>
			{#if !readonly}
				<div class="glass-panel sticky top-0 z-10 -mx-2 -mt-2 mb-3 border-b border-white/10 px-2 py-1.5 backdrop-blur-md">
					<Edra.Toolbar />
				</div>
			{/if}

			<div class="prose prose-invert max-w-none px-3 py-2 focus:outline-none">
				<Edra.Content />
				{#if !readonly}
					<Edra.BubbleMenu />
					<Edra.DragHandle />
				{/if}
			</div>
		</Edra>
	{/if}
</div>
