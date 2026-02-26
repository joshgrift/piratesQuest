#!/bin/bash

dotnet build godot || exit 1

# Kill any processes occupying ports we need (webview:5173, API:5236, DB:5433)
for PORT in 5173 5236 7777; do
  EXISTING_PID=$(lsof -ti tcp:$PORT)
  if [[ -n "$EXISTING_PID" ]]; then
    echo "Killing existing process on port $PORT (PID $EXISTING_PID)..."
    kill $EXISTING_PID 2>/dev/null
  fi
done

# Local development defaults.
SERVER_ID="${SERVER_ID:-1}"
SERVER_API_KEY="${SERVER_API_KEY:-dev-server-api-key}"
WEBVIEW_URL="http://localhost:5173/fragments/webview/"

# Parse CLI arguments
SERVER_ONLY=false
while [[ $# -gt 0 ]]; do
  case "$1" in
    --server)      SERVER_ONLY=true; shift ;;
    --user)        CLIENT_USER="$2"; shift 2 ;;
    --password)    CLIENT_PASS="$2"; shift 2 ;;
    --webview-url) WEBVIEW_URL="$2"; shift 2 ;;
    *)             shift ;;
  esac
done

# ANSI color codes
BLUE='\033[0;34m'
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[0;33m'
RESET='\033[0m'

echo -e "${YELLOW}=== Starting WebView Dev Server ===${RESET}"
npm --prefix webview install
npm --prefix webview run build || exit 1
npm --prefix webview run dev 2>&1 | sed "s/^/$(echo -e ${YELLOW})[WebView ]$(echo -e ${RESET}) /" &
PID_WEBVIEW=$!

echo -e "${BLUE}=== Starting Backend ===${RESET}"
if ! docker info >/dev/null 2>&1; then
  echo -e "${RED}Error: Docker is not running. Please start Docker Desktop and try again.${RESET}"
  exit 1
fi
if ! docker compose -f api/docker-compose.yml ps --status running 2>/dev/null | grep -q "db"; then
  echo "Database container not running, starting it..."
  docker compose -f api/docker-compose.yml up -d
fi
dotnet run --project api 2>&1 | sed "s/^/$(echo -e ${BLUE})[API     ]$(echo -e ${RESET}) /" &
PID_API=$!

sleep 1

echo -e "${RED}=== Starting Server ===${RESET}"
/Applications/Godot_mono.app/Contents/MacOS/Godot --path godot --position 0,50 --server --server-id "${SERVER_ID}" --server-api-key "${SERVER_API_KEY}" 2>&1 | sed "s/^/$(echo -e ${RED})[Server]$(echo -e ${RESET}) /" &
PID1=$!

if [[ "${SERVER_ONLY}" == "false" ]]; then
  sleep 0.5

  CLIENT_ARGS="--webview-url ${WEBVIEW_URL} --creative"
  if [[ -n "${CLIENT_USER}" && -n "${CLIENT_PASS}" ]]; then
    CLIENT_ARGS="${CLIENT_ARGS} --user ${CLIENT_USER} --password ${CLIENT_PASS} --disableSaveUser"
  fi

  echo -e "${GREEN}=== Starting Client ===${RESET}"
  /Applications/Godot_mono.app/Contents/MacOS/Godot --path godot --resolution 2400x1000 --position 0,50 --client1 ${CLIENT_ARGS} 2>&1 | sed "s/^/$(echo -e ${GREEN})[Client 1 ]$(echo -e ${RESET}) /" &
  PID2=$!

  wait $PID1 $PID2
else
  wait $PID1
fi

kill $PID_API $PID_WEBVIEW 2>/dev/null
echo "=== Terminated ==="
