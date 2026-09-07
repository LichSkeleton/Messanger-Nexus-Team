FROM nginx:1.27-alpine

COPY http.conf.template /etc/nexus-gateway/http.conf.template
COPY https.conf.template /etc/nexus-gateway/https.conf.template
COPY configure-gateway /usr/local/bin/configure-gateway
COPY entrypoint /usr/local/bin/nexus-gateway-entrypoint

RUN chmod 0755 /usr/local/bin/configure-gateway /usr/local/bin/nexus-gateway-entrypoint

HEALTHCHECK --interval=15s --timeout=5s --retries=5 \
    CMD wget -qO- http://127.0.0.1/healthz || exit 1

ENTRYPOINT ["/usr/local/bin/nexus-gateway-entrypoint"]
