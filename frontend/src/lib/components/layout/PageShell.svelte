<script lang="ts">
	import Navbar from './Navbar.svelte';
	import Sidebar from './Sidebar.svelte';
	import Footer from './Footer.svelte';
	import type { Snippet } from 'svelte';

	interface Props {
		children: Snippet;
		showSidebar?: boolean;
	}

	let { children, showSidebar = true }: Props = $props();
	let isSidebarOpen = $state(false);

	function toggleSidebar() {
		isSidebarOpen = !isSidebarOpen;
	}

	function closeSidebar() {
		isSidebarOpen = false;
	}
</script>

<div class="flex min-h-screen flex-col bg-base-300">
	<Navbar onToggleSidebar={showSidebar ? toggleSidebar : undefined} />

	<div class="flex flex-1">
		{#if showSidebar}
			<Sidebar isOpen={isSidebarOpen} onClose={closeSidebar} />
		{/if}

		<main class="flex-1 overflow-x-hidden p-4 sm:p-6 lg:p-8">
			<div class="mx-auto max-w-7xl">
				{@render children()}
			</div>
		</main>
	</div>

	<Footer />
</div>
