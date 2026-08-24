import { apiClient } from './client.ts';
import type {
	PaginatedList,
	QuizExam,
	ExamSummaryDto,
	ListExamsParams,
	QuizQuestion,
	QuizSubmission,
	StudentAnswer,
	QuestionType,
	QuestionBank,
	BankQuestion
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
		sections?: Array<{
			id?: string;
			questionBankId: string;
			title: string;
			description?: string | null;
			pointsOverride?: number | null;
			questionCount?: number | null;
			orderIndex: number;
		}>;
	}): Promise<QuizExam> {
		return apiClient.put<QuizExam>(`/api/v1/exams/${id}`, data);
	},

	async deleteExam(id: string): Promise<boolean> {
		return apiClient.delete<boolean>(`/api/v1/exams/${id}`);
	},

	async publishExam(id: string): Promise<void> {
		return apiClient.post(`/api/v1/exams/${id}/publish`);
	},

	// Question Bank Packages CRUD
	async listQuestionBanks(
		params?: { search?: string; category?: string; pageIndex?: number; pageSize?: number },
		customFetch?: typeof fetch
	): Promise<PaginatedList<QuestionBank>> {
		const searchParams = new URLSearchParams();
		if (params?.category) searchParams.set('category', params.category);
		if (params?.search) searchParams.set('search', params.search);
		if (params?.pageIndex) searchParams.set('pageIndex', String(params.pageIndex));
		if (params?.pageSize) searchParams.set('pageSize', String(params.pageSize));

		const queryStr = searchParams.toString();
		return apiClient.get<PaginatedList<QuestionBank>>(
			`/api/v1/exams/question-banks${queryStr ? `?${queryStr}` : ''}`,
			undefined,
			customFetch
		);
	},

	async createQuestionBank(data: {
		title: string;
		description?: string;
		category?: string;
		tags?: string[];
	}): Promise<string> {
		return apiClient.post<string>('/api/v1/exams/question-banks', data);
	},

	async getQuestionBank(id: string, customFetch?: typeof fetch): Promise<QuestionBank> {
		return apiClient.get<QuestionBank>(`/api/v1/exams/question-banks/${id}`, undefined, customFetch);
	},

	async updateQuestionBank(
		id: string,
		data: {
			title: string;
			description?: string;
			category?: string;
			tags?: string[];
		}
	): Promise<boolean> {
		return apiClient.put<boolean>(`/api/v1/exams/question-banks/${id}`, data);
	},

	async deleteQuestionBank(id: string): Promise<boolean> {
		return apiClient.delete<boolean>(`/api/v1/exams/question-banks/${id}`);
	},

	// Bank Questions Query & CRUD
	async listQuestions(
		params?: {
			bankId?: string;
			search?: string;
			type?: string;
			category?: string;
			pageIndex?: number;
			pageSize?: number;
		},
		customFetch?: typeof fetch
	): Promise<PaginatedList<BankQuestion>> {
		const searchParams = new URLSearchParams();
		if (params?.bankId) searchParams.set('bankId', params.bankId);
		if (params?.search) searchParams.set('search', params.search);
		if (params?.type) searchParams.set('type', params.type);
		if (params?.category) searchParams.set('category', params.category);
		if (params?.pageIndex) searchParams.set('pageIndex', String(params.pageIndex));
		if (params?.pageSize) searchParams.set('pageSize', String(params.pageSize));

		const queryStr = searchParams.toString();
		return apiClient.get<PaginatedList<BankQuestion>>(
			`/api/v1/exams/questions${queryStr ? `?${queryStr}` : ''}`,
			undefined,
			customFetch
		);
	},

	async addQuestion(
		bankOrExamId: string | undefined,
		data: {
			bankId?: string;
			questionText: string;
			type: QuestionType | string;
			points: number;
			explanation?: string;
			category?: string;
			tags?: string[];
			options?: Array<{ id?: string; text: string; isCorrect: boolean }>;
			sectionId?: string;
		}
	): Promise<QuizQuestion> {
		const targetBankId = data.bankId || bankOrExamId;
		const endpoint = targetBankId
			? `/api/v1/exams/question-banks/${targetBankId}/questions`
			: '/api/v1/exams/questions';
		return apiClient.post<QuizQuestion>(endpoint, { ...data, bankId: targetBankId });
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
