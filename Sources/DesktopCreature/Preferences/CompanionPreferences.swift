import Foundation
import CoreGraphics

final class CompanionPreferences {
    private let defaults = UserDefaults.standard
    private let visibilityKey = "companionVisible"
    private let positionXKey = "companionX"
    private let positionYKey = "companionY"

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
