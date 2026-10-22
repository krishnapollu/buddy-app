import AppKit
import SwiftUI

@MainActor
@main
enum DesktopCreatureApp {
    private static let appController = AppController()

    static func main() {
        let application = NSApplication.shared
        application.setActivationPolicy(.accessory)
        application.delegate = appController
        application.run()
    }
}

@MainActor
final class AppController: NSObject, NSApplicationDelegate {
    private let preferences = CompanionPreferences()
    private let animator = CreatureAnimator()
    private var panel: CreaturePanel!
    private var statusItem: NSStatusItem!
    private var visibilityItem: NSMenuItem!

    func applicationDidFinishLaunching(_ notification: Notification) {
        let size = NSSize(width: 160, height: 180)
        let origin = WindowPosition.restored(
            preferences.position,
            size: size,
            screens: NSScreen.screens.map(\.visibleFrame),
            defaultScreen: NSScreen.main?.visibleFrame
        )
        panel = CreaturePanel(
            content: CreatureView(animator: animator),
            frame: NSRect(origin: origin, size: size)
        )
        panel.onDragEnd = { [weak self] origin in self?.preferences.position = origin }

        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.squareLength)
        statusItem.button?.image = NSImage(systemSymbolName: "pawprint.fill", accessibilityDescription: "Desktop Creature")
        statusItem.button?.toolTip = "Desktop Creature"
        let menu = NSMenu()
        visibilityItem = NSMenuItem(title: "", action: #selector(toggleVisibility), keyEquivalent: "")
        visibilityItem.target = self
        menu.addItem(visibilityItem)
        let resetItem = NSMenuItem(title: "Reset Position", action: #selector(resetPosition), keyEquivalent: "")
        resetItem.target = self
        menu.addItem(resetItem)
        menu.addItem(.separator())
        let quitItem = NSMenuItem(title: "Quit", action: #selector(quit), keyEquivalent: "")
        quitItem.target = self
        menu.addItem(quitItem)
        statusItem.menu = menu

        setVisible(preferences.isVisible)
    }

    private func setVisible(_ visible: Bool) {
        preferences.isVisible = visible
        visibilityItem.title = visible ? "Hide Companion" : "Show Companion"
        if visible {
            panel.setFrameOrigin(WindowPosition.restored(
                panel.frame.origin,
                size: panel.frame.size,
                screens: NSScreen.screens.map(\.visibleFrame),
                defaultScreen: NSScreen.main?.visibleFrame
            ))
            preferences.position = panel.frame.origin
            panel.orderFrontRegardless()
            animator.start()
        } else {
            animator.stop()
            panel.orderOut(nil)
        }
    }

    @objc private func toggleVisibility() {
        setVisible(!preferences.isVisible)
    }

    @objc private func resetPosition() {
        guard let visibleFrame = NSScreen.main?.visibleFrame ?? NSScreen.screens.first?.visibleFrame else { return }
        let origin = WindowPosition.defaultOrigin(size: panel.frame.size, in: visibleFrame)
        panel.setFrameOrigin(origin)
        preferences.position = origin
    }

    @objc private func quit() {
        NSApplication.shared.terminate(nil)
    }

    func applicationWillTerminate(_ notification: Notification) {
        animator.stop()
    }
}
