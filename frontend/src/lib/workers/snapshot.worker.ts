// Web Worker for periodic webcam snapshot capture triggers

let timer: number | null = null;

function scheduleNextSnapshot() {
	// Random interval between 30 and 60 seconds
	const intervalMs = Math.floor(Math.random() * (60000 - 30000 + 1)) + 30000;

	timer = self.setTimeout(() => {
		self.postMessage({ type: 'TRIGGER_SNAPSHOT' });
		scheduleNextSnapshot();
	}, intervalMs) as unknown as number;
}

self.onmessage = (e: MessageEvent) => {
	if (e.data?.action === 'START') {
		if (timer) clearTimeout(timer);
		scheduleNextSnapshot();
	} else if (e.data?.action === 'STOP') {
		if (timer) clearTimeout(timer);
		timer = null;
	}
};

export {};
