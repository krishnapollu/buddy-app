# Desktop Creature Companion

A small, original SwiftUI creature in a transparent AppKit panel. It breathes, blinks, looks around, reacts to hover and clicks, and can be dragged between displays. The menu bar controls its appearance, short status bubbles, optional local notifications, and optional Codex CLI and Claude Code hook integrations. The app has no third-party dependencies or network calls.

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

Run the positioning, preference, and integration checks with:

```sh
bash Scripts/test.sh
```

## Controls

- Drag anywhere inside the compact creature window to move it. The pointer stays at the same spot on the creature while dragging.
- Hover over the creature to see a curious face. Click it to see a brief happy reaction. A drag does not trigger the click reaction.
- Click the paw icon in the menu bar to **Hide Companion**, **Show Companion**, **Reset Position**, choose an **Expression** or **Skin**, or **Quit**.
- Choose **Show Test Status** to display a four-second bubble. The latest status also appears in the paw menu.
- Turn on **Local Notifications** to request macOS notification permission. When enabled, status events can also appear in Notification Center. This is off by default.
- Turn on **Agent Integrations → Codex CLI** or **Claude Code** only after setting up that tool's local hook. Both are off by default; see [integration setup](docs/phase4.md).
- The menu bar icon remains available when the creature is hidden. If you quit while hidden, relaunch the app and use **Show Companion** there.

Visibility, position, expression, skin, and notification choice are stored in `UserDefaults`. Integration switches are stored as local marker files in the user's Application Support folder. On launch and show, an offscreen saved position is moved into a visible display area. Reset places the creature near the bottom-right of the main display's usable area.

The integration hooks pass only fixed completion or attention labels to the app. Prompts, responses, transcript paths, and working directories are not stored or displayed. The hook setup and limits are documented in [Phase 4](docs/phase4.md); status and notification behavior is documented in [Phase 3](docs/phase3.md).

## Manual test checklist

1. Launch: check that the creature appears above the Dock near the bottom-right and has no rectangular background or Dock icon.
2. Watch for 30 seconds: breathing, several blinks, and gaze shifts should look gentle. Hide it and check that animation activity stops.
3. Drag it around, release, quit, and relaunch; check that its position returns without a drag-start jump.
4. Hide and show it repeatedly from the menu bar, then reset its position and quit.
5. Drag to an external display and relaunch. Disconnect that display, relaunch, and check that the creature is back on a visible display.
6. Enable **System Settings → Accessibility → Display → Reduce motion**; check that bobbing and sway stop while blinking remains.
7. Enter a full-screen app; check that the creature does not cover it. Click the creature and continue typing in another app; keyboard focus should remain there.
8. Hover, click, and drag the creature in turn. Check that hover and click change its face, while dragging only moves it.
9. Select each expression and skin from the paw menu. Quit and relaunch to check that the chosen combination returns. Hide and show the creature to check that the reaction does not continue while hidden.
10. Choose **Show Test Status**. Check that the bubble appears near the creature, follows a drag, and disappears after four seconds. Hide the creature and confirm the bubble closes.
11. Enable **Local Notifications** and grant permission. Choose **Show Test Status** again and check for a macOS notification. Disable notifications and repeat; there should be no new notification.
12. After configuring a Codex CLI or Claude Code hook, enable that integration and finish a turn. Check the bubble or latest menu status, then disable the integration and repeat; no new status should appear.

The panel is deliberately compact (160 × 180 points) but its transparent corners are still part of its drag region. It uses the ordinary floating level and does not opt into full-screen spaces. A regular app window in a separate full-screen space can therefore hide it, as intended.
