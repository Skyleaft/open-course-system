import * as signalR from '@microsoft/signalr';
import { apiClient } from '#lib/api/client.ts';

const browser = typeof window !== 'undefined';

const HUB_BASE_URL = import.meta.env.PUBLIC_SIGNALR_URL || 'http://localhost:8080/hubs';

export function createHubConnection(hubPath: string): signalR.HubConnection | null {
	if (!browser) return null;

	const fullUrl = hubPath.startsWith('http') ? hubPath : `${HUB_BASE_URL}${hubPath.startsWith('/') ? '' : '/'}${hubPath}`;

	return new signalR.HubConnectionBuilder()
		.withUrl(fullUrl, {
			accessTokenFactory: () => apiClient.getAccessToken() || '',
			transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
		})
		.withAutomaticReconnect({
			nextRetryDelayInMilliseconds: (retryContext) => {
				// Exponential backoff with jitter: 0s, 2s, 5s, 10s, 30s
				if (retryContext.previousRetryCount === 0) return 0;
				if (retryContext.previousRetryCount === 1) return 2000;
				if (retryContext.previousRetryCount === 2) return 5000;
				if (retryContext.previousRetryCount === 3) return 10000;
				return 30000;
			}
		})
		.configureLogging(signalR.LogLevel.Warning)
		.build();
}
