FROM nginx:1.27-alpine

COPY http.conf.template /etc/nexus-gateway/http.conf.template
COPY https.conf.template /etc/nexus-gateway/https.conf.template
COPY configure-gateway /usr/local/bin/configure-gateway
COPY entrypoint /usr/local/bin/nexus-gateway-entrypoint

# git archive on Windows can leave CRLF; Alpine then fails with
# "exec ...: no such file or directory" because the shebang becomes /bin/sh\r.
RUN sed -i 's/\r$//' /usr/local/bin/configure-gateway /usr/local/bin/nexus-gateway-entrypoint \
    && chmod 0755 /usr/local/bin/configure-gateway /usr/local/bin/nexus-gateway-entrypoint

HEALTHCHECK --interval=15s --timeout=5s --retries=5 --start-period=20s \
    CMD wget -qO- http://127.0.0.1/healthz || exit 1

ENTRYPOINT ["/bin/sh", "/usr/local/bin/nexus-gateway-entrypoint"]
