import { apiClient } from './client.ts';

export interface SocialLink {
	platform: string;
	url: string;
}

export interface BrandingSettings {
	siteName: string;
	tagline: string;
	description: string;
	logoLightUrl?: string | null;
	logoDarkUrl?: string | null;
	faviconUrl?: string | null;
	footerCopyright: string;
	contactEmail?: string | null;
	privacyPolicyUrl?: string | null;
	termsOfServiceUrl?: string | null;
	socialLinks: SocialLink[];
}

export interface ThemeSettings {
	defaultTheme: string;
	allowThemeSwitch: boolean;
	primaryColor: string;
	secondaryColor: string;
	accentColor: string;
	neutralColor: string;
	fontFamily: string;
	glassmorphism: boolean;
	borderRadius: string;
	customCss?: string | null;
}

export interface FeatureToggleSettings {
	enablePublicCatalog: boolean;
	enableRegistration: boolean;
	registrationDomainRestriction?: string | null;
	enablePayments: boolean;
	defaultCurrency: string;
	enableCertificates: boolean;
	enableDiscussions: boolean;
	enableAnnouncements: boolean;
	maintenanceMode: boolean;
	maintenanceMessage?: string | null;
}

export interface LocalizationSettings {
	defaultLanguage: string;
	supportedLanguages: string[];
	timezone: string;
	dateFormat: string;
	customTerms: Record<string, string>;
}

export interface SecurityProctoringSettings {
	defaultMaxViolations: number;
	snapshotIntervalSeconds: number;
	enforceFullscreen: boolean;
	enforceCamera: boolean;
	enforceMicrophone: boolean;
	blockClipboard: boolean;
	blockInspectElement: boolean;
}

export interface LandingSectionDto {
	id: string;
	sectionType: string;
	title?: string | null;
	subtitle?: string | null;
	orderIndex: number;
	isActive: boolean;
	configJson: string;
}

export interface PublicCustomizationDto {
	branding: BrandingSettings;
	theme: ThemeSettings;
	features: FeatureToggleSettings;
	localization: LocalizationSettings;
	landingSections: LandingSectionDto[];
}

export interface AdminCustomizationDto {
	branding: BrandingSettings;
	theme: ThemeSettings;
	features: FeatureToggleSettings;
	localization: LocalizationSettings;
	security: SecurityProctoringSettings;
	landingSections: LandingSectionDto[];
}

export interface BrandAssetPresignDto {
	bucket: string;
	objectKey: string;
	uploadUrl: string;
	downloadUrl: string;
}

export const customizationApi = {
	async getPublicCustomization(customFetch?: typeof fetch): Promise<PublicCustomizationDto> {
		return apiClient.get<PublicCustomizationDto>('/api/v1/customization/public', undefined, customFetch);
	},

	async getAdminCustomization(customFetch?: typeof fetch): Promise<AdminCustomizationDto> {
		return apiClient.get<AdminCustomizationDto>('/api/v1/customization/admin', undefined, customFetch);
	},

	async updateSiteSetting(settingKey: string, valueJson: string, isPublic?: boolean): Promise<boolean> {
		return apiClient.put<boolean>(`/api/v1/customization/admin/settings/${settingKey}`, {
			valueJson,
			isPublic
		});
	},

	async batchUpdateSettings(data: {
		branding?: BrandingSettings;
		theme?: ThemeSettings;
		features?: FeatureToggleSettings;
		localization?: LocalizationSettings;
		security?: SecurityProctoringSettings;
	}): Promise<boolean> {
		return apiClient.put<boolean>('/api/v1/customization/admin/batch', data);
	},

	async getLandingSections(customFetch?: typeof fetch): Promise<LandingSectionDto[]> {
		return apiClient.get<LandingSectionDto[]>('/api/v1/customization/admin/landing-sections', undefined, customFetch);
	},

	async createLandingSection(data: {
		sectionType: string;
		title?: string;
		subtitle?: string;
		orderIndex?: number;
		isActive?: boolean;
		configJson?: string;
	}): Promise<string> {
		return apiClient.post<string>('/api/v1/customization/admin/landing-sections', {
			sectionType: data.sectionType,
			title: data.title,
			subtitle: data.subtitle,
			orderIndex: data.orderIndex ?? 1,
			isActive: data.isActive ?? true,
			configJson: data.configJson ?? '{}'
		});
	},

	async updateLandingSection(
		id: string,
		data: {
			title?: string;
			subtitle?: string;
			orderIndex: number;
			isActive: boolean;
			configJson: string;
		}
	): Promise<boolean> {
		return apiClient.put<boolean>(`/api/v1/customization/admin/landing-sections/${id}`, data);
	},

	async deleteLandingSection(id: string): Promise<boolean> {
		return apiClient.delete<boolean>(`/api/v1/customization/admin/landing-sections/${id}`);
	},

	async reorderLandingSections(sectionIds: string[]): Promise<boolean> {
		return apiClient.put<boolean>('/api/v1/customization/admin/landing-sections/reorder', {
			sectionIds
		});
	},

	async getPresignedAssetUpload(fileName: string, contentType: string): Promise<BrandAssetPresignDto> {
		return apiClient.post<BrandAssetPresignDto>('/api/v1/customization/admin/assets/presign', {
			fileName,
			contentType
		});
	},

	async uploadAssetFile(file: File): Promise<string> {
		const presign = await this.getPresignedAssetUpload(file.name, file.type || 'application/octet-stream');
		const res = await fetch(presign.uploadUrl, {
			method: 'PUT',
			headers: {
				'Content-Type': file.type || 'application/octet-stream'
			},
			body: file
		});

		if (!res.ok) {
			throw new Error(`Failed to upload file to storage: ${res.statusText}`);
		}

		return presign.downloadUrl;
	}
};
