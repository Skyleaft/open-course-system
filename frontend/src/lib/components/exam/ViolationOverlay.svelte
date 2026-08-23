<script lang="ts">
	import { AlertTriangle, ShieldAlert, XCircle, LogOut } from '@lucide/svelte';
	import { goto } from '$app/navigation';

	interface Props {
		currentViolations: number;
		maxViolations: number;
		isDisqualified: boolean;
		terminationReason?: string;
		onAcknowledgeWarning?: () => void;
	}

	let {
		currentViolations,
		maxViolations,
		isDisqualified = false,
		terminationReason,
		onAcknowledgeWarning
	}: Props = $props();

	let showWarningModal = $state(false);

	$effect(() => {
		if (currentViolations > 0 && !isDisqualified) {
			showWarningModal = true;
		}
	});

	function handleDismiss() {
		showWarningModal = false;
		if (onAcknowledgeWarning) onAcknowledgeWarning();
	}
</script>

<!-- 1. Auto-Disqualification Modal Screen -->
{#if isDisqualified}
	<div
		class="fixed inset-0 z-50 flex items-center justify-center bg-black/90 p-4 backdrop-blur-2xl animate-in fade-in"
		role="alertdialog"
	>
		<div class="glass-modal max-w-md rounded-3xl border-2 border-error/50 p-8 text-center space-y-6 shadow-2xl">
			<div class="mx-auto flex h-20 w-20 items-center justify-center rounded-3xl bg-error/20 text-error border border-error/40 shadow-xl animate-bounce">
				<XCircle class="h-10 w-10" />
			</div>

			<div class="space-y-2">
				<span class="badge badge-error badge-sm font-bold uppercase tracking-widest text-white">
					Disqualified
				</span>
				<h2 class="text-2xl font-black text-base-content tracking-tight">
					Exam Session Terminated
				</h2>
				<p class="text-xs text-base-content/70 leading-relaxed">
					{terminationReason || `Exceeded maximum allowed security violations (${currentViolations}/${maxViolations}). Your attempt has been locked.`}
				</p>
			</div>

			<div class="pt-2">
				<a
					href="/dashboard"
					class="btn btn-error w-full rounded-2xl font-bold text-white shadow-lg gap-2"
				>
					<LogOut class="h-4 w-4" />
					Return to Dashboard
				</a>
			</div>
		</div>
	</div>
{:else if showWarningModal}
	<!-- 2. Warning Interceptor Modal -->
	<div
		class="fixed inset-0 z-50 flex items-center justify-center bg-black/75 p-4 backdrop-blur-md animate-in fade-in"
		role="dialog"
	>
		<div class="glass-modal max-w-md rounded-3xl border border-warning/40 p-6 text-center space-y-5 shadow-2xl">
			<div class="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-warning/20 text-warning border border-warning/30">
				<ShieldAlert class="h-7 w-7" />
			</div>

			<div class="space-y-1">
				<h3 class="text-lg font-bold text-base-content">Security Violation Detected</h3>
				<p class="text-xs text-warning font-semibold">
					Warning {currentViolations} of {maxViolations}
				</p>
				<p class="text-xs text-base-content/70 pt-2 leading-relaxed">
					Leaving the exam window, switching tabs, or exiting fullscreen mode is strictly prohibited. Additional violations will result in automatic disqualification.
				</p>
			</div>

			<div class="pt-2">
				<button
					class="btn btn-warning w-full rounded-xl font-bold text-warning-content shadow-md"
					onclick={handleDismiss}
				>
					I Understand, Return to Exam
				</button>
			</div>
		</div>
	</div>
{/if}
