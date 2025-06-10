# NOTE: this Dockerfile is intended to be built from the root of the project
FROM node:20-alpine AS builder
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:1.25-alpine AS runner

RUN rm /etc/nginx/conf.d/default.conf
COPY admin-ui.nginx.conf /etc/nginx/conf.d/default.conf

COPY --from=builder /app/dist /usr/share/nginx/html
ENTRYPOINT ["nginx", "-g", "daemon off;"]
