import AppKit
import SwiftUI

@MainActor
@main
enum DesktopCreatureApp {
    private static let appController = AppController()

    static func main() {
        if CommandLine.arguments.dropFirst().first == "--hook" {
            HookReceiver.receive(arguments: CommandLine.arguments)
            return
        }
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
    private let inbox = IntegrationInbox()
    private let notifier = LocalNotifier()
    private let bubble = StatusBubblePanel()
    private var panel: CreaturePanel!
    private var statusItem: NSStatusItem!
    private var visibilityItem: NSMenuItem!
    private var latestStatusItem: NSMenuItem!
    private var notificationItem: NSMenuItem!
    private var integrationItems: [AgentSource: NSMenuItem] = [:]
    private var pollTimer: Timer?
    private var expressionItems: [CreatureExpression: NSMenuItem] = [:]
    private var skinItems: [CreatureSkin: NSMenuItem] = [:]

    func applicationWillFinishLaunching(_ notification: Notification) {
        notifier.start()
    }

    func applicationDidFinishLaunching(_ notification: Notification) {
        animator.expression = preferences.expression
        animator.skin = preferences.skin
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
        panel.onDragEnd = { [weak self] origin in
            self?.preferences.position = origin
            if let panel = self?.panel { self?.bubble.reposition(beside: panel) }
        }
        panel.onDragMove = { [weak self] in
            if let panel = self?.panel { self?.bubble.reposition(beside: panel) }
        }
        panel.onHoverChange = { [weak self] hovered in self?.animator.setHovered(hovered) }
        panel.onClick = { [weak self] in self?.animator.reactToClick() }

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
        let expressionMenu = NSMenu()
        for expression in CreatureExpression.selectable {
            let item = NSMenuItem(title: expression.rawValue, action: #selector(selectExpression(_:)), keyEquivalent: "")
            item.target = self
            item.representedObject = expression.rawValue
            expressionMenu.addItem(item)
            expressionItems[expression] = item
        }
        let expressionRoot = NSMenuItem(title: "Expression", action: nil, keyEquivalent: "")
        expressionRoot.submenu = expressionMenu
        menu.addItem(expressionRoot)

        let skinMenu = NSMenu()
        for skin in CreatureSkin.allCases {
            let item = NSMenuItem(title: skin.rawValue, action: #selector(selectSkin(_:)), keyEquivalent: "")
            item.target = self
            item.representedObject = skin.rawValue
            skinMenu.addItem(item)
            skinItems[skin] = item
        }
        let skinRoot = NSMenuItem(title: "Skin", action: nil, keyEquivalent: "")
        skinRoot.submenu = skinMenu
        menu.addItem(skinRoot)
        menu.addItem(.separator())

        latestStatusItem = NSMenuItem(title: "Status: Ready", action: nil, keyEquivalent: "")
        latestStatusItem.isEnabled = false
        menu.addItem(latestStatusItem)
        let testItem = NSMenuItem(title: "Show Test Status", action: #selector(showTestStatus), keyEquivalent: "")
        testItem.target = self
        menu.addItem(testItem)
        notificationItem = NSMenuItem(title: "Local Notifications", action: #selector(toggleNotifications), keyEquivalent: "")
        notificationItem.target = self
        notificationItem.state = preferences.notificationsEnabled ? .on : .off
        menu.addItem(notificationItem)

        let integrationMenu = NSMenu()
        for source in AgentSource.allCases {
            let item = NSMenuItem(title: source.name, action: #selector(toggleIntegration(_:)), keyEquivalent: "")
            item.target = self
            item.representedObject = source.rawValue
            item.state = inbox.isEnabled(source) ? .on : .off
            integrationMenu.addItem(item)
            integrationItems[source] = item
        }
        let integrationRoot = NSMenuItem(title: "Agent Integrations", action: nil, keyEquivalent: "")
        integrationRoot.submenu = integrationMenu
        menu.addItem(integrationRoot)
        menu.addItem(.separator())
        let quitItem = NSMenuItem(title: "Quit", action: #selector(quit), keyEquivalent: "")
        quitItem.target = self
        menu.addItem(quitItem)
        statusItem.menu = menu
        updateSelectionStates()

        setVisible(preferences.isVisible)
        updatePolling()
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
            bubble.dismiss()
            panel.orderOut(nil)
        }
    }

    @objc private func showTestStatus() {
        presentStatus(title: "Buddy is ready", detail: "Status bubbles are working.")
    }

    @objc private func toggleNotifications() {
        if preferences.notificationsEnabled {
            preferences.notificationsEnabled = false
            notificationItem.state = .off
        } else {
            notifier.requestPermission { [weak self] granted in
                guard let self else { return }
                self.preferences.notificationsEnabled = granted
                self.notificationItem.state = granted ? .on : .off
                if !granted { self.presentStatus(title: "Notifications unavailable", detail: "Check macOS notification settings.") }
            }
        }
    }

    @objc private func toggleIntegration(_ sender: NSMenuItem) {
        guard let raw = sender.representedObject as? String,
              let source = AgentSource(rawValue: raw) else { return }
        do {
            let enabled = !inbox.isEnabled(source)
            try inbox.setEnabled(enabled, for: source)
            sender.state = enabled ? .on : .off
            updatePolling()
            presentStatus(title: "\(source.name) \(enabled ? "enabled" : "disabled")",
                          detail: enabled ? "Waiting for local hook events." : "Hook events will be ignored.")
        } catch {
            presentStatus(title: "Could not change integration", detail: "Check Application Support access.")
        }
    }

    private func updatePolling() {
        pollTimer?.invalidate()
        pollTimer = nil
        guard AgentSource.allCases.contains(where: inbox.isEnabled) else { return }
        pollTimer = Timer.scheduledTimer(withTimeInterval: 1, repeats: true) { [weak self] _ in
            Task { @MainActor [weak self] in self?.consumeEvents() }
        }
    }

    private func consumeEvents() {
        for event in inbox.drain() {
            presentStatus(title: event.title, detail: event.detail)
        }
    }

    private func presentStatus(title: String, detail: String) {
        latestStatusItem.title = "Status: \(title)"
        if preferences.isVisible { bubble.show(title, detail: detail, beside: panel) }
        if preferences.notificationsEnabled { notifier.send(title: title, detail: detail) }
    }

    @objc private func toggleVisibility() {
        setVisible(!preferences.isVisible)
    }

    @objc private func selectExpression(_ sender: NSMenuItem) {
        guard let raw = sender.representedObject as? String,
              let expression = CreatureExpression(rawValue: raw) else { return }
        animator.expression = expression
        preferences.expression = expression
        updateSelectionStates()
    }

    @objc private func selectSkin(_ sender: NSMenuItem) {
        guard let raw = sender.representedObject as? String,
              let skin = CreatureSkin(rawValue: raw) else { return }
        animator.skin = skin
        preferences.skin = skin
        updateSelectionStates()
    }

    private func updateSelectionStates() {
        for (expression, item) in expressionItems {
            item.state = expression == animator.expression ? .on : .off
        }
        for (skin, item) in skinItems {
            item.state = skin == animator.skin ? .on : .off
        }
    }

    @objc private func resetPosition() {
        guard let visibleFrame = NSScreen.main?.visibleFrame ?? NSScreen.screens.first?.visibleFrame else { return }
        let origin = WindowPosition.defaultOrigin(size: panel.frame.size, in: visibleFrame)
        panel.setFrameOrigin(origin)
        preferences.position = origin
        bubble.reposition(beside: panel)
    }

    @objc private func quit() {
        NSApplication.shared.terminate(nil)
    }

    func applicationWillTerminate(_ notification: Notification) {
        pollTimer?.invalidate()
        bubble.dismiss()
        animator.stop()
    }
}
