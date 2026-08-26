import { apiClient } from './client.ts';

export interface LiveCandidateViolation {
	violationType: string;
	details?: string;
	timestampUtc: string;
}

export interface LiveCandidate {
	submissionId: string;
	studentId: string;
	studentName: string;
	studentEmail?: string;
	studentAvatarUrl?: string;
	status: 'InProgress' | 'Completed' | 'Disqualified' | 'TimedOut' | string;
	isOnline: boolean;
	violationCount: number;
	violations?: LiveCandidateViolation[];
	latestSnapshotPresignedUrl?: string;
	latestSnapshotTimeUtc?: string;
	remainingSeconds: number;
	startedAtUtc: string;
	maxAllowedEndTimeUtc: string;
	snapshotsCaptured: number;
}

export interface CandidateViolation {
	id: string;
	studentId: string;
	studentName?: string;
	submissionId: string;
	violationType: string;
	details?: string;
	timestampUtc: string;
	violationCount: number;
}

export interface CandidateSnapshotItem {
	id: string;
	submissionId: string;
	storageKey: string;
	presignedUrl: string;
	capturedAtUtc: string;
}

export const proctorApi = {
	async getLiveCandidates(quizId: string, customFetch?: typeof fetch): Promise<LiveCandidate[]> {
		return apiClient.get<LiveCandidate[]>(`/api/v1/proctor/exams/${quizId}/live-candidates`, undefined, customFetch);
	},

	async getCandidateSnapshots(submissionId: string, customFetch?: typeof fetch): Promise<CandidateSnapshotItem[]> {
		return apiClient.get<CandidateSnapshotItem[]>(`/api/v1/proctor/submissions/${submissionId}/snapshots`, undefined, customFetch);
	},

	async sendWarning(submissionId: string, message: string): Promise<void> {
		return apiClient.post(`/api/v1/proctor/submissions/${submissionId}/warn`, { message });
	},

	async broadcastExamMessage(examId: string, message: string): Promise<void> {
		return apiClient.post(`/api/v1/proctor/exams/${examId}/broadcast`, { message });
	},

	async forceDisconnect(submissionId: string, reason: string = 'Disqualified by proctor'): Promise<void> {
		return apiClient.post(`/api/v1/proctor/submissions/${submissionId}/force-disconnect`, { reason });
	}
};

