<script lang="ts">
	import type { Course } from '$lib/api/types.ts';
	import GlassCard from '$lib/components/ui/GlassCard.svelte';
	import { BookOpen, Key, DollarSign, CheckCircle2, ArrowRight, Layers, Users } from 'lucide-svelte';

	interface Props {
		course: Course;
	}

	let { course }: Props = $props();

	const accessTypeConfig: Record<string, { label: string; class: string; icon: any }> = {
		OpenFree: {
			label: 'Free Access',
			class: 'badge-success text-white',
			icon: CheckCircle2
		},
		OpenPaid: {
			label: 'Paid Course',
			class: 'badge-primary text-primary-content',
			icon: DollarSign
		},
		PrivateWithKey: {
			label: 'Secret Passkey',
			class: 'badge-warning text-warning-content',
			icon: Key
		}
	};

	let badge = $derived(accessTypeConfig[course.accessType] || accessTypeConfig.OpenFree);

	function stripHtml(html: string): string {
		return html.replace(/<[^>]*>?/gm, ' ').replace(/\s+/g, ' ').trim();
	}

	let plainDescription = $derived.by(() => {
		const raw = course.description;
		if (!raw) return 'Comprehensive modular curriculum.';
		if (raw.startsWith('{')) {
			try {
				const parsed = JSON.parse(raw);
				const extractText = (node: any): string => {
					if (node.text) return node.text;
					if (node.content && Array.isArray(node.content)) {
						return node.content.map(extractText).join(' ');
					}
					return '';
				};
				const text = extractText(parsed).trim();
				if (text) return text;
			} catch {
				// fallback
			}
		}
		if (raw.includes('<')) {
			const stripped = stripHtml(raw);
			if (stripped) return stripped;
		}
		return raw;
	});
</script>

<GlassCard hover={true} class="flex flex-col justify-between h-full p-5 space-y-4">
	<div class="space-y-3">
		<!-- Card Header: Access Type & Price -->
		<div class="flex items-center justify-between gap-2">
			<span class="badge {badge.class} badge-sm font-semibold gap-1 text-[11px]">
				<badge.icon class="w-3 h-3" />
				{badge.label}
			</span>

			{#if course.accessType === 'OpenPaid'}
				<span class="text-sm font-extrabold text-primary font-mono">
					${Number(course.price || 0).toFixed(2)}
				</span>
			{:else}
				<span class="text-xs font-bold text-success">Free</span>
			{/if}
		</div>

		<!-- Title & Description -->
		<div class="space-y-1.5 text-left">
			<a href="/courses/{course.id}" class="block group">
				<h3 class="text-base font-bold text-base-content line-clamp-2 group-hover:text-primary transition-colors">
					{course.title}
				</h3>
			</a>
			<p class="text-xs text-base-content/65 line-clamp-2 leading-relaxed">
				{plainDescription}
			</p>
		</div>
	</div>

	<!-- Card Footer -->
	<div class="pt-3 border-t border-base-content/10 flex items-center justify-between">
		<div class="flex items-center gap-3 text-[11px] text-base-content/60">
			<span class="flex items-center gap-1">
				<Layers class="w-3.5 h-3.5 text-primary" />
				{course.sections?.length || 0} Sections
			</span>
			<span class="flex items-center gap-1">
				<Users class="w-3.5 h-3.5 text-secondary" />
				{course.enrolledStudentsCount || 0} enrolled
			</span>
		</div>

		<a
			href="/courses/{course.id}"
			class="btn btn-ghost btn-xs rounded-xl text-primary hover:bg-primary/10 gap-1 font-bold"
		>
			View Details
			<ArrowRight class="w-3 h-3" />
		</a>
	</div>
</GlassCard>
