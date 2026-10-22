# Desktop Creature Companion — Phase 1 Specification

**Status:** Ready for implementation  
**Target:** macOS (Apple Silicon, macOS 14+), native application  
**Primary goal:** A cute, expressive animated creature that lives unobtrusively on the desktop, with no other functionality yet.

## 1. Product intent

Create a small desktop companion that feels alive. The companion is a friendly **creature** (not a robot, human, or static icon), visible in a floating, transparent, borderless area near a screen corner. It should blink, breathe, subtly sway, and occasionally look around. The user can drag it somewhere else and manage it from the menu bar.

**Phase 1 is deliberately visual-only.** Do not implement chat, speech, LLMs, agent integration, networking, accounts, analytics, notifications, autostart, or a settings UI beyond the minimal menu-bar actions listed below.

## 2. Platform and technology

- Build a **native macOS application**, preferably Swift + SwiftUI for the creature's drawing/animation and AppKit (`NSPanel`/`NSWindow`, `NSStatusItem`) for desktop window behavior.
- Minimum deployment target: **macOS 14**; support current Apple Silicon Macs. No Electron, browser runtime, web server, third-party animation library, or subscription/API dependency.
- Use an Xcode project that can be opened and run locally, or an explicitly documented Swift Package/Xcode-compatible setup. Include reproducible CLI build instructions when practical.
- Prefer a **procedurally drawn vector creature** in SwiftUI (`Shape`, `Path`, `Canvas`, gradients, etc.), so Phase 1 runs without missing external image assets.
- Keep the creature renderer separate from window/menu logic to allow future character and behavior changes.

## 3. Character design

**Default character:** a small, original, cute, rounded fantasy creature. Suggested look: soft blob-shaped body, two expressive eyes, little ears/antennae, tiny feet or paws, a simple mouth, and a pastel-colored palette. Aim for appealing but understated, not hyperactive. Avoid copyrighted character likenesses.

Rendering requirements:
- Transparent pixels outside the character silhouette; **no opaque rectangle**, white canvas, shadow box, or standard title bar behind it.
- Crisp appearance on Retina screens; scale the drawing without pixelation.
- Creature nominal drawing area roughly **160 × 180 points**, adjustable internally for comfortable rendering. Include padding for motion and soft character shadow if desired.
- Facial elements should move coherently with the body; blinking must visibly close and reopen the eyes.
- Do not require art assets or network downloads to run the app.

## 4. Animation behavior

Implement a lightweight idle animation system with gentle, non-disruptive motion:

| Behavior | Expected outcome |
| --- | --- |
| Breathing | Subtle, continuous rhythmic change to body scale/shape; about 2.5–4 seconds per cycle. |
| Blinking | Brief eye-close/open animation at naturally variable intervals, roughly every 3–7 seconds. |
| Looking around | Occasionally shift gaze left/right and back to neutral, roughly every 5–12 seconds. |
| Sway or bounce | Very small, slow sway/bob; do not move the window itself. |
| Drag feedback | Optional slight squash/stretch while dragging, returning smoothly to idle after release. |

Timings are guidance, not exact deadlines. Ensure events do not fight each other or cause jarring position jumps. Use SwiftUI animation or a modest timer-driven state machine; avoid continuous high-frequency CPU loops when a declarative animation suffices. Respect macOS **Reduce Motion**: switch off bob/sway and keep an unobtrusive blink or static appearance.

## 5. Floating window behavior

1. On first launch, show the creature near the **bottom-right** of the main display, inset about **24 points from the visible screen edges** (above the Dock/menu bar as appropriate).
2. Use a **borderless, non-activating transparent floating panel/window**, visible above ordinary application windows. It must not steal keyboard focus when appearing.
3. Show only the character drawing, with no title bar, toolbar, resize handles, docked application chrome, or background panel.
4. **Drag the creature with the mouse/trackpad** to reposition it. The drag interaction must be discoverable without a dedicated handle. Preserve the pointer-to-window offset so it does not jump at drag start.
5. **Persist the last position** using `UserDefaults`; restore it on subsequent launches. Clamp or recover the position to a visible screen area if the display layout changes or a monitor is disconnected.
6. Keep the character **fully on-screen** on all supported displays. For initial positioning, honor `NSScreen.visibleFrame` rather than assuming the entire frame is usable.
7. Support multiple monitors: allow dragging across screens; restoration should not strand the window offscreen.
8. The companion stays visible above normal windows, but **must not force itself over full-screen apps**. Do not request screen-recording, accessibility, microphone, or other unrelated permissions.
9. Clicking or dragging the creature must not trigger an invisible large overlay that intercepts clicks far outside the character. Keep the window bounds compact.
10. **Do not implement click-through in Phase 1** if it conflicts with reliable dragging; prioritize direct manipulation.

## 6. Menu bar control

Provide a compact menu bar item (a simple creature/paw icon or SF Symbol) with:

- **Show Companion / Hide Companion** toggle, reflecting current state.
- **Reset Position**: move the creature to its default bottom-right location and update persistence.
- **Quit**: terminate the application cleanly.

