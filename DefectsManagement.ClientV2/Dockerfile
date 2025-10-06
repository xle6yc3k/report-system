# build
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
RUN npm run build

# serve (SPA через nginx)
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY <<'NGINX' /etc/nginx/conf.d/default.conf
server {
  listen 80;
  server_name frontend.localhost;

  location / {
    root   /usr/share/nginx/html;
    try_files $uri $uri/ /index.html;
  }
}
NGINX
EXPOSE 80
