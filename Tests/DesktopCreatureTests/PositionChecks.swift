import AppKit

@main
enum PositionChecks {
    static func main() {
        let primary = NSRect(x: 0, y: 0, width: 1440, height: 900)
        let secondary = NSRect(x: 1440, y: 0, width: 1200, height: 800)
        let size = NSSize(width: 160, height: 180)

        assert(WindowPosition.restored(nil, size: size, screens: [primary], defaultScreen: primary)
               == NSPoint(x: 1256, y: 24))
        assert(WindowPosition.restored(NSPoint(x: 1800, y: 100), size: size,
                                       screens: [primary, secondary], defaultScreen: primary)
               == NSPoint(x: 1800, y: 100))
        assert(WindowPosition.restored(NSPoint(x: 1800, y: 100), size: size,
                                       screens: [primary], defaultScreen: primary)
               == NSPoint(x: 1280, y: 100))
        assert(WindowPosition.restored(NSPoint(x: -500, y: -500), size: size,
                                       screens: [primary], defaultScreen: primary)
               == .zero)
        print("Position checks passed")
    }
}
