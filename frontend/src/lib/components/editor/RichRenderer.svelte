<script lang="ts">
	import { Edra, createEditor } from '#lib/components/edra/shadcn/index.ts';
	import '#lib/components/edra/shadcn/editor.css';
	import '#lib/components/edra/onedark.css';

	interface Props {
		content?: string | any;
		class?: string;
	}

	let { content = '', class: className = '' }: Props = $props();

	const editor = createEditor();

	$effect(() => {
		if (editor) {
			editor.setEditable(false);
			if (content) {
				if (typeof content === 'object' && content !== null) {
					try {
						editor.commands.setContent(content);
					} catch {
						editor.commands.setContent(JSON.stringify(content));
					}
				} else if (typeof content === 'string') {
					const trimmed = content.trim();
					if (trimmed.startsWith('{') || trimmed.startsWith('[')) {
						try {
							const parsed = JSON.parse(trimmed);
							editor.commands.setContent(parsed);
						} catch {
							editor.commands.setContent(content);
						}
					} else {
						try {
							editor.commands.setContent(content);
						} catch {
							editor.commands.setContent(`<p>${content}</p>`);
						}
					}
				} else {
					editor.commands.setContent(String(content));
				}
			} else {
				editor.commands.setContent('');
			}
		}
	});
</script>

<div class="prose dark:prose-invert max-w-none text-base-content leading-relaxed {className}">
	{#if editor}
		<Edra {editor}>
			<Edra.Content />
		</Edra>
	{/if}
</div>
