<script lang="ts">
	import { authStore } from '#lib/stores/auth.svelte.ts';
	import { page } from '$app/state';
	import {
		LayoutDashboard,
		BookOpen,
		GraduationCap,
		Award,
		MessageSquare,
		Megaphone,
		Layers,
		PlusCircle,
		ShieldCheck,
		FileCheck2,
		HelpCircle,
		Sliders,
		BarChart3,
		Activity
	} from '@lucide/svelte';

	interface Props {
		isOpen?: boolean;
		onClose?: () => void;
	}

	let { isOpen = true, onClose }: Props = $props();

	function isActive(path: string): boolean {
		return page.url.pathname === path || (path !== '/' && page.url.pathname.startsWith(path));
	}
</script>

<!-- Mobile Overlay Backdrop -->
{#if isOpen}
	<div
		class="fixed inset-0 z-30 bg-black/50 backdrop-blur-xs md:hidden"
		onclick={onClose}
		role="presentation"
	></div>
{/if}

<aside
	class="glass-sidebar fixed bottom-0 top-16 z-30 flex w-64 flex-col border-r backdrop-blur-2xl transition-transform duration-300 md:static md:translate-x-0 {isOpen
		? 'translate-x-0'
		: '-translate-x-full'}"
>
	<div class="flex-1 overflow-y-auto px-4 py-6 space-y-6">
		{#if authStore.isAuthenticated}
			<!-- Student Section -->
			<div>
				<div class="px-3 mb-2 text-[10px] font-bold uppercase tracking-wider text-base-content/50">Learning Portal</div>
				<ul class="space-y-1">
					<li>
						<a
							href="/dashboard"
							class="flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors {isActive('/dashboard')
								? 'bg-primary/15 text-primary font-semibold border border-primary/20'
								: 'text-base-content/80 hover:bg-base-100/40 hover:text-base-content'}"
						>
							<LayoutDashboard class="h-4 w-4" />
							Dashboard
						</a>
					</li>
					<li>
						<a
							href="/my-courses"
							class="flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors {isActive('/my-courses')
								? 'bg-primary/15 text-primary font-semibold border border-primary/20'
								: 'text-base-content/80 hover:bg-base-100/40 hover:text-base-content'}"
						>
							<GraduationCap class="h-4 w-4 text-primary" />
							My Courses
						</a>
					</li>
					<li>
						<a
							href="/courses"
							class="flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors {isActive('/courses')
								? 'bg-primary/15 text-primary font-semibold border border-primary/20'
								: 'text-base-content/80 hover:bg-base-100/40 hover:text-base-content'}"
						>
							<BookOpen class="h-4 w-4" />
							Course Catalog
						</a>
					</li>
					<li>
						<a
							href="/certificates"
							class="flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors {isActive('/certificates')
								? 'bg-primary/15 text-primary font-semibold border border-primary/20'
								: 'text-base-content/80 hover:bg-base-100/40 hover:text-base-content'}"
						>
							<Award class="h-4 w-4" />
							My Certificates
						</a>
					</li>
					<li>
						<a
							href="/announcements"
							class="flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors {isActive('/announcements')
								? 'bg-primary/15 text-primary font-semibold border border-primary/20'
								: 'text-base-content/80 hover:bg-base-100/40 hover:text-base-content'}"
						>
							<Megaphone class="h-4 w-4" />
							Announcements
						</a>
					</li>
				</ul>
			</div>

			<!-- Instructor Studio -->
			{#if authStore.isInstructor || authStore.isAdmin}
				<div>
					<div class="px-3 mb-2 text-[10px] font-bold uppercase tracking-wider text-secondary/70">Instructor Studio</div>
					<ul class="space-y-1">
						<li>
							<a
								href="/instructor/courses"
								class="flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors {isActive('/instructor/courses')
									? 'bg-secondary/15 text-secondary font-semibold border border-secondary/20'
									: 'text-base-content/80 hover:bg-base-100/40 hover:text-base-content'}"
							>
								<Layers class="h-4 w-4" />
								Manage Courses
							</a>
						</li>
						<li>
							<a
								href="/instructor/courses/create"
								class="flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors {isActive('/instructor/courses/create')
									? 'bg-secondary/15 text-secondary font-semibold border border-secondary/20'
									: 'text-base-content/80 hover:bg-base-100/40 hover:text-base-content'}"
							>
								<PlusCircle class="h-4 w-4" />
								Create Course
							</a>
						</li>
						<li>
							<a
								href="/instructor/exams"
								class="flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors {isActive('/instructor/exams')
									? 'bg-secondary/15 text-secondary font-semibold border border-secondary/20'
									: 'text-base-content/80 hover:bg-base-100/40 hover:text-base-content'}"
							>
								<FileCheck2 class="h-4 w-4" />
								Exam Authoring
							</a>
						</li>
						<li>
							<a
								href="/instructor/questions"
								class="flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors {isActive('/instructor/questions')
									? 'bg-secondary/15 text-secondary font-semibold border border-secondary/20'
									: 'text-base-content/80 hover:bg-base-100/40 hover:text-base-content'}"
							>
								<HelpCircle class="h-4 w-4" />
								Question Banks
							</a>
						</li>
						<li>
							<a
								href="/instructor/analytics"
								class="flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors {isActive('/instructor/analytics')
									? 'bg-secondary/15 text-secondary font-semibold border border-secondary/20'
									: 'text-base-content/80 hover:bg-base-100/40 hover:text-base-content'}"
							>
								<BarChart3 class="h-4 w-4" />
								Analytics Studio
							</a>
						</li>
						<li>
							<a
								href="/instructor/settings"
								class="flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors {isActive('/instructor/settings')
									? 'bg-primary/15 text-primary font-semibold border border-primary/20'
									: 'text-base-content/80 hover:bg-base-100/40 hover:text-base-content'}"
							>
								<Sliders class="h-4 w-4 text-primary" />
								Site Customization
							</a>
						</li>
					</ul>
				</div>
			{/if}

			<!-- Proctor Live Console -->
			{#if authStore.isProctor || authStore.isAdmin}
				<div>
					<div class="px-3 mb-2 text-[10px] font-bold uppercase tracking-wider text-warning/70">Exam Supervision</div>
					<ul class="space-y-1">
						<li>
							<a
								href="/proctor/exams"
								class="flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors {isActive('/proctor/exams')
									? 'bg-warning/15 text-warning font-semibold border border-warning/20'
									: 'text-base-content/80 hover:bg-base-100/40 hover:text-base-content'}"
							>
								<ShieldCheck class="h-4 w-4" />
								Live Examination Monitor
							</a>
						</li>
					</ul>
				</div>
			{/if}

			<!-- Admin Console -->
			{#if authStore.isAdmin}
				<div>
					<div class="px-3 mb-2 text-[10px] font-bold uppercase tracking-wider text-primary/70">Administration</div>
					<ul class="space-y-1">
						<li>
							<a
								href="/admin/dashboard"
								class="flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors {isActive('/admin/dashboard')
									? 'bg-primary/15 text-primary font-semibold border border-primary/20'
									: 'text-base-content/80 hover:bg-base-100/40 hover:text-base-content'}"
							>
								<Activity class="h-4 w-4 text-primary" />
								Observability Hub
							</a>
						</li>
					</ul>
				</div>
			{/if}
		{:else}
			<!-- Unauthenticated Visitor Navigation -->
			<div>
				<div class="px-3 mb-2 text-[10px] font-bold uppercase tracking-wider text-base-content/50">Explore</div>
				<ul class="space-y-1">
					<li>
						<a
							href="/courses"
							class="flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors {isActive('/courses')
								? 'bg-primary/15 text-primary font-semibold border border-primary/20'
								: 'text-base-content/80 hover:bg-base-100/40 hover:text-base-content'}"
						>
							<BookOpen class="h-4 w-4" />
							Course Catalog
						</a>
					</li>
				</ul>
			</div>

			<!-- Sign In CTA Box -->
			<div class="glass-card rounded-2xl p-4 border border-white/10 space-y-3">
				<div class="space-y-1">
					<div class="text-xs font-bold text-base-content">Student & Instructor Portal</div>
					<p class="text-[11px] text-base-content/60 leading-relaxed">
						Sign in to access your learning dashboard, active courses, and live examination sessions.
					</p>
				</div>
				<a
					href="/login"
					class="btn btn-primary gradient-accent btn-sm w-full rounded-xl font-semibold text-white border-0 shadow-md"
				>
					Sign In
				</a>
			</div>
		{/if}
	</div>
</aside>
