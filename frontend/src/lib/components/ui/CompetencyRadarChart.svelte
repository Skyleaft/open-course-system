<script lang="ts">
	interface RadarPoint {
		subject: string;
		value: number;
		fullMark: number;
	}

	interface Props {
		points: RadarPoint[];
		size?: number;
	}

	let {
		points = [],
		size = 260
	}: Props = $props();

	const center = 130;
	const radius = 90;
	const levels = [0.25, 0.5, 0.75, 1.0];

	function getCoordinates(index: number, total: number, valueRatio: number) {
		const angle = (Math.PI * 2 / total) * index - Math.PI / 2;
		const r = radius * valueRatio;
		const x = center + r * Math.cos(angle);
		const y = center + r * Math.sin(angle);
		return { x, y };
	}

	let polygonPoints = $derived(
		points.map((p, i) => {
			const ratio = Math.max(0.1, Math.min(1.0, p.value / (p.fullMark || 100)));
			return getCoordinates(i, points.length, ratio);
		})
	);

	let polygonString = $derived(
		polygonPoints.map(p => `${p.x},${p.y}`).join(' ')
	);
</script>

<div class="relative flex flex-col items-center justify-center p-2">
	{#if points.length === 0}
		<div class="flex h-48 w-full items-center justify-center text-xs text-base-content/40">
			No competency data
		</div>
	{:else}
		<svg width={size} height={size} viewBox="0 0 260 260" class="overflow-visible">
			<!-- Background Grid Circles / Polygons -->
			{#each levels as level}
				{@const gridPoints = points.map((_, i) => getCoordinates(i, points.length, level)).map(p => `${p.x},${p.y}`).join(' ')}
				<polygon
					points={gridPoints}
					fill="none"
					stroke="currentColor"
					class="text-white/10"
					stroke-width="1"
				/>
			{/each}

			<!-- Axis Spokes -->
			{#each points as _, i}
				{@const spoke = getCoordinates(i, points.length, 1.0)}
				<line
					x1={center}
					y1={center}
					x2={spoke.x}
					y2={spoke.y}
					stroke="currentColor"
					class="text-white/10"
					stroke-width="1"
				/>
			{/each}

			<!-- Value Polygon -->
			<polygon
				points={polygonString}
				fill="var(--color-primary, #6366f1)"
				fill-opacity="0.25"
				stroke="var(--color-primary, #6366f1)"
				stroke-width="2"
				class="transition-all duration-500"
			/>

			<!-- Vertices Dots -->
			{#each polygonPoints as pt, i}
				<circle
					cx={pt.x}
					cy={pt.y}
					r="4"
					fill="var(--color-primary, #6366f1)"
					stroke="white"
					stroke-width="1.5"
					class="transition-all duration-300 hover:scale-125"
				/>
			{/each}

			<!-- Labels -->
			{#each points as p, i}
				{@const labelPos = getCoordinates(i, points.length, 1.25)}
				<text
					x={labelPos.x}
					y={labelPos.y}
					text-anchor="middle"
					dominant-baseline="central"
					class="fill-base-content/75 text-[9px] font-bold"
				>
					{p.subject} ({p.value}%)
				</text>
			{/each}
		</svg>
	{/if}
</div>
