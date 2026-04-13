#!/bin/bash
set -euo pipefail

# Keep the container interface simple: configure via environment variables,
# then pass the values to the Godot dedicated server using its existing CLI API.

SERVER_ID="${SERVER_ID:-}"
SERVER_API_KEY="${SERVER_API_KEY:-}"
SERVER_PORT="${SERVER_PORT:-7777}"
API_URL="${API_URL:-}"

if [[ -z "${SERVER_ID}" ]]; then
  echo "SERVER_ID is required."
  exit 1
fi

if [[ -z "${SERVER_API_KEY}" ]]; then
  echo "SERVER_API_KEY is required."
  exit 1
fi

args=(
  --server
  --server-id "${SERVER_ID}"
  --server-api-key "${SERVER_API_KEY}"
  --port "${SERVER_PORT}"
)

if [[ -n "${API_URL}" ]]; then
  args+=(--api-url "${API_URL}")
fi

exec /app/piratesquest-server "${args[@]}"
