const browser = typeof window !== 'undefined';

export interface SecurityRules {
	canTabSwitch?: boolean;
	maxTabSwitchesAllowed?: number;
	restrictClipboardAndMouse?: boolean;
	forceFullscreen?: boolean;
	keyboardDetection?: boolean;
}

export interface SecurityInterceptorOptions {
	rules?: SecurityRules;
	onTabSwitch?: () => void;
	onWindowBlur?: () => void;
	onFullscreenExit?: () => void;
}

export function bindSecurityInterceptors(options: SecurityInterceptorOptions): () => void {
	if (!browser) return () => {};

	const rules = options.rules ?? {};
	const shouldCheckTab = rules.canTabSwitch !== true;
	const shouldCheckFullscreen = rules.forceFullscreen !== false;
	const shouldRestrictClipboard = rules.restrictClipboardAndMouse !== false;
	const shouldDetectKeyboard = rules.keyboardDetection !== false;

	// 1. Tab visibility detector
	const handleVisibilityChange = () => {
		if (shouldCheckTab && document.hidden && options.onTabSwitch) {
			options.onTabSwitch();
		}
	};

	// 2. Window focus loss detector
	const handleBlur = () => {
		if (shouldCheckTab && options.onWindowBlur) {
			options.onWindowBlur();
		}
	};

	// 3. Fullscreen exit detector
	const handleFullscreenChange = () => {
		if (shouldCheckFullscreen && !document.fullscreenElement && options.onFullscreenExit) {
			options.onFullscreenExit();
		}
	};

	// 4. Contextmenu and selection lock
	const handleContextMenu = (e: MouseEvent) => {
		if (shouldRestrictClipboard) {
			e.preventDefault();
		}
	};

	const handleCopyPaste = (e: ClipboardEvent) => {
		if (shouldRestrictClipboard) {
			e.preventDefault();
		}
	};

	const handleSelectStart = (e: Event) => {
		if (shouldRestrictClipboard) {
			e.preventDefault();
		}
	};

	// 5. Keyboard shortcuts detector
	const handleKeyDown = (e: KeyboardEvent) => {
		if (!shouldDetectKeyboard) return;

		// Prevent F12, Ctrl+Shift+I, Ctrl+Shift+C, Ctrl+Shift+J, DevTools, Ctrl+C, Ctrl+V, Ctrl+U, Ctrl+P, PrintScreen
		if (
			e.key === 'F12' ||
			e.key === 'PrintScreen' ||
			(e.ctrlKey && e.shiftKey && (e.key === 'I' || e.key === 'i' || e.key === 'C' || e.key === 'c' || e.key === 'J' || e.key === 'j')) ||
			(e.ctrlKey && (e.key === 'c' || e.key === 'C' || e.key === 'v' || e.key === 'V' || e.key === 'u' || e.key === 'U' || e.key === 'p' || e.key === 'P' || e.key === 's' || e.key === 'S')) ||
			(e.altKey && e.key === 'Tab')
		) {
			e.preventDefault();
		}
	};

	if (shouldCheckTab) {
		document.addEventListener('visibilitychange', handleVisibilityChange);
		window.addEventListener('blur', handleBlur);
	}

	if (shouldCheckFullscreen) {
		document.addEventListener('fullscreenchange', handleFullscreenChange);
	}

	if (shouldRestrictClipboard) {
		document.addEventListener('contextmenu', handleContextMenu);
		document.addEventListener('copy', handleCopyPaste);
		document.addEventListener('paste', handleCopyPaste);
		document.addEventListener('cut', handleCopyPaste);
		document.addEventListener('selectstart', handleSelectStart);
	}

	if (shouldDetectKeyboard) {
		document.addEventListener('keydown', handleKeyDown);
	}

	// Cleanup unbinder
	return () => {
		document.removeEventListener('visibilitychange', handleVisibilityChange);
		window.removeEventListener('blur', handleBlur);
		document.removeEventListener('fullscreenchange', handleFullscreenChange);
		document.removeEventListener('contextmenu', handleContextMenu);
		document.removeEventListener('copy', handleCopyPaste);
		document.removeEventListener('paste', handleCopyPaste);
		document.removeEventListener('cut', handleCopyPaste);
		document.removeEventListener('selectstart', handleSelectStart);
		document.removeEventListener('keydown', handleKeyDown);
	};
}
