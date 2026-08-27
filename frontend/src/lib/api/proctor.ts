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

export interface ExamRuleConfigDto {
	name?: string;
	maxAllowedViolations: number;
	forceFullscreen: boolean;
	requireCamera: boolean;
	snapshotIntervalSeconds: number;
	requireMicrophone: boolean;
	canTabSwitch: boolean;
	restrictClipboardAndMouse: boolean;
}

export interface ProctorRoomExam {
	examId: string;
	title: string;
	description?: string;
	durationMinutes: number;
	totalQuestions: number;
	ruleConfig?: ExamRuleConfigDto;
	activeCandidatesCount: number;
	flaggedCount: number;
	isPublished: boolean;
}

export interface ProctorCourseRoom {
	courseId: string;
	courseTitle: string;
	courseDescription?: string;
	thumbnailUrl?: string;
	instructorId: string;
	instructorName?: string;
	enrolledStudentsCount: number;
	totalActiveCandidates: number;
	totalFlaggedViolations: number;
	exams: ProctorRoomExam[];
}

export const proctorApi = {
	async getProctorRooms(customFetch?: typeof fetch): Promise<ProctorCourseRoom[]> {
		return apiClient.get<ProctorCourseRoom[]>('/api/v1/proctor/rooms', undefined, customFetch);
	},

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

