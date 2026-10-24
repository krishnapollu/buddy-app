import Foundation

@main
enum IntegrationChecks {
    static func main() throws {
        let directory = FileManager.default.temporaryDirectory.appendingPathComponent("buddy-integration-\(UUID().uuidString)")
        defer { try? FileManager.default.removeItem(at: directory) }
        let inbox = IntegrationInbox(directory: directory)
        let codex = #"{"type":"agent-turn-complete","last-assistant-message":"private text"}"#

        HookReceiver.receive(arguments: ["buddy", "--hook", "codex", codex], inbox: inbox)
        assert(inbox.drain().isEmpty)

        try inbox.setEnabled(true, for: .codex)
        HookReceiver.receive(arguments: ["buddy", "--hook", "codex", codex], inbox: inbox)
        let events = inbox.drain()
        assert(events.count == 1 && events[0].title == "Codex finished")
        let stored = String(data: try JSONEncoder().encode(events[0]), encoding: .utf8)!
        assert(!stored.contains("private text"))
        assert(inbox.drain().isEmpty)
        assert(HookReceiver.event(source: .codex, data: Data(#"{"type":"unknown"}"#.utf8)) == nil)

        try inbox.setEnabled(true, for: .claude)
        let claude = Data(#"{"hook_event_name":"Notification","message":"secret prompt"}"#.utf8)
        let notification = HookReceiver.event(source: .claude, data: claude)
        assert(notification?.title == "Claude Code needs attention")
        try inbox.enqueue(notification!)
        assert(inbox.drain().map(\.title) == ["Claude Code needs attention"])

        try inbox.setEnabled(false, for: .codex)
        assert(!inbox.isEnabled(.codex))
        assert(inbox.isEnabled(.claude))
        print("Integration checks passed")
    }
}
