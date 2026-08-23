import { apiClient } from './client.ts';
import type { QuizExam, QuizQuestion, QuizSubmission, StudentAnswer } from './types.ts';

export const examsApi = {
	async getExamsByCourse(courseId: string, customFetch?: typeof fetch): Promise<QuizExam[]> {
		return apiClient.get<QuizExam[]>(`/api/v1/exams?courseId=${courseId}`, undefined, customFetch);
	},

	async getExamById(id: string, customFetch?: typeof fetch): Promise<QuizExam> {
		return apiClient.get<QuizExam>(`/api/v1/exams/${id}`, undefined, customFetch);
	},

	async startExam(quizId: string): Promise<{
		submissionId: string;
		activeSessionToken: string;
		startedAtUtc: string;
		maxAllowedEndTimeUtc: string;
		durationMinutes: number;
		mode: string;
	}> {
		return apiClient.post(`/api/v1/exams/${quizId}/start`);
	},

	async getQuestions(
		submissionId: string,
		customFetch?: typeof fetch
	): Promise<{
		questions: QuizQuestion[];
		savedAnswers: Record<string, { selectedOptionIds: string[]; essayText?: string }>;
		remainingSeconds: number;
		maxAllowedEndTimeUtc: string;
		mode: string;
	}> {
		return apiClient.get(`/api/v1/exams/submissions/${submissionId}/questions`, undefined, customFetch);
	},

	async saveAnswer(
		submissionId: string,
		answer: {
			questionId: string;
			selectedOptionIds: string[];
			essayText?: string;
		}
	): Promise<{ isSaved: boolean }> {
		return apiClient.post(`/api/v1/exams/submissions/${submissionId}/answers`, answer);
	},

	async presignSnapshot(
		submissionId: string,
		contentType: string = 'image/webp'
	): Promise<{
		uploadUrl: string;
		storageObjectKey: string;
		expiresAtUtc: string;
	}> {
		return apiClient.post(`/api/v1/exams/submissions/${submissionId}/snapshots/presign`, {
			contentType
		});
	},

	async finishExam(
		submissionId: string
	): Promise<{
		submissionId: string;
		status: string;
		totalScore?: number;
		isGraded: boolean;
	}> {
		return apiClient.post(`/api/v1/exams/submissions/${submissionId}/finish`);
	},

	async getResult(
		submissionId: string,
		customFetch?: typeof fetch
	): Promise<{
		submission: QuizSubmission;
		questionsWithReview?: Array<QuizQuestion & {
			selectedOptionIds: string[];
			essayText?: string;
			awardedScore?: number;
			isCorrect?: boolean;
		}>;
	}> {
		return apiClient.get(`/api/v1/exams/submissions/${submissionId}/result`, undefined, customFetch);
	},

	async createExam(data: {
		courseId: string;
		title: string;
		mode: string;
		durationMinutes: number;
		passingScore: number;
		maxAllowedViolations: number;
	}): Promise<{ id: string }> {
		return apiClient.post<{ id: string }>('/api/v1/exams', data);
	},

	async addQuestions(
		quizId: string,
		questions: Array<{
			text: string;
			type: string;
			points: number;
			orderIndex: number;
			options: Array<{ id?: string; text: string; isCorrect: boolean }>;
			explanation?: string;
		}>
	): Promise<{ count: number }> {
		return apiClient.post<{ count: number }>(`/api/v1/exams/${quizId}/questions`, { questions });
	},

	async publishExam(quizId: string): Promise<void> {
		return apiClient.post(`/api/v1/exams/${quizId}/publish`);
	}
};
