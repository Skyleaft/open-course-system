import type { User, UserRole } from '#lib/api/types.ts';
import { authApi } from '#lib/api/auth.ts';
import { apiClient } from '#lib/api/client.ts';
import { browser } from '$app/env';

class AuthStore {
	user = $state<User | null>(null);
	isLoading = $state<boolean>(true);

	isAuthenticated = $derived(this.user !== null);
	isStudent = $derived(this.user?.roles?.includes('Student') ?? false);
	isInstructor = $derived(this.user?.roles?.includes('Instructor') ?? false);
	isProctor = $derived(this.user?.roles?.includes('Proctor') ?? false);
	isAdmin = $derived(this.user?.roles?.includes('Admin') ?? false);

	constructor() {
		if (browser) {
			this.initialize();
		}
	}

	async initialize(customFetch?: typeof fetch) {
		this.isLoading = true;
		try {
			if (apiClient.getAccessToken() || !browser) {
				this.user = await authApi.getMe(customFetch);
			} else {
				this.user = null;
			}
		} catch {
			this.user = null;
		} finally {
			this.isLoading = false;
		}
	}

	setUser(user: User | null) {
		this.user = user;
	}

	async logout() {
		await authApi.logout();
		this.user = null;
	}
}

export const authStore = new AuthStore();
