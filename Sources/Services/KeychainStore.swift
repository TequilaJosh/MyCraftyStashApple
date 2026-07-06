import Foundation
import Security

/// Minimal Keychain wrapper for the one secret we hold: the mcs_ API key.
/// (The Windows app protects the same key with DPAPI; Keychain is the
/// platform equivalent here.)
enum KeychainStore {
    private static let service = "com.silosoftwarecreations.mycraftystash"
    private static let account = "api-key"

    private static var baseQuery: [String: Any] {
        [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: account,
        ]
    }

    static func saveApiKey(_ key: String) {
        SecItemDelete(baseQuery as CFDictionary)
        var add = baseQuery
        add[kSecValueData as String] = Data(key.utf8)
        SecItemAdd(add as CFDictionary, nil)
    }

    static func loadApiKey() -> String? {
        var query = baseQuery
        query[kSecReturnData as String] = true
        query[kSecMatchLimit as String] = kSecMatchLimitOne
        var out: CFTypeRef?
        guard SecItemCopyMatching(query as CFDictionary, &out) == errSecSuccess,
              let data = out as? Data else { return nil }
        return String(data: data, encoding: .utf8)
    }

    static func deleteApiKey() {
        SecItemDelete(baseQuery as CFDictionary)
    }
}
