#!/bin/bash
set -euo pipefail

# This script manages dedicated server containers on any Docker host.
# We keep names and labels predictable so the script can find the servers later.

CONTAINER_PREFIX="${PQ_SERVER_CONTAINER_PREFIX:-piratesquest-server}"
MANAGED_LABEL_KEY="com.piratesquest.managed"
MANAGED_LABEL_VALUE="true"
SERVER_ID_LABEL_KEY="com.piratesquest.server-id"
DEFAULT_IMAGE="${PQ_SERVER_IMAGE:-registry.digitalocean.com/piratesquest/piratesquest-server:latest}"
DEFAULT_PLATFORM="${PQ_SERVER_PLATFORM:-linux/amd64}"
DEFAULT_API_URL="${PQ_API_URL:-}"

usage() {
  cat <<'EOF'
Usage:
  ./server.sh list
  ./server.sh new <server-id> <server-api-key> <port>
  ./server.sh start <server-id>
  ./server.sh stop <server-id>
  ./server.sh destroy <server-id>

Optional environment variables:
  PQ_SERVER_IMAGE            Docker image to use
  PQ_SERVER_PLATFORM         Docker platform to run (default: linux/amd64)
  PQ_API_URL                 API base URL passed to the game server
  PQ_SERVER_CONTAINER_PREFIX Container name prefix (default: piratesquest-server)
EOF
}

require_docker() {
  if ! command -v docker >/dev/null 2>&1; then
    echo "Docker is required but was not found."
    exit 1
  fi
}

container_name_for_id() {
  local server_id="$1"
  echo "${CONTAINER_PREFIX}-${server_id}"
}

container_exists() {
  local name="$1"
  docker container inspect "${name}" >/dev/null 2>&1
}

validate_port() {
  local port="${1:-}"

  if [[ ! "${port}" =~ ^[0-9]+$ ]] || (( port < 1 || port > 65535 )); then
    echo "Port must be a number between 1 and 65535."
    exit 1
  fi
}

list_servers() {
  docker ps -a \
    --filter "label=${MANAGED_LABEL_KEY}=${MANAGED_LABEL_VALUE}" \
    --format 'table {{.Names}}\t{{.Status}}\t{{.Image}}\t{{.Ports}}'
}

create_server() {
  local server_id="${1:-}"
  local server_api_key="${2:-}"
  local server_port="${3:-}"

  if [[ -z "${server_id}" || -z "${server_api_key}" || -z "${server_port}" ]]; then
    echo "new requires <server-id>, <server-api-key>, and <port>."
    usage
    exit 1
  fi

  validate_port "${server_port}"

  local container_name
  container_name="$(container_name_for_id "${server_id}")"

  if container_exists "${container_name}"; then
    echo "A managed server with id ${server_id} already exists: ${container_name}"
    exit 1
  fi

  echo "Pulling latest image: ${DEFAULT_IMAGE}"
  docker pull --platform "${DEFAULT_PLATFORM}" "${DEFAULT_IMAGE}"

  # We use --restart unless-stopped so a host reboot brings the server back,
  # but a manual stop still leaves it intentionally offline.
  local docker_args=(
    run -d
    --platform "${DEFAULT_PLATFORM}"
    --name "${container_name}"
    --restart unless-stopped
    --label "${MANAGED_LABEL_KEY}=${MANAGED_LABEL_VALUE}"
    --label "${SERVER_ID_LABEL_KEY}=${server_id}"
    -e "SERVER_ID=${server_id}"
    -e "SERVER_API_KEY=${server_api_key}"
    -e "SERVER_PORT=${server_port}"
    -p "${server_port}:${server_port}/udp"
  )

  if [[ -n "${DEFAULT_API_URL}" ]]; then
    docker_args+=(-e "API_URL=${DEFAULT_API_URL}")
  fi

  docker_args+=("${DEFAULT_IMAGE}")

  docker "${docker_args[@]}"

  echo "Created and started ${container_name}"
}

start_server() {
  local server_id="${1:-}"
  if [[ -z "${server_id}" ]]; then
    echo "start requires <server-id>."
    usage
    exit 1
  fi

  local container_name
  container_name="$(container_name_for_id "${server_id}")"

  if ! container_exists "${container_name}"; then
    echo "No managed server found for id ${server_id}."
    exit 1
  fi

  docker start "${container_name}"
}

stop_server() {
  local server_id="${1:-}"
  if [[ -z "${server_id}" ]]; then
    echo "stop requires <server-id>."
    usage
    exit 1
  fi

  local container_name
  container_name="$(container_name_for_id "${server_id}")"

  if ! container_exists "${container_name}"; then
    echo "No managed server found for id ${server_id}."
    exit 1
  fi

  docker stop "${container_name}"
}

destroy_server() {
  local server_id="${1:-}"
  if [[ -z "${server_id}" ]]; then
    echo "destroy requires <server-id>."
    usage
    exit 1
  fi

  local container_name
  container_name="$(container_name_for_id "${server_id}")"

  if ! container_exists "${container_name}"; then
    echo "No managed server found for id ${server_id}."
    exit 1
  fi

  # Force remove so this works whether the container is running or stopped.
  docker rm -f "${container_name}"
}

main() {
  require_docker

  local command="${1:-}"
  case "${command}" in
    list)
      list_servers
      ;;
    new)
      shift || true
      create_server "${1:-}" "${2:-}" "${3:-}"
      ;;
    start)
      shift || true
      start_server "${1:-}"
      ;;
    stop)
      shift || true
      stop_server "${1:-}"
      ;;
    destroy)
      shift || true
      destroy_server "${1:-}"
      ;;
    ""|-h|--help|help)
      usage
      ;;
    *)
      echo "Unknown command: ${command}"
      usage
      exit 1
      ;;
  esac
}

main "$@"
