import { apiClient } from './client.ts';
import type { User, AuthResponse } from './types.ts';

export const authApi = {
	async register(data: { email: string; password: string; fullName: string }): Promise<{ userId: string }> {
		return apiClient.post<{ userId: string }>('/api/v1/auth/register', data);
	},

	async login(data: { email: string; password: string }): Promise<AuthResponse> {
		const response = await apiClient.post<AuthResponse>('/api/v1/auth/login', data);
		if (response?.tokens?.accessToken) {
			apiClient.setTokens(response.tokens.accessToken);
		}
		return response;
	},

	async googleLogin(idToken: string): Promise<AuthResponse> {
		const response = await apiClient.post<AuthResponse>('/api/v1/auth/google', { idToken });
		if (response?.tokens?.accessToken) {
			apiClient.setTokens(response.tokens.accessToken);
		}
		return response;
	},

	async getMe(customFetch?: typeof fetch): Promise<User> {
		return apiClient.get<User>('/api/v1/auth/me', undefined, customFetch);
	},

	async logout(): Promise<void> {
		try {
			await apiClient.post('/api/v1/auth/logout');
		} finally {
			apiClient.setTokens(null);
		}
	}
};
