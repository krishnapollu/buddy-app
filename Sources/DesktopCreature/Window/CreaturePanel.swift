import AppKit
import SwiftUI

final class CreaturePanel: NSPanel {
    var onDragEnd: ((NSPoint) -> Void)?
    var onHoverChange: ((Bool) -> Void)?
    var onClick: (() -> Void)?

    init(content: CreatureView, frame: NSRect) {
        super.init(contentRect: frame, styleMask: [.borderless, .nonactivatingPanel], backing: .buffered, defer: false)
        isOpaque = false
        backgroundColor = .clear
        hasShadow = false
        level = .floating
        hidesOnDeactivate = false
        isMovable = false
        collectionBehavior = [.managed]
        let host = CreatureHostView(rootView: content)
        host.onDragEnd = { [weak self] origin in self?.onDragEnd?(origin) }
        host.onHoverChange = { [weak self] hovered in self?.onHoverChange?(hovered) }
        host.onClick = { [weak self] in self?.onClick?() }
        contentView = host
    }

    override var canBecomeKey: Bool { false }
    override var canBecomeMain: Bool { false }
}

private final class CreatureHostView: NSHostingView<CreatureView> {
    var onDragEnd: ((NSPoint) -> Void)?
    var onHoverChange: ((Bool) -> Void)?
    var onClick: (() -> Void)?
    private var pointerOffset: NSPoint?
    private var pointerDownLocation: NSPoint?
    private var didDrag = false
    private var hoverTrackingArea: NSTrackingArea?

    override func updateTrackingAreas() {
        if let hoverTrackingArea { removeTrackingArea(hoverTrackingArea) }
        super.updateTrackingAreas()
        let area = NSTrackingArea(rect: .zero,
                                  options: [.mouseEnteredAndExited, .activeAlways, .inVisibleRect],
                                  owner: self, userInfo: nil)
        addTrackingArea(area)
        hoverTrackingArea = area
    }

    override func acceptsFirstMouse(for event: NSEvent?) -> Bool { true }
    override func mouseEntered(with event: NSEvent) { onHoverChange?(true) }
    override func mouseExited(with event: NSEvent) { onHoverChange?(false) }

    override func mouseDown(with event: NSEvent) {
        guard let window else { return }
        let pointer = NSEvent.mouseLocation
        pointerOffset = NSPoint(x: pointer.x - window.frame.minX, y: pointer.y - window.frame.minY)
        pointerDownLocation = pointer
        didDrag = false
    }

    override func mouseDragged(with event: NSEvent) {
        guard let window, let pointerOffset, let pointerDownLocation else { return }
        let pointer = NSEvent.mouseLocation
        if !didDrag {
            let distance = hypot(pointer.x - pointerDownLocation.x, pointer.y - pointerDownLocation.y)
            guard distance > 3 else { return }
            didDrag = true
        }
        guard let screen = WindowPosition.screen(containing: pointer, screens: NSScreen.screens) else { return }
        let wanted = NSPoint(x: pointer.x - pointerOffset.x, y: pointer.y - pointerOffset.y)
        window.setFrameOrigin(WindowPosition.clamp(wanted, size: window.frame.size, to: screen.visibleFrame))
    }

    override func mouseUp(with event: NSEvent) {
        guard pointerOffset != nil else { return }
        pointerOffset = nil
        pointerDownLocation = nil
        if didDrag, let window {
            onDragEnd?(window.frame.origin)
        } else {
            onClick?()
        }
        didDrag = false
    }
}
