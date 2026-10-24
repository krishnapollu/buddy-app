#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
if [[ "${1:-}" != mac ]]; then
  echo "Usage: bash Scripts/build-cross-platform.sh mac" >&2
  exit 2
fi
case "$(uname -m)" in
  arm64) runtime=osx-arm64 ;;
  x86_64) runtime=osx-x64 ;;
  *) echo "Unsupported Mac architecture" >&2; exit 2 ;;
esac
dotnet publish Buddy.Desktop -c Release -r "$runtime" --self-contained true -p:PublishSingleFile=true -o .build/mac
mkdir -p .build/Buddy.app/Contents/MacOS
cp -R .build/mac/. .build/Buddy.app/Contents/MacOS/
cat > .build/Buddy.app/Contents/Info.plist <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleIdentifier</key><string>com.krishnapollu.buddy</string>
<key>CFBundleName</key><string>Buddy</string>
<key>CFBundleExecutable</key><string>Buddy.Desktop</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>LSUIElement</key><true/>
<key>NSHighResolutionCapable</key><true/>
</dict></plist>
PLIST
printf 'Built %s\n' "$PWD/.build/Buddy.app"
