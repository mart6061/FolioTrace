FROM node:22-alpine AS build
WORKDIR /app

COPY UI/package*.json ./
RUN npm ci

COPY UI/ ./
RUN npm run build

FROM node:22-alpine AS runtime
WORKDIR /app

ENV NODE_ENV=production
ENV HOST=0.0.0.0

COPY --from=build /app/package*.json ./
COPY --from=build /app/build ./build
COPY --from=build /app/node_modules ./node_modules

CMD ["node", "build/index.js"]
