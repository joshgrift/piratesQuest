#!/bin/bash

# ANSI color codes
BLUE='\033[0;34m'
GREEN='\033[0;32m'
RESET='\033[0m'

echo -e "${BLUE}=== Starting Instance 1 ===${RESET}"
/Applications/Godot_mono.app/Contents/MacOS/Godot --path . 2>&1 | sed "s/^/$(echo -e ${BLUE})[Instance 1]$(echo -e ${RESET}) /" &
PID1=$!

echo -e "${GREEN}=== Starting Instance 2 ===${RESET}"
/Applications/Godot_mono.app/Contents/MacOS/Godot --path . 2>&1 | sed "s/^/$(echo -e ${GREEN})[Instance 2]$(echo -e ${RESET}) /" &
PID2=$!

# Wait for both instances to complete
wait $PID1 $PID2

echo "=== Terminated ==="