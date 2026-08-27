<script lang="ts">
	import {
		dashboardApi,
		type RevenueAnalytics,
		type SystemHealth,
		type SecurityViolationsSummary
	} from '#lib/api/dashboard.ts';
	import GlassCard from '#lib/components/ui/GlassCard.svelte';
	import StatCard from '#lib/components/ui/StatCard.svelte';
	import TrendLineChart from '#lib/components/ui/TrendLineChart.svelte';
	import {
		DollarSign,
		TrendingUp,
		ShieldAlert,
		Activity,
		Server,
		AlertCircle,
		CheckCircle2,
		Sparkles,
		Flame,
		ArrowRight,
		RefreshCw,
		Layers
	} from '@lucide/svelte';
	import { onMount } from 'svelte';

	let revenueData = $state<RevenueAnalytics | null>(null);
	let healthData = $state<SystemHealth | null>(null);
	let securityData = $state<SecurityViolationsSummary | null>(null);

	let isLoading = $state(true);

	async function loadAllMetrics() {
		isLoading = true;
		try {
			const [revRes, healthRes, secRes] = await Promise.allSettled([
				dashboardApi.getAdminRevenueAnalytics(),
				dashboardApi.getAdminSystemHealth(),
				dashboardApi.getAdminSecurityViolations()
			]);

			if (revRes.status === 'fulfilled' && revRes.value) {
				revenueData = revRes.value;
			}
			if (healthRes.status === 'fulfilled' && healthRes.value) {
				healthData = healthRes.value;
			}
			if (secRes.status === 'fulfilled' && secRes.value) {
				securityData = secRes.value;
			}
		} catch (err) {
			console.error('Failed to load admin metrics:', err);
		} finally {
			isLoading = false;
		}
	}

	onMount(() => {
		loadAllMetrics();
	});

	function formatCurrency(amount: number) {
		return new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 }).format(amount);
	}
</script>

