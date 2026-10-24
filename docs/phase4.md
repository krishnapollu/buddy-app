# Phase 4 — Opt-in local agent integrations

Buddy can show a generic status when **Codex CLI** finishes a turn or **Claude Code** finishes or asks for attention. These are local command hooks. Buddy does not call an API, read agent transcripts, or send data over the network. Both integrations start off.

## Install the app at a stable path

Quit Buddy, build it with `bash Scripts/build.sh`, then copy `.build/DesktopCreature.app` to `/Applications/DesktopCreature.app`. Relaunch the installed copy. If you install it elsewhere, replace the executable path in the examples below.

## Codex CLI

Add this **top-level** line to your user-level `~/.codex/config.toml`:

```toml
notify = ["/Applications/DesktopCreature.app/Contents/MacOS/DesktopCreature", "--hook", "codex"]
```

Then enable **Agent Integrations → Codex CLI** in Buddy's paw menu. Codex appends one JSON argument to the command after a completed turn. Buddy checks only the `agent-turn-complete` event type. Codex ignores `notify` in project-local configuration, so place this in your user-level file. If you already have a `notify` command, preserve it with the bundled chain script below.

If `notify` is already configured, use Buddy's bundled chain script instead. Replace the final example command and arguments with your existing `notify` array entries:

```toml
notify = [
  "/bin/bash",
  "/Applications/DesktopCreature.app/Contents/Resources/codex-notify-chain.sh",
  "/Applications/DesktopCreature.app/Contents/MacOS/DesktopCreature",
  "/path/to/existing-notifier", "existing-argument"
]
```

The chain passes Codex's JSON to Buddy, then runs the original notifier with its original arguments and the same JSON. Keep the app installed at the configured path.

## Claude Code

Merge these entries into the `hooks` object of your user-level `~/.claude/settings.json`:

```json
{
  "hooks": {
    "Stop": [{"hooks": [{"type": "command", "command": "/Applications/DesktopCreature.app/Contents/MacOS/DesktopCreature", "args": ["--hook", "claude"]}]}],
    "StopFailure": [{"hooks": [{"type": "command", "command": "/Applications/DesktopCreature.app/Contents/MacOS/DesktopCreature", "args": ["--hook", "claude"]}]}],
    "Notification": [{"hooks": [{"type": "command", "command": "/Applications/DesktopCreature.app/Contents/MacOS/DesktopCreature", "args": ["--hook", "claude"]}]}]
  }
}
```

Then enable **Agent Integrations → Claude Code** in Buddy's paw menu. Claude Code sends hook JSON on stdin. Buddy reads only `hook_event_name`, and gives every event a fixed label; notification messages and assistant text are discarded. Merge with existing hook arrays instead of replacing them.

## Test and turn off

Finish a turn in the configured CLI. The paw menu should show the latest status; when visible, the creature shows a four-second bubble. Optionally turn on **Local Notifications** for a macOS alert. To stop listening, switch the integration off in the menu. Remove the hook configuration to stop the CLI from invoking Buddy's helper process.

Hook events are written as small, short-lived JSON files in `~/Library/Application Support/DesktopCreature/Events`. The directory is user-only, and Buddy ignores old events after 45 seconds. The app checks for events once per second only while at least one integration is enabled. This setup covers local Codex CLI and Claude Code sessions; it does not claim to observe cloud or desktop-app sessions that do not run these hooks.

Sources: [official Codex notification configuration](https://learn.chatgpt.com/docs/config-file/config-advanced#notifications), [Codex configuration scope](https://learn.chatgpt.com/docs/config-file/config-reference), and [Claude Code hooks reference](https://code.claude.com/docs/en/hooks).
