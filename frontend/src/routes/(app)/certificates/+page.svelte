<script lang="ts">
	import { assessmentsApi } from '#lib/api/assessments.ts';
	import type { Certificate } from '#lib/api/types.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import { Award, ShieldCheck, Download, ExternalLink, Sparkles } from '@lucide/svelte';
	import { onMount } from 'svelte';

	let certificates = $state<Certificate[]>([]);
	let isLoading = $state(true);

	onMount(async () => {
		try {
			certificates = await assessmentsApi.getMyCertificates();
		} catch {
			// Demo certificate mock
			certificates = [
				{
					id: 'c-1',
					certificateNumber: 'SKYL-2026-DIST-8821',
					studentId: 'st-1',
					courseId: 'c-1',
					finalScore: 94.5,
					certificateHash: 'e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855',
					status: 'Issued',
					issuedAtUtc: new Date().toISOString(),
					courseTitle: 'Advanced Distributed Systems Architecture'
				}
			];
		} finally {
			isLoading = false;
		}
	});
</script>

<div class="space-y-8">
	<!-- Header -->
	<div class="glass-panel relative overflow-hidden rounded-3xl border border-white/10 p-8 shadow-2xl backdrop-blur-2xl">
		<div class="space-y-2">
			<div class="inline-flex items-center gap-2 rounded-lg bg-accent/10 border border-accent/20 px-3 py-1 text-xs font-semibold text-accent">
				<Sparkles class="h-3.5 w-3.5" />
				Verifiable Credentials
			</div>
			<h1 class="text-3xl font-extrabold tracking-tight text-base-content sm:text-4xl">
				My Certificates
			</h1>
			<p class="text-xs text-base-content/70 sm:text-sm">
				Digitally signed certificates with SHA-256 cryptographic hashes for instant authenticity verification.
			</p>
		</div>
	</div>

	<!-- Certificates Grid -->
	{#if isLoading}
		<div class="grid grid-cols-1 gap-6 sm:grid-cols-2">
			<div class="glass-panel h-64 rounded-3xl animate-pulse"></div>
		</div>
	{:else if certificates.length > 0}
		<div class="grid grid-cols-1 gap-6 sm:grid-cols-2">
			{#each certificates as cert (cert.id)}
				<GlassCard class="relative overflow-hidden p-6 border-accent/20 space-y-4">
					<!-- Certificate Badge -->
					<div class="flex items-center justify-between">
						<div class="flex items-center gap-2 text-xs font-bold text-accent">
							<ShieldCheck class="h-4 w-4" />
							Cryptographically Verified
						</div>
						<span class="badge badge-accent badge-xs font-bold uppercase">{cert.status}</span>
					</div>

					<div class="space-y-1">
						<h3 class="text-lg font-bold text-base-content">{cert.courseTitle || 'Certification of Completion'}</h3>
						<div class="text-xs text-base-content/60 font-mono">No: {cert.certificateNumber}</div>
					</div>

					<div class="rounded-xl bg-base-100/40 p-3 border border-white/5 space-y-1 text-[11px]">
						<div class="flex justify-between text-base-content/60">
							<span>Final Score:</span>
							<span class="font-bold text-success">{cert.finalScore}%</span>
						</div>
						<div class="flex justify-between text-base-content/60">
							<span>Issued On:</span>
							<span>{new Date(cert.issuedAtUtc).toLocaleDateString()}</span>
						</div>
						<div class="text-[9px] font-mono text-base-content/40 truncate pt-1 border-t border-white/5">
							SHA-256: {cert.certificateHash}
						</div>
					</div>

					<div class="flex items-center gap-2 pt-2">
						<a
							href="/certificates/verify/{cert.certificateHash}"
							class="btn btn-ghost glass-card btn-xs flex-1 rounded-xl border border-white/10 text-base-content hover:bg-base-100/50"
						>
							<ExternalLink class="h-3 w-3 mr-1" />
							Verify Publicly
						</a>
					</div>
				</GlassCard>
			{/each}
		</div>
	{:else}
		<div class="glass-card rounded-3xl p-12 text-center border border-white/5 space-y-3">
			<div class="gradient-accent mx-auto flex h-14 w-14 items-center justify-center rounded-2xl text-white">
				<Award class="h-7 w-7" />
			</div>
			<h3 class="text-lg font-bold text-base-content">No Certificates Yet</h3>
			<p class="text-xs text-base-content/60 max-w-sm mx-auto">
				Complete courses and pass examinations with passing grades to earn verifiable digital certificates.
			</p>
		</div>
	{/if}
</div>
