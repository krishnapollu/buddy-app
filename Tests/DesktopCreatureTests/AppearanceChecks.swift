import Foundation

@main
enum AppearanceChecks {
    static func main() {
        let suite = "DesktopCreature.AppearanceChecks.\(UUID().uuidString)"
        guard let defaults = UserDefaults(suiteName: suite) else { fatalError("Could not create test defaults") }
        defer { defaults.removePersistentDomain(forName: suite) }

        let preferences = CompanionPreferences(defaults: defaults)
        assert(preferences.expression == .calm)
        assert(preferences.skin == .mint)
        preferences.expression = .sleepy
        preferences.skin = .lavender

        let restored = CompanionPreferences(defaults: defaults)
        assert(restored.expression == .sleepy)
        assert(restored.skin == .lavender)
        defaults.set("Unknown", forKey: "companionExpression")
        defaults.set("Unknown", forKey: "companionSkin")
        assert(restored.expression == .calm)
        assert(restored.skin == .mint)
        assert(!restored.notificationsEnabled)
        restored.notificationsEnabled = true
        assert(preferences.notificationsEnabled)
        print("Appearance checks passed")
    }
}
