import SwiftUI

@main
struct MyCraftyStashApp: App {
    @StateObject private var session = StashSession()

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(session)
        }
    }
}
