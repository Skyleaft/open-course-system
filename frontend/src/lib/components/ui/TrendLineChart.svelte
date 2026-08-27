<script lang="ts">
	interface DataPoint {
		date: string;
		value: number;
		label?: string;
	}

	interface Props {
		data: DataPoint[];
		color?: 'primary' | 'secondary' | 'accent' | 'success' | 'warning';
		height?: number;
		formatValue?: (val: number) => string;
	}

	let {
		data = [],
		color = 'primary',
		height = 200,
		formatValue = (v: number) => v.toLocaleString()
	}: Props = $props();

	let hoveredIndex = $state<number | null>(null);

	const colorMap = {
		primary: { stroke: 'var(--color-primary, #6366f1)', fill: 'rgba(99, 102, 241, 0.15)', text: 'text-primary' },
		secondary: { stroke: 'var(--color-secondary, #ec4899)', fill: 'rgba(236, 72, 153, 0.15)', text: 'text-secondary' },
		accent: { stroke: 'var(--color-accent, #06b6d4)', fill: 'rgba(6, 182, 212, 0.15)', text: 'text-accent' },
		success: { stroke: 'var(--color-success, #10b981)', fill: 'rgba(16, 185, 129, 0.15)', text: 'text-success' },
		warning: { stroke: 'var(--color-warning, #f59e0b)', fill: 'rgba(245, 158, 11, 0.15)', text: 'text-warning' }
	};

	let maxVal = $derived(Math.max(...data.map(d => d.value), 1));
	let minVal = $derived(Math.min(...data.map(d => d.value), 0));
	let range = $derived(maxVal - minVal || 1);

	let points = $derived(
		data.map((d, i) => {
			const x = data.length > 1 ? (i / (data.length - 1)) * 100 : 50;
			const y = 100 - ((d.value - minVal) / range) * 80 - 10;
			return { x, y, ...d };
		})
	);

	let pathD = $derived(
		points.length > 0
			? points.reduce((acc, p, i) => `${acc} ${i === 0 ? 'M' : 'L'} ${p.x} ${p.y}`, '')
			: ''
	);

	let areaD = $derived(
		points.length > 0
			? `${pathD} L ${points[points.length - 1].x} 100 L ${points[0].x} 100 Z`
			: ''
	);
</script>

<div class="relative w-full overflow-hidden rounded-2xl bg-base-100/30 p-4 border border-white/5 backdrop-blur-md">
	<div class="relative w-full" style="height: {height}px;">
		{#if data.length === 0}
			<div class="flex h-full items-center justify-center text-xs text-base-content/40">
				No trend data available
			</div>
		{:else}
			<svg viewBox="0 0 100 100" preserveAspectRatio="none" class="h-full w-full overflow-visible">
				<defs>
					<linearGradient id="gradient-{color}" x1="0" y1="0" x2="0" y2="1">
						<stop offset="0%" stop-color={colorMap[color].stroke} stop-opacity="0.35" />
						<stop offset="100%" stop-color={colorMap[color].stroke} stop-opacity="0.0" />
					</linearGradient>
				</defs>

				<!-- Area Fill -->
				<path d={areaD} fill="url(#gradient-{color})" />

				<!-- Stroke Line -->
				<path
					d={pathD}
					fill="none"
					stroke={colorMap[color].stroke}
					stroke-width="2.5"
					stroke-linecap="round"
					stroke-linejoin="round"
					class="transition-all duration-300"
				/>

				<!-- Interactive Points -->
				{#each points as point, i}
					<!-- svelte-ignore a11y_no_static_element_interactions -->
					<circle
						cx={point.x}
						cy={point.y}
						r={hoveredIndex === i ? 4 : 2}
						fill={colorMap[color].stroke}
						stroke="white"
						stroke-width={hoveredIndex === i ? "1.5" : "0"}
						class="cursor-pointer transition-all duration-150"
						onmouseenter={() => (hoveredIndex = i)}
						onmouseleave={() => (hoveredIndex = null)}
					/>
				{/each}
			</svg>

			<!-- Hover Tooltip -->
			{#if hoveredIndex !== null && points[hoveredIndex]}
				{@const active = points[hoveredIndex]}
				<div
					class="pointer-events-none absolute -top-2 transform -translate-x-1/2 -translate-y-full rounded-lg bg-base-300/90 px-2.5 py-1 text-[11px] font-medium shadow-xl backdrop-blur-md border border-white/10 z-20"
					style="left: {active.x}%;"
				>
					<div class="text-base-content/70 text-[9px]">{active.date}</div>
					<div class="font-bold {colorMap[color].text}">{formatValue(active.value)}</div>
				</div>
			{/if}
		{/if}
	</div>

	{#if data.length > 0}
		<div class="mt-2 flex justify-between text-[10px] font-semibold text-base-content/40">
			<span>{data[0]?.date}</span>
			<span>{data[Math.floor(data.length / 2)]?.date}</span>
			<span>{data[data.length - 1]?.date}</span>
		</div>
	{/if}
</div>
