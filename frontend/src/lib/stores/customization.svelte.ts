import { customizationApi, type PublicCustomizationDto } from '#lib/api/customization.ts';

const defaultCustomization: PublicCustomizationDto = {
	branding: {
		siteName: 'Open Course System',
		tagline: 'Customizable LMS & Online Examination Platform',
		description: 'Next-generation open education platform with realtime anti-cheat proctoring and interactive learning.',
		logoLightUrl: null,
		logoDarkUrl: null,
		faviconUrl: null,
		footerCopyright: '© 2026 Open Course System. All rights reserved.',
		contactEmail: 'contact@opencourse.io',
		privacyPolicyUrl: null,
		termsOfServiceUrl: null,
		socialLinks: []
	},
	theme: {
		defaultTheme: 'dark',
		allowThemeSwitch: true,
		primaryColor: '#6366f1',
		secondaryColor: '#a855f7',
		accentColor: '#ec4899',
		neutralColor: '#1f2937',
		fontFamily: 'Outfit',
		glassmorphism: true,
		borderRadius: '0.75rem',
		customCss: null
	},
	features: {
		enablePublicCatalog: true,
		enableRegistration: true,
		registrationDomainRestriction: null,
		enablePayments: false,
		defaultCurrency: 'IDR',
		enableCertificates: true,
		enableDiscussions: true,
		enableAnnouncements: true,
		maintenanceMode: false,
		maintenanceMessage: null
	},
	localization: {
		defaultLanguage: 'id',
		supportedLanguages: ['id', 'en'],
		timezone: 'Asia/Jakarta',
		dateFormat: 'DD MMMM YYYY',
		customTerms: {
			Course: 'Kursus',
			Exam: 'Ujian',
			Instructor: 'Instruktur',
			Student: 'Peserta'
		}
	},
	landingSections: []
};

class CustomizationStore {
	data = $state<PublicCustomizationDto>(defaultCustomization);
	isLoading = $state<boolean>(false);
	isInitialized = $state<boolean>(false);

	async initialize(customFetch?: typeof fetch) {
		if (this.isInitialized) return;
		this.isLoading = true;
		try {
			const res = await customizationApi.getPublicCustomization(customFetch);
			if (res) {
				this.data = res;
				this.applyTheme(res.theme);
			}
		} catch (err) {
			console.warn('[CustomizationStore] Using default customization fallback:', err);
		} finally {
			this.isLoading = false;
			this.isInitialized = true;
		}
	}

	applyTheme(theme: typeof defaultCustomization.theme) {
		if (typeof window === 'undefined' || typeof document === 'undefined') return;

		const root = document.documentElement;

		// Set primary / secondary / accent CSS color variables
		if (theme.primaryColor) {
			root.style.setProperty('--color-primary', theme.primaryColor);
		}
		if (theme.secondaryColor) {
			root.style.setProperty('--color-secondary', theme.secondaryColor);
		}
		if (theme.accentColor) {
			root.style.setProperty('--color-accent', theme.accentColor);
		}
		if (theme.borderRadius) {
			root.style.setProperty('--radius-box', theme.borderRadius);
			root.style.setProperty('--radius-field', theme.borderRadius);
			root.style.setProperty('--radius-btn', theme.borderRadius);
		}

		// Inject custom CSS if provided
		let customStyleTag = document.getElementById('open-course-custom-css');
		if (theme.customCss) {
			if (!customStyleTag) {
				customStyleTag = document.createElement('style');
				customStyleTag.id = 'open-course-custom-css';
				document.head.appendChild(customStyleTag);
			}
			customStyleTag.textContent = theme.customCss;
		} else if (customStyleTag) {
			customStyleTag.remove();
		}
	}

	setPreviewData(preview: Partial<PublicCustomizationDto>) {
		this.data = { ...this.data, ...preview };
		if (preview.theme) {
			this.applyTheme(preview.theme);
		}
	}
}

export const customizationStore = new CustomizationStore();
