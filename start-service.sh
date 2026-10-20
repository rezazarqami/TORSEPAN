#!/bin/sh
set -eu
case "${TORSEPAN_SERVICE:-}" in
  api) assembly=TORSEPAN.API.dll ;;
  panel) assembly=TORSEPAN.Panel.dll ;;
  *) echo 'TORSEPAN_SERVICE must be api or panel' >&2; exit 64 ;;
esac
cd "${TORSEPAN_APP_ROOT:-/app}/$TORSEPAN_SERVICE"
exec dotnet "$assembly" "$@"
