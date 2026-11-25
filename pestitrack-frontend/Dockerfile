# syntax=docker/dockerfile:1

FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . ./
RUN npm run build

FROM nginx:1.27-alpine AS final

RUN apk add --no-cache openssl

COPY nginx/default.conf /etc/nginx/conf.d/default.conf
COPY nginx/40-generate-self-signed-cert.sh /docker-entrypoint.d/40-generate-self-signed-cert.sh
RUN chmod +x /docker-entrypoint.d/40-generate-self-signed-cert.sh

COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80 443
