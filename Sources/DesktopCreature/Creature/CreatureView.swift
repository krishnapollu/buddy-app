import SwiftUI

struct CreatureView: View {
    @ObservedObject var animator: CreatureAnimator
    @Environment(\.accessibilityReduceMotion) private var reduceMotion
    @State private var breathing = false
    private var palette: CreaturePalette { animator.skin.palette }

    var body: some View {
        ZStack {
            Ellipse()
                .fill(.black.opacity(0.12))
                .frame(width: 104, height: 13)
                .blur(radius: 6)
                .position(x: 80, y: 159)

            creature
                .scaleEffect(animator.isReacting && !reduceMotion ? 1.055 : 1, anchor: .bottom)
                .scaleEffect(x: breathing && !reduceMotion ? 1.025 : 0.985,
                             y: breathing && !reduceMotion ? 0.985 : 1.015,
                             anchor: .bottom)
                .rotationEffect(.degrees(breathing && !reduceMotion ? 1.1 : -1.1), anchor: .bottom)
                .offset(y: breathing && !reduceMotion ? -2 : 0)
        }
        .frame(width: 160, height: 180)
        .accessibilityLabel("\(animator.skin.rawValue) desktop creature, \(animator.visibleExpression.rawValue) expression")
        .onAppear(perform: updateBreathing)
        .onChange(of: animator.isActive) { updateBreathing() }
        .onChange(of: reduceMotion) { updateBreathing() }
    }

    private var creature: some View {
        ZStack {
            Ellipse()
                .fill(palette.ear)
                .frame(width: 25, height: 37)
                .rotationEffect(.degrees(-22))
                .position(x: 43, y: 42)
            Ellipse()
                .fill(palette.ear)
                .frame(width: 25, height: 37)
                .rotationEffect(.degrees(22))
                .position(x: 117, y: 42)
            Ellipse()
                .fill(Color(red: 0.97, green: 0.75, blue: 0.76))
                .frame(width: 10, height: 22)
                .rotationEffect(.degrees(-22))
                .position(x: 43, y: 43)
            Ellipse()
                .fill(Color(red: 0.97, green: 0.75, blue: 0.76))
                .frame(width: 10, height: 22)
                .rotationEffect(.degrees(22))
                .position(x: 117, y: 43)

            Ellipse()
                .fill(palette.foot)
                .frame(width: 30, height: 14)
                .position(x: 50, y: 147)
            Ellipse()
                .fill(palette.foot)
                .frame(width: 30, height: 14)
                .position(x: 110, y: 147)

            Ellipse()
                .fill(LinearGradient(
                    colors: [palette.light, palette.dark],
                    startPoint: .topLeading, endPoint: .bottomTrailing
                ))
                .overlay {
                    Ellipse().stroke(palette.outline.opacity(0.7), lineWidth: 1.5)
                }
                .frame(width: 126, height: 119)
                .position(x: 80, y: 93)

            Ellipse()
                .fill(.white.opacity(0.23))
                .frame(width: 70, height: 25)
                .rotationEffect(.degrees(-25))
                .blur(radius: 9)
                .position(x: 56, y: 56)

            Ellipse()
                .fill(Color(red: 0.96, green: 0.72, blue: 0.75).opacity(0.65))
                .frame(width: 19, height: 10)
                .position(x: 44, y: 105)
            Ellipse()
                .fill(Color(red: 0.96, green: 0.72, blue: 0.75).opacity(0.65))
                .frame(width: 19, height: 10)
                .position(x: 116, y: 105)

            eye(at: 61)
            eye(at: 99)

            mouth
        }
        .frame(width: 160, height: 180)
    }

    private func eye(at x: CGFloat) -> some View {
        ZStack {
            Ellipse()
                .fill(Color(red: 0.17, green: 0.29, blue: 0.33))
                .frame(width: 12, height: 18)
            Circle()
                .fill(.white.opacity(0.95))
                .frame(width: 4.5, height: 4.5)
                .offset(x: -2, y: -4)
        }
        .scaleEffect(y: animator.isBlinking ? 0.07 : (animator.visibleExpression == .sleepy ? 0.42 : 1))
        .offset(x: animator.isHovered ? 0 : animator.gaze)
        .position(x: x, y: 91)
    }

    @ViewBuilder
    private var mouth: some View {
        if animator.visibleExpression == .curious {
            Ellipse()
                .stroke(Color(red: 0.22, green: 0.36, blue: 0.39), lineWidth: 2)
                .frame(width: 7, height: 9)
                .position(x: 80, y: 113)
        } else {
            Path { path in
                let width: CGFloat = animator.visibleExpression == .happy ? 11 : 6
                let depth: CGFloat = animator.visibleExpression == .happy ? 13 : 7
                path.move(to: CGPoint(x: 80 - width, y: 111))
                path.addQuadCurve(to: CGPoint(x: 80 + width, y: 111),
                                  control: CGPoint(x: 80, y: 111 + depth))
            }
            .stroke(Color(red: 0.22, green: 0.36, blue: 0.39),
                    style: StrokeStyle(lineWidth: 2.3, lineCap: .round))
        }
    }

    private func updateBreathing() {
        if animator.isActive && !reduceMotion {
            breathing = false
            withAnimation(.easeInOut(duration: 1.6).repeatForever(autoreverses: true)) {
                breathing = true
            }
        } else {
            withAnimation(.easeOut(duration: 0.2)) { breathing = false }
        }
    }
}
