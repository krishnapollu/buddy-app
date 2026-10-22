# Desktop Creature Companion — Phase 1

A small, original SwiftUI creature in a transparent AppKit panel. It breathes, blinks, looks around, and can be dragged between displays. The menu bar controls visibility, position reset, and quitting. The app works offline and has no dependencies or permissions to grant.

## Requirements

- Apple Silicon Mac running macOS 14 or later
- Xcode with a macOS SDK and Swift 5.10 or later, or matching Apple Command Line Tools

Build verification for this repository used Apple Swift 6.3.3 Command Line Tools and the macOS 26.5 SDK. Full Xcode was not installed in that environment.

`Package.swift` can be opened in Xcode to inspect and build the source. To run it as a menu-bar app, build the `.app` bundle using the script below; a bare Swift Package executable does not carry the `LSUIElement` bundle setting.

## Build and launch

From the repository root:

```sh
bash Scripts/build.sh
open .build/DesktopCreature.app
```

The build script compiles for Apple Silicon with a macOS 14 deployment target and places the app at `.build/DesktopCreature.app`. It uses the active Xcode toolchain and SDK, or the Command Line Tools default SDK. If the installed compiler and SDK still mismatch, select a matching Xcode toolchain with `xcode-select` or pass `MACOS_SDK=/path/to/a/compatible/MacOSX.sdk` to the build script.

To keep it installed, copy `.build/DesktopCreature.app` into your **Applications** folder using Finder, then open **Desktop Creature** there. The app has no Dock icon; look for the paw icon in the menu bar. Quit from that menu before replacing an installed copy with a new build.

Run the small positioning checks with:

```sh
bash Scripts/test.sh
```

## Controls

- Drag anywhere inside the compact creature window to move it. The pointer stays at the same spot on the creature while dragging.
- Click the paw icon in the menu bar to **Hide Companion**, **Show Companion**, **Reset Position**, or **Quit**.
- The menu bar icon remains available when the creature is hidden. If you quit while hidden, relaunch the app and use **Show Companion** there.

Visibility and position are stored in `UserDefaults`. On launch and show, an offscreen saved position is moved into a visible display area. Reset places the creature near the bottom-right of the main display's usable area.

## Manual test checklist

1. Launch: check that the creature appears above the Dock near the bottom-right and has no rectangular background or Dock icon.
2. Watch for 30 seconds: breathing, several blinks, and gaze shifts should look gentle. Hide it and check that animation activity stops.
3. Drag it around, release, quit, and relaunch; check that its position returns without a drag-start jump.
4. Hide and show it repeatedly from the menu bar, then reset its position and quit.
5. Drag to an external display and relaunch. Disconnect that display, relaunch, and check that the creature is back on a visible display.
6. Enable **System Settings → Accessibility → Display → Reduce motion**; check that bobbing and sway stop while blinking remains.
7. Enter a full-screen app; check that the creature does not cover it. Click the creature and continue typing in another app; keyboard focus should remain there.

The panel is deliberately compact (160 × 180 points) but its transparent corners are still part of its drag region. It uses the ordinary floating level and does not opt into full-screen spaces. A regular app window in a separate full-screen space can therefore hide it, as intended.
