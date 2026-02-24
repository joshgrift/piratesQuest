#!/bin/bash

dotnet build godot || exit 1

# Local development defaults.
SERVER_ID="${SERVER_ID:-2}"
SERVER_API_KEY="${SERVER_API_KEY:-dev-server-api-key}"

# Parse CLI arguments
SERVER_ONLY=false
while [[ $# -gt 0 ]]; do
  case "$1" in
    --server)   SERVER_ONLY=true; shift ;;
    --user)     CLIENT_USER="$2"; shift 2 ;;
    --password) CLIENT_PASS="$2"; shift 2 ;;
    *)          shift ;;
  esac
done

# ANSI color codes
BLUE='\033[0;34m'
GREEN='\033[0;32m'
RED='\033[0;31m'
RESET='\033[0m'

echo -e "${BLUE}=== Starting Backend ===${RESET}"
# Kill any existing process on port 5236
EXISTING_PID=$(lsof -ti tcp:5236)
if [[ -n "$EXISTING_PID" ]]; then
  echo "Killing existing process on port 5236 (PID $EXISTING_PID)..."
  kill "$EXISTING_PID" 2>/dev/null
fi

docker compose -f server/docker-compose.yml up -d
dotnet run --project server 2>&1 | sed "s/^/$(echo -e ${BLUE})[API     ]$(echo -e ${RESET}) /" &
PID_API=$!

sleep 1

echo -e "${RED}=== Starting Server ===${RESET}"
/Applications/Godot_mono.app/Contents/MacOS/Godot --path godot --position 0,50 --server --server-id "${SERVER_ID}" --server-api-key "${SERVER_API_KEY}" 2>&1 | sed "s/^/$(echo -e ${RED})[Server]$(echo -e ${RESET}) /" &
PID1=$!

if [[ "${SERVER_ONLY}" == "false" ]]; then
  sleep 0.5

  CLIENT_ARGS=""
  if [[ -n "${CLIENT_USER}" && -n "${CLIENT_PASS}" ]]; then
    CLIENT_ARGS="--user ${CLIENT_USER} --password ${CLIENT_PASS} --disableSaveUser"
  fi

  echo -e "${GREEN}=== Starting Client ===${RESET}"
  /Applications/Godot_mono.app/Contents/MacOS/Godot --path godot --resolution 2400x1000 --position 0,50 --client1 ${CLIENT_ARGS} 2>&1 | sed "s/^/$(echo -e ${GREEN})[Client 1 ]$(echo -e ${RESET}) /" &
  PID2=$!

  wait $PID1 $PID2
else
  wait $PID1
fi

kill $PID_API 2>/dev/null
echo "=== Terminated ==="
