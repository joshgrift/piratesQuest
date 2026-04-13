FROM debian:bookworm-slim

WORKDIR /app

# Godot dedicated server exports need a few native libraries at runtime.
RUN apt-get update \
  && apt-get install -y --no-install-recommends \
    ca-certificates \
    libfontconfig1 \
    libfreetype6 \
    libgl1 \
    libpulse0 \
    libudev1 \
    libx11-6 \
    libxcursor1 \
    libxi6 \
    libxinerama1 \
    libxrandr2 \
  && rm -rf /var/lib/apt/lists/*

# The release script copies the Linux dedicated server export into this staging folder.
COPY godot/.docker/server-image/ ./
COPY godot/server-docker/entrypoint.sh /usr/local/bin/server-entrypoint.sh

RUN chmod +x /usr/local/bin/server-entrypoint.sh /app/piratesquest-server

EXPOSE 7777/udp

ENTRYPOINT ["/usr/local/bin/server-entrypoint.sh"]
