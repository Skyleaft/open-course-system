import { apiClient } from './client.ts';
import type { ApiResponse } from './types.ts';

export interface RevenueAnalytics {
	grossMerchandiseValue: number;
	averageOrderValue: number;
	totalOrders: number;
	paidOrders: number;
	pendingOrders: number;
	failedOrders: number;
	expiredOrders: number;
	conversionRate: number;
	dailyTrends: Array<{
		date: string;
		revenue: number;
		orderCount: number;
	}>;
	topCourses: Array<{
		courseId: string;
		courseTitle: string;
		totalRevenue: number;
		salesCount: number;
	}>;
}

export interface SystemHealth {
	unresolvedDlqCount: number;
	totalDlqCount: number;
	totalCertificatesIssued: number;
	totalGradeRecords: number;
	redisStreamStatus: string;
	storageStatus: string;
	recentDeadLetters: Array<{
		id: string;
		streamMessageId: string;
		submissionId: string;
		errorMessage: string;
		failedAtUtc: string;
		isResolved: boolean;
	}>;
	checkedAtUtc: string;
}

export interface SecurityViolationsSummary {
	totalSubmissions: number;
	totalViolations: number;
	disqualifiedCount: number;
	disqualificationRate: number;
	violationTypes: Array<{
		type: string;
		count: number;
		percentage: number;
	}>;
	highRiskExams: Array<{
		examId: string;
		examTitle: string;
		totalAttempts: number;
		violationsCount: number;
		disqualifiedCount: number;
	}>;
}

export interface CourseAnalytics {
	courseId: string;
	courseTitle: string;
	totalEnrolled: number;
	completedStudentsCount: number;
	completionRate: number;
	totalSections: number;
	totalLessons: number;
	totalAssignments: number;
	pendingAssignmentReviewsCount: number;
	sectionDropOffs: Array<{
		sectionId: string;
		sectionTitle: string;
		orderIndex: number;
		studentsCompletedCount: number;
		retentionRate: number;
	}>;
}

export interface ExamAnalytics {
	examId: string;
	examTitle: string;
	totalSubmissions: number;
	completedSubmissions: number;
	disqualifiedSubmissions: number;
	averageScore: number;
	medianScore: number;
	highestScore: number;
	lowestScore: number;
	standardDeviation: number;
	passingScore: number;
	passedCount: number;
	failedCount: number;
	passRate: number;
	scoreBuckets: Array<{
		rangeLabel: string;
		minScore: number;
		maxScore: number;
		studentCount: number;
	}>;
	itemPsychometrics: Array<{
		questionId: string;
		questionText: string;
		questionType: string;
		maxPoints: number;
		totalAttempts: number;
		correctCount: number;
		difficultyIndex: number;
		difficultyLabel: string;
		discriminationIndex: number;
		discriminationStatus: string;
	}>;
}

export interface ProctorLiveSummary {
	activeExamsCount: number;
	activeExamineesCount: number;
	highRiskCandidatesCount: number;
	flaggedCandidates: Array<{
		submissionId: string;
		examId: string;
		examTitle: string;
		studentId: string;
		violationsCount: number;
		riskScore: number;
		riskLevel: string;
		startedAtUtc: string;
		maxAllowedEndTimeUtc: string;
	}>;
	activeExams: Array<{
		examId: string;
		title: string;
		activeExaminees: number;
	}>;
}

export interface StudentDashboardOverview {
	activeCoursesCount: number;
	completedCoursesCount: number;
	certificatesCount: number;
	pendingAssignmentsCount: number;
	enrolledCourses: Array<{
		courseId: string;
		title: string;
		thumbnailUrl?: string;
		accessType: string;
		totalLessons: number;
		completedLessons: number;
		progressPercentage: number;
		lastLessonId?: string;
		lastLessonTitle?: string;
	}>;
	upcomingDeadlines: Array<{
		id: string;
		title: string;
		itemType: string;
		courseTitle: string;
		deadlineUtc: string;
		remainingHours: number;
		isUrgent: boolean;
	}>;
	competencyRadar: Array<{
		subject: string;
		value: number;
		fullMark: number;
	}>;
}

export const dashboardApi = {
	getAdminRevenueAnalytics: async (params?: { fromUtc?: string; toUtc?: string }): Promise<RevenueAnalytics> => {
		const searchParams = new URLSearchParams();
		if (params?.fromUtc) searchParams.append('fromUtc', params.fromUtc);
		if (params?.toUtc) searchParams.append('toUtc', params.toUtc);
		const qs = searchParams.toString() ? `?${searchParams.toString()}` : '';
		return apiClient.get<RevenueAnalytics>(`/api/v1/dashboard/admin/revenue-analytics${qs}`);
	},

	getAdminSystemHealth: async (): Promise<SystemHealth> => {
		return apiClient.get<SystemHealth>('/api/v1/dashboard/admin/system-health');
	},

	getAdminSecurityViolations: async (): Promise<SecurityViolationsSummary> => {
		return apiClient.get<SecurityViolationsSummary>('/api/v1/dashboard/admin/security-violations');
	},

	getInstructorCourseAnalytics: async (courseId: string): Promise<CourseAnalytics> => {
		return apiClient.get<CourseAnalytics>(`/api/v1/dashboard/instructor/courses/${courseId}/analytics`);
	},

	getInstructorExamAnalytics: async (examId: string): Promise<ExamAnalytics> => {
		return apiClient.get<ExamAnalytics>(`/api/v1/dashboard/instructor/exams/${examId}/analytics`);
	},

	getProctorLiveSummary: async (): Promise<ProctorLiveSummary> => {
		return apiClient.get<ProctorLiveSummary>('/api/v1/dashboard/proctor/live-summary');
	},

	getStudentDashboardOverview: async (): Promise<StudentDashboardOverview> => {
		return apiClient.get<StudentDashboardOverview>('/api/v1/dashboard/student/overview');
	}
};
