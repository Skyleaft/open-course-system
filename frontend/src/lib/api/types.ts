// Shared Domain Enums & Types

export type UserRole = 'Student' | 'Instructor' | 'Admin' | 'Proctor';

export interface UserInfoDto {
	id: string;
	email: string;
	fullName?: string;
	firstName?: string;
	lastName?: string;
	role?: UserRole;
	roles?: string[];
	avatarUrl?: string;
	createdAtUtc?: string;
}

export type User = UserInfoDto;
export type UserProfile = UserInfoDto;

export interface UserResponseDto {
	user: UserInfoDto;
}

export interface LoginResponseDto {
	accessToken: string;
	refreshToken: string;
	expiresInSeconds: number;
	user: UserInfoDto;
}

export interface RefreshTokenResponseDto {
	accessToken: string;
	refreshToken: string;
	expiresInSeconds: number;
}

export interface AuthResponse {
	accessToken: string;
	refreshToken: string;
	expiresInSeconds: number;
	user: UserProfile;
}

// Course Types
export type CourseAccessType = 'OpenFree' | 'OpenPaid' | 'PrivateWithKey';
export type LessonType = 'Text' | 'Video' | 'PdfDocument' | 'DownloadableFile';

export interface CourseFilterParams {
	category?: string;
	accessType?: string;
	isPublished?: boolean;
	search?: string;
	searchTerm?: string;
	instructorId?: string;
	minPrice?: number;
	maxPrice?: number;
	sortBy?: string;
	sortOrder?: string;
	pageIndex?: number;
	pageNumber?: number;
	page?: number;
	pageSize?: number;
}

