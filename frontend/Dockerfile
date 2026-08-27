# ==========================================
# 1. Base Stage with pnpm
# ==========================================
FROM node:22-alpine AS base
ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
RUN npm install -g pnpm@latest

WORKDIR /app

# ==========================================
# 2. Dependencies Stage
# ==========================================
FROM base AS deps
COPY package.json pnpm-lock.yaml ./
RUN --mount=type=cache,id=pnpm,target=/pnpm/store pnpm install --frozen-lockfile

# ==========================================
# 3. Build Stage
# ==========================================
FROM base AS builder
ARG PUBLIC_API_URL=http://localhost:8080
ARG PUBLIC_SIGNALR_URL=http://localhost:8080/hubs
ARG PUBLIC_MINIO_URL=http://localhost:9000
ENV PUBLIC_API_URL=$PUBLIC_API_URL
ENV PUBLIC_SIGNALR_URL=$PUBLIC_SIGNALR_URL
ENV PUBLIC_MINIO_URL=$PUBLIC_MINIO_URL

COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN pnpm build
RUN pnpm prune --prod

# ==========================================
# 4. Production Runner Stage
# ==========================================
FROM node:22-alpine AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV PORT=3001
ENV HOST=0.0.0.0

COPY --from=builder /app/build ./build
COPY --from=builder /app/package.json ./package.json
COPY --from=builder /app/node_modules ./node_modules

EXPOSE 3001

CMD ["node", "build/index.js"]
