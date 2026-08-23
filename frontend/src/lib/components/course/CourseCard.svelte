<script lang="ts">
	import type { Course } from '#lib/api/types.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import { BookOpen, Key, DollarSign, CheckCircle2, ArrowRight } from '@lucide/svelte';

	interface Props {
		course: Course;
	}

	let { course }: Props = $props();

	const accessTypeConfig = {
		OpenFree: {
			label: 'Free',
			class: 'badge-success text-success-content',
			icon: CheckCircle2
		},
		OpenPaid: {
			label: 'Paid',
			class: 'badge-primary text-primary-content',
			icon: DollarSign
		},
		PrivateWithKey: {
			label: 'Private Key',
			class: 'badge-warning text-warning-content',
			icon: Key
		}
	};

	let badge = $derived(accessTypeConfig[course.accessType] || accessTypeConfig.OpenFree);
</script>

<GlassCard hover={true} class="flex flex-col justify-between h-full p-5">
	<div class="space-y-3">
		<!-- Card Header: Access Type & Price -->
		<div class="flex items-center justify-between">
			<span class="badge {badge.class} badge-sm font-semibold gap-1">
				<badge.icon class="h-3 w-3" />
				{badge.label}
			</span>

			{#if course.accessType === 'OpenPaid'}
				<span class="text-sm font-bold text-primary">
					${course.price?.toFixed(2) || '0.00'}
				</span>
			{/if}
		</div>

		<!-- Title & Description -->
		<div class="space-y-1 text-left">
			<h3 class="text-base font-bold text-base-content line-clamp-1 hover:text-primary transition-colors">
				{course.title}
			</h3>
			<p class="text-xs text-base-content/65 line-clamp-2 leading-relaxed">
				{course.description || 'Comprehensive modular curriculum.'}
			</p>
		</div>
	</div>

	<!-- Card Footer -->
	<div class="mt-5 pt-3 border-t border-white/10 flex items-center justify-between">
		<span class="text-[11px] text-base-content/60 flex items-center gap-1">
			<BookOpen class="h-3.5 w-3.5" />
			{course.sections?.length || 0} Sections
		</span>

		<a
			href="/courses/{course.id}"
			class="btn btn-ghost btn-xs rounded-lg text-primary hover:bg-primary/10 gap-1 font-semibold"
		>
			Details
			<ArrowRight class="h-3 w-3" />
		</a>
	</div>
</GlassCard>
