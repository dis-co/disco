#!/usr/bin/env bash

set -eu
set -o pipefail

cd `dirname $0`

FSIARGS=""
OS=${OS:-"unknown"}
if [[ "$OS"   != "Windows_NT" ]]
then
  FSIARGS="--fsiargs -d:MONO"
fi

function run() {
  if [[ "$OS" != "Windows_NT" ]]
  then
    mono "$@"
  else
    "$@"
  fi
}

if [[ "$OS" != "Windows_NT" ]] &&
       [ ! -e ~/.config/.mono/certs ]
then
  mozroots --import --sync --quiet
fi

if test "$OS" = "Windows_NT"; then
  MONO=""
else
  # Mono fix for https://github.com/fsharp/FAKE/issues/805
  export MONO_MANAGED_WATCHER=false
  MONO="mono"
fi

if [ -e "paket.lock" ]; then
$MONO .paket/paket.exe restore
else
$MONO .paket/paket.exe install
fi
exit_code=$?
if [ $exit_code -ne 0 ]; then
exit $exit_code
fi

run packages/build/FAKE/tools/FAKE.exe build.fsx "$@" "parallel-jobs=4" $FSIARGS
