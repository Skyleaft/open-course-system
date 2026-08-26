<script lang="ts" module>
	import type { Component } from 'svelte';

	export interface TabItem<K extends string = string> {
		id: K;
		label: string;
		icon?: Component<any> | any;
		count?: number | string | null;
		disabled?: boolean;
	}
</script>

<script lang="ts" generics="T extends string = string">
	import { crossfade } from 'svelte/transition';
	import { cubicOut } from 'svelte/easing';

	interface Props {
		tabs: TabItem<T>[];
		active: T;
		onChange?: (tabId: T) => void;
		class?: string;
		size?: 'xs' | 'sm' | 'md';
	}

	let {
		tabs,
		active = $bindable(),
		onChange,
		class: className = '',
		size = 'sm'
	}: Props = $props();

	const [send, receive] = crossfade({
		duration: 250,
		easing: cubicOut,
		fallback(node) {
			const style = getComputedStyle(node);
			const transform = style.transform === 'none' ? '' : style.transform;

			return {
				duration: 200,
				easing: cubicOut,
				css: (t) => `
					transform: ${transform} scale(${t});
					opacity: ${t}
				`
			};
		}
	});

	function handleSelect(tab: TabItem<T>) {
		if (tab.disabled) return;
		active = tab.id;
		onChange?.(tab.id);
	}
</script>

<div
	class="inline-flex items-center gap-1 p-1 rounded-2xl bg-base-200/80 backdrop-blur-sm border border-base-content/10 shadow-xs max-w-full overflow-x-auto no-scrollbar {className}"
	role="tablist"
>
	{#each tabs as tab (tab.id)}
		{@const isActive = active === tab.id}
		{@const Icon = tab.icon}
		<button
			type="button"
			role="tab"
			aria-selected={isActive}
			disabled={tab.disabled}
			class="relative flex items-center justify-center {size === 'xs'
				? 'h-7 px-2.5 text-xs'
				: size === 'md'
					? 'h-10 px-4 text-sm'
					: 'h-8 px-3 text-xs'} rounded-xl font-bold transition-colors gap-2 duration-200 shrink-0 z-0 {isActive
				? 'text-primary-content'
				: 'text-base-content/70 hover:text-base-content hover:bg-base-300/40'} {tab.disabled
				? 'opacity-50 pointer-events-none'
				: ''}"
			onclick={() => handleSelect(tab)}
		>
			{#if isActive}
				<div
					in:receive={{ key: 'active-pill' }}
					out:send={{ key: 'active-pill' }}
					class="absolute inset-0 bg-primary rounded-xl shadow-xs -z-10"
				></div>
			{/if}

			{#if Icon}
				<Icon class="w-4 h-4 shrink-0 transition-transform duration-200 {isActive ? 'scale-105' : ''}" />
			{/if}
			<span class="whitespace-nowrap">{tab.label}</span>
			{#if tab.count !== undefined && tab.count !== null}
				<span
					class="badge badge-xs px-1.5 py-0.5 rounded-full font-mono text-[10px] transition-all duration-200 {isActive
						? 'bg-primary-content/20 text-primary-content border-transparent shadow-xs'
						: 'bg-base-content/10 text-base-content/70 border-transparent'}"
				>
					{tab.count}
				</span>
			{/if}
		</button>
	{/each}
</div>
