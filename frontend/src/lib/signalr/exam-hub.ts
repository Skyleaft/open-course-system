import * as signalR from '@microsoft/signalr';
import { createHubConnection } from './connection.ts';
import { toast } from '#lib/stores/toast.svelte.ts';

export class ExamHubClient {
	private connection: signalR.HubConnection | null = null;
	private heartbeatInterval: ReturnType<typeof setInterval> | null = null;

	// Svelte 5 rune reactive connection status
	isConnected = $state<boolean>(false);
	serverDriftMs = $state<number>(0);

	constructor() {
		this.connection = createHubConnection('exam');
		if (this.connection) {
			this.connection.onreconnecting(() => {
				this.isConnected = false;
				toast.warning('Exam connection lost. Reconnecting...');
			});
			this.connection.onreconnected(() => {
				this.isConnected = true;
				toast.success('Exam connection re-established.');
			});
			this.connection.onclose(() => {
				this.isConnected = false;
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
			console.error('Failed to connect to ExamHub:', err);
		}
	}

	async stop(): Promise<void> {
		this.stopHeartbeat();
		if (this.connection) {
			try {
				await this.connection.stop();
			} finally {
				this.isConnected = false;
			}
		}
	}

	// Client-to-Server Methods
	async joinExamRoom(submissionId: string, sessionToken: string): Promise<void> {
		if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
			await this.start();
		}
		if (this.connection) {
			await this.connection.invoke('JoinExamRoom', submissionId, sessionToken);
			this.startHeartbeat(submissionId, sessionToken);
		}
	}

	async heartbeat(submissionId: string, sessionToken: string): Promise<void> {
		if (this.connection?.state === signalR.HubConnectionState.Connected) {
			await this.connection.invoke('Heartbeat', submissionId, sessionToken);
		}
	}

	async reportViolation(submissionId: string, violationType: string, details?: string): Promise<void> {
		if (this.connection?.state === signalR.HubConnectionState.Connected) {
			await this.connection.invoke('ReportViolation', submissionId, violationType, details || null);
		}
	}

	async reportSnapshotUploaded(submissionId: string, objectKey: string): Promise<void> {
		if (this.connection?.state === signalR.HubConnectionState.Connected) {
			await this.connection.invoke('ReportSnapshotUploaded', submissionId, objectKey);
		}
	}

	// Server-to-Client Event Listeners
	onSyncTimer(callback: (remainingSeconds: number, serverTimeUtc: string) => void) {
		this.connection?.on('SyncTimer', (remainingSeconds: number, serverTimeUtc: string) => {
			const serverTime = new Date(serverTimeUtc).getTime();
			this.serverDriftMs = Date.now() - serverTime;
			callback(remainingSeconds, serverTimeUtc);
		});
	}

	onViolationWarning(callback: (currentCount: number, maxAllowed: number) => void) {
		this.connection?.on('ViolationWarning', callback);
	}

	onForceDisconnectExam(callback: (terminationReason: string) => void) {
		this.connection?.on('ForceDisconnectExam', callback);
	}

	// Server-to-Proctor Event Listeners
	onProctorViolationAlert(
		callback: (studentId: string, submissionId: string, violationType: string, count: number) => void
	) {
		this.connection?.on('ProctorViolationAlert', callback);
	}

	onProctorSnapshotReceived(callback: (studentId: string, snapshotPresignedViewUrl: string) => void) {
		this.connection?.on('ProctorSnapshotReceived', callback);
	}

	private startHeartbeat(submissionId: string, sessionToken: string) {
		this.stopHeartbeat();
		this.heartbeatInterval = setInterval(() => {
			this.heartbeat(submissionId, sessionToken).catch((err) => {
				console.warn('Heartbeat tick failed:', err);
			});
		}, 15000);
	}

	private stopHeartbeat() {
		if (this.heartbeatInterval) {
			clearInterval(this.heartbeatInterval);
			this.heartbeatInterval = null;
		}
	}
}
