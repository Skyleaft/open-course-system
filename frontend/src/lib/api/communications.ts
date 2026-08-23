import { apiClient } from './client.ts';
import type { Announcement, DiscussionThread, ThreadComment } from './types.ts';

export const communicationsApi = {
	async getAnnouncements(courseId?: string, customFetch?: typeof fetch): Promise<Announcement[]> {
		const qs = courseId ? `?courseId=${courseId}` : '';
		return apiClient.get<Announcement[]>(`/api/v1/communications/announcements${qs}`, undefined, customFetch);
	},

	async createAnnouncement(data: {
		courseId?: string | null;
		title: string;
		content: string;
		isPinned?: boolean;
	}): Promise<{ id: string }> {
		return apiClient.post<{ id: string }>('/api/v1/communications/announcements', data);
	},

	async getThreads(courseId: string, lessonId?: string, customFetch?: typeof fetch): Promise<DiscussionThread[]> {
		const query = new URLSearchParams({ courseId });
		if (lessonId) query.set('lessonId', lessonId);
		return apiClient.get<DiscussionThread[]>(`/api/v1/communications/threads?${query.toString()}`, undefined, customFetch);
	},

	async createThread(data: {
		courseId: string;
		lessonId?: string | null;
		title: string;
		content: string;
	}): Promise<{ id: string }> {
		return apiClient.post<{ id: string }>('/api/v1/communications/threads', data);
	},

	async addComment(
		threadId: string,
		data: {
			content: string;
			parentCommentId?: string | null;
		}
	): Promise<{ id: string }> {
		return apiClient.post<{ id: string }>(`/api/v1/communications/threads/${threadId}/comments`, data);
	},

	async closeThread(threadId: string): Promise<void> {
		return apiClient.post(`/api/v1/communications/threads/${threadId}/close`);
	}
};
