#!/usr/bin/env bash
# Restarts the FolioTrace API and UI dev server.
#
#   scripts/restart-dev.sh              restart all three
#   scripts/restart-dev.sh api ui       restart only the ones named
#   scripts/restart-dev.sh trader       FoleoTrader only
#   scripts/restart-dev.sh --no-cert    skip the dev certificate check
#
# Processes are stopped by listening port, never by image name. A blanket
# "taskkill /IM dotnet.exe" also kills unrelated dotnet processes and can cause
# ASP.NET to regenerate its HTTPS dev certificate, which then breaks the UI's
# server-side health probe with DEPTH_ZERO_SELF_SIGNED_CERT.

set -uo pipefail

API_PORT=7058
UI_PORT=5173
TRADER_PORT=5001
API_HEALTH="https://localhost:${API_PORT}/API/System/Health"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOG_DIR="${TEMP:-/tmp}"

do_api=0
do_ui=0
do_trader=0
check_cert=1

for arg in "$@"; do
  case "$arg" in
    api) do_api=1 ;;
    ui) do_ui=1 ;;
    trader|foleotrader) do_trader=1 ;;
    all|both) do_api=1; do_ui=1; do_trader=1 ;;
    --no-cert) check_cert=0 ;;
    *) echo "Unknown argument: $arg" >&2; exit 2 ;;
  esac
done

# No service named means all of them.
if [ "$do_api" -eq 0 ] && [ "$do_ui" -eq 0 ] && [ "$do_trader" -eq 0 ]; then
  do_api=1
  do_ui=1
  do_trader=1
fi

log() { printf '%s\n' "$*"; }

# Returns the PIDs listening on a port. Matches the port only in the local
# address column so a client socket to the same port elsewhere is not caught.
listening_pids() {
  netstat -ano 2>/dev/null \
    | awk -v port=":$1" '$1 ~ /^(TCP|UDP)$/ && $2 ~ port"$" && $4 == "LISTENING" {print $5}' \
    | sort -u
}

stop_port() {
  local port=$1 label=$2 pids
  pids=$(listening_pids "$port")

  if [ -z "$pids" ]; then
    log "  $label: nothing listening on $port"
    return 0
  fi

  for pid in $pids; do
    log "  $label: stopping PID $pid on port $port"
    taskkill //PID "$pid" //F >/dev/null 2>&1 || log "  $label: could not stop PID $pid"
  done

  for _ in $(seq 1 20); do
    [ -z "$(listening_pids "$port")" ] && return 0
    sleep 1
  done

  log "  $label: port $port still in use after 20s" >&2
  return 1
}

ensure_cert() {
  [ "$check_cert" -eq 1 ] || return 0

  # Node rejects an untrusted self-signed certificate, which is what the UI uses
  # for its server-side calls to the API. Exit code 3 means specifically that.
  node -e "fetch('$API_HEALTH').then(()=>process.exit(0)).catch(e=>process.exit(e.cause?.code==='DEPTH_ZERO_SELF_SIGNED_CERT'?3:0))" 2>/dev/null
  local code=$?

  if [ "$code" -eq 3 ]; then
    log "  certificate is not trusted by Node; running dotnet dev-certs https --trust"
    dotnet dev-certs https --trust >/dev/null 2>&1 \
      || log "  could not trust the certificate automatically; run 'dotnet dev-certs https --trust' yourself"
  fi
}

wait_for_api() {
  log "  waiting for API readiness"
  for i in $(seq 1 60); do
    if curl -sk --max-time 5 "$API_HEALTH" 2>/dev/null | grep -q '"ready":true'; then
      log "  API ready after ~$((i * 3))s"
      return 0
    fi
    sleep 3
  done

  log "  API did not become ready within 180s. Recent log:" >&2
  tail -c 1200 "$LOG_DIR/foliotrace-api.log" 2>/dev/null | tr -d '\r' | grep -a -iE 'err|exception' | tail -5 >&2
  return 1
}

start_api() {
  log "Starting API"
  ( cd "$ROOT" && nohup dotnet run --project API --launch-profile https > "$LOG_DIR/foliotrace-api.log" 2>&1 & )
  wait_for_api
}

start_trader() {
  log "Starting FoleoTrader"
  ( cd "$ROOT" && nohup dotnet run --project FoleoTrader --launch-profile https > "$LOG_DIR/foleotrader.log" 2>&1 & )

  for i in $(seq 1 40); do
    if [ -n "$(listening_pids "$TRADER_PORT")" ]; then
      log "  FoleoTrader listening after ~$((i * 3))s"
      return 0
    fi
    sleep 3
  done

  log "  FoleoTrader did not start within 120s. Recent log:" >&2
  tail -c 1200 "$LOG_DIR/foleotrader.log" 2>/dev/null | tr -d '\r' | grep -a -iE 'err|exception' | tail -5 >&2
  return 1
}

start_ui() {
  log "Starting UI dev server"
  ( cd "$ROOT" && nohup npm --prefix UI run dev > "$LOG_DIR/foliotrace-ui.log" 2>&1 & )

  for i in $(seq 1 40); do
    if curl -sk --max-time 5 "https://localhost:${UI_PORT}/health" 2>/dev/null | grep -qi 'ok'; then
      log "  UI ready after ~$((i * 3))s"
      return 0
    fi
    sleep 3
  done

  log "  UI did not respond within 120s. See $LOG_DIR/foliotrace-ui.log" >&2
  return 1
}

status=0

log "Stopping"
[ "$do_api" -eq 1 ] && { stop_port "$API_PORT" "API" || status=1; }
[ "$do_trader" -eq 1 ] && { stop_port "$TRADER_PORT" "FoleoTrader" || status=1; }
[ "$do_ui" -eq 1 ] && { stop_port "$UI_PORT" "UI" || status=1; }

# The API first, then FoleoTrader, then the UI. The UI probes API health on its
# first request and caches the result, so starting it last avoids a cold failure.
if [ "$do_api" -eq 1 ]; then
  start_api || status=1
  ensure_cert
fi

[ "$do_trader" -eq 1 ] && { start_trader || status=1; }
[ "$do_ui" -eq 1 ] && { start_ui || status=1; }

log ""
if [ "$status" -eq 0 ]; then
  log "Done."
  [ "$do_api" -eq 1 ] && log "  API          https://localhost:${API_PORT}"
  [ "$do_trader" -eq 1 ] && log "  FoleoTrader  https://localhost:${TRADER_PORT}"
  [ "$do_ui" -eq 1 ] && log "  UI           https://localhost:${UI_PORT}"
else
  log "Finished with problems; see the messages above." >&2
fi

exit "$status"
