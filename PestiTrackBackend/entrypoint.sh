#!/bin/sh
# PestiTrack container entrypoint.
#
# - Render / any host that sets $PORT: listen on that port instead of 8080.
#   (Render free web services set PORT and health-check that port.)
# - On a normal start, apply EF Core migrations first so the API never
#   serves against a database without tables. Migrations are idempotent;
#   if the database is already current this is a fast no-op.
# - One-shot commands (--migrate, --bootstrap-admin, --healthcheck) run
#   directly without a redundant migrate first (the healthcheck runs
#   every 15s — it must stay cheap).
set -e

if [ -n "$PORT" ]; then
  export ASPNETCORE_URLS="http://+:$PORT"
fi

case "$*" in
  *--healthcheck*|*--migrate*|*--bootstrap-admin*)
    exec dotnet PestiTrackBackend.dll "$@"
    ;;
esac

if [ "${RUN_MIGRATIONS_ON_STARTUP:-true}" = "true" ]; then
  echo "Applying database migrations..."
  dotnet PestiTrackBackend.dll --migrate
fi

exec dotnet PestiTrackBackend.dll "$@"
