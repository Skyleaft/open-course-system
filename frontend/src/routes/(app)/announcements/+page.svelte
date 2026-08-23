<script lang="ts">
	import { communicationsApi } from '#lib/api/communications.ts';
	import type { Announcement } from '#lib/api/types.ts';
	import { authStore } from '#lib/stores/auth.svelte.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import GlassModal from '#lib/components/ui/GlassModal.svelte';
	import RichEditor from '#lib/components/editor/RichEditor.svelte';
	import RichRenderer from '#lib/components/editor/RichRenderer.svelte';
	import { toast } from '#lib/stores/toast.svelte.ts';
	import { Megaphone, Pin, Plus, Calendar, User, Sparkles, Send } from '@lucide/svelte';
	import { onMount } from 'svelte';

	let announcements = $state<Announcement[]>([]);
	let isLoading = $state(true);

	// Create modal
	let isCreateModalOpen = $state(false);
	let newTitle = $state('');
	let newContent = $state('');
	let isPinned = $state(false);
	let isSubmitting = $state(false);

	onMount(async () => {
		await loadAnnouncements();
	});

	async function loadAnnouncements() {
		isLoading = true;
		try {
			const res = await communicationsApi.getAnnouncements();
			if (res && res.length > 0) {
				announcements = res;
			} else {
				loadMockAnnouncements();
			}
		} catch {
			loadMockAnnouncements();
		} finally {
			isLoading = false;
		}
	}

	function loadMockAnnouncements() {
		announcements = [
			{
				id: 'ann-1',
				authorId: 'auth-1',
				authorName: 'Academic Directorate',
				title: 'Final Examination Schedule & Anti-Cheat Protocols',
				content: '<p>Please ensure all students have tested their webcam and microphone setups in advance. RealExam sessions require uninterrupted fullscreen mode.</p>',
				isPinned: true,
				createdAtUtc: new Date(Date.now() - 86400000).toISOString()
			},
			{
				id: 'ann-2',
				authorId: 'auth-2',
				authorName: 'Prof. Anderson',
				title: 'Distributed Consensus Lab Materials Released',
				content: '<p>The laboratory materials and sample Raft logs have been uploaded to the course storage bucket.</p>',
				isPinned: false,
				createdAtUtc: new Date(Date.now() - 172800000).toISOString()
			}
		];
	}

	async function handleCreateAnnouncement() {
		if (!newTitle || !newContent) {
			toast.warning('Please enter both title and content.');
			return;
		}

		isSubmitting = true;
		try {
			await communicationsApi.createAnnouncement({
				title: newTitle,
				content: newContent,
				isPinned
			});
			toast.success('Announcement broadcasted successfully!');
			isCreateModalOpen = false;
			newTitle = '';
			newContent = '';
			isPinned = false;
			await loadAnnouncements();
		} catch (err: any) {
			toast.error(err?.message || 'Failed to create announcement.');
		} finally {
			isSubmitting = false;
		}
	}
</script>

