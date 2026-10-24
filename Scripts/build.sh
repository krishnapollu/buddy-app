#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."

default_sdk="$(xcrun --sdk macosx --show-sdk-path)"
if [[ "$(xcode-select -p)" == /Library/Developer/CommandLineTools && -d /Library/Developer/CommandLineTools/SDKs/MacOSX.sdk ]]; then
  default_sdk=/Library/Developer/CommandLineTools/SDKs/MacOSX.sdk
fi
sdk="${MACOS_SDK:-$default_sdk}"
output="${BUILD_DIR:-.build/DesktopCreature.app}"
mkdir -p "$output/Contents/MacOS" "$output/Contents/Resources" .build/module-cache
export CLANG_MODULE_CACHE_PATH="$PWD/.build/module-cache"

sources=()
while IFS= read -r -d '' file; do sources+=("$file"); done < <(find Sources/DesktopCreature -name '*.swift' -print0)
"$(xcrun --find swiftc)" -sdk "$sdk" -target arm64-apple-macos14.0 \
  -module-cache-path "$PWD/.build/module-cache" -parse-as-library \
  -o "$output/Contents/MacOS/DesktopCreature" "${sources[@]}"
cp Resources/Info.plist "$output/Contents/Info.plist"
cp Scripts/codex-notify-chain.sh "$output/Contents/Resources/codex-notify-chain.sh"
codesign --force --sign - "$output"
echo "Built $output"