The menu bar item must remain accessible when the companion is hidden. Prefer an accessory/menu-bar app (`LSUIElement` or equivalent) without a redundant Dock icon, provided development/debugging remain straightforward.

## 7. State and implementation boundaries

Suggested separations (names are illustrative):

```text
DesktopCreature/
  App/                 # SwiftUI app entry and lifecycle
  Window/              # Floating panel configuration, positioning, drag behavior
  Creature/            # Procedural drawing and facial components
  Animation/           # Idle animation state/timing and Reduce Motion support
  Preferences/         # Persistence of visibility and position
  Resources/           # App icon and minimal assets, if any
  README.md
```

Keep the MVP single-character, single-window, offline, and dependency-free. Favor simple, maintainable Swift over premature plugin systems. Introduce narrow interfaces for later behaviors but do not build speculative frameworks.

Persist at minimum:

- Companion visibility (show/hide).
- Last known window origin or an equivalent screen-aware position representation.

If the app is hidden on quit, keep the menu bar accessible on next launch and honor saved visibility; document how to show it again.

## 8. User flows

**First launch:** User opens the app → menu bar item appears → creature appears near bottom-right → animations start automatically.

**Move:** User presses on the visible creature → drags to another area or monitor → releases → creature continues idling → position persists after relaunch.

**Hide/show:** User chooses Hide Companion → creature disappears but menu bar remains → chooses Show Companion → creature reappears at its saved location.

**Reset:** User chooses Reset Position → creature moves to default bottom-right placement of the current main display.

**Quit/reopen:** User quits via menu bar → process exits → upon relaunch, saved visibility and location return, with offscreen recovery if necessary.

## 9. Acceptance criteria (definition of done)

- [ ] Project opens and builds on an Apple Silicon Mac running macOS 14+ with a documented Xcode version/toolchain.
- [ ] App launches without accounts, downloads, API keys, or network access.
- [ ] An original, friendly creature (with visible face/body) appears in a transparent, borderless floating window.
- [ ] No opaque background or rectangular white/black canvas surrounds the creature.
- [ ] Idle breathing, blinking, and occasional gaze changes are visible and smooth.
- [ ] Reduce Motion prevents pronounced bobbing/sway.
- [ ] User can reliably drag the character and release it without a jump.
- [ ] Placement is remembered across app restart and recovered after monitor disconnection/resolution changes.
- [ ] Menu bar Show/Hide, Reset Position, and Quit work.
- [ ] Hidden companion does not remove the menu bar controls.
- [ ] App does not unexpectedly acquire keyboard focus or interfere with normal typing.
- [ ] App does not cover full-screen content via aggressive window-level settings.
- [ ] Resource consumption is modest when idle and when hidden (pause unnecessary animation/timers while hidden).
- [ ] README includes prerequisites, exact build/run steps, controls, limitations, and a small manual test checklist.

## 10. Manual verification scenarios

1. Launch app: character appears bottom-right on primary monitor, not behind Dock.
2. Observe for ~30 seconds: multiple natural blinks, breathing, gaze changes, no abrupt movement.
3. Drag around desktop: movement is smooth; click target stays compact.
4. Quit/relaunch: position is restored.
5. Hide/show via menu bar: reliable in both directions; no duplicate creature instances.
6. Reset position: returns to usable bottom-right desktop area.
7. Test with external display connected; drag to second monitor and relaunch.
8. Disconnect external display after saving position on it; relaunch and verify character is visible on remaining display.
9. Enable Reduce Motion in macOS; verify animation adaptation.
10. Open full-screen application; verify companion doesn't intrude unexpectedly.
11. Hide companion and observe: no continued high-frequency redraw/animation workload.

## 11. Non-goals / future milestones

Explicitly **out of scope for Phase 1**: speech, microphone, chat bubbles, emotional reactions to user activity, keyboard shortcuts, clickable actions beyond drag, system monitoring, Codex/Claude integration, remote control, notifications, auto-updates, cloud features, cross-platform builds, and character marketplace.

Possible future progression (do not implement now):
- **Phase 2:** character reactions, hover interactions, interchangeable expressions/skins.
- **Phase 3:** status bubbles and local notifications.
- **Phase 4:** opt-in integrations with coding agents such as Codex/Claude Code.

## 12. Implementation instructions for Codex

1. Treat this document as the authoritative Phase 1 scope and implement the **complete runnable app**, not a mockup or isolated SwiftUI preview.
2. Choose pragmatic macOS-native APIs for a transparent, non-activating floating panel and proper multi-monitor positioning. Explain any platform compromises in README.
3. Start with the original creature drawing; integrate animations; then window dragging, persistence, and menu bar controls.
4. Add any lightweight unit tests practical for positioning/persistence logic. If UI behavior cannot be automated in the environment, provide detailed manual test instructions; do not claim it was tested.
5. Build the project if the toolchain is available. Fix all compilation errors. Report exactly what was verified and what needs macOS GUI testing.
6. Do **not** add features outside Phase 1 or introduce dependencies without a concrete need.

**Result expected:** a self-contained repository containing working source code, project configuration, and README. The user should be able to open the project in Xcode and see the animated companion on their Mac.
