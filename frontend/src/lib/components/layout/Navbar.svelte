<script lang="ts">
	import { authStore } from '#lib/stores/auth.svelte.ts';
	import { customizationStore } from '#lib/stores/customization.svelte.ts';
	import { page } from '$app/state';
	import {
		Sun,
		Moon,
		LogOut,
		User as UserIcon,
		BookOpen,
		GraduationCap,
		ShieldAlert,
		Sparkles,
		Menu,
		Sliders,
		Award,
		LayoutDashboard,
		BarChart3,
		Activity,
		ShieldCheck,
		Layers,
		CheckCircle2
	} from '@lucide/svelte';

	const browser = typeof window !== 'undefined';

	interface Props {
		onToggleSidebar?: () => void;
	}

	let { onToggleSidebar }: Props = $props();

	let currentTheme = $state<'dark' | 'light'>('dark');

	$effect(() => {
		if (browser) {
			const saved = (localStorage.getItem('theme') as 'dark' | 'light') || (customizationStore.data.theme.defaultTheme as 'dark' | 'light') || 'dark';
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

	const isExamSession = $derived(
		page.url.pathname.includes('/exams/') &&
		(page.url.pathname.endsWith('/start') || page.url.pathname.includes('/submissions/'))
	);

	function isActive(path: string): boolean {
		return page.url.pathname === path || (path !== '/' && page.url.pathname.startsWith(path));
	}
</script>

<header class="glass-navbar sticky top-0 z-40 w-full border-b border-white/10 bg-base-300/50 backdrop-blur-2xl transition-all">
	<div class="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
		<!-- Brand & Sidebar Toggle -->
		<div class="flex items-center gap-3">
			{#if onToggleSidebar && !isExamSession}
				<button
					class="btn btn-ghost btn-circle btn-sm md:hidden text-base-content/80"
					onclick={onToggleSidebar}
					aria-label="Toggle navigation"
				>
					<Menu class="h-5 w-5" />
				</button>
			{/if}

			<a href="/" class="group flex items-center gap-2.5 transition-transform duration-200 hover:scale-[1.02]">
				{#if currentTheme === 'dark' && customizationStore.data.branding.logoDarkUrl}
					<img src={customizationStore.data.branding.logoDarkUrl} alt={customizationStore.data.branding.siteName} class="h-9 w-auto object-contain rounded-lg" />
				{:else if currentTheme === 'light' && customizationStore.data.branding.logoLightUrl}
					<img src={customizationStore.data.branding.logoLightUrl} alt={customizationStore.data.branding.siteName} class="h-9 w-auto object-contain rounded-lg" />
				{:else}
					<div class="gradient-accent flex h-9 w-9 items-center justify-center rounded-xl shadow-md shadow-primary/20">
						<Sparkles class="h-5 w-5 text-white" />
					</div>
				{/if}
				<div class="flex flex-col">
					<span class="text-gradient font-extrabold tracking-tight text-base sm:text-lg">{customizationStore.data.branding.siteName}</span>
					<span class="text-[10px] uppercase tracking-widest text-base-content/50 -mt-1 font-semibold truncate max-w-[170px]">{customizationStore.data.branding.tagline}</span>
				</div>
			</a>
		</div>

		<!-- Center Navigation -->
		{#if isExamSession}
			<!-- Distraction-Free Proctored Mode Banner in Header -->
			<div class="hidden sm:flex items-center gap-2 rounded-full border border-warning/30 bg-warning/10 px-4 py-1 text-xs font-semibold text-warning shadow-sm">
				<ShieldCheck class="h-4 w-4 animate-pulse text-warning" />
				<span>Secure Proctored Examination</span>
			</div>
		{:else}
			<nav class="hidden md:flex items-center gap-1.5">
				<a
					href="/courses"
					class="btn btn-ghost btn-sm rounded-xl text-xs font-semibold transition-all {isActive('/courses')
						? 'bg-primary/15 text-primary border border-primary/20 shadow-sm'
						: 'text-base-content/80 hover:bg-base-100/50 hover:text-base-content'}"
				>
					<BookOpen class="h-4 w-4 mr-1 text-primary opacity-90" />
					Explore Catalog
				</a>

				{#if authStore.isAuthenticated}
					<a
						href="/dashboard"
						class="btn btn-ghost btn-sm rounded-xl text-xs font-semibold transition-all {isActive('/dashboard')
							? 'bg-primary/15 text-primary border border-primary/20 shadow-sm'
							: 'text-base-content/80 hover:bg-base-100/50 hover:text-base-content'}"
					>
						<LayoutDashboard class="h-4 w-4 mr-1 text-primary opacity-90" />
						Dashboard
					</a>

					<a
						href="/my-courses"
						class="btn btn-ghost btn-sm rounded-xl text-xs font-semibold transition-all {isActive('/my-courses')
							? 'bg-primary/15 text-primary border border-primary/20 shadow-sm'
							: 'text-base-content/80 hover:bg-base-100/50 hover:text-base-content'}"
					>
						<GraduationCap class="h-4 w-4 mr-1 text-primary opacity-90" />
						My Courses
					</a>

					{#if authStore.isInstructor || authStore.isAdmin}
						<a
							href="/instructor/analytics"
							class="btn btn-ghost btn-sm rounded-xl text-xs font-semibold transition-all {isActive('/instructor/analytics')
								? 'bg-secondary/15 text-secondary border border-secondary/20 shadow-sm'
								: 'text-base-content/80 hover:bg-base-100/50 hover:text-base-content'}"
						>
							<BarChart3 class="h-4 w-4 mr-1 text-secondary opacity-90" />
							Analytics Studio
						</a>
					{/if}

					{#if authStore.isAdmin}
						<a
							href="/admin/dashboard"
							class="btn btn-ghost btn-sm rounded-xl text-xs font-semibold transition-all {isActive('/admin/dashboard')
								? 'bg-primary/15 text-primary border border-primary/20 shadow-sm'
								: 'text-base-content/80 hover:bg-base-100/50 hover:text-base-content'}"
						>
							<Activity class="h-4 w-4 mr-1 text-primary opacity-90" />
							Observability Hub
						</a>
					{/if}
				{/if}
			</nav>
		{/if}

		<!-- Right Section (Theme Toggle & User Menu) -->
		<div class="flex items-center gap-2.5">
			<button
				class="btn btn-ghost btn-circle btn-sm text-base-content/80 hover:bg-base-100/50 transition-transform active:scale-95"
				onclick={toggleTheme}
				aria-label="Toggle theme"
				title="Toggle Dark / Light Theme"
			>
				{#if currentTheme === 'dark'}
					<Sun class="h-4 w-4 text-warning" />
				{:else}
					<Moon class="h-4 w-4 text-primary" />
				{/if}
			</button>

			{#if authStore.isAuthenticated && authStore.user}
				<div class="dropdown dropdown-end">
					<div
						tabindex="0"
						role="button"
						class="btn btn-ghost btn-sm gap-2.5 rounded-2xl border border-white/10 bg-base-100/40 px-2.5 py-1 hover:bg-base-100/70 transition-all shadow-sm"
					>
						<div class="avatar placeholder">
							<div class="gradient-accent h-7 w-7 rounded-xl text-white font-bold text-xs flex items-center justify-center shadow">
								<span>{(authStore.user.fullName || authStore.user.firstName || authStore.user.email || 'U').charAt(0).toUpperCase()}</span>
							</div>
						</div>
						<div class="hidden flex-col items-start text-left sm:flex">
							<span class="text-xs font-bold leading-tight text-base-content">{authStore.user.fullName || authStore.user.firstName || authStore.user.email}</span>
							<span class="badge badge-ghost badge-xs text-[9px] font-semibold text-primary px-1 -ml-0.5">
								{authStore.user.roles?.[0] || authStore.user.role || 'Student'}
							</span>
						</div>
					</div>

					<!-- Dropdown menu -->
					<ul
						tabindex="0"
						class="dropdown-content menu z-50 mt-2.5 w-64 rounded-3xl border border-base-content/10 bg-base-100 dark:bg-base-200 p-2.5 shadow-2xl shadow-black/25 text-base-content"
					>
						<!-- User Info Header -->
						<div class="px-3 py-2 border-b border-base-content/10 mb-1.5 space-y-0.5 bg-base-200/40 dark:bg-base-300/40 rounded-2xl">
							<div class="text-xs font-bold text-base-content truncate">{authStore.user.fullName || authStore.user.firstName || authStore.user.email}</div>
							<div class="text-[10px] text-base-content/60 truncate font-mono">{authStore.user.email}</div>
						</div>

						<li class="menu-title px-3 py-1 text-[10px] uppercase font-bold text-base-content/50">Personal Portal</li>
						<li>
							<a href="/dashboard" class="rounded-xl py-2 text-xs font-medium hover:bg-base-200 dark:hover:bg-base-300/60">
								<LayoutDashboard class="h-4 w-4 text-primary" />
								Student Dashboard
							</a>
						</li>
						<li>
							<a href="/my-courses" class="rounded-xl py-2 text-xs font-medium hover:bg-base-200 dark:hover:bg-base-300/60">
								<GraduationCap class="h-4 w-4 text-primary" />
								My Enrolled Courses
							</a>
						</li>
						<li>
							<a href="/certificates" class="rounded-xl py-2 text-xs font-medium hover:bg-base-200 dark:hover:bg-base-300/60">
								<Award class="h-4 w-4 text-accent" />
								Verified Certificates
							</a>
						</li>

						{#if authStore.isInstructor || authStore.isAdmin}
							<div class="divider my-1 opacity-10"></div>
							<li class="menu-title px-3 py-1 text-[10px] uppercase font-bold text-secondary/80">Instructor Studio</li>
							<li>
								<a href="/instructor/courses" class="rounded-xl py-2 text-xs font-medium hover:bg-base-200 dark:hover:bg-base-300/60">
									<Layers class="h-4 w-4 text-secondary" />
									Course Management
								</a>
							</li>
							<li>
								<a href="/instructor/analytics" class="rounded-xl py-2 text-xs font-medium hover:bg-base-200 dark:hover:bg-base-300/60">
									<BarChart3 class="h-4 w-4 text-secondary" />
									Analytics & Psychometrics
								</a>
							</li>
						{/if}

						{#if authStore.isProctor || authStore.isAdmin}
							<div class="divider my-1 opacity-10"></div>
							<li>
								<a href="/proctor/exams" class="rounded-xl py-2 text-xs font-medium text-warning hover:bg-warning/10">
									<ShieldCheck class="h-4 w-4 text-warning" />
									Proctor Live Monitor
								</a>
							</li>
						{/if}

						{#if authStore.isAdmin}
							<div class="divider my-1 opacity-10"></div>
							<li class="menu-title px-3 py-1 text-[10px] uppercase font-bold text-primary/80">Administration</li>
							<li>
								<a href="/admin/dashboard" class="rounded-xl py-2 text-xs font-medium text-primary hover:bg-primary/10">
									<Activity class="h-4 w-4 text-primary" />
									Admin Observability Hub
								</a>
							</li>
							<li>
								<a href="/instructor/settings" class="rounded-xl py-2 text-xs font-medium hover:bg-base-200 dark:hover:bg-base-300/60 text-primary">
									<Sliders class="h-4 w-4 text-primary" />
									Site Customization
								</a>
							</li>
						{/if}

						<div class="divider my-1.5 opacity-20"></div>
						<li>
							<button
								class="rounded-xl py-2 text-xs font-semibold text-error hover:bg-error/15 transition-colors"
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
					<a href="/login" class="btn btn-ghost btn-sm rounded-xl text-xs font-semibold text-base-content/80 hover:text-base-content">
						Sign In
					</a>
					<a
						href="/register"
						class="btn btn-primary btn-sm gradient-accent rounded-xl text-xs font-bold text-white border-0 shadow-md shadow-primary/25 hover:brightness-110"
					>
						Get Started
					</a>
				</div>
			{/if}
		</div>
	</div>
</header>
