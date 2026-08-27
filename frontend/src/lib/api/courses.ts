import { apiClient } from './client.ts';
import type {
	Course,
	CourseSection,
	Lesson,
	Assignment,
	PaginatedList,
	EnrollmentResultDto,
	SectionResultDto,
	LessonResultDto,
	AssignmentResultDto,
	SubmissionResultDto,
	CourseAccessType,
	LessonType,
	CourseFilterParams,
	EnrolledCourseDto,
	CourseProgressDto,
	LessonProgressResultDto,
	CourseStudentEnrollmentDto,
	AdminEnrollStudentRequest,
	AdminEnrollStudentResultDto
} from './types.ts';

export const coursesApi = {
	async getCourses(
		params?: CourseFilterParams,
		customFetch?: typeof fetch
	): Promise<PaginatedList<Course>> {
		const query = new URLSearchParams();
		const accessType = params?.accessType;
		if (accessType && accessType !== 'All') query.set('AccessType', accessType);

		const search = params?.searchTerm || params?.search;
		if (search && search.trim()) query.set('SearchTerm', search.trim());

		if (params?.instructorId) query.set('InstructorId', params.instructorId);
		if (params?.minPrice !== undefined && params.minPrice !== null) query.set('MinPrice', params.minPrice.toString());
		if (params?.maxPrice !== undefined && params.maxPrice !== null) query.set('MaxPrice', params.maxPrice.toString());
		if (params?.isPublished !== undefined && params.isPublished !== null) query.set('IsPublished', params.isPublished.toString());
		if (params?.sortBy) query.set('SortBy', params.sortBy);
		if (params?.sortOrder) query.set('SortOrder', params.sortOrder);

		const pageIndex = params?.pageIndex || params?.pageNumber || params?.page || 1;
		query.set('PageIndex', pageIndex.toString());

		const pageSize = params?.pageSize || 10;
		query.set('PageSize', pageSize.toString());

		const qs = query.toString() ? `?${query.toString()}` : '';
		return apiClient.get<PaginatedList<Course>>(`/api/v1/courses${qs}`, undefined, customFetch);
	},

	async getCourseById(id: string, customFetch?: typeof fetch): Promise<Course> {
		return apiClient.get<Course>(`/api/v1/courses/${id}`, undefined, customFetch);
	},

	async enroll(courseId: string, enrollmentKey?: string): Promise<EnrollmentResultDto> {
		return apiClient.post<EnrollmentResultDto>(`/api/v1/courses/${courseId}/enroll`, {
			enrollmentKey: enrollmentKey || undefined
		});
	},

	async createCourse(data: {
		title: string;
		description?: string;
		accessType: CourseAccessType | string;
		price: number;
		enrollmentKey?: string;
		thumbnailUrl?: string;
	}): Promise<Course> {
		return apiClient.post<Course>('/api/v1/courses', data);
	},

	async updateCourse(
		id: string,
		data: {
			title: string;
			description?: string;
			accessType: CourseAccessType | string;
			price: number;
			enrollmentKey?: string;
			thumbnailUrl?: string;
		}
	): Promise<Course> {
		return apiClient.put<Course>(`/api/v1/courses/${id}`, data);
	},

	async publishCourse(id: string): Promise<{ id: string; isPublished: boolean }> {
		return apiClient.post<{ id: string; isPublished: boolean }>(`/api/v1/courses/${id}/publish`);
	},

	async unpublishCourse(id: string): Promise<void> {
		return apiClient.post<void>(`/api/v1/courses/${id}/unpublish`);
	},

	async addSection(courseId: string, data: { title: string }): Promise<SectionResultDto> {
		return apiClient.post<SectionResultDto>(`/api/v1/courses/${courseId}/sections`, {
			title: data.title
		});
	},

	async addLesson(
		sectionId: string,
		data: {
			title: string;
			type?: LessonType | string;
			contentUrl?: string | null;
			textContent?: string | null;
			durationMinutes?: number;
		}
	): Promise<LessonResultDto> {
		return apiClient.post<LessonResultDto>(`/api/v1/courses/sections/${sectionId}/lessons`, {
			title: data.title,
			type: data.type || 'Text',
			contentUrl: data.contentUrl || undefined,
			textContent: data.textContent || undefined,
			durationMinutes: data.durationMinutes ?? 0
		});
	},

	async deleteCourse(id: string): Promise<void> {
		return apiClient.delete<void>(`/api/v1/courses/${id}`);
	},

	async updateSection(
		sectionId: string,
		data: { title: string; orderIndex?: number }
	): Promise<SectionResultDto> {
		return apiClient.put<SectionResultDto>(`/api/v1/courses/sections/${sectionId}`, data);
	},

	async deleteSection(sectionId: string): Promise<void> {
		return apiClient.delete<void>(`/api/v1/courses/sections/${sectionId}`);
	},

	async getLesson(lessonId: string): Promise<LessonResultDto> {
		return apiClient.get<LessonResultDto>(`/api/v1/courses/lessons/${lessonId}`);
	},

	async updateLesson(
		lessonId: string,
		data: {
			title: string;
			type?: LessonType | string;
			contentUrl?: string | null;
			textContent?: string | null;
			durationMinutes?: number;
			orderIndex?: number;
		}
	): Promise<LessonResultDto> {
		return apiClient.put<LessonResultDto>(`/api/v1/courses/lessons/${lessonId}`, {
			title: data.title,
			type: data.type || 'Text',
			contentUrl: data.contentUrl || undefined,
			textContent: data.textContent || undefined,
			durationMinutes: data.durationMinutes ?? 0,
			orderIndex: data.orderIndex
		});
	},

	async deleteLesson(lessonId: string): Promise<void> {
		return apiClient.delete<void>(`/api/v1/courses/lessons/${lessonId}`);
	},

	async addAssignment(
		courseId: string,
		data: {
			title: string;
			instruction: string;
			deadlineUtc: string;
			maxScore: number;
		}
	): Promise<AssignmentResultDto> {
		return apiClient.post<AssignmentResultDto>(`/api/v1/courses/${courseId}/assignments`, data);
	},

	async createAssignment(
		courseId: string,
		data: {
			title: string;
			instruction: string;
			deadlineUtc: string;
			maxScore: number;
		}
	): Promise<AssignmentResultDto> {
		return this.addAssignment(courseId, data);
	},

	async presignCourseThumbnail(
		fileName: string,
		contentType?: string
	): Promise<{ storageKey: string; uploadUrl: string; downloadUrl: string; expiresAtUtc: string }> {
		return apiClient.post<{ storageKey: string; uploadUrl: string; downloadUrl: string; expiresAtUtc: string }>(
			'/api/v1/courses/thumbnails/presign',
			{
				fileName,
				contentType: contentType || 'image/jpeg'
			}
		);
	},

	async uploadCourseThumbnail(file: File): Promise<string> {
		const presign = await this.presignCourseThumbnail(file.name, file.type || 'image/jpeg');
		const res = await fetch(presign.uploadUrl, {
			method: 'PUT',
			headers: {
				'Content-Type': file.type || 'image/jpeg'
			},
			body: file
		});

		if (!res.ok) {
			throw new Error(`Failed to upload thumbnail image: ${res.statusText}`);
		}

		return presign.downloadUrl;
	},

	async presignAssignmentSubmission(
		assignmentId: string,
		fileName: string,
		contentType?: string
	): Promise<{ storageKey: string; uploadUrl: string; expiresAtUtc: string }> {
		return apiClient.post<{ storageKey: string; uploadUrl: string; expiresAtUtc: string }>(
			`/api/v1/courses/assignments/${assignmentId}/presign`,
			{
				fileName,
				contentType: contentType || 'application/octet-stream'
			}
		);
	},

	async submitAssignment(
		assignmentId: string,
		data: {
			fileUrl?: string;
			fileAttachmentUrl?: string;
			studentNotes?: string;
		}
	): Promise<SubmissionResultDto> {
		const fileUrl = data.fileUrl || data.fileAttachmentUrl || '';
		return apiClient.post<SubmissionResultDto>(`/api/v1/courses/assignments/${assignmentId}/submit`, {
			fileUrl
		});
	},

	async getEnrolledCourses(customFetch?: typeof fetch): Promise<EnrolledCourseDto[]> {
		return apiClient.get<EnrolledCourseDto[]>('/api/v1/courses/enrolled', undefined, customFetch);
	},

	async getCourseProgress(courseId: string, customFetch?: typeof fetch): Promise<CourseProgressDto> {
		return apiClient.get<CourseProgressDto>(`/api/v1/courses/${courseId}/progress`, undefined, customFetch);
	},

	async completeLesson(
		courseId: string,
		lessonId: string,
		isCompleted?: boolean
	): Promise<LessonProgressResultDto> {
		return apiClient.post<LessonProgressResultDto>(`/api/v1/courses/${courseId}/lessons/${lessonId}/complete`, {
			isCompleted
		});
	},

	async attachExam(
		courseId: string,
		examId: string,
		data?: { orderIndex?: number; isMandatory?: boolean }
	): Promise<void> {
		return apiClient.post<void>(`/api/v1/courses/${courseId}/exams`, {
			examId,
			orderIndex: data?.orderIndex ?? 1,
			isMandatory: data?.isMandatory ?? true
		});
	},

	async detachExam(courseId: string, examId: string): Promise<void> {
		return apiClient.delete<void>(`/api/v1/courses/${courseId}/exams/${examId}`);
	},

	async getCourseEnrollments(
		courseId: string,
		params?: { pageIndex?: number; pageSize?: number; search?: string },
		customFetch?: typeof fetch
	): Promise<PaginatedList<CourseStudentEnrollmentDto>> {
		const query = new URLSearchParams();
		if (params?.pageIndex) query.set('pageIndex', params.pageIndex.toString());
		if (params?.pageSize) query.set('pageSize', params.pageSize.toString());
		if (params?.search) query.set('search', params.search);
		const qs = query.toString() ? `?${query.toString()}` : '';
		return apiClient.get<PaginatedList<CourseStudentEnrollmentDto>>(`/api/v1/courses/${courseId}/enrollments${qs}`, undefined, customFetch);
	},

	async adminEnrollStudent(
		courseId: string,
		data: AdminEnrollStudentRequest
	): Promise<AdminEnrollStudentResultDto> {
		return apiClient.post<AdminEnrollStudentResultDto>(`/api/v1/courses/${courseId}/enrollments`, data);
	},

	async adminRemoveEnrollment(
		courseId: string,
		enrollmentId: string
	): Promise<boolean> {
		return apiClient.delete<boolean>(`/api/v1/courses/${courseId}/enrollments/${enrollmentId}`);
	}
};


