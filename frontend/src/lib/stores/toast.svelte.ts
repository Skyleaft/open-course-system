export type ToastType = 'info' | 'success' | 'warning' | 'error';

export interface ToastMessage {
	id: string;
	type: ToastType;
	message: string;
	durationMs?: number;
}

class ToastStore {
	toasts = $state<ToastMessage[]>([]);

	add(type: ToastType, message: string, durationMs: number = 4000) {
		const id = Math.random().toString(36).substring(2, 9);
		const toast: ToastMessage = { id, type, message, durationMs };
		this.toasts = [...this.toasts, toast];

		if (durationMs > 0) {
			setTimeout(() => {
				this.remove(id);
			}, durationMs);
		}
	}

	info(message: string, durationMs?: number) {
		this.add('info', message, durationMs);
	}

	success(message: string, durationMs?: number) {
		this.add('success', message, durationMs);
	}

	warning(message: string, durationMs?: number) {
		this.add('warning', message, durationMs);
	}

	error(message: string, durationMs?: number) {
		this.add('error', message, durationMs);
	}

	remove(id: string) {
		this.toasts = this.toasts.filter((t) => t.id !== id);
	}
}

export const toast = new ToastStore();
