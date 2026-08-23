<script lang="ts">
	import { authStore } from '#lib/stores/auth.svelte.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import StatCard from '#lib/components/ui/StatCard.svelte';
	import {
		BookOpen,
		GraduationCap,
		Award,
		Clock,
		ArrowRight,
		PlayCircle,
		Sparkles,
		Layers
	} from '@lucide/svelte';
</script>

<div class="space-y-8">
	<!-- Welcome Banner -->
	<div class="glass-panel relative overflow-hidden rounded-3xl border border-white/10 p-8 shadow-2xl backdrop-blur-2xl">
		<div class="relative z-10 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
			<div class="space-y-2">
				<div class="inline-flex items-center gap-2 rounded-lg bg-primary/10 border border-primary/20 px-3 py-1 text-xs font-semibold text-primary">
					<Sparkles class="h-3.5 w-3.5" />
					Welcome to Open Course System
				</div>
				<h1 class="text-3xl font-extrabold tracking-tight text-base-content sm:text-4xl">
					Hello, {authStore.user?.fullName || 'Student'}! 👋
				</h1>
				<p class="text-xs text-base-content/70 sm:text-sm">
					Ready to continue your courses and prepare for upcoming examinations?
				</p>
			</div>

			<div class="flex items-center gap-3">
				<a href="/courses" class="btn btn-primary gradient-accent rounded-xl text-xs font-semibold text-white border-0 shadow-md">
					<BookOpen class="h-4 w-4 mr-1" />
					Explore Courses
				</a>
				<a href="/exams" class="btn btn-ghost glass-card rounded-xl text-xs font-medium border border-white/10 hover:bg-base-100/40">
					<GraduationCap class="h-4 w-4 mr-1" />
					My Exams
				</a>
			</div>
		</div>
	</div>

	<!-- Metric Stat Cards -->
	<div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
		<StatCard
			title="Enrolled Courses"
			value="4"
			description="Active learning paths"
			icon={BookOpen}
			color="primary"
		/>
		<StatCard
			title="Exams Completed"
			value="6"
			description="Passing rate: 100%"
			icon={GraduationCap}
			color="secondary"
		/>
		<StatCard
			title="Certificates Earned"
			value="3"
			description="Cryptographically signed"
			icon={Award}
			color="accent"
		/>
		<StatCard
			title="Study Time"
			value="28h"
			description="+4h this week"
			icon={Clock}
			trend="12%"
			trendUp={true}
			color="info"
		/>
	</div>

	<!-- Quick Jump Sections -->
	<div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
		<!-- Active Courses -->
		<GlassCard>
			<div class="space-y-4">
				<div class="flex items-center justify-between border-b border-white/10 pb-3">
					<h3 class="text-base font-bold text-base-content">Continue Learning</h3>
					<a href="/courses" class="text-xs font-medium text-primary hover:underline flex items-center gap-1">
						View All <ArrowRight class="h-3 w-3" />
					</a>
				</div>

				<div class="space-y-3">
					<div class="flex items-center justify-between rounded-xl bg-base-100/40 p-3.5 border border-white/5">
						<div class="flex items-center gap-3">
							<div class="gradient-accent flex h-10 w-10 shrink-0 items-center justify-center rounded-xl text-white font-bold text-sm">
								CS
							</div>
							<div class="text-left">
								<div class="text-xs font-bold text-base-content">Advanced Distributed Systems</div>
								<div class="text-[10px] text-base-content/60">Lesson 4: Redis Streams & Event Sourcing</div>
							</div>
						</div>
						<a href="/courses" class="btn btn-ghost btn-sm btn-circle text-primary hover:bg-primary/10">
							<PlayCircle class="h-5 w-5" />
						</a>
					</div>

					<div class="flex items-center justify-between rounded-xl bg-base-100/40 p-3.5 border border-white/5">
						<div class="flex items-center gap-3">
							<div class="bg-secondary/20 border border-secondary/30 flex h-10 w-10 shrink-0 items-center justify-center rounded-xl text-secondary font-bold text-sm">
								AI
							</div>
							<div class="text-left">
								<div class="text-xs font-bold text-base-content">Fullstack Machine Learning</div>
								<div class="text-[10px] text-base-content/60">Lesson 2: Realtime Vision Interceptors</div>
							</div>
						</div>
						<a href="/courses" class="btn btn-ghost btn-sm btn-circle text-primary hover:bg-primary/10">
							<PlayCircle class="h-5 w-5" />
						</a>
					</div>
				</div>
			</div>
		</GlassCard>

		<!-- Upcoming & Practice Exams -->
		<GlassCard>
			<div class="space-y-4">
				<div class="flex items-center justify-between border-b border-white/10 pb-3">
					<h3 class="text-base font-bold text-base-content">Available Examinations</h3>
					<a href="/exams" class="text-xs font-medium text-primary hover:underline flex items-center gap-1">
						All Exams <ArrowRight class="h-3 w-3" />
					</a>
				</div>

				<div class="space-y-3">
					<div class="flex items-center justify-between rounded-xl bg-primary/10 border border-primary/20 p-3.5">
						<div class="space-y-0.5 text-left">
							<div class="flex items-center gap-2">
								<span class="badge badge-primary badge-xs uppercase font-bold">RealExam</span>
								<span class="text-xs font-bold text-base-content">Distributed Architectures Final</span>
							</div>
							<div class="text-[10px] text-base-content/60">60 mins • Passing Score: 75% • Proctored</div>
						</div>
						<a href="/exams" class="btn btn-primary gradient-accent btn-xs rounded-lg text-white border-0">
							Take Exam
						</a>
					</div>

					<div class="flex items-center justify-between rounded-xl bg-base-100/40 p-3.5 border border-white/5">
						<div class="space-y-0.5 text-left">
							<div class="flex items-center gap-2">
								<span class="badge badge-ghost badge-xs uppercase font-bold">Simulation</span>
								<span class="text-xs font-bold text-base-content">Mock Practice Test: ASP.NET Core</span>
							</div>
							<div class="text-[10px] text-base-content/60">30 mins • Free Practice • Instant Explanations</div>
						</div>
						<a href="/exams" class="btn btn-ghost btn-xs glass-card rounded-lg border border-white/10">
							Practice
						</a>
					</div>
				</div>
			</div>
		</GlassCard>
	</div>
</div>
