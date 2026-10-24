import SwiftUI

enum CreatureExpression: String, CaseIterable {
    case calm = "Calm"
    case happy = "Happy"
    case sleepy = "Sleepy"
    case curious = "Curious"

    static var selectable: [Self] { [.calm, .happy, .sleepy] }
}

enum CreatureSkin: String, CaseIterable {
    case mint = "Mint"
    case peach = "Peach"
    case lavender = "Lavender"

    var palette: CreaturePalette {
        switch self {
        case .mint:
            CreaturePalette(light: Color(red: 0.75, green: 0.91, blue: 0.88),
                            dark: Color(red: 0.47, green: 0.72, blue: 0.73),
                            outline: Color(red: 0.35, green: 0.59, blue: 0.62))
        case .peach:
            CreaturePalette(light: Color(red: 1.00, green: 0.88, blue: 0.76),
                            dark: Color(red: 0.94, green: 0.63, blue: 0.61),
                            outline: Color(red: 0.73, green: 0.45, blue: 0.48))
        case .lavender:
            CreaturePalette(light: Color(red: 0.88, green: 0.84, blue: 0.98),
                            dark: Color(red: 0.65, green: 0.62, blue: 0.85),
                            outline: Color(red: 0.48, green: 0.45, blue: 0.68))
        }
    }
}

struct CreaturePalette {
    let light: Color
    let dark: Color
    let outline: Color

    var ear: Color { dark.opacity(0.95) }
    var foot: Color { outline.opacity(0.9) }
}
