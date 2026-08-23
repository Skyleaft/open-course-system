import { browser } from '$app/env';

export interface SecurityInterceptorOptions {
	onTabSwitch?: () => void;
	onWindowBlur?: () => void;
	onFullscreenExit?: () => void;
}

export function bindSecurityInterceptors(options: SecurityInterceptorOptions): () => void {
	if (!browser) return () => {};

	// 1. Tab visibility detector
	const handleVisibilityChange = () => {
		if (document.hidden && options.onTabSwitch) {
			options.onTabSwitch();
		}
	};

	// 2. Window focus loss detector
	const handleBlur = () => {
		if (options.onWindowBlur) {
			options.onWindowBlur();
		}
	};

	// 3. Fullscreen exit detector
	const handleFullscreenChange = () => {
		if (!document.fullscreenElement && options.onFullscreenExit) {
			options.onFullscreenExit();
		}
	};

	// 4. Keyboard shortcuts and contextmenu suppressor
	const handleContextMenu = (e: MouseEvent) => {
		e.preventDefault();
	};

	const handleKeyDown = (e: KeyboardEvent) => {
		// Prevent F12, Ctrl+Shift+I, Ctrl+Shift+C, Ctrl+C, Ctrl+V, Alt+Tab, etc.
		if (
			e.key === 'F12' ||
			(e.ctrlKey && e.shiftKey && (e.key === 'I' || e.key === 'C' || e.key === 'J')) ||
			(e.ctrlKey && (e.key === 'c' || e.key === 'C' || e.key === 'v' || e.key === 'V' || e.key === 'u' || e.key === 'U'))
		) {
			e.preventDefault();
		}
	};

	document.addEventListener('visibilitychange', handleVisibilityChange);
	window.addEventListener('blur', handleBlur);
	document.addEventListener('fullscreenchange', handleFullscreenChange);
	document.addEventListener('contextmenu', handleContextMenu);
	document.addEventListener('keydown', handleKeyDown);

	// Cleanup unbinder
	return () => {
		document.removeEventListener('visibilitychange', handleVisibilityChange);
		window.removeEventListener('blur', handleBlur);
		document.removeEventListener('fullscreenchange', handleFullscreenChange);
		document.removeEventListener('contextmenu', handleContextMenu);
		document.removeEventListener('keydown', handleKeyDown);
	};
}
