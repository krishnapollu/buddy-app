import Foundation
import CoreGraphics

final class CompanionPreferences {
    private let defaults: UserDefaults
    private let visibilityKey = "companionVisible"
    private let positionXKey = "companionX"
    private let positionYKey = "companionY"
    private let expressionKey = "companionExpression"
    private let skinKey = "companionSkin"
    private let notificationsKey = "localNotificationsEnabled"

    init(defaults: UserDefaults = .standard) {
        self.defaults = defaults
    }

    var expression: CreatureExpression {
        get { CreatureExpression(rawValue: defaults.string(forKey: expressionKey) ?? "") ?? .calm }
        set { defaults.set(newValue.rawValue, forKey: expressionKey) }
    }

    var skin: CreatureSkin {
        get { CreatureSkin(rawValue: defaults.string(forKey: skinKey) ?? "") ?? .mint }
        set { defaults.set(newValue.rawValue, forKey: skinKey) }
    }

    var notificationsEnabled: Bool {
        get { defaults.bool(forKey: notificationsKey) }
        set { defaults.set(newValue, forKey: notificationsKey) }
    }

    var isVisible: Bool {
        get { defaults.object(forKey: visibilityKey) as? Bool ?? true }
        set { defaults.set(newValue, forKey: visibilityKey) }
    }

    var position: CGPoint? {
        get {
            guard let x = defaults.object(forKey: positionXKey) as? Double,
                  let y = defaults.object(forKey: positionYKey) as? Double else { return nil }
            return CGPoint(x: x, y: y)
        }
        set {
            if let newValue {
                defaults.set(Double(newValue.x), forKey: positionXKey)
                defaults.set(Double(newValue.y), forKey: positionYKey)
            } else {
                defaults.removeObject(forKey: positionXKey)
                defaults.removeObject(forKey: positionYKey)
            }
        }
    }
}