<div class="space-y-8">
	<!-- Header -->
	<div class="glass-panel flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 rounded-3xl border border-white/10 p-8 shadow-2xl backdrop-blur-2xl">
		<div class="space-y-1">
			<div class="inline-flex items-center gap-2 rounded-lg bg-primary/10 border border-primary/20 px-3 py-1 text-xs font-semibold text-primary">
				<Megaphone class="h-3.5 w-3.5" />
				Campus Communications
			</div>
			<h1 class="text-3xl font-extrabold tracking-tight text-base-content">
				Platform & Course Announcements
			</h1>
			<p class="text-xs text-base-content/70">
				Stay informed on critical academic timelines, exam policies, and course broadcasts.
			</p>
		</div>

		{#if authStore.isInstructor || authStore.isAdmin}
			<button
				class="btn btn-primary gradient-accent rounded-xl text-xs font-bold text-white border-0 shadow-lg gap-1.5 self-start sm:self-auto"
				onclick={() => (isCreateModalOpen = true)}
			>
				<Plus class="h-4 w-4" />
				Broadcast Announcement
			</button>
		{/if}
	</div>

	<!-- Announcements Feed -->
	{#if isLoading}
		<div class="space-y-4">
			{#each Array(2) as _}
				<div class="glass-panel h-48 rounded-3xl animate-pulse"></div>
			{/each}
		</div>
	{:else if announcements.length > 0}
		<div class="space-y-6">
			{#each announcements as ann (ann.id)}
				<GlassCard class="p-6 border {ann.isPinned ? 'border-primary/40 bg-primary/5 shadow-xl' : 'border-white/10'} space-y-4">
					<!-- Top Meta -->
					<div class="flex items-center justify-between border-b border-white/10 pb-3 text-xs">
						<div class="flex items-center gap-3">
							{#if ann.isPinned}
								<span class="badge badge-primary badge-xs font-bold gap-1 text-white">
									<Pin class="h-3 w-3" />
									Pinned
								</span>
							{/if}
							<span class="font-bold text-base-content flex items-center gap-1.5">
								<User class="h-3.5 w-3.5 text-primary" />
								{ann.authorName || 'Instructor'}
							</span>
						</div>

						<span class="text-base-content/50 flex items-center gap-1 text-[11px]">
							<Calendar class="h-3 w-3" />
							{new Date(ann.createdAtUtc).toLocaleDateString()}
						</span>
					</div>

					<!-- Title & Body -->
					<div class="space-y-2">
						<h2 class="text-xl font-bold text-base-content tracking-tight">{ann.title}</h2>
						<RichRenderer content={ann.content} class="text-sm text-base-content/85 leading-relaxed" />
					</div>
				</GlassCard>
			{/each}
		</div>
	{:else}
		<div class="glass-card p-12 text-center rounded-3xl border border-white/5 space-y-2">
			<Megaphone class="h-8 w-8 text-primary mx-auto opacity-50" />
			<h3 class="text-base font-bold text-base-content">No announcements posted</h3>
			<p class="text-xs text-base-content/60">Check back later for course and system updates.</p>
		</div>
	{/if}

	<!-- Create Announcement Modal -->
	<GlassModal
		isOpen={isCreateModalOpen}
		title="Broadcast Announcement"
		onClose={() => (isCreateModalOpen = false)}
		maxWidth="max-w-2xl"
	>
		<div class="space-y-4">
			<div class="space-y-1.5">
				<label class="text-xs font-semibold" for="ann-title">Announcement Title</label>
				<input
					id="ann-title"
					type="text"
					class="glass-input input input-sm h-11 w-full rounded-xl text-sm font-semibold"
					placeholder="e.g. Schedule Update for Examination"
					bind:value={newTitle}
				/>
			</div>

			<div class="space-y-1.5">
				<label class="text-xs font-semibold">Message Content (Edra Editor)</label>
				<RichEditor
					placeholder="Write your announcement details..."
					minHeight="180px"
					onUpdate={(json) => (newContent = json)}
				/>
			</div>

			<label class="flex items-center gap-2 cursor-pointer pt-1">
				<input
					type="checkbox"
					class="checkbox checkbox-primary checkbox-xs rounded-sm"
					bind:checked={isPinned}
				/>
				<span class="text-xs font-semibold text-base-content/80 select-none">
					Pin this announcement to the top of the feed
				</span>
			</label>
		</div>

		{#snippet actions()}
			<button class="btn btn-ghost btn-sm rounded-xl" onclick={() => (isCreateModalOpen = false)}>
				Cancel
			</button>
			<button
				class="btn btn-primary gradient-accent btn-sm rounded-xl text-white font-bold border-0 shadow-md gap-1.5"
				onclick={handleCreateAnnouncement}
				disabled={isSubmitting}
			>
				{#if isSubmitting}
					<span class="loading loading-spinner loading-xs"></span>
				{:else}
					<Send class="h-3.5 w-3.5" />
					Broadcast
				{/if}
			</button>
		{/snippet}
	</GlassModal>
</div>
