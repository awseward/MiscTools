#!/usr/bin/env bash
set -euo pipefail

./paket.macOS.sh restore --fail-on-checks

dotnet restore

mono ./packages/fakebuild/FAKE/tools/FAKE.exe build.fsx $@ --removeLegacyFakeWarning
