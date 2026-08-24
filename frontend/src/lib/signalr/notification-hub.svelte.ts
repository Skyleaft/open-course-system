import * as signalR from '@microsoft/signalr';
import { createHubConnection } from './connection.ts';
import { toast } from '#lib/stores/toast.svelte.ts';

export class NotificationHubClient {
	private connection: signalR.HubConnection | null = null;
	isConnected = $state<boolean>(false);

	constructor() {
		this.connection = createHubConnection('notification');
		if (this.connection) {
			this.connection.onreconnecting(() => {
				this.isConnected = false;
			});
			this.connection.onreconnected(() => {
				this.isConnected = true;
			});
			this.connection.onclose(() => {
				this.isConnected = false;
			});

			this.connection.on('ReceiveNotification', (title: string, message: string) => {
				toast.info(`${title}: ${message}`);
			});
		}
	}

	async start(): Promise<void> {
		if (!this.connection) return;
		if (this.connection.state === signalR.HubConnectionState.Connected) return;

		try {
			await this.connection.start();
			this.isConnected = true;
		} catch (err) {
			this.isConnected = false;
		}
	}

	async stop(): Promise<void> {
		if (this.connection) {
			try {
				await this.connection.stop();
			} finally {
				this.isConnected = false;
			}
		}
	}
}

export const notificationHub = new NotificationHubClient();