<div class="space-y-8">
	<!-- Header -->
	<div class="glass-panel relative overflow-hidden rounded-3xl border border-white/10 p-8 shadow-2xl backdrop-blur-2xl">
		<div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
			<div class="space-y-2">
				<div class="inline-flex items-center gap-2 rounded-lg bg-primary/10 border border-primary/20 px-3 py-1 text-xs font-semibold text-primary">
					<Sparkles class="h-3.5 w-3.5" />
					Platform Executive Studio
				</div>
				<h1 class="text-3xl font-extrabold tracking-tight text-base-content sm:text-4xl">
					System & Financial Observability
				</h1>
				<p class="text-xs text-base-content/70 sm:text-sm">
					Real-time financial telemetry, asynchronous worker health & DLQ alerting, and anti-cheat security metrics.
				</p>
			</div>

			<button
				class="btn btn-ghost glass-card border border-white/10 text-xs rounded-xl self-start sm:self-auto gap-2"
				onclick={loadAllMetrics}
				disabled={isLoading}
			>
				<RefreshCw class="h-4 w-4 {isLoading ? 'animate-spin' : ''}" />
				Refresh Metrics
			</button>
		</div>
	</div>

	<!-- SECTION 1: Financial & Commercial KPIs -->
	<div class="space-y-4">
		<h2 class="text-lg font-bold text-base-content flex items-center gap-2">
			<DollarSign class="h-5 w-5 text-success" />
			Commercial Performance & GMV
		</h2>

		<div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
			<StatCard
				title="Gross Revenue (GMV)"
				value={revenueData ? formatCurrency(revenueData.grossMerchandiseValue) : 'IDR 0'}
				description={`${revenueData?.paidOrders ?? 0} paid transactions`}
				icon={DollarSign}
				color="success"
			/>
			<StatCard
				title="Average Order Value"
				value={revenueData ? formatCurrency(revenueData.averageOrderValue) : 'IDR 0'}
				description="Per successful checkout"
				icon={TrendingUp}
				color="primary"
			/>
			<StatCard
				title="Conversion Rate"
				value={revenueData ? `${revenueData.conversionRate}%` : '0%'}
				description={`${revenueData?.totalOrders ?? 0} total orders initiated`}
				icon={Activity}
				color="accent"
			/>
			<StatCard
				title="Order Funnel"
				value={`${revenueData?.paidOrders ?? 0} Paid • ${revenueData?.pendingOrders ?? 0} Pend`}
				description={`${revenueData?.failedOrders ?? 0} failed, ${revenueData?.expiredOrders ?? 0} expired`}
				icon={Layers}
				color="info"
			/>
		</div>

		<div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
			<!-- 30-Day Sales Trend Curve (Span 2 cols) -->
			<div class="lg:col-span-2 space-y-3">
				<GlassCard>
					<div class="space-y-4">
						<div class="flex items-center justify-between border-b border-white/10 pb-3">
							<div>
								<h3 class="text-sm font-bold text-base-content">Daily Revenue Trend</h3>
								<p class="text-[10px] text-base-content/60">30-day transactional trajectory</p>
							</div>
						</div>

						{#if revenueData && revenueData.dailyTrends.length > 0}
							<TrendLineChart
								data={revenueData.dailyTrends.map(d => ({ date: d.date, value: d.revenue }))}
								color="success"
								height={200}
								formatValue={formatCurrency}
							/>
						{:else}
							<div class="h-48 flex items-center justify-center text-xs text-base-content/40">
								No revenue recorded in this period.
							</div>
						{/if}
					</div>
				</GlassCard>
			</div>

			<!-- Top Revenue Courses -->
			<GlassCard>
				<div class="space-y-4">
					<div class="border-b border-white/10 pb-3">
						<h3 class="text-sm font-bold text-base-content">Top Performing Courses</h3>
						<p class="text-[10px] text-base-content/60">By gross revenue generated</p>
					</div>

					{#if revenueData && revenueData.topCourses.length > 0}
						<div class="space-y-3">
							{#each revenueData.topCourses as course, i}
								<div class="flex items-center justify-between rounded-xl bg-base-100/40 border border-white/5 p-3">
									<div class="space-y-0.5 max-w-[65%]">
										<div class="text-xs font-bold text-base-content truncate">{course.courseTitle}</div>
										<div class="text-[10px] text-base-content/50">{course.salesCount} enrollments</div>
									</div>
									<div class="text-right">
										<div class="text-xs font-mono font-bold text-success">
											{formatCurrency(course.totalRevenue)}
										</div>
									</div>
								</div>
							{/each}
						</div>
					{:else}
						<div class="p-6 text-center text-xs text-base-content/40">
							No course sales recorded.
						</div>
					{/if}
				</div>
			</GlassCard>
		</div>
	</div>

	<div class="divider opacity-10"></div>

	<!-- SECTION 2: Infrastructure Health & DLQ Observability -->
	<div class="space-y-4">
		<h2 class="text-lg font-bold text-base-content flex items-center gap-2">
			<Server class="h-5 w-5 text-warning" />
			Asynchronous Workers & Dead-Letter Queue (DLQ) Health
		</h2>

		<div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
			<StatCard
				title="Unresolved DLQ"
				value={healthData ? String(healthData.unresolvedDlqCount) : '0'}
				description="Poison messages in stream:grading-dlq"
				icon={AlertCircle}
				color={healthData && healthData.unresolvedDlqCount > 0 ? 'warning' : 'success'}
			/>
			<StatCard
				title="Total DLQ Events"
				value={healthData ? String(healthData.totalDlqCount) : '0'}
				description="Historical grading worker errors"
				icon={Server}
				color="info"
			/>
			<StatCard
				title="Certificates Issued"
				value={healthData ? String(healthData.totalCertificatesIssued) : '0'}
				description="Cryptographic credentials"
				icon={CheckCircle2}
				color="accent"
			/>
			<StatCard
				title="Redis Stream Status"
				value={healthData?.redisStreamStatus ?? 'Healthy'}
				description="Grading consumer lag: OK"
				icon={Activity}
				color={healthData?.redisStreamStatus === 'Warning' ? 'warning' : 'success'}
			/>
		</div>

		<!-- Recent Dead-Letter Incidents Table -->
		<GlassCard>
			<div class="space-y-4">
				<div class="flex items-center justify-between border-b border-white/10 pb-3">
					<div>
						<h3 class="text-sm font-bold text-base-content">Recent Poison Messages (`stream:grading-dlq`)</h3>
						<p class="text-[10px] text-base-content/60">Worker errors requiring retry or manual review</p>
					</div>
					<a href="/admin/assessments" class="text-xs text-primary hover:underline flex items-center gap-1 font-semibold">
						DLQ Redrive Panel <ArrowRight class="h-3 w-3" />
					</a>
				</div>

				{#if healthData && healthData.recentDeadLetters.length > 0}
					<div class="overflow-x-auto">
						<table class="table table-xs w-full">
							<thead>
								<tr class="text-base-content/60 border-b border-white/10">
									<th>Message ID</th>
									<th>Submission ID</th>
									<th>Error Summary</th>
									<th>Failed At</th>
									<th>Status</th>
								</tr>
							</thead>
							<tbody>
								{#each healthData.recentDeadLetters as dlq}
									<tr class="border-b border-white/5">
										<td class="font-mono text-[10px] text-primary">{dlq.streamMessageId}</td>
										<td class="font-mono text-[10px] text-base-content/70">{dlq.submissionId.slice(0, 8)}...</td>
										<td class="max-w-md truncate text-xs text-error font-medium" title={dlq.errorMessage}>
											{dlq.errorMessage}
										</td>
										<td class="text-[10px] text-base-content/50">
											{new Date(dlq.failedAtUtc).toLocaleDateString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })}
										</td>
										<td>
											<span class="badge {dlq.isResolved ? 'badge-success' : 'badge-warning'} badge-xs font-bold text-[9px]">
												{dlq.isResolved ? 'Resolved' : 'Pending'}
											</span>
										</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				{:else}
					<div class="p-6 text-center text-xs text-success font-semibold">
						✅ Zero poison messages in Dead-Letter Queue. All asynchronous grading jobs operating normally.
					</div>
				{/if}
			</div>
		</GlassCard>
	</div>

	<div class="divider opacity-10"></div>

	<!-- SECTION 3: Global Anti-Cheat Security Telemetry -->
	<div class="space-y-4">
		<h2 class="text-lg font-bold text-base-content flex items-center gap-2">
			<ShieldAlert class="h-5 w-5 text-error" />
			Platform Anti-Cheat & Proctoring Telemetry
		</h2>

		<div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
			<StatCard
				title="Total Submissions"
				value={securityData ? String(securityData.totalSubmissions) : '0'}
				description="Exam attempts processed"
				icon={Layers}
				color="primary"
			/>
			<StatCard
				title="Total Violations Logged"
				value={securityData ? String(securityData.totalViolations) : '0'}
				description="Interception events recorded"
				icon={ShieldAlert}
				color="warning"
			/>
			<StatCard
				title="Disqualification Rate"
				value={securityData ? `${securityData.disqualificationRate}%` : '0%'}
				description={`${securityData?.disqualifiedCount ?? 0} candidates disqualified`}
				icon={Flame}
				color="warning"
			/>
		</div>

		<div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
			<!-- Violation Types Distribution -->
			<GlassCard>
				<div class="space-y-4">
					<div class="border-b border-white/10 pb-3">
						<h3 class="text-sm font-bold text-base-content">Violation Type Distribution</h3>
						<p class="text-[10px] text-base-content/60">Frequency of rule triggers across all active exams</p>
					</div>

					{#if securityData && securityData.violationTypes.length > 0}
						<div class="space-y-3">
							{#each securityData.violationTypes as v}
								<div class="space-y-1">
									<div class="flex justify-between text-xs font-semibold">
										<span class="text-base-content/80">{v.type}</span>
										<span class="font-mono text-error">{v.count} ({v.percentage}%)</span>
									</div>
									<progress
										class="progress progress-error w-full h-1.5 bg-base-300/60"
										value={v.percentage}
										max="100"
									></progress>
								</div>
							{/each}
						</div>
					{:else}
						<div class="p-6 text-center text-xs text-base-content/40">
							No anti-cheat violations recorded.
						</div>
					{/if}
				</div>
			</GlassCard>

			<!-- High-Risk Exams -->
			<GlassCard>
				<div class="space-y-4">
					<div class="border-b border-white/10 pb-3">
						<h3 class="text-sm font-bold text-base-content">Exams with Highest Incident Rate</h3>
						<p class="text-[10px] text-base-content/60">Sorted by total violation frequency</p>
					</div>

					{#if securityData && securityData.highRiskExams.length > 0}
						<div class="space-y-3">
							{#each securityData.highRiskExams as item}
								<div class="flex items-center justify-between rounded-xl bg-base-100/40 border border-white/5 p-3">
									<div class="space-y-0.5 max-w-[65%]">
										<div class="text-xs font-bold text-base-content truncate">{item.examTitle}</div>
										<div class="text-[10px] text-base-content/50">{item.totalAttempts} total attempts</div>
									</div>
									<div class="text-right">
										<div class="text-xs font-mono font-bold text-warning">
											{item.violationsCount} violations
										</div>
										<div class="text-[10px] text-error font-semibold">
											{item.disqualifiedCount} disqualified
										</div>
									</div>
								</div>
							{/each}
						</div>
					{:else}
						<div class="p-6 text-center text-xs text-base-content/40">
							No high-incident exams detected.
						</div>
					{/if}
				</div>
			</GlassCard>
		</div>
	</div>
</div>
