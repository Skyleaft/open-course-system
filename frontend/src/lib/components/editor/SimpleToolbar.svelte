<script lang="ts">
	import { getEditor, useEditorTransaction } from '$lib/components/edra/tiptap/index.js';
	import {
		Bold,
		Italic,
		Underline,
		Strikethrough,
		Code,
		FileCode,
		Quote,
		List,
		ListOrdered,
		Link2,
		Undo2,
		Redo2
	} from '@lucide/svelte';
	import Tooltip from '$lib/components/edra/shadcn/components/Tooltip.svelte';
	import { cn } from '#lib/utils.js';

	interface Props {
		class?: string;
	}

	const { class: className }: Props = $props();

	const editor = getEditor();
	const transaction = useEditorTransaction(editor);

	const isMac = typeof navigator !== 'undefined' && /Mac|iPod|iPhone|iPad/.test(navigator.platform);
	const modKey = isMac ? '⌘' : 'Ctrl+';

	function isActive(name: string, attrs?: Record<string, any>): boolean {
		void transaction.version;
		return editor.isActive(name, attrs);
	}

	function canUndo(): boolean {
		void transaction.version;
		return editor.can().undo();
	}

	function canRedo(): boolean {
		void transaction.version;
		return editor.can().redo();
	}

	function handleLink() {
		if (editor.isActive('link')) {
			editor.chain().focus().unsetLink().run();
		} else {
			const url = window.prompt('Enter URL:');
			if (url && url.trim()) {
				editor.chain().focus().toggleLink({ href: url.trim() }).run();
			}
		}
	}
</script>

