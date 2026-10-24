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
    private var expressionItems: [CreatureExpression: NSMenuItem] = [:]
    private var skinItems: [CreatureSkin: NSMenuItem] = [:]

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
        panel.onDragEnd = { [weak self] origin in self?.preferences.position = origin }
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
        let quitItem = NSMenuItem(title: "Quit", action: #selector(quit), keyEquivalent: "")
        quitItem.target = self
        menu.addItem(quitItem)
        statusItem.menu = menu
        updateSelectionStates()

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
    }

    @objc private func quit() {
        NSApplication.shared.terminate(nil)
    }

    func applicationWillTerminate(_ notification: Notification) {
        animator.stop()
    }
}
