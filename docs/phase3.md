# Phase 3 — Status bubbles and local notifications

- **Show Test Status** in the paw menu displays a four-second, noninteractive bubble beside the creature and updates the menu's latest-status line.
- Agent hook events use the same bubble. It follows the creature when dragged, stays within the visible screen area, and closes when the companion is hidden.
- **Local Notifications** is off by default. Turning it on requests macOS notification authorization. Status events then create local alert notifications, including while the creature is hidden. Turning it off stops new notifications. macOS notification settings can still deny delivery.
- The bubble is another transparent, nonactivating panel and does not join full-screen spaces. Its fixed text is deliberately brief.

Use **Show Test Status** to verify bubbles and notifications before configuring an agent integration. Notification appearance depends on macOS Focus and Notification settings and requires GUI testing.
