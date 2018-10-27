#!/usr/bin/env bash
set -euo pipefail

./paket.sh restore --fail-on-checks

./packages/fakebuild/FAKE/tools/FAKE.exe build.fsx $@ --removeLegacyFakeWarning
