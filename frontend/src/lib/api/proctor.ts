import { apiClient } from './client.ts';

export interface LiveCandidate {
	studentId: string;
	studentName: string;
	submissionId: string;
	startedAtUtc: string;
	maxAllowedEndTimeUtc: string;
	violationCount: number;
	lastHeartbeatUtc: string;
	latestSnapshotPresignedUrl?: string;
	status: string;
}

export interface CandidateViolation {
	id: string;
	studentId: string;
	submissionId: string;
	violationType: string;
	details?: string;
	timestampUtc: string;
	violationCount: number;
}

export const proctorApi = {
	async getLiveCandidates(quizId: string, customFetch?: typeof fetch): Promise<LiveCandidate[]> {
		return apiClient.get<LiveCandidate[]>(`/api/v1/proctor/exams/${quizId}/live-candidates`, undefined, customFetch);
	},

	async sendWarning(submissionId: string, message: string): Promise<void> {
		return apiClient.post(`/api/v1/proctor/submissions/${submissionId}/warn`, { message });
	},

	async forceDisconnect(submissionId: string, reason: string = 'Disqualified by proctor'): Promise<void> {
		return apiClient.post(`/api/v1/proctor/submissions/${submissionId}/force-disconnect`, { reason });
	}
};
