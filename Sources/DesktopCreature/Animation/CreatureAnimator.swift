import SwiftUI

@MainActor
final class CreatureAnimator: ObservableObject {
    @Published private(set) var isActive = false
    @Published private(set) var isBlinking = false
    @Published private(set) var gaze: CGFloat = 0

    private var blinkTask: Task<Void, Never>?
    private var gazeTask: Task<Void, Never>?

    func start() {
        guard !isActive else { return }
        isActive = true
        blinkTask = Task { [weak self] in
            while !Task.isCancelled {
                try? await Task.sleep(for: .seconds(Double.random(in: 3...7)))
                guard !Task.isCancelled else { break }
                withAnimation(.easeInOut(duration: 0.10)) { self?.isBlinking = true }
                try? await Task.sleep(for: .milliseconds(140))
                guard !Task.isCancelled else { break }
                withAnimation(.easeInOut(duration: 0.13)) { self?.isBlinking = false }
            }
        }
        gazeTask = Task { [weak self] in
            while !Task.isCancelled {
                try? await Task.sleep(for: .seconds(Double.random(in: 5...12)))
                guard !Task.isCancelled else { break }
                withAnimation(.easeInOut(duration: 0.35)) { self?.gaze = Bool.random() ? -2.5 : 2.5 }
                try? await Task.sleep(for: .seconds(Double.random(in: 0.8...1.6)))
                guard !Task.isCancelled else { break }
                withAnimation(.easeInOut(duration: 0.4)) { self?.gaze = 0 }
            }
        }
    }

    func stop() {
        blinkTask?.cancel()
        gazeTask?.cancel()
        blinkTask = nil
        gazeTask = nil
        isActive = false
        isBlinking = false
        gaze = 0
    }
}
