<script lang="ts">
	import { Search, X } from '@lucide/svelte';

	interface Props {
		value?: string;
		placeholder?: string;
		onInput?: (value: string) => void;
		class?: string;
	}

	let {
		value = $bindable(''),
		placeholder = 'Search...',
		onInput,
		class: className = ''
	}: Props = $props();

	function handleClear() {
		value = '';
		if (onInput) onInput('');
	}

	function handleInput(e: Event) {
		const target = e.target as HTMLInputElement;
		value = target.value;
		if (onInput) onInput(target.value);
	}
</script>

<div class="relative w-full {className}">
	<div class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-base-content/50">
		<Search class="h-4 w-4" />
	</div>
	<input
		type="text"
		class="glass-input input input-sm h-10 w-full rounded-xl pl-9 pr-8 text-sm placeholder:text-base-content/40 focus:outline-none"
		{placeholder}
		{value}
		oninput={handleInput}
	/>
	{#if value}
		<button
			type="button"
			class="absolute inset-y-0 right-0 flex items-center pr-2.5 text-base-content/50 hover:text-base-content"
			onclick={handleClear}
			aria-label="Clear search"
		>
			<X class="h-4 w-4" />
		</button>
	{/if}
</div>
