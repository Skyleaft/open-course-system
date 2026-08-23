import { apiClient } from './client.ts';

export interface Order {
	id: string;
	userId: string;
	courseId: string;
	amount: number;
	currency: string;
	status: 'Pending' | 'Paid' | 'Expired' | 'Failed';
	createdAtUtc: string;
	paidAtUtc?: string;
}

export const paymentsApi = {
	async createCheckout(courseId: string): Promise<{ orderId: string; amount: number; paymentUrl?: string }> {
		return apiClient.post('/api/v1/payments/checkout', { courseId });
	},

	async getOrder(orderId: string, customFetch?: typeof fetch): Promise<Order> {
		return apiClient.get<Order>(`/api/v1/payments/orders/${orderId}`, undefined, customFetch);
	},

	// Mock webhook trigger for development/testing
	async mockPayOrder(orderId: string): Promise<{ isPaid: boolean }> {
		return apiClient.post(`/api/v1/payments/mock-pay/${orderId}`);
	}
};