export interface Lesson {
	id: string;
	sectionId: string;
	title: string;
	type: LessonType;
	contentUrl?: string | null;
	textContent?: string | null;
	durationMinutes: number;
	orderIndex: number;
	createdAtUtc?: string;
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

export interface CourseExam {
	id: string;
	courseId: string;
	examId: string;
	orderIndex: number;
	isMandatory: boolean;
	createdAtUtc: string;
	examTitle?: string;
}

export interface Course {
	id: string;
	instructorId?: string;
	title: string;
	description?: string;
	accessType: CourseAccessType | string;
	price: number;
	isPublished: boolean;
	thumbnailUrl?: string | null;
	createdAtUtc?: string;
	sections?: CourseSection[];
	assignments?: Assignment[];
	exams?: CourseExam[];
	isEnrolled?: boolean;
	enrolledStudentsCount?: number;
}

export interface EnrollmentResultDto {
	enrollmentId: string;
	userId: string;
	courseId: string;
	enrolledAtUtc: string;
}

export interface SectionResultDto {
	id: string;
	courseId: string;
	title: string;
	orderIndex: number;
}

export interface LessonResultDto {
	id: string;
	sectionId: string;
	title: string;
	type: string;
	contentUrl?: string | null;
	textContent?: string | null;
	durationMinutes: number;
	orderIndex: number;
}

export interface AssignmentResultDto {
	id: string;
	courseId: string;
	title: string;
	instruction: string;
	deadlineUtc: string;
	maxScore: number;
}

export interface SubmissionResultDto {
	submissionId: string;
	assignmentId: string;
	studentId: string;
	fileUrl: string;
	submittedAtUtc: string;
}

export interface EnrolledCourseDto {
	id: string;
	title: string;
	description?: string | null;
	thumbnailUrl?: string | null;
	accessType: string;
	instructorId: string;
	enrolledAtUtc: string;
	progressPercent: number;
	totalLessonsCount: number;
	completedLessonsCount: number;
	totalAssignmentsCount: number;
	completedAssignmentsCount: number;
	totalExamsCount: number;
	completedExamsCount: number;
	lastAccessedLessonId?: string | null;
	lastAccessedLessonTitle?: string | null;
}

export interface CourseProgressDto {
	courseId: string;
	completedLessonIds: string[];
	completedAssignmentIds: string[];
	completedExamIds: string[];
	progressPercent: number;
	lastAccessedLessonId?: string | null;
}

export interface LessonProgressResultDto {
	courseId: string;
	lessonId: string;
	isCompleted: boolean;
	completedAtUtc?: string | null;
	updatedCourseProgressPercent: number;
}

export interface CourseStudentEnrollmentDto {
	enrollmentId: string;
	userId: string;
	fullName: string;
	email: string;
	avatarUrl?: string | null;
	enrolledAtUtc: string;
	progressPercent: number;
	completedLessonsCount: number;
	totalLessonsCount: number;
	completedAssignmentsCount: number;
	totalAssignmentsCount: number;
	lastAccessedAtUtc?: string | null;
}

export interface AdminEnrollStudentRequest {
	userId?: string;
	email?: string;
}

export interface AdminEnrollStudentResultDto {
	enrollmentId: string;
	courseId: string;
	userId: string;
	studentName: string;
	studentEmail: string;
	enrolledAtUtc: string;
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

export interface QuestionBank {
	id: string;
	title: string;
	description?: string | null;
	category?: string | null;
	tags?: string[];
	createdBy: string;
	updatedBy?: string | null;
	createdAtUtc: string;
	updatedAtUtc?: string | null;
	questionCount?: number;
	questions?: QuestionBankItem[];
}

export interface BankQuestion {
	id: string;
	bankId?: string;
	bankTitle?: string;
	bankCategory?: string;
	quizId?: string;
	examId?: string;
	questionText?: string;
	text?: string;
	type: QuestionType;
	points: number;
	orderIndex?: number;
	explanation?: string;
	category?: string;
	tags?: string[];
	options: QuestionOption[];
	createdBy?: string;
	updatedBy?: string | null;
	createdAtUtc?: string;
	updatedAtUtc?: string | null;
}

export type QuestionBankItem = BankQuestion;
export type QuizQuestion = BankQuestion;

export interface QuizSection {
	id: string;
	examId: string;
	questionBankId: string;
	questionBankTitle?: string;
	title: string;
	description?: string | null;
	orderIndex: number;
	pointsOverride?: number | null;
	questionCount?: number | null;
	questions?: BankQuestion[];
	questionBank?: QuestionBank;
}

export interface StudentAnswer {
	questionId: string;
	selectedOptionIds: string[];
	essayText?: string;
}

export interface StudentOptionDto {
	id: string;
	text: string;
}

export interface StudentQuestionDto {
	id: string;
	questionText: string;
	type: QuestionType | string;
	points: number;
	displayOrder: number;
	selectedOptionIds?: string[];
	essayText?: string | null;
	options: StudentOptionDto[];
}

export interface StudentExamPaperDto {
	submissionId: string;
	examId: string;
	title: string;
	mode: QuizMode | string;
	startedAtUtc: string;
	maxAllowedEndTimeUtc: string;
	activeSessionToken: string;
	questions: StudentQuestionDto[];
}

export interface OptionReviewDto {
	id: string;
	text: string;
	isCorrect: boolean;
}

export interface QuestionReviewDto {
	questionId: string;
	questionText: string;
	type: string;
	points: number;
	awardedScore?: number | null;
	selectedOptionIds: string[];
	essayText?: string | null;
	explanation?: string | null;
	options: OptionReviewDto[];
}

export interface ExamResultDetailsDto {
	submissionId: string;
	examId: string;
	examTitle: string;
	mode: string;
	status: string;
	score?: number | null;
	isPassed?: boolean | null;
	startedAtUtc: string;
	submittedAtUtc?: string | null;
	questions: QuestionReviewDto[];
}

export interface ListExamsParams {
	mode?: QuizMode | string;
	isPublished?: boolean;
	search?: string;
	searchTerm?: string;
	pageIndex?: number;
	pageSize?: number;
}

export interface ExamSummaryDto {
	id: string;
	instructorId: string;
	title: string;
	description?: string | null;
	mode: QuizMode | string;
	durationMinutes: number;
	passingScore: number;
	maxAllowedViolations: number;
	isPublished: boolean;
	sectionsCount?: number;
	questionsCount: number;
	createdBy?: string;
	updatedBy?: string | null;
	createdAtUtc: string;
}

export interface QuizExam {
	id: string;
	instructorId?: string;
	title: string;
	description?: string | null;
	mode: QuizMode | string;
	durationMinutes: number;
	passingScore: number;
	maxAllowedViolations: number;
	maxAttempts?: number;
	availableFromUtc?: string | null;
	availableToUtc?: string | null;
	isPublished: boolean;
	shuffleQuestions?: boolean;
	shuffleOptions?: boolean;
	sectionsCount?: number;
	questionsCount?: number;
	sections?: QuizSection[];
	questions?: QuizQuestion[];
	createdBy?: string;
	updatedBy?: string | null;
	createdAtUtc?: string;
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
	title?: string;
	score: number;
	maxScore: number;
	weightPercentage: number;
	evaluatedAtUtc: string;
}

export interface DeadLetterJob {
	id: string;
	streamMessageId: string;
	errorMessage: string;
	failedAtUtc: string;
	isResolved: boolean;
	retryCount: number;
}

// Communications
export interface Announcement {
	id: string;
	courseId?: string;
	instructorId?: string;
	authorId?: string;
	authorName?: string;
	title: string;
	content: string;
	targetScope?: 'CourseOnly' | 'GlobalAll';
	isPinned?: boolean;
	createdAtUtc: string;
}

export interface DiscussionThread {
	id: string;
	courseId: string;
	authorId: string;
	title: string;
	content: string;
	isPinned: boolean;
	isClosed: boolean;
	createdAtUtc: string;
	authorName?: string;
	comments?: DiscussionComment[];
}

export interface DiscussionComment {
	id: string;
	threadId: string;
	authorId: string;
	content: string;
	isInstructorEndorsed: boolean;
	createdAtUtc: string;
	authorName?: string;
}

export type ThreadComment = DiscussionComment;

// Orders & Checkout
export type OrderStatus = 'Pending' | 'Paid' | 'Cancelled' | 'Expired';

export interface Order {
	id: string;
	userId: string;
	courseId: string;
	courseTitle: string;
	amount: number;
	currency: string;
	status: OrderStatus;
	snapToken?: string;
	redirectUrl?: string;
	createdAtUtc: string;
	paidAtUtc?: string;
}

// Common Responses
export interface ApiResponse<T> {
	success: boolean;
	data: T;
	message?: string;
	errors?: string[];
	statusCode: number;
}

export interface ApiErrorResponse {
	success: boolean;
	message: string;
	errors?: string[];
	statusCode: number;
}

export interface PaginatedList<T> {
	items: T[];
	totalCount: number;
	pageIndex: number;
	pageSize: number;
	totalPages: number;
	hasPreviousPage: boolean;
	hasNextPage: boolean;
}
