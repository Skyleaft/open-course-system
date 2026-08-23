import { apiClient } from './client.ts';
import type { Course, CourseSection, Lesson, Assignment } from './types.ts';

export const coursesApi = {
	async getCourses(
		params?: { category?: string; accessType?: string; search?: string; page?: number; pageSize?: number },
		customFetch?: typeof fetch
	): Promise<{ items: Course[]; totalCount: number; page: number; pageSize: number }> {
		const query = new URLSearchParams();
		if (params?.category) query.set('category', params.category);
		if (params?.accessType) query.set('accessType', params.accessType);
		if (params?.search) query.set('search', params.search);
		if (params?.page) query.set('page', params.page.toString());
		if (params?.pageSize) query.set('pageSize', params.pageSize.toString());

		const qs = query.toString() ? `?${query.toString()}` : '';
		return apiClient.get(`/api/v1/courses${qs}`, undefined, customFetch);
	},

	async getCourseById(id: string, customFetch?: typeof fetch): Promise<Course> {
		return apiClient.get<Course>(`/api/v1/courses/${id}`, undefined, customFetch);
	},

	async enroll(courseId: string, enrollmentKey?: string): Promise<{ enrollmentId: string }> {
		return apiClient.post<{ enrollmentId: string }>(`/api/v1/courses/${courseId}/enroll`, {
			enrollmentKey
		});
	},

	async createCourse(data: {
		title: string;
		description: string;
		accessType: string;
		price: number;
		enrollmentKey?: string;
	}): Promise<{ id: string }> {
		return apiClient.post<{ id: string }>('/api/v1/courses', data);
	},

	async updateCourse(
		id: string,
		data: {
			title: string;
			description: string;
			accessType: string;
			price: number;
			enrollmentKey?: string;
		}
	): Promise<void> {
		return apiClient.put(`/api/v1/courses/${id}`, data);
	},

	async publishCourse(id: string): Promise<void> {
		return apiClient.post(`/api/v1/courses/${id}/publish`);
	},

	async addSection(courseId: string, data: { title: string; orderIndex: number }): Promise<{ id: string }> {
		return apiClient.post<{ id: string }>(`/api/v1/courses/${courseId}/sections`, data);
	},

	async addLesson(
		sectionId: string,
		data: {
			title: string;
			type: string;
			contentUrl: string;
			durationMinutes: number;
			orderIndex: number;
		}
	): Promise<{ id: string }> {
		return apiClient.post<{ id: string }>(`/api/v1/courses/sections/${sectionId}/lessons`, data);
	},

	async addAssignment(
		courseId: string,
		data: {
			title: string;
			instruction: string;
			deadlineUtc: string;
			maxScore: number;
		}
	): Promise<{ id: string }> {
		return apiClient.post<{ id: string }>(`/api/v1/courses/${courseId}/assignments`, data);
	},

	async submitAssignment(
		assignmentId: string,
		data: {
			fileAttachmentUrl: string;
			studentNotes?: string;
		}
	): Promise<{ id: string }> {
		return apiClient.post<{ id: string }>(`/api/v1/courses/assignments/${assignmentId}/submit`, data);
	}
};