<div class={cn('flex flex-wrap items-center gap-1 max-w-full text-xs', className)}>
	<!-- Undo / Redo -->
	<div class="flex items-center gap-0.5">
		<Tooltip tooltip="Undo" shortCut={`${modKey}Z`}>
			<button
				type="button"
				class="btn btn-ghost btn-xs size-7 rounded-lg text-base-content/70 hover:text-base-content hover:bg-base-content/10 p-0 flex items-center justify-center disabled:opacity-30"
				disabled={!canUndo()}
				onclick={() => editor.chain().focus().undo().run()}
			>
				<Undo2 class="size-3.5" />
			</button>
		</Tooltip>
		<Tooltip tooltip="Redo" shortCut={`${modKey}Y`}>
			<button
				type="button"
				class="btn btn-ghost btn-xs size-7 rounded-lg text-base-content/70 hover:text-base-content hover:bg-base-content/10 p-0 flex items-center justify-center disabled:opacity-30"
				disabled={!canRedo()}
				onclick={() => editor.chain().focus().redo().run()}
			>
				<Redo2 class="size-3.5" />
			</button>
		</Tooltip>
	</div>

	<div class="h-4 w-px bg-base-content/15 mx-0.5"></div>

	<!-- Text Styling -->
	<div class="flex items-center gap-0.5">
		<Tooltip tooltip="Bold" shortCut={`${modKey}B`}>
			<button
				type="button"
				class={cn(
					'btn btn-ghost btn-xs size-7 rounded-lg text-base-content/70 transition-all p-0 flex items-center justify-center',
					isActive('bold')
						? 'bg-primary/20 text-primary font-bold border border-primary/30 shadow-xs'
						: 'hover:text-base-content hover:bg-base-content/10'
				)}
				onclick={() => editor.chain().focus().toggleBold().run()}
			>
				<Bold class="size-3.5" />
			</button>
		</Tooltip>

		<Tooltip tooltip="Italic" shortCut={`${modKey}I`}>
			<button
				type="button"
				class={cn(
					'btn btn-ghost btn-xs size-7 rounded-lg text-base-content/70 transition-all p-0 flex items-center justify-center',
					isActive('italic')
						? 'bg-primary/20 text-primary font-bold border border-primary/30 shadow-xs'
						: 'hover:text-base-content hover:bg-base-content/10'
				)}
				onclick={() => editor.chain().focus().toggleItalic().run()}
			>
				<Italic class="size-3.5" />
			</button>
		</Tooltip>

		<Tooltip tooltip="Underline" shortCut={`${modKey}U`}>
			<button
				type="button"
				class={cn(
					'btn btn-ghost btn-xs size-7 rounded-lg text-base-content/70 transition-all p-0 flex items-center justify-center',
					isActive('underline')
						? 'bg-primary/20 text-primary font-bold border border-primary/30 shadow-xs'
						: 'hover:text-base-content hover:bg-base-content/10'
				)}
				onclick={() => editor.chain().focus().toggleUnderline().run()}
			>
				<Underline class="size-3.5" />
			</button>
		</Tooltip>

		<Tooltip tooltip="Strikethrough" shortCut={`${modKey}Shift+S`}>
			<button
				type="button"
				class={cn(
					'btn btn-ghost btn-xs size-7 rounded-lg text-base-content/70 transition-all p-0 flex items-center justify-center',
					isActive('strike')
						? 'bg-primary/20 text-primary font-bold border border-primary/30 shadow-xs'
						: 'hover:text-base-content hover:bg-base-content/10'
				)}
				onclick={() => editor.chain().focus().toggleStrike().run()}
			>
				<Strikethrough class="size-3.5" />
			</button>
		</Tooltip>
	</div>

	<div class="h-4 w-px bg-base-content/15 mx-0.5"></div>

	<!-- Code & Quote -->
	<div class="flex items-center gap-0.5">
		<Tooltip tooltip="Inline Code" shortCut={`${modKey}E`}>
			<button
				type="button"
				class={cn(
					'btn btn-ghost btn-xs size-7 rounded-lg text-base-content/70 transition-all p-0 flex items-center justify-center',
					isActive('code')
						? 'bg-primary/20 text-primary font-bold border border-primary/30 shadow-xs'
						: 'hover:text-base-content hover:bg-base-content/10'
				)}
				onclick={() => editor.chain().focus().toggleCode().run()}
			>
				<Code class="size-3.5" />
			</button>
		</Tooltip>

		<Tooltip tooltip="Code Block" shortCut={`${modKey}Alt+C`}>
			<button
				type="button"
				class={cn(
					'btn btn-ghost btn-xs size-7 rounded-lg text-base-content/70 transition-all p-0 flex items-center justify-center',
					isActive('codeBlock')
						? 'bg-primary/20 text-primary font-bold border border-primary/30 shadow-xs'
						: 'hover:text-base-content hover:bg-base-content/10'
				)}
				onclick={() => editor.chain().focus().toggleCodeBlock().run()}
			>
				<FileCode class="size-3.5" />
			</button>
		</Tooltip>

		<Tooltip tooltip="Blockquote" shortCut={`${modKey}Shift+B`}>
			<button
				type="button"
				class={cn(
					'btn btn-ghost btn-xs size-7 rounded-lg text-base-content/70 transition-all p-0 flex items-center justify-center',
					isActive('blockquote')
						? 'bg-primary/20 text-primary font-bold border border-primary/30 shadow-xs'
						: 'hover:text-base-content hover:bg-base-content/10'
				)}
				onclick={() => editor.chain().focus().toggleBlockquote().run()}
			>
				<Quote class="size-3.5" />
			</button>
		</Tooltip>
	</div>

	<div class="h-4 w-px bg-base-content/15 mx-0.5"></div>

	<!-- Lists -->
	<div class="flex items-center gap-0.5">
		<Tooltip tooltip="Bullet List" shortCut={`${modKey}Shift+8`}>
			<button
				type="button"
				class={cn(
					'btn btn-ghost btn-xs size-7 rounded-lg text-base-content/70 transition-all p-0 flex items-center justify-center',
					isActive('bulletList')
						? 'bg-primary/20 text-primary font-bold border border-primary/30 shadow-xs'
						: 'hover:text-base-content hover:bg-base-content/10'
				)}
				onclick={() => editor.chain().focus().toggleBulletList().run()}
			>
				<List class="size-3.5" />
			</button>
		</Tooltip>

		<Tooltip tooltip="Numbered List" shortCut={`${modKey}Shift+7`}>
			<button
				type="button"
				class={cn(
					'btn btn-ghost btn-xs size-7 rounded-lg text-base-content/70 transition-all p-0 flex items-center justify-center',
					isActive('orderedList')
						? 'bg-primary/20 text-primary font-bold border border-primary/30 shadow-xs'
						: 'hover:text-base-content hover:bg-base-content/10'
				)}
				onclick={() => editor.chain().focus().toggleOrderedList().run()}
			>
				<ListOrdered class="size-3.5" />
			</button>
		</Tooltip>
	</div>

	<div class="h-4 w-px bg-base-content/15 mx-0.5"></div>

	<!-- Link -->
	<div class="flex items-center gap-0.5">
		<Tooltip tooltip="Insert / Edit Link">
			<button
				type="button"
				class={cn(
					'btn btn-ghost btn-xs size-7 rounded-lg text-base-content/70 transition-all p-0 flex items-center justify-center',
					isActive('link')
						? 'bg-primary/20 text-primary font-bold border border-primary/30 shadow-xs'
						: 'hover:text-base-content hover:bg-base-content/10'
				)}
				onclick={handleLink}
			>
				<Link2 class="size-3.5" />
			</button>
		</Tooltip>
	</div>
</div>
