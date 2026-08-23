import { apiClient } from './client.ts';
import type {
	User,
	UserInfoDto,
	UserResponseDto,
	LoginResponseDto,
	RefreshTokenResponseDto
} from './types.ts';

export interface LoginRequest {
	userNameOrEmail?: string;
	email?: string;
	password: string;
	rememberMe?: boolean;
}

export interface RegisterRequest {
	email: string;
	password: string;
	userName?: string;
	fullName?: string;
	firstName?: string;
	lastName?: string;
}

export interface RefreshTokenRequest {
	accessToken: string;
	refreshToken: string;
}

export interface AssignRoleRequest {
	userId: string;
	roleName: string;
}

export const authApi = {
	async register(data: RegisterRequest): Promise<UserResponseDto> {
		return apiClient.post<UserResponseDto>('/api/v1/auth/register', data);
	},

	async login(data: LoginRequest): Promise<LoginResponseDto> {
		const payload = {
			userNameOrEmail: data.userNameOrEmail || data.email || '',
			password: data.password,
			rememberMe: data.rememberMe ?? true
		};

		const response = await apiClient.post<LoginResponseDto>('/api/v1/auth/login', payload);
		if (response?.accessToken) {
			apiClient.setTokens(response.accessToken, response.refreshToken);
		}
		return response;
	},

	async googleLogin(idToken: string): Promise<LoginResponseDto> {
		const response = await apiClient.post<LoginResponseDto>('/api/v1/auth/google', { idToken });
		if (response?.accessToken) {
			apiClient.setTokens(response.accessToken, response.refreshToken);
		}
		return response;
	},

	async refreshToken(data: RefreshTokenRequest): Promise<RefreshTokenResponseDto> {
		const response = await apiClient.post<RefreshTokenResponseDto>('/api/v1/auth/refresh-token', data);
		if (response?.accessToken) {
			apiClient.setTokens(response.accessToken, response.refreshToken);
		}
		return response;
	},

	async getMe(customFetch?: typeof fetch): Promise<UserResponseDto> {
		return apiClient.get<UserResponseDto>('/api/v1/auth/me', undefined, customFetch);
	},

	async assignRole(data: AssignRoleRequest): Promise<void> {
		return apiClient.post('/api/v1/auth/assign-role', data);
	},

	async logout(): Promise<void> {
		try {
			await apiClient.post('/api/v1/auth/logout');
		} finally {
			apiClient.setTokens(null, null);
		}
	}
};

