<script lang="ts">
	import { buttonVariants } from '$lib/components/ui/button/button.svelte';
	import * as DropdownMenu from '$lib/components/ui/dropdown-menu/index.js';
	import { getEditor } from '../../../tiptap/index.js';
	import { ChevronDown, Download } from '@lucide/svelte';
	const editor = getEditor();
	const handleExport = (as: 'markdown' | 'html' | 'json') => {
		let text = '';
		let mimeType = '';
		let extension = '';

		switch (as) {
			case 'markdown':
				text = editor.getMarkdown();
				mimeType = 'text/markdown;charset=utf-8';
				extension = 'md';
				break;
			case 'html':
				text = editor.getHTML();
				mimeType = 'text/html;charset=utf-8';
				extension = 'html';
				break;
			case 'json':
				text = JSON.stringify(editor.getJSON(), null, 2);
				mimeType = 'application/json;charset=utf-8';
				extension = 'json';
				break;
		}

		// Try to find a title from the first heading, or use a default
		let filename = 'document';
		const firstNode = editor.state.doc.firstChild;
		if (firstNode && firstNode.type.name === 'heading') {
			const textContent = firstNode.textContent.trim();
			if (textContent) {
				filename = textContent
					.toLowerCase()
					.replace(/[^a-z0-9]+/g, '-')
					.replace(/(^-|-$)/g, '');
			}
		}
		if (!filename) {
			filename = 'document';
		}

		const blob = new Blob([text], { type: mimeType });
		const url = URL.createObjectURL(blob);
		const a = document.createElement('a');
		a.href = url;
		a.download = `${filename}.${extension}`;
		document.body.appendChild(a);
		a.click();
		document.body.removeChild(a);
		URL.revokeObjectURL(url);
	};
</script>

<DropdownMenu.Root>
	<DropdownMenu.Trigger class="btn btn-ghost btn-xs size-7 rounded-lg text-base-content/80 hover:text-base-content hover:bg-base-content/10 flex items-center justify-center gap-0.5 p-0">
		<Download class="size-3.5" />
		<ChevronDown class="size-2 text-base-content/60" />
	</DropdownMenu.Trigger>
	<DropdownMenu.Portal>
		<DropdownMenu.Content>
			<DropdownMenu.Label>Export As</DropdownMenu.Label>
			<DropdownMenu.Item onclick={() => handleExport('markdown')}>Markdown</DropdownMenu.Item>
			<DropdownMenu.Item onclick={() => handleExport('html')}>HTML</DropdownMenu.Item>
			<DropdownMenu.Item onclick={() => handleExport('json')}>JSON</DropdownMenu.Item>
		</DropdownMenu.Content>
	</DropdownMenu.Portal>
</DropdownMenu.Root>
