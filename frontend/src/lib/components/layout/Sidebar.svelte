<script lang="ts">
	import { authStore } from '#lib/stores/auth.svelte.ts';
	import { page } from '$app/state';
	import {
		LayoutDashboard,
		GraduationCap,
		Award,
		Megaphone,
		Layers,
		PlusCircle,
		ShieldCheck,
		FileCheck2,
		HelpCircle,
		Sliders,
		BarChart3,
		Activity,
		Sparkles,
		LogOut,
		ChevronRight,
		Compass
	} from '@lucide/svelte';

	interface Props {
		isOpen?: boolean;
		onClose?: () => void;
	}

	let { isOpen = true, onClose }: Props = $props();

	function isActive(path: string): boolean {
		if (path === '/dashboard') return page.url.pathname === '/dashboard';
		return page.url.pathname === path || (path !== '/' && page.url.pathname.startsWith(path));
	}
</script>

<!-- Mobile Overlay Backdrop -->
{#if isOpen}
	<!-- svelte-ignore a11y_no_static_element_interactions -->
	<div
		class="fixed inset-0 z-30 bg-black/60 backdrop-blur-xs md:hidden"
		onclick={onClose}
		onkeydown={(e) => e.key === 'Escape' && onClose?.()}
	></div>
{/if}

<aside
	class="glass-sidebar fixed bottom-0 top-16 z-30 flex w-68 flex-col border-r border-white/10 bg-base-300/80 backdrop-blur-2xl transition-transform duration-300 md:static md:translate-x-0 {isOpen
		? 'translate-x-0'
		: '-translate-x-full'}"
