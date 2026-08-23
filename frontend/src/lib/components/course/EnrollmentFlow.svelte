<script lang="ts">
	import type { Course } from '#lib/api/types.ts';
	import { coursesApi } from '#lib/api/courses.ts';
	import { paymentsApi } from '#lib/api/payments.ts';
	import { authStore } from '#lib/stores/auth.svelte.ts';
	import { toast } from '#lib/stores/toast.svelte.ts';
	import GlassModal from '#lib/components/ui/GlassModal.svelte';
	import { Key, DollarSign, CheckCircle2, ArrowRight } from '@lucide/svelte';
	import { goto } from '$app/navigation';

	interface Props {
		course: Course;
		onEnrolled?: () => void;
	}

	let { course, onEnrolled }: Props = $props();

	let isKeyModalOpen = $state(false);
	let isPaidModalOpen = $state(false);
	let enrollmentKey = $state('');
	let isLoading = $state(false);
	let currentOrderId = $state<string | null>(null);

	async function handleEnrollClick() {
		if (!authStore.isAuthenticated) {
			toast.info('Please sign in to enroll in this course.');
			goto(`/login?returnUrl=${encodeURIComponent('/courses/' + course.id)}`);
			return;
		}

		if (course.accessType === 'OpenFree') {
			await executeFreeEnroll();
		} else if (course.accessType === 'PrivateWithKey') {
			isKeyModalOpen = true;
		} else if (course.accessType === 'OpenPaid') {
			await initiateCheckout();
		}
	}

	async function executeFreeEnroll() {
		isLoading = true;
		try {
			await coursesApi.enroll(course.id);
			toast.success('Successfully enrolled in course!');
			if (onEnrolled) onEnrolled();
			goto(`/courses/${course.id}/learn`);
		} catch (err: any) {
			toast.error(err?.message || 'Enrollment failed.');
		} finally {
			isLoading = false;
		}
	}

	async function executeKeyEnroll() {
		if (!enrollmentKey) {
			toast.warning('Please enter the enrollment key.');
			return;
		}

		isLoading = true;
		try {
			await coursesApi.enroll(course.id, enrollmentKey);
			toast.success('Access granted with key!');
			isKeyModalOpen = false;
			if (onEnrolled) onEnrolled();
			goto(`/courses/${course.id}/learn`);
		} catch (err: any) {
			toast.error(err?.message || 'Invalid enrollment key.');
		} finally {
			isLoading = false;
		}
	}

	async function initiateCheckout() {
		isLoading = true;
		try {
			const checkout = await paymentsApi.createCheckout(course.id);
			currentOrderId = checkout.orderId;
			isPaidModalOpen = true;
		} catch (err: any) {
			toast.error(err?.message || 'Failed to create order.');
		} finally {
			isLoading = false;
		}
	}

	async function completeMockPayment() {
		if (!currentOrderId) return;
		isLoading = true;
		try {
			await paymentsApi.mockPayOrder(currentOrderId);
			toast.success('Payment confirmed! Enrolling in course...');
			isPaidModalOpen = false;
			if (onEnrolled) onEnrolled();
			goto(`/courses/${course.id}/learn`);
		} catch (err: any) {
			toast.error(err?.message || 'Payment verification failed.');
		} finally {
			isLoading = false;
		}
	}
</script>

<div>
	<button
		class="btn btn-primary gradient-accent w-full rounded-2xl font-bold text-white border-0 shadow-lg h-12 text-sm"
		onclick={handleEnrollClick}
		disabled={isLoading}
	>
		{#if isLoading}
			<span class="loading loading-spinner loading-sm"></span>
		{:else if course.accessType === 'OpenFree'}
			<CheckCircle2 class="h-4 w-4 mr-1" />
			Enroll for Free
		{:else if course.accessType === 'OpenPaid'}
			<DollarSign class="h-4 w-4 mr-1" />
			Enroll for ${course.price?.toFixed(2) || '0.00'}
		{:else}
			<Key class="h-4 w-4 mr-1" />
			Enter with Key
		{/if}
	</button>

	<!-- Key Modal -->
	<GlassModal
		isOpen={isKeyModalOpen}
		title="Private Course Access"
		onClose={() => (isKeyModalOpen = false)}
	>
		<div class="space-y-4">
			<p class="text-xs text-base-content/70">
				This course requires a private enrollment key from your instructor.
			</p>
			<div class="space-y-1.5">
				<label class="text-xs font-semibold" for="secret-key">Enrollment Key</label>
				<input
					id="secret-key"
					type="password"
					class="glass-input input input-sm h-11 w-full rounded-xl text-sm"
					placeholder="Enter enrollment key..."
					bind:value={enrollmentKey}
				/>
			</div>
		</div>

		{#snippet actions()}
			<button class="btn btn-ghost btn-sm rounded-xl" onclick={() => (isKeyModalOpen = false)}>
				Cancel
			</button>
			<button
				class="btn btn-primary gradient-accent btn-sm rounded-xl text-white font-semibold border-0"
				onclick={executeKeyEnroll}
				disabled={isLoading}
			>
				{#if isLoading}
					<span class="loading loading-spinner loading-xs"></span>
				{/if}
				Verify Key
			</button>
		{/snippet}
	</GlassModal>

	<!-- Paid Checkout Mock Modal -->
	<GlassModal
		isOpen={isPaidModalOpen}
		title="Course Checkout"
		onClose={() => (isPaidModalOpen = false)}
	>
		<div class="space-y-4">
			<div class="glass-card rounded-xl p-4 border border-white/10 space-y-2">
				<div class="flex justify-between text-xs text-base-content/70">
					<span>Course</span>
					<span class="font-semibold text-base-content">{course.title}</span>
				</div>
				<div class="flex justify-between text-sm font-bold border-t border-white/10 pt-2 text-primary">
					<span>Total Amount</span>
					<span>${course.price?.toFixed(2) || '0.00'}</span>
				</div>
			</div>
			<div class="rounded-xl bg-info/10 border border-info/20 p-3 text-xs text-info">
				💡 Mock payment simulation enabled. Click below to simulate instantaneous payment webhook.
			</div>
		</div>

		{#snippet actions()}
			<button class="btn btn-ghost btn-sm rounded-xl" onclick={() => (isPaidModalOpen = false)}>
				Cancel
			</button>
			<button
				class="btn btn-primary gradient-accent btn-sm rounded-xl text-white font-semibold border-0"
				onclick={completeMockPayment}
				disabled={isLoading}
			>
				{#if isLoading}
					<span class="loading loading-spinner loading-xs"></span>
				{/if}
				Simulate Payment ($ {course.price?.toFixed(2)})
			</button>
		{/snippet}
	</GlassModal>
</div>
