#!/usr/bin/env bash
set -euo pipefail

API_URL="${PQ_API_URL:-http://localhost:5236}"
TOKEN=""

usage() {
    cat <<EOF
Pirate's Quest — Server Management CLI

Usage: ./manage.sh <command> [args]

Commands:
  login <username> <password>    Authenticate (must be admin)
  users                          List all users
  servers                        List all servers (including inactive)
  add-server <name> <addr> <port>  Register a new game server
  rm-server <id>                 Delete a game server
  set-role <user-id> <role>      Set user role (Player, Mod, Admin)
  status                         Show current game version (public)
  set-version <version>          Set the game version (admin)

Environment:
  PQ_API_URL   API base URL (default: http://localhost:5236)
  PQ_TOKEN     Pre-set JWT token (skips login)

EOF
    exit 1
}

get_token() {
    if [[ -n "${PQ_TOKEN:-}" ]]; then
        TOKEN="$PQ_TOKEN"
        return
    fi
    if [[ -f "$HOME/.pq_token" ]]; then
        TOKEN=$(cat "$HOME/.pq_token")
        return
    fi
    echo "Error: not logged in. Run: ./manage.sh login <username> <password>"
    exit 1
}

cmd_login() {
    local user="$1" pass="$2"
    local resp
    resp=$(curl -sf -X POST "$API_URL/api/login" \
        -H "Content-Type: application/json" \
        -d "{\"username\":\"$user\",\"password\":\"$pass\"}")

    TOKEN=$(echo "$resp" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)

    if [[ -z "$TOKEN" ]]; then
        echo "Login failed."
        exit 1
    fi

    echo "$TOKEN" > "$HOME/.pq_token"
    echo "Logged in as $user. Token saved to ~/.pq_token"
}

cmd_users() {
    get_token
    curl -sf -X GET "$API_URL/api/management/users" \
        -H "Authorization: Bearer $TOKEN" | jq .
}

cmd_servers() {
    get_token
    curl -sf -X GET "$API_URL/api/management/servers" \
        -H "Authorization: Bearer $TOKEN" | jq .
}

cmd_add_server() {
    local name="$1" addr="$2" port="$3"
    get_token
    curl -sf -X PUT "$API_URL/api/management/server" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d "{\"name\":\"$name\",\"address\":\"$addr\",\"port\":$port}" | jq .
}

cmd_rm_server() {
    local id="$1"
    get_token
    curl -sf -X DELETE "$API_URL/api/management/server/$id" \
        -H "Authorization: Bearer $TOKEN" | jq .
}

cmd_set_role() {
    local id="$1" role="$2"
    get_token
    curl -sf -X PUT "$API_URL/api/management/user/$id/role" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d "{\"role\":\"$role\"}" | jq .
}

cmd_status() {
    curl -sf -X GET "$API_URL/api/status" | jq .
}

cmd_set_version() {
    local version="$1"
    get_token
    curl -sf -X POST "$API_URL/api/management/version" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d "{\"version\":\"$version\"}" | jq .
}

[[ $# -lt 1 ]] && usage

case "$1" in
    login)
        [[ $# -lt 3 ]] && { echo "Usage: ./manage.sh login <username> <password>"; exit 1; }
        cmd_login "$2" "$3"
        ;;
    users)
        cmd_users
        ;;
    servers)
        cmd_servers
        ;;
    add-server)
        [[ $# -lt 4 ]] && { echo "Usage: ./manage.sh add-server <name> <address> <port>"; exit 1; }
        cmd_add_server "$2" "$3" "$4"
        ;;
    rm-server)
        [[ $# -lt 2 ]] && { echo "Usage: ./manage.sh rm-server <id>"; exit 1; }
        cmd_rm_server "$2"
        ;;
    set-role)
        [[ $# -lt 3 ]] && { echo "Usage: ./manage.sh set-role <user-id> <role>"; exit 1; }
        cmd_set_role "$2" "$3"
        ;;
    status)
        cmd_status
        ;;
    set-version)
        [[ $# -lt 2 ]] && { echo "Usage: ./manage.sh set-version <version>"; exit 1; }
        cmd_set_version "$2"
        ;;
    *)
        usage
        ;;
esac
