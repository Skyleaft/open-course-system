import type { ApiResponse, ApiErrorResponse } from './types.ts';
import { browser } from '$app/env';

const API_BASE_URL = import.meta.env.PUBLIC_API_URL || 'http://localhost:8080';

export class ApiError extends Error {
	constructor(
		public code: string,
		public message: string,
		public details?: Record<string, string[]>,
		public status?: number
	) {
		super(message);
		this.name = 'ApiError';
	}
}

class ApiClient {
	private baseUrl: string;
	private accessToken: string | null = null;
	private refreshToken: string | null = null;
	private refreshPromise: Promise<boolean> | null = null;

	constructor(baseUrl: string = API_BASE_URL) {
		this.baseUrl = baseUrl;
		if (browser) {
			this.accessToken = localStorage.getItem('access_token');
			this.refreshToken = localStorage.getItem('refresh_token');
		}
	}

	setTokens(accessToken: string | null, refreshToken: string | null = null) {
		this.accessToken = accessToken;
		this.refreshToken = refreshToken;
		if (browser) {
			if (accessToken) {
				localStorage.setItem('access_token', accessToken);
			} else {
				localStorage.removeItem('access_token');
			}

			if (refreshToken) {
				localStorage.setItem('refresh_token', refreshToken);
			} else if (refreshToken === null && accessToken === null) {
				localStorage.removeItem('refresh_token');
			}
		}
	}

	getAccessToken(): string | null {
		return this.accessToken;
	}

	getRefreshToken(): string | null {
		return this.refreshToken;
	}

	private async handleRefreshToken(): Promise<boolean> {
		if (this.refreshPromise) {
			return this.refreshPromise;
		}

		this.refreshPromise = (async () => {
			try {
				const currentAccessToken = this.accessToken;
				const currentRefreshToken = this.refreshToken;

				if (!currentAccessToken || !currentRefreshToken) {
					this.setTokens(null, null);
					return false;
				}

				const response = await fetch(`${this.baseUrl}/api/v1/auth/refresh-token`, {
					method: 'POST',
					headers: { 'Content-Type': 'application/json' },
					body: JSON.stringify({
						accessToken: currentAccessToken,
						refreshToken: currentRefreshToken
					}),
					credentials: 'include' // sends HttpOnly refresh token cookie if enabled
				});

				if (!response.ok) {
					this.setTokens(null, null);
					return false;
				}

				const result: ApiResponse<{ accessToken: string; refreshToken: string; expiresAt: string }> = await response.json();
				if (result.isSuccess && result.data?.accessToken) {
					this.setTokens(result.data.accessToken, result.data.refreshToken);
					return true;
				}

				this.setTokens(null, null);
				return false;
			} catch {
				this.setTokens(null, null);
				return false;
			} finally {
				this.refreshPromise = null;
			}
		})();

		return this.refreshPromise;
	}

	async request<T>(
		path: string,
		options: RequestInit = {},
		customFetch: typeof fetch = fetch
	): Promise<T> {
		const url = path.startsWith('http') ? path : `${this.baseUrl}${path.startsWith('/') ? '' : '/'}${path}`;
		const headers = new Headers(options.headers || {});

		if (!headers.has('Content-Type') && !(options.body instanceof FormData)) {
			headers.set('Content-Type', 'application/json');
		}

		if (this.accessToken && !headers.has('Authorization')) {
			headers.set('Authorization', `Bearer ${this.accessToken}`);
		}

		let response = await customFetch(url, {
			...options,
			headers,
			credentials: 'include'
		});

		// 401 Unauthorized -> Attempt single refresh token rotation
		if (response.status === 401 && browser) {
			const refreshed = await this.handleRefreshToken();
			if (refreshed && this.accessToken) {
				headers.set('Authorization', `Bearer ${this.accessToken}`);
				response = await customFetch(url, {
					...options,
					headers,
					credentials: 'include'
				});
			} else {
				throw new ApiError('UNAUTHORIZED', 'Session expired. Please log in again.', undefined, 401);
			}
		}

		const contentType = response.headers.get('content-type');
		if (contentType && contentType.includes('application/json')) {
			const result: ApiResponse<T> = await response.json();

			if (!response.ok || !result.isSuccess) {
				const err = result.error || { code: 'HTTP_ERROR', message: response.statusText };
				throw new ApiError(err.code, err.message, err.details, response.status);
			}

			return result.data as T;
		}

		if (!response.ok) {
			throw new ApiError('HTTP_ERROR', response.statusText, undefined, response.status);
		}

		return (await response.text()) as unknown as T;
	}

	get<T>(path: string, options?: RequestInit, customFetch?: typeof fetch): Promise<T> {
		return this.request<T>(path, { ...options, method: 'GET' }, customFetch);
	}

	post<T>(path: string, body?: any, options?: RequestInit, customFetch?: typeof fetch): Promise<T> {
		const isFormData = body instanceof FormData;
		return this.request<T>(
			path,
			{
				...options,
				method: 'POST',
				body: isFormData ? body : body !== undefined ? JSON.stringify(body) : undefined
			},
			customFetch
		);
	}

	put<T>(path: string, body?: any, options?: RequestInit, customFetch?: typeof fetch): Promise<T> {
		const isFormData = body instanceof FormData;
		return this.request<T>(
			path,
			{
				...options,
				method: 'PUT',
				body: isFormData ? body : body !== undefined ? JSON.stringify(body) : undefined
			},
			customFetch
		);
	}

	delete<T>(path: string, options?: RequestInit, customFetch?: typeof fetch): Promise<T> {
		return this.request<T>(path, { ...options, method: 'DELETE' }, customFetch);
	}
}

export const apiClient = new ApiClient();
