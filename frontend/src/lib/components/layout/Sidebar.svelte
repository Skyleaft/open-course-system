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
		FileCheck2
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
		<!-- Student Section -->
		<div>
			<div class="px-3 mb-2 text-[10px] font-bold uppercase tracking-wider text-base-content/50">Learning</div>
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
						href="/exams"
						class="flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors {isActive('/exams')
							? 'bg-primary/15 text-primary font-semibold border border-primary/20'
							: 'text-base-content/80 hover:bg-base-100/40 hover:text-base-content'}"
					>
						<GraduationCap class="h-4 w-4" />
						Examinations
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
	</div>
</aside>
