<script lang="ts">
	import { UploadCloud, File as FileIcon, CheckCircle2, AlertCircle, X } from '@lucide/svelte';

	interface Props {
		accept?: string;
		maxSizeMb?: number;
		onFileSelected?: (file: File) => void;
		onUploadComplete?: (objectKeyOrUrl: string) => void;
		presignFn?: (file: File) => Promise<{ uploadUrl: string; storageKey: string }>;
	}

	let {
		accept = '*/*',
		maxSizeMb = 50,
		onFileSelected,
		onUploadComplete,
		presignFn
	}: Props = $props();

	let isDragging = $state(false);
	let selectedFile = $state<File | null>(null);
	let isUploading = $state(false);
	let progress = $state(0);
	let errorMessage = $state<string | null>(null);
	let isSuccess = $state(false);

	function handleFiles(files: FileList | null) {
		if (!files || files.length === 0) return;
		const file = files[0];

		if (file.size > maxSizeMb * 1024 * 1024) {
			errorMessage = `File exceeds maximum allowed size of ${maxSizeMb} MB`;
			return;
		}

		errorMessage = null;
		selectedFile = file;
		if (onFileSelected) onFileSelected(file);

		if (presignFn) {
			uploadDirect(file);
		}
	}

	async function uploadDirect(file: File) {
		if (!presignFn) return;
		isUploading = true;
		progress = 10;
		errorMessage = null;

		try {
			const { uploadUrl, storageKey } = await presignFn(file);
			progress = 30;

			const xhr = new XMLHttpRequest();
			xhr.open('PUT', uploadUrl, true);
			xhr.setRequestHeader('Content-Type', file.type || 'application/octet-stream');

			xhr.upload.onprogress = (e) => {
				if (e.lengthComputable) {
					progress = Math.round(30 + (e.loaded / e.total) * 65);
				}
			};

			xhr.onload = () => {
				if (xhr.status >= 200 && xhr.status < 300) {
					progress = 100;
					isSuccess = true;
					if (onUploadComplete) onUploadComplete(storageKey);
				} else {
					errorMessage = `Upload failed with HTTP status ${xhr.status}`;
				}
				isUploading = false;
			};

			xhr.onerror = () => {
				errorMessage = 'Network error during file upload.';
				isUploading = false;
			};

			xhr.send(file);
		} catch (err: any) {
			errorMessage = err?.message || 'Failed to initiate upload.';
			isUploading = false;
		}
	}

	function clearFile() {
		selectedFile = null;
		progress = 0;
		isSuccess = false;
		errorMessage = null;
	}
</script>

<div class="w-full space-y-3">
	{#if !selectedFile}
		<label
			class="glass-card flex flex-col items-center justify-center rounded-2xl border-2 border-dashed border-white/20 p-8 text-center transition-all duration-300 hover:border-primary/50 hover:bg-base-100/50 cursor-pointer {isDragging ? 'border-primary bg-primary/10' : ''}"
			ondragover={(e) => { e.preventDefault(); isDragging = true; }}
			ondragleave={() => isDragging = false}
			ondrop={(e) => { e.preventDefault(); isDragging = false; handleFiles(e.dataTransfer?.files || null); }}
		>
			<div class="gradient-accent mb-3 flex h-12 w-12 items-center justify-center rounded-2xl text-white shadow-md">
				<UploadCloud class="h-6 w-6" />
			</div>
			<div class="text-sm font-semibold text-base-content">
				Click to upload or drag & drop
			</div>
			<div class="text-xs text-base-content/60 mt-1">
				Max file size: {maxSizeMb} MB
			</div>
			<input
				type="file"
				class="hidden"
				{accept}
				onchange={(e) => handleFiles((e.target as HTMLInputElement).files)}
			/>
		</label>
	{:else}
		<div class="glass-card flex items-center justify-between rounded-2xl border border-white/10 p-4">
			<div class="flex items-center gap-3 overflow-hidden">
				<div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary border border-primary/20">
					{#if isSuccess}
						<CheckCircle2 class="h-5 w-5 text-success" />
					{:else}
						<FileIcon class="h-5 w-5" />
					{/if}
				</div>
				<div class="truncate text-left">
					<div class="truncate text-xs font-semibold text-base-content">{selectedFile.name}</div>
					<div class="text-[10px] text-base-content/60">
						{(selectedFile.size / (1024 * 1024)).toFixed(2)} MB
					</div>
				</div>
			</div>

			{#if !isUploading}
				<button
					class="btn btn-ghost btn-circle btn-sm text-base-content/60 hover:text-base-content"
					onclick={clearFile}
					aria-label="Remove file"
				>
					<X class="h-4 w-4" />
				</button>
			{/if}
		</div>

		{#if isUploading}
			<div class="space-y-1">
				<div class="flex justify-between text-[11px] text-base-content/60">
					<span>Uploading directly to storage...</span>
					<span>{progress}%</span>
				</div>
				<progress class="progress progress-primary w-full h-1.5" value={progress} max="100"></progress>
			</div>
		{/if}
	{/if}

	{#if errorMessage}
		<div class="flex items-center gap-2 rounded-xl bg-error/10 border border-error/20 p-3 text-xs text-error">
			<AlertCircle class="h-4 w-4 shrink-0" />
			<span>{errorMessage}</span>
		</div>
	{/if}
</div>
