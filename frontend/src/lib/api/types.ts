// Core API Envelope and Data Types

export interface ApiResponse<T> {
	data?: T;
	isSuccess: boolean;
	error?: {
		code: string;
		message: string;
		details?: Record<string, string[]>;
	};
}

export interface ApiErrorResponse {
	code: string;
	message: string;
	details?: Record<string, string[]>;
}

export type UserRole = 'Student' | 'Instructor' | 'Admin' | 'Proctor' | string;

export interface UserInfoDto {
	id: string;
	userName: string;
	email: string;
	fullName: string;
	picture?: string | null;
	roles: string[];
}

export interface UserResponseDto {
	id: string;
	userName: string;
	email: string;
	fullName: string;
	firstName?: string | null;
	lastName?: string | null;
	picture?: string | null;
	roles: string[];
	createdAt?: string;
}

export interface User {
	id: string;
	userName?: string;
	email: string;
	fullName: string;
	firstName?: string | null;
	lastName?: string | null;
	picture?: string | null;
	roles: string[];
	lastSeen?: string;
	isActive?: boolean;
	createdAt?: string;
}

export interface LoginResponseDto {
	accessToken: string;
	refreshToken: string;
	expiresAt: string;
	user: UserInfoDto;
}

export interface RefreshTokenResponseDto {
	accessToken: string;
	refreshToken: string;
	expiresAt: string;
}

// Backward compatibility alias
export type AuthResponse = LoginResponseDto;

// Course Types
export type CourseAccessType = 'OpenFree' | 'OpenPaid' | 'PrivateWithKey';
export type LessonType = 'Video' | 'PdfDocument' | 'DownloadableFile';

export interface Lesson {
	id: string;
	sectionId: string;
	title: string;
	type: LessonType;
	contentUrl: string;
	durationMinutes: number;
	orderIndex: number;
}

export interface CourseSection {
	id: string;
	courseId: string;
	title: string;
	orderIndex: number;
	lessons: Lesson[];
}

export interface Assignment {
	id: string;
	courseId: string;
	title: string;
	instruction: string;
	deadlineUtc: string;
	maxScore: number;
}

export interface Course {
	id: string;
	title: string;
	description: string;
	accessType: CourseAccessType;
	price: number;
	isPublished: boolean;
	sections?: CourseSection[];
	assignments?: Assignment[];
	isEnrolled?: boolean;
}

// Exam Types
export type QuizMode = 'Simulation' | 'RealExam';
export type QuestionType = 'SingleChoice' | 'MultipleChoice' | 'Essay' | 'TrueFalse';
export type SubmissionStatus = 'InProgress' | 'Completed' | 'Disqualified' | 'TimedOut';

export interface QuestionOption {
	id: string;
	text: string;
	isCorrect?: boolean;
}

export interface QuizQuestion {
	id: string;
	quizId: string;
	text: string;
	type: QuestionType;
	points: number;
	orderIndex: number;
	options: QuestionOption[];
	explanation?: string;
}

export interface StudentAnswer {
	questionId: string;
	selectedOptionIds: string[];
	essayText?: string;
}

export interface QuizExam {
	id: string;
	courseId: string;
	title: string;
	mode: QuizMode;
	durationMinutes: number;
	passingScore: number;
	maxAllowedViolations: number;
	settings?: Record<string, any>;
	isPublished: boolean;
	questionsCount?: number;
}

export interface QuizSubmission {
	id: string;
	quizId: string;
	studentId: string;
	mode: QuizMode;
	startedAtUtc: string;
	maxAllowedEndTimeUtc: string;
	finishedAtUtc?: string;
	status: SubmissionStatus;
	totalScore: number;
	activeSessionToken: string;
	violations: Array<{
		type: string;
		details?: string;
		timestampUtc: string;
	}>;
}

// Assessments & Certificates
export interface Certificate {
	id: string;
	certificateNumber: string;
	studentId: string;
	courseId: string;
	finalScore: number;
	certificateHash: string;
	status: 'Issued' | 'Revoked';
	issuedAtUtc: string;
	studentName?: string;
	courseTitle?: string;
}

export interface GradeRecord {
	id: string;
	studentId: string;
	courseId: string;
	itemType: 'Quiz' | 'Assignment';
	referenceId: string;
	score: number;
	maxScore: number;
	weightPercentage: number;
	evaluatedAtUtc: string;
	title?: string;
}

// Communications
export interface Announcement {
	id: string;
	courseId?: string | null;
	authorId: string;
	authorName?: string;
	title: string;
	content: string;
	isPinned: boolean;
	createdAtUtc: string;
}

export interface ThreadComment {
	id: string;
	threadId: string;
	authorId: string;
	authorName?: string;
	parentCommentId?: string | null;
	content: string;
	createdAtUtc: string;
	replies?: ThreadComment[];
}

export interface DiscussionThread {
	id: string;
	courseId: string;
	lessonId?: string | null;
	authorId: string;
	authorName?: string;
	title: string;
	content: string;
	isClosed: boolean;
	createdAtUtc: string;
	commentsCount?: number;
	comments?: ThreadComment[];
}
