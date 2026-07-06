import SwiftUI

/// Read-only detail for one item: big photo + the fields the desktop shows.
struct ItemDetailView: View {
    let item: StashItem

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 20) {
                AsyncImage(url: (item.imageUrl ?? item.thumbUrl).flatMap(URL.init)) { phase in
                    switch phase {
                    case .success(let image):
                        image.resizable().scaledToFit()
                    default:
                        Image(systemName: "photo")
                            .font(.system(size: 48))
                            .foregroundStyle(.tertiary)
                            .frame(height: 200)
                    }
                }
                .frame(maxWidth: .infinity)
                .frame(maxHeight: 320)
                .background(.white, in: RoundedRectangle(cornerRadius: 12))

                Grid(alignment: .leading, horizontalSpacing: 24, verticalSpacing: 10) {
                    detailRow("Type", item.type)
                    detailRow("Subtype", item.subtype)
                    detailRow("Theme", item.theme)
                    detailRow("Item #", item.itemNumber)
                    detailRow("Price", item.price.map { String(format: "$%.2f", $0) })
                    detailRow("Purchased", item.datePurchased)
                    detailRow("From", item.purchasedFrom)
                    detailRow("Location", item.location)
                    detailRow("Stock", item.currentStock.map(String.init))
                    detailRow("Stencil layers", item.stencilLayers.map(String.init))
                }

                if item.isDiscontinued {
                    Label("Discontinued", systemImage: "exclamationmark.triangle")
                        .foregroundStyle(.orange)
                }

                if !item.sentiments.isEmpty {
                    VStack(alignment: .leading, spacing: 6) {
                        Text("Sentiments").font(.headline)
                        ForEach(item.sentiments, id: \.self) { sentiment in
                            Text("•  \(sentiment)")
                        }
                    }
                }

                if let notes = item.notes, !notes.isEmpty {
                    VStack(alignment: .leading, spacing: 6) {
                        Text("Notes").font(.headline)
                        Text(notes)
                    }
                }
            }
            .padding()
        }
        .navigationTitle(item.name)
    }

    @ViewBuilder
    private func detailRow(_ label: String, _ value: String?) -> some View {
        if let value, !value.isEmpty {
            GridRow {
                Text(label).foregroundStyle(.secondary)
                Text(value)
            }
        }
    }
}
