import SwiftUI

@main
struct TorsepanApp: App {
    var body: some Scene {
        WindowGroup { PortalView().ignoresSafeArea(edges: .bottom) }
    }
}

struct PortalView: UIViewControllerRepresentable {
    func makeUIViewController(context: Context) -> PortalController { PortalController() }
    func updateUIViewController(_ controller: PortalController, context: Context) {}
}
