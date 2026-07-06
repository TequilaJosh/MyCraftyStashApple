import SwiftUI

struct SettingsView: View {
    @EnvironmentObject private var session: StashSession

    var body: some View {
        Form {
            Section("Account") {
                LabeledContent("Signed in as", value: session.firstName ?? "Connected")
                Button("Sign out", role: .destructive) {
                    session.signOut()
                }
            }
            Section("About") {
                LabeledContent("Version", value: "0.1.0")
                Text("Read-only companion to the My Crafty Stash Windows app. Your stash syncs from the desktop to the cloud; this app browses it.")
                    .font(.callout)
                    .foregroundStyle(.secondary)
            }
        }
        .formStyle(.grouped)
        .navigationTitle("Settings")
    }
}
