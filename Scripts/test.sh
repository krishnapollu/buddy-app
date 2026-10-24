#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."

default_sdk="$(xcrun --sdk macosx --show-sdk-path)"
if [[ "$(xcode-select -p)" == /Library/Developer/CommandLineTools && -d /Library/Developer/CommandLineTools/SDKs/MacOSX.sdk ]]; then
  default_sdk=/Library/Developer/CommandLineTools/SDKs/MacOSX.sdk
fi
sdk="${MACOS_SDK:-$default_sdk}"
mkdir -p .build/module-cache
export CLANG_MODULE_CACHE_PATH="$PWD/.build/module-cache"
"$(xcrun --find swiftc)" -sdk "$sdk" -target arm64-apple-macos14.0 \
  -module-cache-path "$PWD/.build/module-cache" \
  -o .build/PositionChecks \
  Sources/DesktopCreature/Window/WindowPosition.swift \
  Tests/DesktopCreatureTests/PositionChecks.swift
.build/PositionChecks
"$(xcrun --find swiftc)" -sdk "$sdk" -target arm64-apple-macos14.0 \
  -module-cache-path "$PWD/.build/module-cache" \
  -o .build/AppearanceChecks \
  Sources/DesktopCreature/Creature/CreatureAppearance.swift \
  Sources/DesktopCreature/Preferences/CompanionPreferences.swift \
  Tests/DesktopCreatureTests/AppearanceChecks.swift
.build/AppearanceChecks
"$(xcrun --find swiftc)" -sdk "$sdk" -target arm64-apple-macos14.0 \
  -module-cache-path "$PWD/.build/module-cache" \
  -o .build/IntegrationChecks \
  Sources/DesktopCreature/Integration/IntegrationInbox.swift \
  Tests/DesktopCreatureTests/IntegrationChecks.swift
.build/IntegrationChecks
chain_output="$(/bin/bash Scripts/codex-notify-chain.sh /usr/bin/true /bin/echo existing '{"type":"agent-turn-complete"}')"
[[ "$chain_output" == 'existing {"type":"agent-turn-complete"}' ]]
echo "Codex hook chain check passed"
