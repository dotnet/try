#!/usr/bin/env bash

set -e

source="${BASH_SOURCE[0]}"

# resolve $source until the file is no longer a symlink
while [[ -h "$source" ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"
  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done
scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"

# parse args
args=""
no_dotnet=false
run_tests=false

# scan for `--test` or `--ci` switches
while [[ $# > 0 ]]; do
  opt="$(echo "$1" | awk '{print tolower($0)}')"
  case "$opt" in
    --no-dotnet)
      no_dotnet=true
      ;;
    --test|-t)
      run_tests=true
      ;;
    *)
      args="$args $1"
      ;;
  esac
  shift
done

# build and test NPM
npmDirs='src/microsoft-trydotnet
         src/microsoft-trydotnet-editor
         src/microsoft-trydotnet-styles
         src/microsoft-learn-mock'
for npmDir in $npmDirs;
do
  echo "Building NPM in directory $npmDir"
  pushd $npmDir
  npm ci
  npm run buildProd
  if [[ "$run_tests" == true ]]; then
    echo "Testing NPM in directory $npmDir"
    npm run ciTest
  fi
  popd
done

if [[ "$no_dotnet" != true ]]; then
  # invoke regular build script
  . "$scriptroot/common/build.sh" $args
fi
