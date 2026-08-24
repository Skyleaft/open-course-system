<script lang="ts">
	import type { Course } from '$lib/api/types.ts';
	import { coursesApi } from '$lib/api/courses.ts';
	import { paymentsApi } from '$lib/api/payments.ts';
	import { authStore } from '$lib/stores/auth.svelte.ts';
	import { toast } from '$lib/stores/toast.svelte.ts';
	import GlassModal from '$lib/components/ui/GlassModal.svelte';
	import { Key, DollarSign, CheckCircle2, ArrowRight } from 'lucide-svelte';
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
		if (!enrollmentKey.trim()) {
			toast.warning('Please enter the enrollment key.');
			return;
		}

		isLoading = true;
		try {
			await coursesApi.enroll(course.id, enrollmentKey.trim());
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
		type="button"
		class="btn btn-primary w-full rounded-2xl font-bold text-primary-content shadow-lg h-12 text-sm gap-2"
		onclick={handleEnrollClick}
		disabled={isLoading}
	>
		{#if isLoading}
			<span class="loading loading-spinner loading-sm"></span>
		{:else if course.accessType === 'OpenFree'}
			<CheckCircle2 class="w-4 h-4" />
			<span>Enroll for Free</span>
		{:else if course.accessType === 'OpenPaid'}
			<DollarSign class="w-4 h-4" />
			<span>Enroll for ${Number(course.price || 0).toFixed(2)}</span>
		{:else}
			<Key class="w-4 h-4" />
			<span>Enter with Passkey</span>
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
				<label class="label label-text text-xs font-bold uppercase tracking-wider text-base-content/70" for="secret-key">
					Enrollment Passkey
				</label>
				<input
					id="secret-key"
					type="password"
					class="input input-bordered input-sm h-11 w-full bg-base-200/50 text-sm"
					placeholder="Enter secret passkey..."
					bind:value={enrollmentKey}
				/>
			</div>
		</div>

		{#snippet actions()}
			<button type="button" class="btn btn-ghost btn-sm" onclick={() => (isKeyModalOpen = false)}>
				Cancel
			</button>
			<button
				type="button"
				class="btn btn-primary btn-sm font-semibold"
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
			<div class="p-4 rounded-xl border border-base-content/10 bg-base-200/50 space-y-2">
				<div class="flex justify-between text-xs text-base-content/70">
					<span>Course</span>
					<span class="font-bold text-base-content truncate max-w-xs">{course.title}</span>
				</div>
				<div class="flex justify-between text-sm font-extrabold border-t border-base-content/10 pt-2 text-primary font-mono">
					<span>Total Amount</span>
					<span>${Number(course.price || 0).toFixed(2)}</span>
				</div>
			</div>
			<div class="rounded-xl bg-info/10 border border-info/20 p-3 text-xs text-info leading-relaxed">
				💡 Mock payment simulation enabled. Click below to simulate instantaneous payment webhook confirmation.
			</div>
		</div>

		{#snippet actions()}
			<button type="button" class="btn btn-ghost btn-sm" onclick={() => (isPaidModalOpen = false)}>
				Cancel
			</button>
			<button
				type="button"
				class="btn btn-primary btn-sm font-semibold"
				onclick={completeMockPayment}
				disabled={isLoading}
			>
				{#if isLoading}
					<span class="loading loading-spinner loading-xs"></span>
				{/if}
				Simulate Payment (${Number(course.price || 0).toFixed(2)})
			</button>
		{/snippet}
	</GlassModal>
</div>
