# Multi-stage build. Build context = repo root:
#   docker build -f src/web-portal/Dockerfile -t localhost:5000/web-portal:1 .

# ---- Stage 1: compile Angular to static files ----
FROM node:24-alpine AS build
WORKDIR /src
COPY src/web-portal/package.json src/web-portal/package-lock.json ./
RUN npm ci
COPY src/web-portal/ ./
RUN npx ng build --configuration production

# ---- Stage 2: serve the static files with Nginx ----
FROM nginx:1.27-alpine
COPY src/web-portal/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist/web-portal/browser /usr/share/nginx/html
EXPOSE 80
