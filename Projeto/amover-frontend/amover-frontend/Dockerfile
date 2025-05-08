# ─── estágio de build ────────────────────────────────────────────────────────
FROM node:latest AS builder
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build        # produz /app/dist

# ─── estágio de produção ─────────────────────────────────────────────────────
FROM nginx:latest
# ficheiro de configuração nginx personalizado
COPY nginx.conf /etc/nginx/conf.d/default.conf
# ficheiros estáticos gerados pelo Vite
COPY --from=builder /app/dist /usr/share/nginx/html

# certificados TLS (montados em runtime via volume)
VOLUME ["/etc/ssl/private", "/etc/ssl/certs"]

EXPOSE 443
CMD ["nginx", "-g", "daemon off;"]
