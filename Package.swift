// swift-tools-version: 5.10
import PackageDescription

let package = Package(
    name: "DesktopCreature",
    platforms: [.macOS(.v14)],
    products: [.executable(name: "DesktopCreature", targets: ["DesktopCreature"])],
    targets: [.executableTarget(name: "DesktopCreature")]
)
