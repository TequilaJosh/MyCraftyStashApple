import SwiftUI

struct ContentView: View {
    @EnvironmentObject private var session: StashSession

    var body: some View {
        if session.isSignedIn {
            StashSplitView()
        } else {
            SignInView()
        }
    }
}

/// Main navigation: sidebar on Mac / iPad, mirroring the desktop app's
/// left-nav (Inventory now; Projects, Wishlist etc. come later phases).
private struct StashSplitView: View {
    enum SidebarSection: String, CaseIterable, Identifiable {
        case inventory = "Inventory"
        case settings = "Settings"

        var id: String { rawValue }

        var icon: String {
            switch self {
            case .inventory: return "square.grid.2x2"
            case .settings: return "gearshape"
            }
        }
    }

    @State private var selection: SidebarSection? = .inventory

    var body: some View {
        NavigationSplitView {
            List(SidebarSection.allCases, selection: $selection) { section in
                Label(section.rawValue, systemImage: section.icon)
                    .tag(section)
            }
            .navigationTitle("My Crafty Stash")
        } detail: {
            NavigationStack {
                switch selection ?? .inventory {
                case .inventory: InventoryView()
                case .settings: SettingsView()
                }
            }
        }
    }
}
