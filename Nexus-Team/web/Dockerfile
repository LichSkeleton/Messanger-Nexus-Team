# ============================================================================
# NexusTeam web frontend (Nginx)
# Serves the static responsive SPA and reverse-proxies /api and /ws to the
# .NET server container. No build step is required for the static assets.
# ============================================================================
FROM nginx:1.27-alpine

# Replace the default site config with ours (static + API/WS reverse proxy)
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Static web client
COPY index.html styles.css app.js icon.svg /usr/share/nginx/html/

EXPOSE 80

# Use 127.0.0.1 (IPv4). "localhost" may resolve to ::1 while nginx only listens on IPv4,
# which makes the healthcheck fail even though the site is serving correctly.
HEALTHCHECK --interval=15s --timeout=5s --retries=5 \
    CMD wget -qO- http://127.0.0.1/healthz || exit 1

CMD ["nginx", "-g", "daemon off;"]
