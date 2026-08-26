<script lang="ts">
	import { onMount } from 'svelte';
	import { customizationApi, type AdminCustomizationDto, type LandingSectionDto } from '#lib/api/customization.ts';
	import { customizationStore } from '#lib/stores/customization.svelte.ts';
	import { toast } from '#lib/stores/toast.svelte.ts';
	import {
		Palette,
		Image,
		Sliders,
		ShieldAlert,
		Layout,
		Save,
		RefreshCw,
		UploadCloud,
		CheckCircle2,
		ExternalLink,
		Sparkles,
		Plus,
		Trash2,
		Eye,
		MoveUp,
		MoveDown
	} from '@lucide/svelte';

	let activeTab = $state<'theme' | 'branding' | 'features' | 'security' | 'landing'>('theme');
	let isLoading = $state<boolean>(true);
	let isSaving = $state<boolean>(false);
	let isUploading = $state<string | null>(null);

	let form = $state<AdminCustomizationDto>({
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
				Exam: 'Ujian'
			}
		},
		security: {
			defaultMaxViolations: 3,
			snapshotIntervalSeconds: 45,
			enforceFullscreen: true,
			enforceCamera: true,
			enforceMicrophone: true,
			blockClipboard: true,
			blockInspectElement: true
		},
		landingSections: []
	});

	const themePresets = [
		{ name: 'Cosmic Indigo (Default)', primary: '#6366f1', secondary: '#a855f7', accent: '#ec4899' },
		{ name: 'Emerald Oasis', primary: '#10b981', secondary: '#06b6d4', accent: '#3b82f6' },
		{ name: 'Cyberpunk Neon', primary: '#f43f5e', secondary: '#8b5cf6', accent: '#eab308' },
		{ name: 'Deep Sapphire', primary: '#2563eb', secondary: '#4f46e5', accent: '#06b6d4' },
		{ name: 'Sunset Crimson', primary: '#ea580c', secondary: '#db2777', accent: '#f59e0b' }
	];

	const fontOptions = [
		{ label: 'Outfit (Modern Clean)', value: 'Outfit' },
		{ label: 'Inter (Professional UI)', value: 'Inter' },
		{ label: 'Plus Jakarta Sans (Sleek Geometric)', value: 'Plus Jakarta Sans' },
		{ label: 'Roboto (Standard)', value: 'Roboto' }
	];

	onMount(async () => {
		try {
			const res = await customizationApi.getAdminCustomization();
			if (res) {
				form = res;
			}
		} catch (err) {
			console.error('Failed to load admin customization:', err);
			toast.warning('Could not load existing settings; using defaults.');
		} finally {
			isLoading = false;
		}
	});

	async function handleSave() {
		isSaving = true;
		try {
			await customizationApi.batchUpdateSettings({
				branding: form.branding,
				theme: form.theme,
				features: form.features,
				localization: form.localization,
				security: form.security
			});

			customizationStore.setPreviewData({
				branding: form.branding,
				theme: form.theme,
				features: form.features,
				localization: form.localization,
				landingSections: form.landingSections
			});

			toast.success('Website settings and theme saved successfully!');
		} catch (err: any) {
			toast.error(err?.message || 'Failed to save settings.');
		} finally {
			isSaving = false;
		}
	}

	function applyPreset(preset: typeof themePresets[0]) {
		form.theme.primaryColor = preset.primary;
		form.theme.secondaryColor = preset.secondary;
		form.theme.accentColor = preset.accent;
		customizationStore.applyTheme(form.theme);
	}

	async function handleFileUpload(event: Event, targetField: 'logoLightUrl' | 'logoDarkUrl' | 'faviconUrl') {
		const input = event.target as HTMLInputElement;
		if (!input.files || input.files.length === 0) return;

		const file = input.files[0];
		isUploading = targetField;
		try {
			const url = await customizationApi.uploadAssetFile(file);
			form.branding[targetField] = url;
			toast.success(`${file.name} uploaded successfully!`);
		} catch (err: any) {
			toast.error(`Upload failed: ${err.message}`);
		} finally {
			isUploading = null;
		}
	}

	function addSocialLink() {
		form.branding.socialLinks = [
			...form.branding.socialLinks,
			{ platform: 'GitHub', url: 'https://github.com' }
		];
	}

	function removeSocialLink(index: number) {
		form.branding.socialLinks = form.branding.socialLinks.filter((_, i) => i !== index);
	}

	function moveLandingSection(index: number, direction: 'up' | 'down') {
		const newIndex = direction === 'up' ? index - 1 : index + 1;
		if (newIndex < 0 || newIndex >= form.landingSections.length) return;

		const temp = form.landingSections[index];
		form.landingSections[index] = form.landingSections[newIndex];
		form.landingSections[newIndex] = temp;

		// update order indices
		form.landingSections.forEach((s, idx) => {
			s.orderIndex = idx + 1;
		});
	}

	async function saveLandingOrder() {
		try {
			const ids = form.landingSections.map((s) => s.id);
			await customizationApi.reorderLandingSections(ids);
			toast.success('Landing section order updated!');
		} catch (err: any) {
			toast.error(`Failed to reorder sections: ${err.message}`);
		}
	}
