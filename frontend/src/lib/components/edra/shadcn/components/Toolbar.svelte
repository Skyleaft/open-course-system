<script lang="ts">
	import { Button } from '$lib/components/ui/button/index.js';
	import { Separator } from '$lib/components/ui/separator/index.js';
	import { commands } from '../../commands/index.js';
	import { addAIHighlight, getEditor, useEditorTransaction } from '../../tiptap/index.js';
	import { cn } from '#lib/utils.js';
	import { WandSparkles } from '@lucide/svelte';
	import Colors from './tools/Colors.svelte';
	import Export from './tools/Export.svelte';
	import Tooltip from './Tooltip.svelte';
	interface Props {
		class?: string;
	}
	const { class: className }: Props = $props();

	const editor = getEditor();

	const transaction = useEditorTransaction(editor);
	const commandsKeys = Object.keys(commands);

	function useAI() {
		void transaction.version;
		return editor.extensionManager.extensions.some(
			(e) => e.name === 'ai-highlight' && e.options?.callAI != null
		);
	}

	function isActive(command: (typeof commands)[string][number]): boolean {
		void transaction.version;
		return command.isActive?.(editor) ?? false;
	}
	function isClickable(command: (typeof commands)[string][number]): boolean {
		void transaction.version;
		return command.clickable?.(editor) ?? true;
	}
</script>

<div class={cn('flex flex-wrap items-center gap-0.5 max-w-full', className)}>
	{#if useAI()}
		<Tooltip tooltip="Use AI">
			<button
				type="button"
				onmousedown={(e: MouseEvent) => {
					e.preventDefault();
					addAIHighlight(editor);
				}}
				class="btn btn-ghost btn-xs size-7 rounded-lg text-base-content/80 hover:text-base-content hover:bg-base-content/10 p-0 flex items-center justify-center"
			>
				<WandSparkles class="size-3.5" />
			</button>
		</Tooltip>
	{/if}
	{#each commandsKeys as key (key)}
		{@const group = commands[key]}
		<div class="flex items-center gap-0.5">
			{#each group as command, idx (idx)}
				{@const Icon = command.icon}
				{@const active = isActive(command)}
				<Tooltip tooltip={command.tooltip} shortCut={command.shortCut ?? ''}>
					<button
						type="button"
						class={cn(
							'btn btn-ghost btn-xs size-7 rounded-lg text-base-content/75 transition-all p-0 flex items-center justify-center',
							active
								? 'bg-primary/20 text-primary font-bold border border-primary/30 shadow-xs'
								: 'hover:text-base-content hover:bg-base-content/10'
						)}
						disabled={!isClickable(command)}
						onclick={() => {
							command.onClick?.(editor);
						}}
					>
						<Icon class="size-3.5" />
					</button>
				</Tooltip>
			{/each}
		</div>
		<div class="h-4 w-px bg-base-content/15 mx-0.5"></div>
	{/each}
	<Colors />
	<Export />
</div>
