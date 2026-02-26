#!/usr/bin/env bash
set -euo pipefail

API_URL="${PQ_API_URL:-http://localhost:5236}"
API_URL="${API_URL%/}"
TOKEN=""

# --prod flag swaps to the production API
for arg in "$@"; do
  if [[ "$arg" == "--prod" ]]; then
    API_URL="https://pirates.quest"
    echo "⚓ Using production API: $API_URL" >&2
    break
  fi
done

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

Flags:
  --prod       Use production API (https://pirates.quest)

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

# Wrapper around curl that captures the HTTP status code and body separately.
# On non-2xx responses it prints a detailed error and exits.
#   api_call <method> <path> [curl extra args...]
api_call() {
    local method="$1" path="$2"
    shift 2

    local url="${API_URL}${path}"
    local http_code body tmp

    tmp=$(mktemp)
    http_code=$(curl -s -o "$tmp" -w "%{http_code}" -X "$method" "$url" "$@") || {
        rm -f "$tmp"
        echo "Error: could not reach API at $url" >&2
        echo "  Is the server running? Check PQ_API_URL (currently: $API_URL)" >&2
        exit 1
    }
    body=$(cat "$tmp")
    rm -f "$tmp"

    if [[ "$http_code" -ge 200 && "$http_code" -lt 300 ]]; then
        echo "$body"
        return 0
    fi

    echo "Error: $method $url returned HTTP $http_code" >&2
    case "$http_code" in
        401) echo "  Unauthorized — token may be expired. Try: ./manage.sh login <user> <pass>" >&2 ;;
        403) echo "  Forbidden — your account may lack the required role (Admin)" >&2 ;;
        404) echo "  Not found — check the id/path you provided" >&2 ;;
        405) echo "  Method not allowed — the API may be behind an HTTP→HTTPS redirect. Try PQ_API_URL with https://" >&2 ;;
        409) echo "  Conflict — resource already exists" >&2 ;;
        500) echo "  Internal server error — check the API logs" >&2 ;;
    esac
    if [[ -n "$body" ]]; then
        echo "  Response: $body" >&2
    fi
    exit 1
}

cmd_login() {
    local user="$1" pass="$2"
    local resp
    resp=$(api_call POST "/api/login" \
        -H "Content-Type: application/json" \
        -d "{\"username\":\"$user\",\"password\":\"$pass\"}")

    TOKEN=$(echo "$resp" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)

    if [[ -z "$TOKEN" ]]; then
        echo "Error: login succeeded but response did not contain a token." >&2
        echo "  Response: $resp" >&2
        exit 1
    fi

    echo "$TOKEN" > "$HOME/.pq_token"
    echo "Logged in as $user. Token saved to ~/.pq_token"
}

cmd_users() {
    get_token
    api_call GET "/api/management/users" \
        -H "Authorization: Bearer $TOKEN" | jq .
}

cmd_servers() {
    get_token
    api_call GET "/api/management/servers" \
        -H "Authorization: Bearer $TOKEN" | jq .
}

cmd_add_server() {
    local name="$1" addr="$2" port="$3"
    get_token
    api_call PUT "/api/management/server" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d "{\"name\":\"$name\",\"address\":\"$addr\",\"port\":$port}" | jq .
}

cmd_rm_server() {
    local id="$1"
    get_token
    api_call DELETE "/api/management/server/$id" \
        -H "Authorization: Bearer $TOKEN" | jq .
}

cmd_set_role() {
    local id="$1" role="$2"
    get_token
    api_call PUT "/api/management/user/$id/role" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d "{\"role\":\"$role\"}" | jq .
}

cmd_status() {
    api_call GET "/api/status" | jq .
}

cmd_set_version() {
    local version="$1"
    get_token
    api_call POST "/api/management/version" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d "{\"version\":\"$version\"}" | jq .
}

# Strip --prod from positional args so it doesn't interfere with commands.
args=()
for arg in "$@"; do
  [[ "$arg" != "--prod" ]] && args+=("$arg")
done
set -- "${args[@]+"${args[@]}"}"

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
