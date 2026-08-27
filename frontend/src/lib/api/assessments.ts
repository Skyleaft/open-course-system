import { apiClient } from './client.ts';
import type { Certificate, GradeRecord } from './types.ts';

export const assessmentsApi = {
	async getMyCertificates(customFetch?: typeof fetch): Promise<Certificate[]> {
		return apiClient.get<Certificate[]>('/api/v1/certificates/my-certificates', undefined, customFetch);
	},

	async getCertificateByNumber(certNumber: string, customFetch?: typeof fetch): Promise<Certificate> {
		return apiClient.get<Certificate>(`/api/v1/certificates/${certNumber}`, undefined, customFetch);
	},

	async verifyCertificate(certificateHash: string, customFetch?: typeof fetch): Promise<{
		isValid: boolean;
		certificate: Certificate;
	}> {
		return apiClient.get(`/api/v1/certificates/verify/${certificateHash}`, undefined, customFetch);
	},

	async getMyGrades(courseId?: string, customFetch?: typeof fetch): Promise<GradeRecord[]> {
		const qs = courseId ? `?courseId=${courseId}` : '';
		return apiClient.get<GradeRecord[]>(`/api/v1/assessments/grades${qs}`, undefined, customFetch);
	},

	async issueCertificate(
		studentId: string,
		courseId: string,
		finalScore: number
	): Promise<Certificate> {
		const res = await apiClient.post<any>(
			'/api/v1/certificates/issue',
			{ studentId, courseId, finalScore }
		);
		return res?.data || res;
	}
};
