#!/bin/bash

# ANSI color codes
BLUE='\033[0;34m'
GREEN='\033[0;32m'
RED='\033[0;31m'
RESET='\033[0m'

echo -e "${RED}=== Starting Server ===${RESET}"
/Applications/Godot_mono.app/Contents/MacOS/Godot --path . --position 0,50 --server 2>&1 | sed "s/^/$(echo -e ${RED})[Server]$(echo -e ${RESET}) /" &
PID1=$!

sleep 0.5

echo -e "${GREEN}=== Starting Client 1 ===${RESET}"
/Applications/Godot_mono.app/Contents/MacOS/Godot --path . --position 0,50 --client1 2>&1 | sed "s/^/$(echo -e ${GREEN})[Client 1 ]$(echo -e ${RESET}) /" &
PID2=$!

echo -e "${BLUE}=== Starting Client 2 ===${RESET}"
/Applications/Godot_mono.app/Contents/MacOS/Godot --path . --position 0,840 --client2 2>&1 | sed "s/^/$(echo -e ${BLUE})[Client 2]$(echo -e ${RESET}) /" &
PID3=$!

# Wait for both instances to complete
wait $PID1 $PID2 $PID3

echo "=== Terminated ==="