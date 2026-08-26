import { apiClient } from './client.ts';
import type {
	ExamRuleDto,
	CreateExamRuleRequest,
	UpdateExamRuleRequest
} from './types.ts';

export const examRulesApi = {
	async listRules(
		params?: { systemPresetsOnly?: boolean },
		customFetch?: typeof fetch
	): Promise<ExamRuleDto[]> {
		const searchParams = new URLSearchParams();
		if (params?.systemPresetsOnly !== undefined) {
			searchParams.set('systemPresetsOnly', String(params.systemPresetsOnly));
		}
		const qs = searchParams.toString();
		return apiClient.get<ExamRuleDto[]>(`/api/v1/exams/rules${qs ? `?${qs}` : ''}`, undefined, customFetch);
	},

	async getRule(id: string, customFetch?: typeof fetch): Promise<ExamRuleDto> {
		return apiClient.get<ExamRuleDto>(`/api/v1/exams/rules/${id}`, undefined, customFetch);
	},

	async createRule(data: CreateExamRuleRequest): Promise<string> {
		return apiClient.post<string>('/api/v1/exams/rules', data);
	},

	async updateRule(id: string, data: UpdateExamRuleRequest): Promise<boolean> {
		return apiClient.put<boolean>(`/api/v1/exams/rules/${id}`, data);
	},

	async deleteRule(id: string): Promise<boolean> {
		return apiClient.delete<boolean>(`/api/v1/exams/rules/${id}`);
	}
};
