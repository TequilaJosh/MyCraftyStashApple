import SwiftUI

/// Read-only inventory browser: card grid + search, paged from /api/items.
struct InventoryView: View {
    @EnvironmentObject private var session: StashSession
    @State private var items: [StashItem] = []
    @State private var searchText = ""
    @State private var page = 1
    @State private var totalPages = 1
    @State private var loading = false
    @State private var errorMessage: String?

    private let columns = [GridItem(.adaptive(minimum: 160), spacing: 16)]

    var body: some View {
        ScrollView {
            LazyVGrid(columns: columns, spacing: 16) {
                ForEach(items) { item in
                    NavigationLink(value: item) {
                        ItemCard(item: item)
                    }
                    .buttonStyle(.plain)
                }
            }
            .padding()

            if page < totalPages {
                Button("Load more") {
                    Task { await load(page: page + 1) }
                }
                .buttonStyle(.bordered)
                .disabled(loading)
                .padding(.bottom, 24)
            }
        }
        .navigationTitle("Inventory")
        .navigationDestination(for: StashItem.self) { ItemDetailView(item: $0) }
        .searchable(text: $searchText, prompt: "Search your stash")
        .onSubmit(of: .search) { Task { await load(page: 1) } }
        .task { await load(page: 1) }
        .refreshable { await load(page: 1) }
        .overlay {
            if loading && items.isEmpty {
                ProgressView()
            } else if let errorMessage {
                ContentUnavailableView("Couldn't load your stash",
                                       systemImage: "wifi.exclamationmark",
                                       description: Text(errorMessage))
            } else if !loading && items.isEmpty {
                ContentUnavailableView("Nothing here yet",
                                       systemImage: "shippingbox",
                                       description: Text("Sync your stash from the Windows app, or adjust your search."))
            }
        }
    }

    private func load(page newPage: Int) async {
        guard let api = session.api else { return }
        loading = true
        defer { loading = false }
        errorMessage = nil
        do {
            let result = try await api.items(page: newPage, search: searchText)
            items = newPage == 1 ? result.items : items + result.items
            page = result.page
            totalPages = result.totalPages
        } catch {
            errorMessage = error.localizedDescription
        }
    }
}

private struct ItemCard: View {
    let item: StashItem

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            AsyncImage(url: item.thumbUrl.flatMap(URL.init)) { phase in
                switch phase {
                case .success(let image):
                    image.resizable().scaledToFit()
                default:
                    Image(systemName: "photo")
                        .font(.largeTitle)
                        .foregroundStyle(.tertiary)
                }
            }
            .frame(maxWidth: .infinity)
            .frame(height: 120)
            // Product shots are on white; a white well hides letterboxing.
            .background(.white, in: RoundedRectangle(cornerRadius: 8))

            Text(item.name)
                .font(.callout.weight(.semibold))
                .lineLimit(2, reservesSpace: true)

            Text(item.type ?? " ")
                .font(.caption)
                .foregroundStyle(.secondary)
        }
        .padding(12)
        .background(Color.gray.opacity(0.12), in: RoundedRectangle(cornerRadius: 12))
    }
}
