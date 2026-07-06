import Foundation

enum StashAPIError: LocalizedError {
    case badKey
    case http(Int)

    var errorDescription: String? {
        switch self {
        case .badKey:
            return "That API key was rejected. Check it in the Windows app under Settings › Cloud Sync › API Keys."
        case .http(let code):
            return "The stash service returned an error (HTTP \(code))."
        }
    }
}

/// Thin client for the My Crafty Stash cloud API (Azure Static Web Apps).
/// Auth is the same mcs_ API key the Windows desktop app uses, sent as an
/// X-Api-Key header.
struct StashAPI {
    static let defaultBaseURL = URL(string: "https://wonderful-meadow-0a1a40110.7.azurestaticapps.net")!

    var baseURL: URL = StashAPI.defaultBaseURL
    var apiKey: String

    func whoami() async throws -> Whoami {
        try await get("/api/whoami")
    }

    func items(page: Int = 1, perPage: Int = 48, search: String? = nil, type: String? = nil) async throws -> ItemsPage {
        var query = [
            URLQueryItem(name: "page", value: String(page)),
            URLQueryItem(name: "perPage", value: String(perPage)),
        ]
        if let search, !search.isEmpty { query.append(URLQueryItem(name: "search", value: search)) }
        if let type, !type.isEmpty { query.append(URLQueryItem(name: "type", value: type)) }
        return try await get("/api/items", query: query)
    }

    private func get<T: Decodable>(_ path: String, query: [URLQueryItem] = []) async throws -> T {
        var comps = URLComponents(url: baseURL.appendingPathComponent(path), resolvingAgainstBaseURL: false)!
        if !query.isEmpty { comps.queryItems = query }

        var req = URLRequest(url: comps.url!)
        req.setValue(apiKey, forHTTPHeaderField: "X-Api-Key")

        let (data, response) = try await URLSession.shared.data(for: req)
        let status = (response as? HTTPURLResponse)?.statusCode ?? 0
        switch status {
        case 200:
            return try JSONDecoder().decode(T.self, from: data)
        case 401:
            throw StashAPIError.badKey
        default:
            throw StashAPIError.http(status)
        }
    }
}
