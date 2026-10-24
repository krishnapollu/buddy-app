# Buddy

Buddy is a desktop creature companion for macOS and Windows. The shared Avalonia/.NET app lets you choose from 18 original themed avatars or import a PNG, JPEG, or WebP image. It reminds you to drink water, blink, and stretch; accepts manual meetings and an HTTPS ICS calendar feed; moves when a reminder appears; and can speak reminders using the operating system's voice. It also accepts the earlier opt-in local Codex CLI and Claude Code status hooks.

## Requirements

- macOS 12+ or Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) to build or run from source

## Run from source

```sh
dotnet run --project Buddy.Desktop -c Release
```

Buddy appears near the bottom-right of your screen. Drag it to move it, click **⚙** or double-click the avatar for settings, and use the paw icon in the macOS menu bar or Windows notification area to show, hide, open settings, or quit. The **×** button hides Buddy; it does not stop reminder timers. Choose **Quit Buddy** from the tray menu to close it.

Settings are saved locally in the user's application data folder under `Buddy/settings.json`. Imported images are copied there. An ICS feed is fetched over HTTPS every five minutes. Calendar event titles remain local to Buddy and are only spoken when voice is enabled. The feed URL itself may contain a private token; treat it as a secret.

## Build an installable app

On macOS:

```sh
bash Scripts/build-cross-platform.sh mac
open .build/Buddy.app
```

Copy `.build/Buddy.app` to Applications if desired. On Windows, run in PowerShell:

```powershell
dotnet publish Buddy.Desktop -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .build/windows
.\.build\windows\Buddy.Desktop.exe
```

The Windows publish folder includes native DLLs alongside the executable; keep the whole folder together. These builds are unsigned, so the operating system may ask you to approve launching your own build.

## Optional agent status hooks

In Settings, enable **Codex CLI** or **Claude Code** after configuring that tool's local command hook. Use Buddy's executable with `--hook codex` for Codex CLI's `notify` command, or `--hook claude` for Claude Code's Stop, StopFailure, and Notification hooks. On macOS, an installed executable is `/Applications/Buddy.app/Contents/MacOS/Buddy.Desktop`; on Windows, use the full path to `Buddy.Desktop.exe`. Codex passes JSON as the next argument; Claude sends JSON on standard input. Buddy reads only the event type and shows a fixed local status label. The [older hook guide](docs/phase4.md) shows the config format for each tool; replace its legacy executable path with the new Buddy executable. These hooks are off by default.

## Test

```sh
dotnet run --project Buddy.Desktop -c Release -- --self-test
```

Then launch the app and check: choose each category in Settings, import an image, set a one-minute water reminder, add a meeting a few minutes ahead, and confirm Buddy shows and speaks the reminder and travels across the screen. For calendar sync, enter an HTTPS ICS URL containing an upcoming event and check the sync status in Settings. Hide and restore Buddy from the tray. Quit and relaunch to check the saved avatar and position. Windows publishing is checked from macOS, but the Windows GUI and speech need a Windows machine for final manual verification.

The previous Swift macOS app and its build scripts remain in the repo as [legacy documentation](docs/legacy-swift.md). Its local settings are separate from this cross-platform app.
