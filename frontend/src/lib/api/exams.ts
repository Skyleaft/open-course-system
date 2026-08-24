import { apiClient } from './client.ts';
import type {
	PaginatedList,
	QuizExam,
	ExamSummaryDto,
	ListExamsParams,
	QuizQuestion,
	QuizSubmission,
	StudentAnswer,
	QuestionType
} from './types.ts';

export const examsApi = {
	async listExams(params?: ListExamsParams, customFetch?: typeof fetch): Promise<PaginatedList<QuizExam>> {
		const searchParams = new URLSearchParams();
		if (params?.mode) searchParams.set('mode', params.mode);
		if (params?.isPublished !== undefined && params.isPublished !== null) {
			searchParams.set('isPublished', String(params.isPublished));
		}
		const search = params?.searchTerm || params?.search;
		if (search && search.trim()) {
			searchParams.set('search', search.trim());
		}
		const pageIndex = params?.pageIndex ?? 1;
		const pageSize = params?.pageSize ?? 20;
		searchParams.set('pageIndex', String(pageIndex));
		searchParams.set('pageSize', String(pageSize));

		const queryStr = searchParams.toString();
		const endpoint = `/api/v1/exams${queryStr ? `?${queryStr}` : ''}`;
		return apiClient.get<PaginatedList<QuizExam>>(endpoint, undefined, customFetch);
	},

	async getExamsByCourse(courseId: string, customFetch?: typeof fetch): Promise<PaginatedList<QuizExam>> {
		return apiClient.get<PaginatedList<QuizExam>>(`/api/v1/courses/${courseId}/exams`, undefined, customFetch);
	},

	async getExamById(id: string, customFetch?: typeof fetch): Promise<QuizExam> {
		return apiClient.get<QuizExam>(`/api/v1/exams/${id}`, undefined, customFetch);
	},

	async createExam(data: {
		title: string;
		description?: string;
		mode: string;
		durationMinutes: number;
		passingScore: number;
		maxAllowedViolations?: number;
		maxAttempts?: number;
		availableFromUtc?: string;
		availableToUtc?: string;
		shuffleQuestions?: boolean;
		shuffleOptions?: boolean;
	}): Promise<{ id: string }> {
		return apiClient.post<{ id: string }>('/api/v1/exams', data);
	},

	async updateExam(id: string, data: {
		title: string;
		description?: string;
		mode: string;
		durationMinutes: number;
		passingScore: number;
		maxAllowedViolations?: number;
		maxAttempts?: number;
		availableFromUtc?: string;
		availableToUtc?: string;
		shuffleQuestions?: boolean;
		shuffleOptions?: boolean;
	}): Promise<QuizExam> {
		return apiClient.put<QuizExam>(`/api/v1/exams/${id}`, data);
	},

	async deleteExam(id: string): Promise<boolean> {
		return apiClient.delete<boolean>(`/api/v1/exams/${id}`);
	},

	async publishExam(id: string): Promise<void> {
		return apiClient.post(`/api/v1/exams/${id}/publish`);
	},

	// Question Bank CRUD
	async addQuestion(examId: string | undefined, data: {
		questionText: string;
		type: QuestionType | string;
		points: number;
		explanation?: string;
		category?: string;
		tags?: string[];
		options?: Array<{ id?: string; text: string; isCorrect: boolean }>;
		sectionId?: string;
	}): Promise<QuizQuestion> {
		const endpoint = examId ? `/api/v1/exams/${examId}/questions` : '/api/v1/exams/questions';
		return apiClient.post<QuizQuestion>(endpoint, { ...data, examId });
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
			category?: string;
			tags?: string[];
		}>
	): Promise<{ count: number }> {
		let count = 0;
		for (const q of questions) {
			await this.addQuestion(quizId, {
				questionText: q.text,
				type: q.type,
				points: q.points,
				explanation: q.explanation,
				category: q.category,
				tags: q.tags,
				options: q.options
			});
			count++;
		}
		return { count };
	},

	async getQuestion(questionId: string, customFetch?: typeof fetch): Promise<QuizQuestion> {
		return apiClient.get<QuizQuestion>(`/api/v1/exams/questions/${questionId}`, undefined, customFetch);
	},

	async updateQuestion(questionId: string, data: {
		questionText: string;
		type: QuestionType | string;
		points: number;
		explanation?: string;
		category?: string;
		tags?: string[];
		options?: Array<{ id?: string; text: string; isCorrect: boolean }>;
	}): Promise<QuizQuestion> {
		return apiClient.put<QuizQuestion>(`/api/v1/exams/questions/${questionId}`, data);
	},

	async deleteQuestion(questionId: string): Promise<boolean> {
		return apiClient.delete<boolean>(`/api/v1/exams/questions/${questionId}`);
	},

	// Exam Execution & Proctoring
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
	}
};
