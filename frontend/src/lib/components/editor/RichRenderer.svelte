<script lang="ts">
	import { Edra, createEditor } from '#lib/components/edra/shadcn/index.ts';
	import '#lib/components/edra/shadcn/editor.css';
	import '#lib/components/edra/onedark.css';

	interface Props {
		content?: string;
		class?: string;
	}

	let { content = '', class: className = '' }: Props = $props();

	const editor = createEditor();

	$effect(() => {
		if (editor) {
			editor.setEditable(false);
			if (content) {
				try {
					const parsed = JSON.parse(content);
					editor.commands.setContent(parsed);
				} catch {
					editor.commands.setContent(content);
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
