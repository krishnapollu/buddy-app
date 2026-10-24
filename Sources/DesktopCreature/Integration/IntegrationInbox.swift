import Foundation

enum AgentSource: String, CaseIterable, Codable {
    case codex
    case claude

    var name: String { self == .codex ? "Codex CLI" : "Claude Code" }
}

struct CompanionEvent: Codable {
    let source: AgentSource
    let title: String
    let detail: String
    let createdAt: Date
}

enum HookReceiver {
    static func event(source: AgentSource, data: Data) -> CompanionEvent? {
        guard data.count <= 65_536,
              let payload = try? JSONSerialization.jsonObject(with: data) as? [String: Any] else { return nil }

        switch source {
        case .codex where payload["type"] as? String == "agent-turn-complete":
            return CompanionEvent(source: source, title: "Codex finished", detail: "A turn is complete.", createdAt: Date())
        case .claude:
            switch payload["hook_event_name"] as? String {
            case "Stop":
                return CompanionEvent(source: source, title: "Claude Code finished", detail: "A response is complete.", createdAt: Date())
            case "Notification":
                return CompanionEvent(source: source, title: "Claude Code needs attention", detail: "Check your Claude Code session.", createdAt: Date())
            case "StopFailure":
                return CompanionEvent(source: source, title: "Claude Code stopped", detail: "Check your Claude Code session.", createdAt: Date())
            default:
                return nil
            }
        default:
            return nil
        }
    }

    static func receive(arguments: [String], inbox: IntegrationInbox = IntegrationInbox()) {
        guard arguments.count >= 3, arguments[1] == "--hook",
              let source = AgentSource(rawValue: arguments[2]) else { return }
        let data: Data
        if source == .codex {
            guard arguments.count >= 4 else { return }
            data = Data(arguments[3].utf8)
        } else {
            data = (try? FileHandle.standardInput.read(upToCount: 65_537)) ?? Data()
        }
        guard inbox.isEnabled(source), let event = event(source: source, data: data) else { return }
        try? inbox.enqueue(event)
    }
}

struct IntegrationInbox {
    let directory: URL

    init(directory: URL? = nil) {
        let support = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask).first
            ?? FileManager.default.homeDirectoryForCurrentUser.appendingPathComponent("Library/Application Support")
        self.directory = directory ?? support.appendingPathComponent("DesktopCreature/Events", isDirectory: true)
    }

    func isEnabled(_ source: AgentSource) -> Bool {
        FileManager.default.fileExists(atPath: marker(for: source).path)
    }

    func setEnabled(_ enabled: Bool, for source: AgentSource) throws {
        if enabled {
            try prepareDirectory()
            try Data().write(to: marker(for: source), options: .atomic)
        } else {
            try? FileManager.default.removeItem(at: marker(for: source))
            for url in pendingFiles() where url.lastPathComponent.hasPrefix("\(source.rawValue)-") {
                try? FileManager.default.removeItem(at: url)
            }
        }
    }

    func enqueue(_ event: CompanionEvent) throws {
        guard isEnabled(event.source) else { return }
        try prepareDirectory()
        let name = "\(event.source.rawValue)-\(UUID().uuidString).json"
        try JSONEncoder().encode(event).write(to: directory.appendingPathComponent(name), options: .atomic)
    }

    func drain() -> [CompanionEvent] {
        pendingFiles().prefix(20).compactMap { url in
            defer { try? FileManager.default.removeItem(at: url) }
            guard let size = try? url.resourceValues(forKeys: [.fileSizeKey]).fileSize, size <= 4_096,
                  let data = try? Data(contentsOf: url),
                  let event = try? JSONDecoder().decode(CompanionEvent.self, from: data),
                  isEnabled(event.source),
                  abs(event.createdAt.timeIntervalSinceNow) < 45 else { return nil }
            return event
        }
    }

    private func marker(for source: AgentSource) -> URL {
        directory.appendingPathComponent("\(source.rawValue).enabled")
    }

    private func prepareDirectory() throws {
        try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true,
                                                attributes: [.posixPermissions: 0o700])
        try FileManager.default.setAttributes([.posixPermissions: 0o700], ofItemAtPath: directory.path)
    }

    private func pendingFiles() -> [URL] {
        ((try? FileManager.default.contentsOfDirectory(at: directory, includingPropertiesForKeys: nil)) ?? [])
            .filter { $0.pathExtension == "json" }
            .sorted { $0.lastPathComponent < $1.lastPathComponent }
    }
}
