import Foundation
import UserNotifications

final class LocalNotifier: NSObject, UNUserNotificationCenterDelegate {
    private let center = UNUserNotificationCenter.current()

    func start() {
        center.delegate = self
    }

    func requestPermission(_ completion: @escaping (Bool) -> Void) {
        center.requestAuthorization(options: [.alert]) { granted, _ in
            DispatchQueue.main.async { completion(granted) }
        }
    }

    func send(title: String, detail: String) {
        center.getNotificationSettings { [center] settings in
            guard settings.authorizationStatus == .authorized || settings.authorizationStatus == .provisional else { return }
            let content = UNMutableNotificationContent()
            content.title = title
            content.body = detail
            center.add(UNNotificationRequest(identifier: UUID().uuidString, content: content, trigger: nil))
        }
    }

    func userNotificationCenter(_ center: UNUserNotificationCenter, willPresent notification: UNNotification,
                                withCompletionHandler completionHandler: @escaping (UNNotificationPresentationOptions) -> Void) {
        completionHandler([.banner, .list])
    }
}
