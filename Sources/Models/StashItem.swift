import Foundation

/// One inventory item as returned by GET /api/items.
///
/// Mirrors the desktop app's cloud_items shape. Dates arrive as ISO-8601
/// strings and stay strings here until something needs to do math on them
/// (.NET emits fractional seconds that Foundation's .iso8601 strategy
/// rejects, so decoding them as Date is a trap).
struct StashItem: Codable, Identifiable, Hashable {
    let id: UUID
    let localId: Int
    let name: String
    let type: String?
    let subtype: String?
    let theme: String?
    let sentiments: [String]
    let itemNumber: String?
    let price: Double?
    let datePurchased: String?
    let isDiscontinued: Bool
    let stencilLayers: Int?
    let packSize: Int?
    let currentStock: Int?
    let purchasedFrom: String?
    let location: String?
    let notes: String?
    /// 15-minute SAS URLs minted per response; nil when no photo was uploaded.
    let imageUrl: String?
    let thumbUrl: String?
    let uploadedAt: String
    let updatedAt: String
}

/// Envelope for the paginated /api/items response.
struct ItemsPage: Codable {
    let items: [StashItem]
    let page: Int
    let perPage: Int
    let total: Int
    let totalPages: Int
}

/// Subset of /api/whoami we care about (used to validate an API key).
struct Whoami: Codable {
    let userId: String
    let userDetails: String?
    let firstName: String?
}
