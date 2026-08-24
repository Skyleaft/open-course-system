import type { Handle } from '@sveltejs/kit/hooks';

export const handle: Handle = async ({ event, resolve }) => {
	const accessToken = event.cookies.get('access_token');
	const refreshToken = event.cookies.get('refresh_token');

	// Set auth metadata in event.locals
	event.locals.accessToken = accessToken || null;
	event.locals.refreshToken = refreshToken || null;

	const response = await resolve(event, {
		transformPageChunk: ({ html }) => html
	});

	// Security Headers
	response.headers.set('X-Frame-Options', 'SAMEORIGIN');
	response.headers.set('X-Content-Type-Options', 'nosniff');
	response.headers.set('Referrer-Policy', 'strict-origin-when-cross-origin');

	return response;
};
