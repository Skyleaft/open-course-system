<script lang="ts">
	import { examsApi } from '#lib/api/exams.ts';
	import type { ExamHubClient } from '#lib/signalr/exam-hub.ts';
	import { onMount, onDestroy } from 'svelte';

	const browser = typeof window !== 'undefined';

	interface Props {
		submissionId: string;
		stream: MediaStream | null;
		examHub: ExamHubClient;
	}

	let { submissionId, stream, examHub }: Props = $props();

	let videoElement: HTMLVideoElement | null = null;
	let worker: Worker | null = null;
	let isCapturing = $state(false);

	onMount(() => {
		if (browser && stream) {
			// Initialize hidden video element
			videoElement = document.createElement('video');
			videoElement.srcObject = stream;
			videoElement.autoplay = true;
			videoElement.muted = true;
			videoElement.playsInline = true;
			videoElement.play().catch(console.warn);

			// Initialize Web Worker
			worker = new Worker(new URL('../../workers/snapshot.worker.ts', import.meta.url), {
				type: 'module'
			});

			worker.onmessage = (e) => {
				if (e.data?.type === 'TRIGGER_SNAPSHOT') {
					captureAndUploadSnapshot();
				}
			};

			worker.postMessage({ action: 'START' });
		}
	});

	onDestroy(() => {
		if (worker) {
			worker.postMessage({ action: 'STOP' });
			worker.terminate();
			worker = null;
		}
		if (videoElement) {
			videoElement.srcObject = null;
			videoElement = null;
		}
	});

	async function captureAndUploadSnapshot() {
		if (!videoElement || !videoElement.videoWidth || isCapturing) return;
		isCapturing = true;

		try {
			// 1. Render frame to canvas
			const canvas = document.createElement('canvas');
			canvas.width = videoElement.videoWidth || 640;
			canvas.height = videoElement.videoHeight || 480;

			const ctx = canvas.getContext('2d');
			if (!ctx) return;
			ctx.drawImage(videoElement, 0, 0, canvas.width, canvas.height);

			// 2. Export WebP Blob
			canvas.toBlob(async (blob) => {
				if (!blob) return;

				try {
					// 3. Request presigned URL from API
					const presign = await examsApi.presignSnapshot(submissionId, 'image/webp');

					// 4. Direct HTTP PUT upload to MinIO
					const uploadRes = await fetch(presign.uploadUrl, {
						method: 'PUT',
						headers: {
							'Content-Type': 'image/webp'
						},
						body: blob
					});

					if (uploadRes.ok) {
						// 5. Notify ExamHub
						await examHub.reportSnapshotUploaded(submissionId, presign.storageObjectKey);
					}
				} catch (uploadErr) {
					console.warn('Snapshot direct upload to MinIO failed:', uploadErr);
				} finally {
					isCapturing = false;
				}
			}, 'image/webp', 0.8);
		} catch (err) {
			console.warn('Snapshot capture failed:', err);
			isCapturing = false;
		}
	}
</script>

<!-- Headless snapshot engine background orchestrator -->
