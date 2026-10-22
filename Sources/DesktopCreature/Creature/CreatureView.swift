import SwiftUI

struct CreatureView: View {
    @ObservedObject var animator: CreatureAnimator
    @Environment(\.accessibilityReduceMotion) private var reduceMotion
    @State private var breathing = false

    var body: some View {
        ZStack {
            Ellipse()
                .fill(.black.opacity(0.12))
                .frame(width: 104, height: 13)
                .blur(radius: 6)
                .position(x: 80, y: 159)

            creature
                .scaleEffect(x: breathing && !reduceMotion ? 1.025 : 0.985,
                             y: breathing && !reduceMotion ? 0.985 : 1.015,
                             anchor: .bottom)
                .rotationEffect(.degrees(breathing && !reduceMotion ? 1.1 : -1.1), anchor: .bottom)
                .offset(y: breathing && !reduceMotion ? -2 : 0)
        }
        .frame(width: 160, height: 180)
        .accessibilityLabel("Animated desktop creature")
        .onAppear(perform: updateBreathing)
        .onChange(of: animator.isActive) { updateBreathing() }
        .onChange(of: reduceMotion) { updateBreathing() }
    }

    private var creature: some View {
        ZStack {
            Ellipse()
                .fill(Color(red: 0.48, green: 0.70, blue: 0.72))
                .frame(width: 25, height: 37)
                .rotationEffect(.degrees(-22))
                .position(x: 43, y: 42)
            Ellipse()
                .fill(Color(red: 0.48, green: 0.70, blue: 0.72))
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
                .fill(Color(red: 0.42, green: 0.64, blue: 0.67))
                .frame(width: 30, height: 14)
                .position(x: 50, y: 147)
            Ellipse()
                .fill(Color(red: 0.42, green: 0.64, blue: 0.67))
                .frame(width: 30, height: 14)
                .position(x: 110, y: 147)

            Ellipse()
                .fill(LinearGradient(
                    colors: [Color(red: 0.75, green: 0.91, blue: 0.88), Color(red: 0.47, green: 0.72, blue: 0.73)],
                    startPoint: .topLeading, endPoint: .bottomTrailing
                ))
                .overlay {
                    Ellipse().stroke(Color(red: 0.35, green: 0.59, blue: 0.62).opacity(0.7), lineWidth: 1.5)
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

            Path { path in
                path.move(to: CGPoint(x: 74, y: 111))
                path.addQuadCurve(to: CGPoint(x: 86, y: 111), control: CGPoint(x: 80, y: 119))
            }
            .stroke(Color(red: 0.22, green: 0.36, blue: 0.39), style: StrokeStyle(lineWidth: 2.3, lineCap: .round))
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
        .scaleEffect(y: animator.isBlinking ? 0.07 : 1)
        .offset(x: animator.gaze)
        .position(x: x, y: 91)
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
