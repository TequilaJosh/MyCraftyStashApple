import Foundation
import SwiftUI

/// App-wide auth state: holds the API key (Keychain-backed) and who it
/// belongs to. Views watch this to decide sign-in vs. main UI.
@MainActor
final class StashSession: ObservableObject {
    @Published var apiKey: String? = KeychainStore.loadApiKey()
    @Published var firstName: String?

    var isSignedIn: Bool { apiKey != nil }

    var api: StashAPI? {
        guard let apiKey else { return nil }
        return StashAPI(apiKey: apiKey)
    }

    /// Validates the key against /api/whoami before storing it.
    func signIn(apiKey raw: String) async throws {
        let key = raw.trimmingCharacters(in: .whitespacesAndNewlines)
        let who = try await StashAPI(apiKey: key).whoami()
        KeychainStore.saveApiKey(key)
        apiKey = key
        firstName = who.firstName
    }

    func signOut() {
        KeychainStore.deleteApiKey()
        apiKey = nil
        firstName = nil
    }
}
