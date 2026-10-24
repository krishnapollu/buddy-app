import AppKit
import SwiftUI

@MainActor
final class StatusBubblePanel: NSPanel {
    private var hideTask: Task<Void, Never>?

    init() {
        super.init(contentRect: NSRect(x: 0, y: 0, width: 224, height: 58),
                   styleMask: [.borderless, .nonactivatingPanel], backing: .buffered, defer: false)
        isOpaque = false
        backgroundColor = .clear
        hasShadow = false
        ignoresMouseEvents = true
        level = .floating
        collectionBehavior = [.managed]
    }

    override var canBecomeKey: Bool { false }
    override var canBecomeMain: Bool { false }

    func show(_ title: String, detail: String, beside companion: NSWindow) {
        hideTask?.cancel()
        contentView = NSHostingView(rootView: StatusBubbleView(title: title, detail: detail))
        reposition(beside: companion)
        orderFrontRegardless()
        hideTask = Task { [weak self] in
            try? await Task.sleep(for: .seconds(4))
            guard !Task.isCancelled else { return }
            self?.orderOut(nil)
        }
    }

    func reposition(beside companion: NSWindow) {
        guard isVisible || contentView != nil else { return }
        let center = NSPoint(x: companion.frame.midX, y: companion.frame.midY)
        guard let screen = WindowPosition.screen(containing: center, screens: NSScreen.screens) else { return }
        let visible = screen.visibleFrame
        let above = companion.frame.maxY + 6
        let y = above + frame.height <= visible.maxY ? above : companion.frame.minY - frame.height - 6
        let origin = NSPoint(x: companion.frame.midX - frame.width / 2, y: y)
        setFrameOrigin(WindowPosition.clamp(origin, size: frame.size, to: visible))
    }

    func dismiss() {
        hideTask?.cancel()
        hideTask = nil
        orderOut(nil)
    }
}

private struct StatusBubbleView: View {
    let title: String
    let detail: String

    var body: some View {
        VStack(alignment: .leading, spacing: 2) {
            Text(title).font(.system(size: 13, weight: .semibold))
            Text(detail).font(.system(size: 11)).lineLimit(1)
        }
        .foregroundStyle(Color.primary)
        .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .leading)
        .padding(.horizontal, 14)
        .background(.regularMaterial, in: RoundedRectangle(cornerRadius: 16))
        .overlay(RoundedRectangle(cornerRadius: 16).stroke(.white.opacity(0.45)))
        .padding(4)
        .frame(width: 224, height: 58)
    }
}
