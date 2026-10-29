import Foundation

enum NavigationPolicy {
    static let home = URL(string: "https://torsepan.liara.run/")!
    static func isPortal(_ url: URL) -> Bool {
        url.scheme?.lowercased() == "https" &&
        url.host?.lowercased() == home.host &&
        (url.port == nil || url.port == 443) && url.user == nil && url.password == nil
    }
    static func mayOpenExternally(_ url: URL) -> Bool {
        ["https", "mailto", "tel"].contains(url.scheme?.lowercased() ?? "")
    }
}
