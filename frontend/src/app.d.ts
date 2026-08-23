import type { User } from '#lib/api/types.ts';

declare global {
	namespace App {
		// interface Error {}
		interface Locals {
			user?: User | null;
			accessToken?: string | null;
			refreshToken?: string | null;
		}
		// interface PageData {}
		// interface PageState {}
		// interface Platform {}
	}
}

export {};
