FROM node:22-alpine AS builder
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
ARG NEXT_PUBLIC_API_BASE_URL
ARG NEXT_PUBLIC_APP_ENV
ENV NEXT_PUBLIC_API_BASE_URL=$NEXT_PUBLIC_API_BASE_URL
ENV NEXT_PUBLIC_APP_ENV=$NEXT_PUBLIC_APP_ENV
RUN npm run build

FROM node:22-alpine AS runner
WORKDIR /app
ENV NODE_ENV=production

# add non-root user
RUN addgroup -g 1001 -S nodejs && adduser -u 1001 -S node -G nodejs

COPY --from=builder /app/public ./public
COPY --from=builder --chown=node:nodejs /app/.next/standalone ./
COPY --from=builder --chown=node:nodejs /app/.next/static ./.next/static

USER node

EXPOSE 3000
ENV PORT=3000

CMD ["node", "server.js"]
