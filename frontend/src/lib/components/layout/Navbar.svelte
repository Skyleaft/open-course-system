<script lang="ts">
	import { authStore } from '#lib/stores/auth.svelte.ts';
	import { Sun, Moon, LogOut, User as UserIcon, BookOpen, GraduationCap, ShieldAlert, Sparkles, Menu } from '@lucide/svelte';

	const browser = typeof window !== 'undefined';

	interface Props {
		onToggleSidebar?: () => void;
	}

	let { onToggleSidebar }: Props = $props();

	let currentTheme = $state<'dark' | 'light'>('dark');

	$effect(() => {
		if (browser) {
			const saved = localStorage.getItem('theme') as 'dark' | 'light' || 'dark';
			currentTheme = saved;
			document.documentElement.setAttribute('data-theme', saved);
		}
	});

	function toggleTheme() {
		const next = currentTheme === 'dark' ? 'light' : 'dark';
		currentTheme = next;
		if (browser) {
			localStorage.setItem('theme', next);
			document.documentElement.setAttribute('data-theme', next);
		}
	}
</script>

<header class="glass-navbar sticky top-0 z-40 w-full border-b backdrop-blur-2xl">
	<div class="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
		<!-- Brand & Sidebar Toggle -->
		<div class="flex items-center gap-3">
			{#if onToggleSidebar}
				<button
					class="btn btn-ghost btn-circle btn-sm md:hidden"
					onclick={onToggleSidebar}
					aria-label="Toggle navigation"
				>
					<Menu class="h-5 w-5" />
				</button>
			{/if}

			<a href="/" class="group flex items-center gap-2.5 transition-transform duration-200 hover:scale-105">
				<div class="gradient-accent flex h-9 w-9 items-center justify-center rounded-xl shadow-md">
					<Sparkles class="h-5 w-5 text-white" />
				</div>
				<div class="flex flex-col">
					<span class="text-gradient font-bold tracking-tight text-base sm:text-lg">Open Course System</span>
					<span class="text-[10px] uppercase tracking-widest text-base-content/60 -mt-1 font-medium">LMS & Exams</span>
				</div>
			</a>
		</div>

		<!-- Center Navigation (Quick Links) -->
		<nav class="hidden md:flex items-center gap-1">
			{#if authStore.isAuthenticated}
				<a href="/my-courses" class="btn btn-ghost btn-sm rounded-lg font-medium text-sm hover:bg-base-100/40">
					<GraduationCap class="h-4 w-4 mr-1 text-primary opacity-90" />
					My Courses
				</a>
			{/if}
			<a href="/courses" class="btn btn-ghost btn-sm rounded-lg font-medium text-sm hover:bg-base-100/40">
				<BookOpen class="h-4 w-4 mr-1 opacity-70" />
				Catalog
			</a>
			{#if authStore.isAuthenticated}
				{#if authStore.isInstructor || authStore.isAdmin}
					<a href="/instructor/courses" class="btn btn-ghost btn-sm rounded-lg font-medium text-sm hover:bg-base-100/40">
						Instructor Studio
					</a>
				{/if}
				{#if authStore.isProctor || authStore.isAdmin}
					<a href="/proctor/exams" class="btn btn-ghost btn-sm rounded-lg font-medium text-sm text-warning hover:bg-warning/10">
						<ShieldAlert class="h-4 w-4 mr-1 opacity-70" />
						Proctor Console
					</a>
				{/if}
			{/if}
		</nav>

		<!-- Right Section (Theme Toggle & User Menu) -->
		<div class="flex items-center gap-2">
			<button
				class="btn btn-ghost btn-circle btn-sm text-base-content/80 hover:bg-base-100/50"
				onclick={toggleTheme}
				aria-label="Toggle theme"
			>
				{#if currentTheme === 'dark'}
					<Sun class="h-4 w-4 text-warning" />
				{:else}
					<Moon class="h-4 w-4 text-primary" />
				{/if}
			</button>

			{#if authStore.isAuthenticated && authStore.user}
				<div class="dropdown dropdown-end">
					<div tabindex="0" role="button" class="btn btn-ghost btn-sm gap-2 rounded-xl border border-white/10 bg-base-100/40 px-2 py-1">
						<div class="avatar placeholder">
							<div class="gradient-accent h-7 w-7 rounded-lg text-white font-bold text-xs flex items-center justify-center">
								<span>{(authStore.user.fullName || authStore.user.firstName || authStore.user.email || 'U').charAt(0).toUpperCase()}</span>
							</div>
						</div>
						<div class="hidden flex-col items-start text-left sm:flex">
							<span class="text-xs font-semibold leading-tight">{authStore.user.fullName || authStore.user.firstName || authStore.user.email}</span>
							<span class="text-[10px] text-base-content/60 leading-tight">
								{authStore.user.roles?.[0] || authStore.user.role || 'Student'}
							</span>
						</div>
					</div>
					<!-- Dropdown menu -->
					<ul
						tabindex="0"
						class="glass-panel dropdown-content menu z-50 mt-2 w-56 rounded-2xl border border-white/10 p-2 shadow-2xl backdrop-blur-2xl"
					>
						<li class="menu-title px-3 py-1.5 text-xs text-base-content/60">Account</li>
						<li>
							<a href="/dashboard" class="rounded-lg py-2 text-xs font-medium">
								<UserIcon class="h-4 w-4 opacity-70" />
								My Dashboard
							</a>
						</li>
						<li>
							<a href="/certificates" class="rounded-lg py-2 text-xs font-medium">
								<GraduationCap class="h-4 w-4 opacity-70" />
								My Certificates
							</a>
						</li>
						<div class="divider my-1 opacity-20"></div>
						<li>
							<button
								class="rounded-lg py-2 text-xs font-medium text-error hover:bg-error/10"
								onclick={() => authStore.logout()}
							>
								<LogOut class="h-4 w-4" />
								Sign Out
							</button>
						</li>
					</ul>
				</div>
			{:else}
				<div class="flex items-center gap-2">
					<a href="/login" class="btn btn-ghost btn-sm rounded-lg text-xs font-medium">Sign In</a>
					<a href="/register" class="btn btn-primary btn-sm gradient-accent rounded-lg text-xs font-semibold text-white border-0 shadow-md">
						Get Started
					</a>
				</div>
			{/if}
		</div>
	</div>
</header>
