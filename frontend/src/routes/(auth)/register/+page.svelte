<script lang="ts">
	import { authApi } from '#lib/api/auth.ts';
	import { toast } from '#lib/stores/toast.svelte.ts';
	import { goto } from '$app/navigation';
	import { Mail, Lock, User, AtSign, ArrowRight, AlertCircle } from '@lucide/svelte';

	let fullName = $state('');
	let userName = $state('');
	let email = $state('');
	let password = $state('');
	let confirmPassword = $state('');
	let isLoading = $state(false);
	let errorMessage = $state<string | null>(null);

	async function handleRegister(e: Event) {
		e.preventDefault();

		if (password !== confirmPassword) {
			errorMessage = 'Passwords do not match.';
			return;
		}

		if (password.length < 6) {
			errorMessage = 'Password must be at least 6 characters.';
			return;
		}

		isLoading = true;
		errorMessage = null;

		try {
			await authApi.register({ fullName, userName, email, password });
			toast.success('Registration successful! Please sign in with your credentials.');
			goto('/login');
		} catch (err: any) {
			errorMessage = err?.message || 'Registration failed.';
		} finally {
			isLoading = false;
		}
	}
</script>

<div class="space-y-6">
	<div class="text-center space-y-1">
		<h2 class="text-2xl font-bold text-base-content tracking-tight">Create Account</h2>
		<p class="text-xs text-base-content/60">Join Open Course System to start learning and taking exams</p>
	</div>

	{#if errorMessage}
		<div class="flex items-center gap-2 rounded-xl bg-error/15 border border-error/25 p-3 text-xs text-error">
			<AlertCircle class="h-4 w-4 shrink-0" />
			<span>{errorMessage}</span>
		</div>
	{/if}

	<form onsubmit={handleRegister} class="space-y-4">
		<div class="space-y-1.5">
			<label class="text-xs font-semibold text-base-content/80" for="fullName">Full Name</label>
			<div class="relative">
				<div class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-base-content/40">
					<User class="h-4 w-4" />
				</div>
				<input
					id="fullName"
					type="text"
					class="glass-input input input-sm h-11 w-full rounded-xl pl-9 text-sm focus:outline-none"
					placeholder="Jane Doe"
					bind:value={fullName}
					required
				/>
			</div>
		</div>

		<div class="space-y-1.5">
			<label class="text-xs font-semibold text-base-content/80" for="userName">Username</label>
			<div class="relative">
				<div class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-base-content/40">
					<AtSign class="h-4 w-4" />
				</div>
				<input
					id="userName"
					type="text"
					class="glass-input input input-sm h-11 w-full rounded-xl pl-9 text-sm focus:outline-none"
					placeholder="janedoe"
					bind:value={userName}
					required
				/>
			</div>
		</div>

		<div class="space-y-1.5">
			<label class="text-xs font-semibold text-base-content/80" for="reg-email">Email Address</label>
			<div class="relative">
				<div class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-base-content/40">
					<Mail class="h-4 w-4" />
				</div>
				<input
					id="reg-email"
					type="email"
					class="glass-input input input-sm h-11 w-full rounded-xl pl-9 text-sm focus:outline-none"
					placeholder="student@example.com"
					bind:value={email}
					required
				/>
			</div>
		</div>

		<div class="space-y-1.5">
			<label class="text-xs font-semibold text-base-content/80" for="reg-password">Password</label>
			<div class="relative">
				<div class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-base-content/40">
					<Lock class="h-4 w-4" />
				</div>
				<input
					id="reg-password"
					type="password"
					class="glass-input input input-sm h-11 w-full rounded-xl pl-9 text-sm focus:outline-none"
					placeholder="••••••••"
					bind:value={password}
					required
				/>
			</div>
		</div>

		<div class="space-y-1.5">
			<label class="text-xs font-semibold text-base-content/80" for="confirmPassword">Confirm Password</label>
			<div class="relative">
				<div class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-base-content/40">
					<Lock class="h-4 w-4" />
				</div>
				<input
					id="confirmPassword"
					type="password"
					class="glass-input input input-sm h-11 w-full rounded-xl pl-9 text-sm focus:outline-none"
					placeholder="••••••••"
					bind:value={confirmPassword}
					required
				/>
			</div>
		</div>

		<button
			type="submit"
			class="btn btn-primary gradient-accent w-full rounded-xl font-semibold text-white border-0 shadow-lg mt-2 h-11"
			disabled={isLoading}
		>
			{#if isLoading}
				<span class="loading loading-spinner loading-sm"></span>
			{:else}
				<span>Sign Up</span>
				<ArrowRight class="h-4 w-4 ml-1" />
			{/if}
		</button>
	</form>

	<div class="text-center text-xs text-base-content/60">
		Already have an account?
		<a href="/login" class="font-semibold text-primary hover:underline ml-1">Sign In</a>
	</div>
</div>
