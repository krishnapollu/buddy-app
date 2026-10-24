import SwiftUI

@MainActor
final class CreatureAnimator: ObservableObject {
    @Published private(set) var isActive = false
    @Published private(set) var isBlinking = false
    @Published private(set) var gaze: CGFloat = 0
    @Published private(set) var isHovered = false
    @Published private(set) var isReacting = false
    @Published var expression: CreatureExpression = .calm
    @Published var skin: CreatureSkin = .mint

    private var blinkTask: Task<Void, Never>?
    private var gazeTask: Task<Void, Never>?
    private var reactionTask: Task<Void, Never>?

    var visibleExpression: CreatureExpression {
        isReacting ? .happy : (isHovered ? .curious : expression)
    }

    func setHovered(_ hovered: Bool) {
        guard isActive else { return }
        withAnimation(.easeInOut(duration: 0.2)) { isHovered = hovered }
    }

    func reactToClick() {
        guard isActive else { return }
        reactionTask?.cancel()
        withAnimation(.easeOut(duration: 0.16)) { isReacting = true }
        reactionTask = Task { [weak self] in
            try? await Task.sleep(for: .milliseconds(700))
            guard !Task.isCancelled else { return }
            withAnimation(.easeInOut(duration: 0.25)) { self?.isReacting = false }
        }
    }

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
        reactionTask?.cancel()
        blinkTask = nil
        gazeTask = nil
        reactionTask = nil
        isActive = false
        isBlinking = false
        gaze = 0
        isHovered = false
        isReacting = false
    }
}