>
	<div class="flex-1 overflow-y-auto px-3.5 py-5 space-y-6">
		{#if authStore.isAuthenticated}
			<!-- SECTION 1: Student Learning Hub -->
			<div class="space-y-1.5">
				<div class="flex items-center gap-2 px-2.5 pb-1">
					<span class="h-1.5 w-1.5 rounded-full bg-primary animate-pulse"></span>
					<span class="text-[10px] font-extrabold uppercase tracking-wider text-base-content/50">Learning Portal</span>
				</div>

				<ul class="space-y-1">
					<li>
						<a
							href="/dashboard"
							class="group flex items-center justify-between rounded-2xl px-3 py-2.5 text-xs font-semibold transition-all duration-200 {isActive('/dashboard')
								? 'bg-primary/15 text-primary shadow-sm border border-primary/20 font-bold'
								: 'text-base-content/75 hover:bg-base-100/60 hover:text-base-content hover:translate-x-0.5'}"
						>
							<div class="flex items-center gap-2.5">
								<div class="flex h-7 w-7 items-center justify-center rounded-xl transition-colors {isActive('/dashboard') ? 'bg-primary text-white shadow-xs' : 'bg-base-200/60 text-base-content/70 group-hover:bg-primary/10 group-hover:text-primary'}">
									<LayoutDashboard class="h-4 w-4" />
								</div>
								<span>Dashboard</span>
							</div>
							{#if isActive('/dashboard')}
								<ChevronRight class="h-3.5 w-3.5 opacity-60" />
							{/if}
						</a>
					</li>

					<li>
						<a
							href="/my-courses"
							class="group flex items-center justify-between rounded-2xl px-3 py-2.5 text-xs font-semibold transition-all duration-200 {isActive('/my-courses')
								? 'bg-primary/15 text-primary shadow-sm border border-primary/20 font-bold'
								: 'text-base-content/75 hover:bg-base-100/60 hover:text-base-content hover:translate-x-0.5'}"
						>
							<div class="flex items-center gap-2.5">
								<div class="flex h-7 w-7 items-center justify-center rounded-xl transition-colors {isActive('/my-courses') ? 'bg-primary text-white shadow-xs' : 'bg-base-200/60 text-base-content/70 group-hover:bg-primary/10 group-hover:text-primary'}">
									<GraduationCap class="h-4 w-4" />
								</div>
								<span>My Courses</span>
							</div>
							{#if isActive('/my-courses')}
								<ChevronRight class="h-3.5 w-3.5 opacity-60" />
							{/if}
						</a>
					</li>

					<li>
						<a
							href="/certificates"
							class="group flex items-center justify-between rounded-2xl px-3 py-2.5 text-xs font-semibold transition-all duration-200 {isActive('/certificates')
								? 'bg-primary/15 text-primary shadow-sm border border-primary/20 font-bold'
								: 'text-base-content/75 hover:bg-base-100/60 hover:text-base-content hover:translate-x-0.5'}"
						>
							<div class="flex items-center gap-2.5">
								<div class="flex h-7 w-7 items-center justify-center rounded-xl transition-colors {isActive('/certificates') ? 'bg-primary text-white shadow-xs' : 'bg-base-200/60 text-base-content/70 group-hover:bg-primary/10 group-hover:text-primary'}">
									<Award class="h-4 w-4" />
								</div>
								<span>Certificates</span>
							</div>
							{#if isActive('/certificates')}
								<ChevronRight class="h-3.5 w-3.5 opacity-60" />
							{/if}
						</a>
					</li>

					<li>
						<a
							href="/announcements"
							class="group flex items-center justify-between rounded-2xl px-3 py-2.5 text-xs font-semibold transition-all duration-200 {isActive('/announcements')
								? 'bg-primary/15 text-primary shadow-sm border border-primary/20 font-bold'
								: 'text-base-content/75 hover:bg-base-100/60 hover:text-base-content hover:translate-x-0.5'}"
						>
							<div class="flex items-center gap-2.5">
								<div class="flex h-7 w-7 items-center justify-center rounded-xl transition-colors {isActive('/announcements') ? 'bg-primary text-white shadow-xs' : 'bg-base-200/60 text-base-content/70 group-hover:bg-primary/10 group-hover:text-primary'}">
									<Megaphone class="h-4 w-4" />
								</div>
								<span>Announcements</span>
							</div>
							{#if isActive('/announcements')}
								<ChevronRight class="h-3.5 w-3.5 opacity-60" />
							{/if}
						</a>
					</li>
				</ul>
			</div>

			<!-- SECTION 2: Instructor Studio -->
			{#if authStore.isInstructor || authStore.isAdmin}
				<div class="space-y-1.5">
					<div class="flex items-center justify-between px-2.5 pb-1">
						<div class="flex items-center gap-2">
							<span class="h-1.5 w-1.5 rounded-full bg-secondary"></span>
							<span class="text-[10px] font-extrabold uppercase tracking-wider text-secondary/80">Instructor Studio</span>
						</div>
						<span class="badge badge-secondary badge-xs text-[9px] font-bold">Author</span>
					</div>

					<ul class="space-y-1">
						<li>
							<a
								href="/instructor/courses"
								class="group flex items-center justify-between rounded-2xl px-3 py-2 text-xs font-semibold transition-all duration-200 {isActive('/instructor/courses')
									? 'bg-secondary/15 text-secondary shadow-sm border border-secondary/20 font-bold'
									: 'text-base-content/75 hover:bg-base-100/60 hover:text-base-content hover:translate-x-0.5'}"
							>
								<div class="flex items-center gap-2.5">
									<div class="flex h-7 w-7 items-center justify-center rounded-xl transition-colors {isActive('/instructor/courses') ? 'bg-secondary text-white shadow-xs' : 'bg-base-200/60 text-base-content/70 group-hover:bg-secondary/10 group-hover:text-secondary'}">
										<Layers class="h-4 w-4" />
									</div>
									<span>Course Manager</span>
								</div>
								{#if isActive('/instructor/courses')}
									<ChevronRight class="h-3.5 w-3.5 opacity-60" />
								{/if}
							</a>
						</li>

						<li>
							<a
								href="/instructor/exams"
								class="group flex items-center justify-between rounded-2xl px-3 py-2 text-xs font-semibold transition-all duration-200 {isActive('/instructor/exams')
									? 'bg-secondary/15 text-secondary shadow-sm border border-secondary/20 font-bold'
									: 'text-base-content/75 hover:bg-base-100/60 hover:text-base-content hover:translate-x-0.5'}"
							>
								<div class="flex items-center gap-2.5">
									<div class="flex h-7 w-7 items-center justify-center rounded-xl transition-colors {isActive('/instructor/exams') ? 'bg-secondary text-white shadow-xs' : 'bg-base-200/60 text-base-content/70 group-hover:bg-secondary/10 group-hover:text-secondary'}">
										<FileCheck2 class="h-4 w-4" />
									</div>
									<span>Exam Authoring</span>
								</div>
								{#if isActive('/instructor/exams')}
									<ChevronRight class="h-3.5 w-3.5 opacity-60" />
								{/if}
							</a>
						</li>

						<li>
							<a
								href="/instructor/questions"
								class="group flex items-center justify-between rounded-2xl px-3 py-2 text-xs font-semibold transition-all duration-200 {isActive('/instructor/questions')
									? 'bg-secondary/15 text-secondary shadow-sm border border-secondary/20 font-bold'
									: 'text-base-content/75 hover:bg-base-100/60 hover:text-base-content hover:translate-x-0.5'}"
							>
								<div class="flex items-center gap-2.5">
									<div class="flex h-7 w-7 items-center justify-center rounded-xl transition-colors {isActive('/instructor/questions') ? 'bg-secondary text-white shadow-xs' : 'bg-base-200/60 text-base-content/70 group-hover:bg-secondary/10 group-hover:text-secondary'}">
										<HelpCircle class="h-4 w-4" />
									</div>
									<span>Question Banks</span>
								</div>
								{#if isActive('/instructor/questions')}
									<ChevronRight class="h-3.5 w-3.5 opacity-60" />
								{/if}
							</a>
						</li>

						<li>
							<a
								href="/instructor/analytics"
								class="group flex items-center justify-between rounded-2xl px-3 py-2 text-xs font-semibold transition-all duration-200 {isActive('/instructor/analytics')
									? 'bg-secondary/15 text-secondary shadow-sm border border-secondary/20 font-bold'
									: 'text-base-content/75 hover:bg-base-100/60 hover:text-base-content hover:translate-x-0.5'}"
							>
								<div class="flex items-center gap-2.5">
									<div class="flex h-7 w-7 items-center justify-center rounded-xl transition-colors {isActive('/instructor/analytics') ? 'bg-secondary text-white shadow-xs' : 'bg-base-200/60 text-base-content/70 group-hover:bg-secondary/10 group-hover:text-secondary'}">
										<BarChart3 class="h-4 w-4" />
									</div>
									<span>Analytics Studio</span>
								</div>
								{#if isActive('/instructor/analytics')}
									<ChevronRight class="h-3.5 w-3.5 opacity-60" />
								{/if}
							</a>
						</li>
					</ul>
				</div>
			{/if}

			<!-- SECTION 3: Supervision & Proctoring -->
			{#if authStore.isProctor || authStore.isAdmin}
				<div class="space-y-1.5">
					<div class="flex items-center justify-between px-2.5 pb-1">
						<div class="flex items-center gap-2">
							<span class="h-1.5 w-1.5 rounded-full bg-warning animate-ping"></span>
							<span class="text-[10px] font-extrabold uppercase tracking-wider text-warning/90">Supervision</span>
						</div>
						<span class="badge badge-warning badge-xs text-[9px] font-bold">Live</span>
					</div>

					<ul class="space-y-1">
						<li>
							<a
								href="/proctor/exams"
								class="group flex items-center justify-between rounded-2xl px-3 py-2.5 text-xs font-semibold transition-all duration-200 {isActive('/proctor/exams')
									? 'bg-warning/15 text-warning shadow-sm border border-warning/20 font-bold'
									: 'text-base-content/75 hover:bg-base-100/60 hover:text-base-content hover:translate-x-0.5'}"
							>
								<div class="flex items-center gap-2.5">
									<div class="flex h-7 w-7 items-center justify-center rounded-xl transition-colors {isActive('/proctor/exams') ? 'bg-warning text-warning-content shadow-xs' : 'bg-base-200/60 text-base-content/70 group-hover:bg-warning/10 group-hover:text-warning'}">
										<ShieldCheck class="h-4 w-4" />
									</div>
									<span>Proctor Monitor</span>
								</div>
								{#if isActive('/proctor/exams')}
									<ChevronRight class="h-3.5 w-3.5 opacity-60" />
								{/if}
							</a>
						</li>
					</ul>
				</div>
			{/if}

			<!-- SECTION 4: Administration -->
			{#if authStore.isAdmin}
				<div class="space-y-1.5">
					<div class="flex items-center justify-between px-2.5 pb-1">
						<div class="flex items-center gap-2">
							<span class="h-1.5 w-1.5 rounded-full bg-primary"></span>
							<span class="text-[10px] font-extrabold uppercase tracking-wider text-primary/90">Administration</span>
						</div>
						<span class="badge badge-primary badge-xs text-[9px] font-bold">Admin</span>
					</div>

					<ul class="space-y-1">
						<li>
							<a
								href="/admin/dashboard"
								class="group flex items-center justify-between rounded-2xl px-3 py-2.5 text-xs font-semibold transition-all duration-200 {isActive('/admin/dashboard')
									? 'bg-primary/15 text-primary shadow-sm border border-primary/20 font-bold'
									: 'text-base-content/75 hover:bg-base-100/60 hover:text-base-content hover:translate-x-0.5'}"
							>
								<div class="flex items-center gap-2.5">
									<div class="flex h-7 w-7 items-center justify-center rounded-xl transition-colors {isActive('/admin/dashboard') ? 'bg-primary text-white shadow-xs' : 'bg-base-200/60 text-base-content/70 group-hover:bg-primary/10 group-hover:text-primary'}">
										<Activity class="h-4 w-4" />
									</div>
									<span>Observability Hub</span>
								</div>
								{#if isActive('/admin/dashboard')}
									<ChevronRight class="h-3.5 w-3.5 opacity-60" />
								{/if}
							</a>
						</li>

						<li>
							<a
								href="/instructor/settings"
								class="group flex items-center justify-between rounded-2xl px-3 py-2.5 text-xs font-semibold transition-all duration-200 {isActive('/instructor/settings')
									? 'bg-primary/15 text-primary shadow-sm border border-primary/20 font-bold'
									: 'text-base-content/75 hover:bg-base-100/60 hover:text-base-content hover:translate-x-0.5'}"
							>
								<div class="flex items-center gap-2.5">
									<div class="flex h-7 w-7 items-center justify-center rounded-xl transition-colors {isActive('/instructor/settings') ? 'bg-primary text-white shadow-xs' : 'bg-base-200/60 text-base-content/70 group-hover:bg-primary/10 group-hover:text-primary'}">
										<Sliders class="h-4 w-4" />
									</div>
									<span>Site Customization</span>
								</div>
								{#if isActive('/instructor/settings')}
									<ChevronRight class="h-3.5 w-3.5 opacity-60" />
								{/if}
							</a>
						</li>
					</ul>
				</div>
			{/if}
		{/if}
	</div>

	<!-- Sidebar Footer User Card -->
	{#if authStore.isAuthenticated && authStore.user}
		<div class="p-3 border-t border-white/10 bg-base-200/40 backdrop-blur-md">
			<div class="flex items-center justify-between gap-2 p-2 rounded-2xl bg-base-100/50 border border-white/5">
				<div class="flex items-center gap-2.5 min-w-0">
					<div class="avatar placeholder shrink-0">
						<div class="gradient-accent h-8 w-8 rounded-xl text-white font-bold text-xs flex items-center justify-center shadow-xs">
							<span>{(authStore.user.fullName || authStore.user.firstName || authStore.user.email || 'U').charAt(0).toUpperCase()}</span>
						</div>
					</div>
					<div class="overflow-hidden space-y-0.5">
						<div class="text-xs font-bold text-base-content truncate leading-tight">
							{authStore.user.fullName || authStore.user.firstName || authStore.user.email}
						</div>
						<div class="text-[10px] font-semibold text-primary truncate leading-tight">
							{authStore.user.roles?.[0] || authStore.user.role || 'Student'}
						</div>
					</div>
				</div>

				<button
					class="btn btn-ghost btn-circle btn-xs text-base-content/50 hover:text-error hover:bg-error/10 shrink-0 transition-colors"
					onclick={() => authStore.logout()}
					title="Sign Out"
					aria-label="Sign Out"
				>
					<LogOut class="h-3.5 w-3.5" />
				</button>
			</div>
		</div>
	{/if}
</aside>
