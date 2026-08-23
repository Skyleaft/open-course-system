<script lang="ts">
	import { authApi } from '#lib/api/auth.ts';
	import { authStore, getDefaultRouteForUser } from '#lib/stores/auth.svelte.ts';
	import { toast } from '#lib/stores/toast.svelte.ts';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { User, Lock, ArrowRight, AlertCircle } from '@lucide/svelte';

	let userNameOrEmail = $state('');
	let password = $state('');
	let isLoading = $state(false);
	let errorMessage = $state<string | null>(null);

	async function handleLogin(e: Event) {
		e.preventDefault();
		if (!userNameOrEmail || !password) {
			errorMessage = 'Please enter both email/username and password.';
			return;
		}

		isLoading = true;
		errorMessage = null;

		try {
			const res = await authApi.login({ userNameOrEmail, password });
			authStore.setUser(res.user);
			toast.success(`Welcome back, ${res.user.fullName}!`);

			const returnUrl = page.url.searchParams.get('returnUrl') || page.url.searchParams.get('redirect');
			if (returnUrl && returnUrl.startsWith('/') && !returnUrl.startsWith('/login') && !returnUrl.startsWith('/register')) {
				goto(returnUrl);
			} else {
				goto(getDefaultRouteForUser(res.user));
			}
		} catch (err: any) {
			errorMessage = err?.message || 'Invalid email, username, or password.';
		} finally {
			isLoading = false;
		}
	}
</script>

<div class="space-y-6">
	<div class="text-center space-y-1">
		<h2 class="text-2xl font-bold text-base-content tracking-tight">Sign In</h2>
		<p class="text-xs text-base-content/60">Enter your credentials to access your portal</p>
	</div>

	{#if errorMessage}
		<div class="flex items-center gap-2 rounded-xl bg-error/15 border border-error/25 p-3 text-xs text-error">
			<AlertCircle class="h-4 w-4 shrink-0" />
			<span>{errorMessage}</span>
		</div>
	{/if}

	<form onsubmit={handleLogin} class="space-y-4">
		<div class="space-y-1.5">
			<label class="text-xs font-semibold text-base-content/80" for="userNameOrEmail">Email or Username</label>
			<div class="relative">
				<div class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-base-content/40">
					<User class="h-4 w-4" />
				</div>
				<input
					id="userNameOrEmail"
					type="text"
					class="glass-input input input-sm h-11 w-full rounded-xl pl-9 text-sm focus:outline-none"
					placeholder="student@example.com or username"
					bind:value={userNameOrEmail}
					required
				/>
			</div>
		</div>

		<div class="space-y-1.5">
			<div class="flex items-center justify-between">
				<label class="text-xs font-semibold text-base-content/80" for="password">Password</label>
			</div>
			<div class="relative">
				<div class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-base-content/40">
					<Lock class="h-4 w-4" />
				</div>
				<input
					id="password"
					type="password"
					class="glass-input input input-sm h-11 w-full rounded-xl pl-9 text-sm focus:outline-none"
					placeholder="••••••••"
					bind:value={password}
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
				<span>Sign In</span>
				<ArrowRight class="h-4 w-4 ml-1" />
			{/if}
		</button>
	</form>

	<div class="text-center text-xs text-base-content/60">
		Don't have an account?
		<a href="/register" class="font-semibold text-primary hover:underline ml-1">Create an account</a>
	</div>
</div>