</script>

<svelte:head>
	<title>Site Settings & Customization — {customizationStore.data.branding.siteName}</title>
</svelte:head>

<div class="space-y-6 pb-16">
	<!-- Page Header -->
	<div class="glass-panel relative overflow-hidden rounded-3xl p-6 sm:p-8 border border-white/10">
		<div class="relative z-10 flex flex-col justify-between gap-4 md:flex-row md:items-center">
			<div class="space-y-1.5">
				<div class="inline-flex items-center gap-2 rounded-full bg-primary/10 px-3 py-1 text-xs font-semibold text-primary border border-primary/20">
					<Sliders class="h-3.5 w-3.5" />
					Open Course Engine White-Label Studio
				</div>
				<h1 class="text-2xl sm:text-3xl font-extrabold text-base-content tracking-tight">
					Website Settings & Customization
				</h1>
				<p class="text-xs sm:text-sm text-base-content/70 max-w-2xl leading-relaxed">
					Tailor your Open Course System instance: customize branding assets, visual theme tokens, modular feature flags, anti-cheat defaults, and public landing page layout.
				</p>
			</div>

			<div class="flex items-center gap-3">
				<button
					class="btn btn-primary gradient-accent rounded-xl text-white font-semibold shadow-lg hover:shadow-primary/30 disabled:opacity-50"
					onclick={handleSave}
					disabled={isSaving || isLoading}
				>
					{#if isSaving}
						<RefreshCw class="h-4 w-4 animate-spin mr-1.5" />
						Saving Changes...
					{:else}
						<Save class="h-4 w-4 mr-1.5" />
						Save Customization
					{/if}
				</button>
			</div>
		</div>
	</div>

	{#if isLoading}
		<div class="grid grid-cols-1 md:grid-cols-4 gap-6">
			<div class="glass-panel h-64 rounded-3xl animate-pulse"></div>
			<div class="glass-panel col-span-3 h-96 rounded-3xl animate-pulse"></div>
		</div>
	{:else}
		<!-- Main Tabs Container -->
		<div class="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
			<!-- Left Sidebar Navigation -->
			<div class="lg:col-span-3 space-y-2">
				<div class="glass-card rounded-2xl p-2 border border-white/10 space-y-1">
					<button
						class="w-full flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-medium transition-all {activeTab === 'theme'
							? 'bg-primary/20 text-primary font-bold border border-primary/30 shadow-xs'
							: 'text-base-content/80 hover:bg-base-100/50 hover:text-base-content'}"
						onclick={() => (activeTab = 'theme')}
					>
						<Palette class="h-4 w-4" />
						Theme & Styling
					</button>

					<button
						class="w-full flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-medium transition-all {activeTab === 'branding'
							? 'bg-primary/20 text-primary font-bold border border-primary/30 shadow-xs'
							: 'text-base-content/80 hover:bg-base-100/50 hover:text-base-content'}"
						onclick={() => (activeTab = 'branding')}
					>
						<Image class="h-4 w-4" />
						Branding & Assets
					</button>

					<button
						class="w-full flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-medium transition-all {activeTab === 'features'
							? 'bg-primary/20 text-primary font-bold border border-primary/30 shadow-xs'
							: 'text-base-content/80 hover:bg-base-100/50 hover:text-base-content'}"
						onclick={() => (activeTab = 'features')}
					>
						<Sliders class="h-4 w-4" />
						Feature Switchboard
					</button>

					<button
						class="w-full flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-medium transition-all {activeTab === 'security'
							? 'bg-primary/20 text-primary font-bold border border-primary/30 shadow-xs'
							: 'text-base-content/80 hover:bg-base-100/50 hover:text-base-content'}"
						onclick={() => (activeTab = 'security')}
					>
						<ShieldAlert class="h-4 w-4" />
						Proctoring & Security
					</button>

					<button
						class="w-full flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-medium transition-all {activeTab === 'landing'
							? 'bg-primary/20 text-primary font-bold border border-primary/30 shadow-xs'
							: 'text-base-content/80 hover:bg-base-100/50 hover:text-base-content'}"
						onclick={() => (activeTab = 'landing')}
					>
						<Layout class="h-4 w-4" />
						Landing Page Builder
					</button>
				</div>

				<!-- Live Preview Quick Card -->
				<div class="glass-card rounded-2xl p-4 border border-white/10 space-y-3">
					<div class="text-xs font-bold text-base-content flex items-center gap-2">
						<Eye class="h-3.5 w-3.5 text-primary" />
						Theme Visualizer
					</div>
					<div class="rounded-xl p-3 border border-white/10 space-y-2.5" style="background: color-mix(in oklab, var(--color-base-200) 80%, transparent);">
						<div class="flex items-center justify-between text-xs font-semibold">
							<span>Sample Card</span>
							<span class="badge badge-primary badge-xs">Live</span>
						</div>
						<p class="text-[11px] text-base-content/60">
							This card reflects current theme color variables in real-time.
						</p>
						<button
							class="btn btn-primary btn-xs w-full rounded-lg text-white font-semibold"
							style="background-color: {form.theme.primaryColor}; border-color: {form.theme.primaryColor};"
						>
							Primary Button
						</button>
					</div>
				</div>
			</div>

			<!-- Right Content Panel -->
			<div class="lg:col-span-9 space-y-6">
				<!-- TAB 1: THEME & STYLING -->
				{#if activeTab === 'theme'}
					<div class="glass-panel rounded-3xl p-6 sm:p-8 border border-white/10 space-y-8">
						<div class="border-b border-white/10 pb-4">
							<h2 class="text-lg font-bold text-base-content">Design System & Theme Tokens</h2>
							<p class="text-xs text-base-content/60">
								Configure global brand colors, typography, border radius, and glassmorphism styling.
							</p>
						</div>

						<!-- Color Presets -->
						<div class="space-y-3">
							<label class="text-xs font-bold text-base-content uppercase tracking-wider">Curated Palette Presets</label>
							<div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
								{#each themePresets as preset}
									<button
										class="glass-card flex items-center justify-between p-3 rounded-2xl border border-white/10 hover:border-primary/50 text-left transition-all"
										onclick={() => applyPreset(preset)}
									>
										<span class="text-xs font-semibold text-base-content">{preset.name}</span>
										<div class="flex items-center gap-1">
											<div class="h-4 w-4 rounded-full shadow-xs" style="background-color: {preset.primary};"></div>
											<div class="h-4 w-4 rounded-full shadow-xs" style="background-color: {preset.secondary};"></div>
											<div class="h-4 w-4 rounded-full shadow-xs" style="background-color: {preset.accent};"></div>
										</div>
									</button>
								{/each}
							</div>
						</div>

						<!-- Custom Color Pickers -->
						<div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
							<div class="space-y-1.5">
								<label class="text-xs font-semibold text-base-content">Primary Brand Color</label>
								<div class="flex items-center gap-2">
									<input
										type="color"
										bind:value={form.theme.primaryColor}
										oninput={() => customizationStore.applyTheme(form.theme)}
										class="h-10 w-12 rounded-xl cursor-pointer bg-transparent border border-white/20"
									/>
									<input
										type="text"
										bind:value={form.theme.primaryColor}
										oninput={() => customizationStore.applyTheme(form.theme)}
										class="input input-sm glass-input flex-1 rounded-xl font-mono text-xs"
									/>
								</div>
							</div>

							<div class="space-y-1.5">
								<label class="text-xs font-semibold text-base-content">Secondary Color</label>
								<div class="flex items-center gap-2">
									<input
										type="color"
										bind:value={form.theme.secondaryColor}
										oninput={() => customizationStore.applyTheme(form.theme)}
										class="h-10 w-12 rounded-xl cursor-pointer bg-transparent border border-white/20"
									/>
									<input
										type="text"
										bind:value={form.theme.secondaryColor}
										oninput={() => customizationStore.applyTheme(form.theme)}
										class="input input-sm glass-input flex-1 rounded-xl font-mono text-xs"
									/>
								</div>
							</div>

							<div class="space-y-1.5">
								<label class="text-xs font-semibold text-base-content">Accent Glow Color</label>
								<div class="flex items-center gap-2">
									<input
										type="color"
										bind:value={form.theme.accentColor}
										oninput={() => customizationStore.applyTheme(form.theme)}
										class="h-10 w-12 rounded-xl cursor-pointer bg-transparent border border-white/20"
									/>
									<input
										type="text"
										bind:value={form.theme.accentColor}
										oninput={() => customizationStore.applyTheme(form.theme)}
										class="input input-sm glass-input flex-1 rounded-xl font-mono text-xs"
									/>
								</div>
							</div>
						</div>

						<!-- Typography & UI Shape -->
						<div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
							<div class="space-y-1.5">
								<label class="text-xs font-semibold text-base-content">Typography Font Family</label>
								<select
									bind:value={form.theme.fontFamily}
									class="select select-sm glass-input w-full rounded-xl text-xs"
								>
									{#each fontOptions as opt}
										<option value={opt.value}>{opt.label}</option>
									{/each}
								</select>
							</div>

							<div class="space-y-1.5">
								<label class="text-xs font-semibold text-base-content">Component Border Radius</label>
								<select
									bind:value={form.theme.borderRadius}
									onchange={() => customizationStore.applyTheme(form.theme)}
									class="select select-sm glass-input w-full rounded-xl text-xs"
								>
									<option value="0.5rem">Subtle Rounded (0.5rem)</option>
									<option value="0.75rem">Modern Smooth (0.75rem - Default)</option>
									<option value="1rem">Curved Pill (1.0rem)</option>
									<option value="1.5rem">Ultra Rounded (1.5rem)</option>
								</select>
							</div>
						</div>

						<!-- Custom CSS Injection -->
						<div class="space-y-1.5">
							<label class="text-xs font-semibold text-base-content">Custom Global CSS</label>
							<textarea
								bind:value={form.theme.customCss}
								oninput={() => customizationStore.applyTheme(form.theme)}
								rows={4}
								placeholder={'/* Custom CSS overrides (e.g. .glass-card) */'}
								class="textarea glass-input w-full rounded-2xl font-mono text-xs leading-relaxed"
							></textarea>
						</div>
					</div>
				{/if}

				<!-- TAB 2: BRANDING & ASSETS -->
				{#if activeTab === 'branding'}
					<div class="glass-panel rounded-3xl p-6 sm:p-8 border border-white/10 space-y-8">
						<div class="border-b border-white/10 pb-4">
							<h2 class="text-lg font-bold text-base-content">Brand Assets & Identity</h2>
							<p class="text-xs text-base-content/60">
								Upload logos, set site title, tagline, and configure footer copyright details.
							</p>
						</div>

						<div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
							<div class="space-y-1.5">
								<label class="text-xs font-semibold text-base-content">Site Name / Platform Title</label>
								<input
									type="text"
									bind:value={form.branding.siteName}
									class="input input-sm glass-input w-full rounded-xl text-xs font-medium"
								/>
							</div>

							<div class="space-y-1.5">
								<label class="text-xs font-semibold text-base-content">Tagline</label>
								<input
									type="text"
									bind:value={form.branding.tagline}
									class="input input-sm glass-input w-full rounded-xl text-xs font-medium"
								/>
							</div>
						</div>

						<div class="space-y-1.5">
							<label class="text-xs font-semibold text-base-content">Platform Meta Description</label>
							<textarea
								bind:value={form.branding.description}
								rows={2}
								class="textarea glass-input w-full rounded-2xl text-xs"
							></textarea>
						</div>

						<!-- Media Uploads (Logos & Favicon) -->
						<div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
							<!-- Dark Logo -->
							<div class="glass-card rounded-2xl p-4 border border-white/10 space-y-3 text-center">
								<div class="text-xs font-bold text-base-content">Dark Mode Logo</div>
								<div class="h-20 w-full rounded-xl bg-black/40 border border-white/10 flex items-center justify-center overflow-hidden">
									{#if form.branding.logoDarkUrl}
										<img src={form.branding.logoDarkUrl} alt="Dark Logo Preview" class="max-h-16 max-w-full object-contain" />
									{:else}
										<span class="text-[11px] text-base-content/40">No Logo Uploaded</span>
									{/if}
								</div>
								<label class="btn btn-ghost btn-xs glass-panel w-full rounded-lg border border-white/10">
									<UploadCloud class="h-3.5 w-3.5 mr-1" />
									{isUploading === 'logoDarkUrl' ? 'Uploading...' : 'Upload Image'}
									<input
										type="file"
										accept="image/*"
										class="hidden"
										onchange={(e) => handleFileUpload(e, 'logoDarkUrl')}
										disabled={isUploading !== null}
									/>
								</label>
							</div>

							<!-- Light Logo -->
							<div class="glass-card rounded-2xl p-4 border border-white/10 space-y-3 text-center">
								<div class="text-xs font-bold text-base-content">Light Mode Logo</div>
								<div class="h-20 w-full rounded-xl bg-white/80 border border-white/10 flex items-center justify-center overflow-hidden">
									{#if form.branding.logoLightUrl}
										<img src={form.branding.logoLightUrl} alt="Light Logo Preview" class="max-h-16 max-w-full object-contain" />
									{:else}
										<span class="text-[11px] text-black/40">No Logo Uploaded</span>
									{/if}
								</div>
								<label class="btn btn-ghost btn-xs glass-panel w-full rounded-lg border border-white/10">
									<UploadCloud class="h-3.5 w-3.5 mr-1" />
									{isUploading === 'logoLightUrl' ? 'Uploading...' : 'Upload Image'}
									<input
										type="file"
										accept="image/*"
										class="hidden"
										onchange={(e) => handleFileUpload(e, 'logoLightUrl')}
										disabled={isUploading !== null}
									/>
								</label>
							</div>

							<!-- Favicon -->
							<div class="glass-card rounded-2xl p-4 border border-white/10 space-y-3 text-center">
								<div class="text-xs font-bold text-base-content">Site Favicon</div>
								<div class="h-20 w-full rounded-xl bg-base-100/40 border border-white/10 flex items-center justify-center overflow-hidden">
									{#if form.branding.faviconUrl}
										<img src={form.branding.faviconUrl} alt="Favicon Preview" class="h-10 w-10 object-contain" />
									{:else}
										<span class="text-2xl">⚡</span>
									{/if}
								</div>
								<label class="btn btn-ghost btn-xs glass-panel w-full rounded-lg border border-white/10">
									<UploadCloud class="h-3.5 w-3.5 mr-1" />
									{isUploading === 'faviconUrl' ? 'Uploading...' : 'Upload Icon'}
									<input
										type="file"
										accept="image/*,.ico"
										class="hidden"
										onchange={(e) => handleFileUpload(e, 'faviconUrl')}
										disabled={isUploading !== null}
									/>
								</label>
							</div>
						</div>

						<div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
							<div class="space-y-1.5">
								<label class="text-xs font-semibold text-base-content">Footer Copyright Text</label>
								<input
									type="text"
									bind:value={form.branding.footerCopyright}
									class="input input-sm glass-input w-full rounded-xl text-xs"
								/>
							</div>

							<div class="space-y-1.5">
								<label class="text-xs font-semibold text-base-content">Official Support Email</label>
								<input
									type="email"
									bind:value={form.branding.contactEmail}
									class="input input-sm glass-input w-full rounded-xl text-xs"
								/>
							</div>
						</div>

						<!-- Social Links Manager -->
						<div class="space-y-3">
							<div class="flex items-center justify-between">
								<label class="text-xs font-bold text-base-content uppercase tracking-wider">Social Links</label>
								<button class="btn btn-ghost btn-xs glass-panel rounded-lg border border-white/10" onclick={addSocialLink}>
									<Plus class="h-3.5 w-3.5 mr-1" />
									Add Link
								</button>
							</div>

							{#each form.branding.socialLinks as link, index}
								<div class="flex items-center gap-2">
									<input
										type="text"
										placeholder="Platform (e.g. GitHub)"
										bind:value={link.platform}
										class="input input-sm glass-input w-1/3 rounded-xl text-xs font-medium"
									/>
									<input
										type="url"
										placeholder="URL (e.g. https://github.com/...)"
										bind:value={link.url}
										class="input input-sm glass-input flex-1 rounded-xl text-xs"
									/>
									<button
										class="btn btn-ghost btn-circle btn-sm text-error hover:bg-error/10"
										onclick={() => removeSocialLink(index)}
									>
										<Trash2 class="h-4 w-4" />
									</button>
								</div>
							{/each}
						</div>
					</div>
				{/if}

				<!-- TAB 3: FEATURE SWITCHBOARD -->
				{#if activeTab === 'features'}
					<div class="glass-panel rounded-3xl p-6 sm:p-8 border border-white/10 space-y-8">
						<div class="border-b border-white/10 pb-4">
							<h2 class="text-lg font-bold text-base-content">Platform Modular Switchboard</h2>
							<p class="text-xs text-base-content/60">
								Toggle platform capabilities, registration gates, and maintenance mode.
							</p>
						</div>

						<div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
							<div class="glass-card flex items-center justify-between p-4 rounded-2xl border border-white/10">
								<div class="space-y-0.5">
									<div class="text-xs font-bold text-base-content">Public Course Catalog</div>
									<div class="text-[11px] text-base-content/60">Allow unauthenticated guests to explore courses.</div>
								</div>
								<input
									type="checkbox"
									class="toggle toggle-primary toggle-sm"
									bind:checked={form.features.enablePublicCatalog}
								/>
							</div>

							<div class="glass-card flex items-center justify-between p-4 rounded-2xl border border-white/10">
								<div class="space-y-0.5">
									<div class="text-xs font-bold text-base-content">Open Student Registration</div>
									<div class="text-[11px] text-base-content/60">Allow anyone to register an account.</div>
								</div>
								<input
									type="checkbox"
									class="toggle toggle-primary toggle-sm"
									bind:checked={form.features.enableRegistration}
								/>
							</div>

							<div class="glass-card flex items-center justify-between p-4 rounded-2xl border border-white/10">
								<div class="space-y-0.5">
									<div class="text-xs font-bold text-base-content">Certificates Issuance</div>
									<div class="text-[11px] text-base-content/60">Generate cryptographic SHA-256 certificate records.</div>
								</div>
								<input
									type="checkbox"
									class="toggle toggle-primary toggle-sm"
									bind:checked={form.features.enableCertificates}
								/>
							</div>

							<div class="glass-card flex items-center justify-between p-4 rounded-2xl border border-white/10">
								<div class="space-y-0.5">
									<div class="text-xs font-bold text-base-content">Discussion Forums</div>
									<div class="text-[11px] text-base-content/60">Enable student & instructor discussion threads.</div>
								</div>
								<input
									type="checkbox"
									class="toggle toggle-primary toggle-sm"
									bind:checked={form.features.enableDiscussions}
								/>
							</div>
						</div>

						<!-- Registration Domain Restriction -->
						<div class="space-y-1.5">
							<label class="text-xs font-semibold text-base-content">Registration Domain Restriction (Optional)</label>
							<input
								type="text"
								placeholder="e.g. university.ac.id;company.com (leave blank for any email)"
								bind:value={form.features.registrationDomainRestriction}
								class="input input-sm glass-input w-full rounded-xl text-xs font-mono"
							/>
							<span class="text-[11px] text-base-content/50">Separate multiple allowed email domains with semicolons.</span>
						</div>

						<!-- Maintenance Mode -->
						<div class="glass-card rounded-2xl p-5 border border-error/20 space-y-4">
							<div class="flex items-center justify-between">
								<div class="space-y-0.5">
									<div class="text-xs font-bold text-error">Platform Maintenance Mode</div>
									<div class="text-[11px] text-base-content/60">Display maintenance banner to non-admin visitors.</div>
								</div>
								<input
									type="checkbox"
									class="toggle toggle-error toggle-sm"
									bind:checked={form.features.maintenanceMode}
								/>
							</div>

							{#if form.features.maintenanceMode}
								<div class="space-y-1.5">
									<label class="text-xs font-semibold text-base-content">Maintenance Announcement Message</label>
									<input
										type="text"
										placeholder="Platform scheduled maintenance in progress. We'll be back shortly."
										bind:value={form.features.maintenanceMessage}
										class="input input-sm glass-input w-full rounded-xl text-xs"
									/>
								</div>
							{/if}
						</div>
					</div>
				{/if}

				<!-- TAB 4: PROCTORING & SECURITY DEFAULTS -->
				{#if activeTab === 'security'}
					<div class="glass-panel rounded-3xl p-6 sm:p-8 border border-white/10 space-y-8">
						<div class="border-b border-white/10 pb-4">
							<h2 class="text-lg font-bold text-base-content">Anti-Cheat Proctoring Defaults</h2>
							<p class="text-xs text-base-content/60">
								Set global default integrity constraints and webcam snapshot frequencies for RealExam sessions.
							</p>
						</div>

						<div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
							<div class="space-y-1.5">
								<label class="text-xs font-semibold text-base-content">Default Max Allowed Violations</label>
								<input
									type="number"
									min="1"
									max="10"
									bind:value={form.security.defaultMaxViolations}
									class="input input-sm glass-input w-full rounded-xl text-xs font-mono"
								/>
								<span class="text-[11px] text-base-content/50">Attempts exceeding this threshold are auto-disqualified.</span>
							</div>

							<div class="space-y-1.5">
								<label class="text-xs font-semibold text-base-content">Webcam Snapshot Interval (Seconds)</label>
								<input
									type="number"
									min="15"
									max="300"
									bind:value={form.security.snapshotIntervalSeconds}
									class="input input-sm glass-input w-full rounded-xl text-xs font-mono"
								/>
								<span class="text-[11px] text-base-content/50">Periodic interval for background web worker snapshot captures.</span>
							</div>
						</div>

						<div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
							<div class="glass-card flex items-center justify-between p-4 rounded-2xl border border-white/10">
								<div class="space-y-0.5">
									<div class="text-xs font-bold text-base-content">Enforce Fullscreen Lock</div>
									<div class="text-[11px] text-base-content/60">Require full-screen mode during active exam runner.</div>
								</div>
								<input
									type="checkbox"
									class="toggle toggle-primary toggle-sm"
									bind:checked={form.security.enforceFullscreen}
								/>
							</div>

							<div class="glass-card flex items-center justify-between p-4 rounded-2xl border border-white/10">
								<div class="space-y-0.5">
									<div class="text-xs font-bold text-base-content">Enforce Camera Stream</div>
									<div class="text-[11px] text-base-content/60">Require candidate webcam access before starting attempt.</div>
								</div>
								<input
									type="checkbox"
									class="toggle toggle-primary toggle-sm"
									bind:checked={form.security.enforceCamera}
								/>
							</div>

							<div class="glass-card flex items-center justify-between p-4 rounded-2xl border border-white/10">
								<div class="space-y-0.5">
									<div class="text-xs font-bold text-base-content">Enforce Microphone Stream</div>
									<div class="text-[11px] text-base-content/60">Request audio input channel for ambient proctoring.</div>
								</div>
								<input
									type="checkbox"
									class="toggle toggle-primary toggle-sm"
									bind:checked={form.security.enforceMicrophone}
								/>
							</div>

							<div class="glass-card flex items-center justify-between p-4 rounded-2xl border border-white/10">
								<div class="space-y-0.5">
									<div class="text-xs font-bold text-base-content">Block Clipboard & Context Menu</div>
									<div class="text-[11px] text-base-content/60">Prevent copy/paste and right-click during testing.</div>
								</div>
								<input
									type="checkbox"
									class="toggle toggle-primary toggle-sm"
									bind:checked={form.security.blockClipboard}
								/>
							</div>
						</div>
					</div>
				{/if}

				<!-- TAB 5: LANDING PAGE BUILDER -->
				{#if activeTab === 'landing'}
					<div class="glass-panel rounded-3xl p-6 sm:p-8 border border-white/10 space-y-6">
						<div class="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-3 border-b border-white/10 pb-4">
							<div>
								<h2 class="text-lg font-bold text-base-content">Landing Page Modular Sections</h2>
								<p class="text-xs text-base-content/60">
									Reorder and toggle sections displayed on the public landing page.
								</p>
							</div>
							<button class="btn btn-ghost btn-xs glass-card rounded-xl border border-white/10" onclick={saveLandingOrder}>
								Save Order
							</button>
						</div>

						<div class="space-y-3">
							{#each form.landingSections as section, index}
								<div class="glass-card flex items-center justify-between p-4 rounded-2xl border border-white/10 hover:border-primary/40 transition-all">
									<div class="flex items-center gap-3">
										<div class="flex flex-col gap-1">
											<button
												class="btn btn-ghost btn-xs btn-circle h-6 w-6"
												disabled={index === 0}
												onclick={() => moveLandingSection(index, 'up')}
											>
												<MoveUp class="h-3.5 w-3.5" />
											</button>
											<button
												class="btn btn-ghost btn-xs btn-circle h-6 w-6"
												disabled={index === form.landingSections.length - 1}
												onclick={() => moveLandingSection(index, 'down')}
											>
												<MoveDown class="h-3.5 w-3.5" />
											</button>
										</div>

										<div class="space-y-0.5">
											<div class="text-xs font-bold text-base-content flex items-center gap-2">
												<span>{section.title || section.sectionType}</span>
												<span class="badge badge-xs badge-neutral">{section.sectionType}</span>
											</div>
											<div class="text-[11px] text-base-content/50">{section.subtitle || 'Order index: ' + section.orderIndex}</div>
										</div>
									</div>

									<div class="flex items-center gap-4">
										<input
											type="checkbox"
											class="toggle toggle-primary toggle-sm"
											bind:checked={section.isActive}
										/>
									</div>
								</div>
							{/each}
						</div>
					</div>
				{/if}
			</div>
		</div>
	{/if}
</div>
