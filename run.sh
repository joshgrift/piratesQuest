#!/bin/bash

# ANSI color codes
BLUE='\033[0;34m'
GREEN='\033[0;32m'
RESET='\033[0m'

echo -e "${BLUE}=== Starting Host ===${RESET}"
/Applications/Godot_mono.app/Contents/MacOS/Godot --path . --position 0,100 --host 2>&1 | sed "s/^/$(echo -e ${BLUE})[Host]$(echo -e ${RESET}) /" &
PID1=$!

echo -e "${GREEN}=== Starting Client ===${RESET}"
/Applications/Godot_mono.app/Contents/MacOS/Godot --path . --position 1200,100 --client 2>&1 | sed "s/^/$(echo -e ${GREEN})[Client]$(echo -e ${RESET}) /" &
PID2=$!

# Wait for both instances to complete
wait $PID1 $PID2

echo "=== Terminated ==="