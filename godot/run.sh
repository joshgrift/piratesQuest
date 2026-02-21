#!/bin/bash

dotnet build || exit 1

# Dedicated server auth args for local development.
# You can override these per run:
#   SERVER_ID=2 SERVER_API_KEY=my-key ./run.sh --server
SERVER_ID="${SERVER_ID:-2}"
SERVER_API_KEY="${SERVER_API_KEY:-dev-server-api-key}"

# ANSI color codes
BLUE='\033[0;34m'
GREEN='\033[0;32m'
RED='\033[0;31m'
RESET='\033[0m'

echo -e "${RED}=== Starting Server ===${RESET}"
/Applications/Godot_mono.app/Contents/MacOS/Godot --path . --position 0,50 --server --server-id "${SERVER_ID}" --server-api-key "${SERVER_API_KEY}" 2>&1 | sed "s/^/$(echo -e ${RED})[Server]$(echo -e ${RESET}) /" &
PID1=$!

# Only start clients if --server flag is not present
if [[ "$1" != "--server" ]]; then
  sleep 0.5

  echo -e "${GREEN}=== Starting Client 1 ===${RESET}"
  /Applications/Godot_mono.app/Contents/MacOS/Godot --path . --resolution 2400x1000 --position 0,50 --client1 2>&1 | sed "s/^/$(echo -e ${GREEN})[Client 1 ]$(echo -e ${RESET}) /" &
  PID2=$!

  # Wait for all instances to complete
  wait $PID1 $PID2
else
  # Wait for server only
  wait $PID1
fi

echo "=== Terminated ==="
