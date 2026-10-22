import AppKit

enum WindowPosition {
    static func defaultOrigin(size: NSSize, in visibleFrame: NSRect) -> NSPoint {
        NSPoint(x: visibleFrame.maxX - size.width - 24,
                y: visibleFrame.minY + 24)
    }

    static func restored(_ saved: NSPoint?, size: NSSize, screens: [NSRect], defaultScreen: NSRect?) -> NSPoint {
        guard let fallback = defaultScreen ?? screens.first else { return saved ?? .zero }
        guard let saved, saved.x.isFinite, saved.y.isFinite else {
            return defaultOrigin(size: size, in: fallback)
        }
        if let screen = screens.first(where: { $0.contains(NSRect(origin: saved, size: size)) }) {
            return clamp(saved, size: size, to: screen)
        }
        let nearest = screens.min { distanceSquared(saved, to: $0) < distanceSquared(saved, to: $1) } ?? fallback
        return clamp(saved, size: size, to: nearest)
    }

    static func clamp(_ origin: NSPoint, size: NSSize, to visibleFrame: NSRect) -> NSPoint {
        NSPoint(
            x: min(max(origin.x, visibleFrame.minX), max(visibleFrame.minX, visibleFrame.maxX - size.width)),
            y: min(max(origin.y, visibleFrame.minY), max(visibleFrame.minY, visibleFrame.maxY - size.height))
        )
    }

    static func screen(containing point: NSPoint, screens: [NSScreen]) -> NSScreen? {
        screens.first(where: { $0.frame.contains(point) })
            ?? screens.min { distanceSquared(point, to: $0.frame) < distanceSquared(point, to: $1.frame) }
    }

    private static func distanceSquared(_ point: NSPoint, to rect: NSRect) -> CGFloat {
        let dx = point.x - min(max(point.x, rect.minX), rect.maxX)
        let dy = point.y - min(max(point.y, rect.minY), rect.maxY)
        return dx * dx + dy * dy
    }
}
