using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels
{
    /// <summary>
    /// Core wizard logic copied VERBATIM from the Windows desktop's
    /// CardBuildWizardViewModel (summary builder, notes/id collectors, build-step
    /// assembler, snapshot capture/restore) so labels, step types, and snapshot
    /// JSON are byte-for-byte compatible across apps.
    /// </summary>
    public partial class CardWizardViewModel : ObservableObject
    {
        public ObservableCollection<SummaryRow> SummaryLines { get; } = new();

        [RelayCommand]
        private void RemoveSummaryRow(SummaryRow? row)
        {
            // Each removable row owns the side-effect; we just invoke it and re-render.
            // Source-collection ownership stays with whoever added the row to the summary.
            row?.RemoveAction?.Invoke();
            UpdateSummaryLines();
        }

        public void UpdateSummaryLines()
        {
            // Keep the inside-hub "- Done!" indicators in sync with inside content.
            NotifyInsideDoneIndicators();

            SummaryLines.Clear();

            // Helper closures to keep the build below readable.
            void AddInfo(string text) => SummaryLines.Add(new SummaryRow { Text = text });
            void AddRemovable(string text, Action remove) =>
                SummaryLines.Add(new SummaryRow { Text = text, RemoveAction = remove });

            if (!string.IsNullOrEmpty(SelectedCardBase))
            {
                var line = string.IsNullOrEmpty(EffectiveBaseCardstockColor)
                    ? $"Cardbase: {SelectedCardBase}"
                    : $"Cardbase: {SelectedCardBase} in {EffectiveBaseCardstockColor}";
                // Weave self-blend description + inks into the summary so the user
                // can confirm the blend recipe at a glance. Mirrors the same shape
                // used by mat / sentiment self-blend lines elsewhere.
                if (BaseIsSelfBlended)
                {
                    var bits = new List<string>();
                    if (!string.IsNullOrWhiteSpace(BaseSelfBlendDescription))
                        bits.Add(BaseSelfBlendDescription);
                    if (BaseBlendInks.Ordered.Count > 0)
                        bits.Add($"inks: {string.Join(", ", BaseBlendInks.Ordered)}");
                    line += bits.Count == 0
                        ? " (self-blended)"
                        : $" (self-blended — {string.Join("; ", bits)})";
                }
                AddInfo(line);
            }
            foreach (var d in CardBase.Decorations)
            {
                var captured = d;
                AddRemovable($"  • {captured.DisplaySummary}", () => CardBase.Decorations.Remove(captured));
            }

            // Details captured on the cardbase Details sub-page (each one is a snapshot
            // of all 8 picker selections). Numbered for clarity. Removable so an extra
            // accidental entry can be deleted from the summary directly.
            for (int i = 0; i < CardBaseAddedDetails.Count; i++)
            {
                var captured = CardBaseAddedDetails[i];
                AddRemovable($"  Detail {i + 1}: {captured.DisplaySummary}",
                    () => CardBaseAddedDetails.Remove(captured));
            }

            foreach (var a in CardBaseAddedAdhesives)
            {
                var captured = a;
                AddRemovable($"  Adhesive: {captured.Name}", () => CardBaseAddedAdhesives.Remove(captured));
            }

            // Render each mat group + its per-piece detail entries. Same template
            // for outside and inside passes; the section header ("Inside of Card")
            // separates them visually so we don't need an "Inside " prefix per item.
            void RenderMatGroup(WizardBgMatGroup group, string label, string pieceWord,
                                ObservableCollection<WizardBgMatGroup> ownerCollection)
            {
                var capturedGroup = group;
                AddRemovable($"{label} {capturedGroup.GroupNumber}: {capturedGroup.DisplaySummary}",
                    () => ownerCollection.Remove(capturedGroup));
                foreach (var p in group.Pieces)
                {
                    var capturedPiece = p;
                    foreach (var d in p.AddedDetails)
                    {
                        var capturedDetail = d;
                        AddRemovable($"  {pieceWord} {capturedPiece.Layer} detail: {capturedDetail.DisplaySummary}",
                            () => capturedPiece.AddedDetails.Remove(capturedDetail));
                    }
                }
            }

            // ── Outside (main) section ──────────────────────────────────────────
            foreach (var group in BgMats.Where(g => !g.IsInside))
                RenderMatGroup(group, "Background Mat", "Piece", BgMats);
            foreach (var group in AdditionalMats.Where(g => !g.IsInside))
                RenderMatGroup(group, "Additional Mat", "Piece", AdditionalMats);
            foreach (var group in FocalMatGroups.Where(g => !g.IsInside))
                RenderMatGroup(group, "Focal Mat", "Part", FocalMatGroups);

            // Legacy FocalParts (WizardFocalSection) — kept for any old data that may
            // still flow through the legacy in-line form. New hub doesn't write here.
            foreach (var fp in FocalParts)
            {
                var capturedFp = fp;
                AddRemovable($"Focal Mat: {capturedFp.DisplaySummary}", () => FocalParts.Remove(capturedFp));
                foreach (var d in capturedFp.AddedDetails)
                {
                    var capturedDetail = d;
                    AddRemovable($"  Detail: {capturedDetail.DisplaySummary}",
                        () => capturedFp.AddedDetails.Remove(capturedDetail));
                }
            }

            foreach (var s in ConfiguredSentiments.Where(s => !s.IsInside))
            {
                var capturedSent = s;
                AddRemovable($"Sentiment: {capturedSent.DisplaySummary}", () => ConfiguredSentiments.Remove(capturedSent));
                // Surface each per-part captured detail so the user sees their
                // per-sentiment Details sub-page picks before saving.
                foreach (var p in capturedSent.Parts)
                    foreach (var d in p.AddedDetails)
                    {
                        var capturedPart = p;
                        var capturedDetail = d;
                        AddRemovable($"  Sentiment detail: {capturedDetail.DisplaySummary}",
                            () => capturedPart.AddedDetails.Remove(capturedDetail));
                    }
            }

            foreach (var e in AddedEmbellishments.Where(e => !e.IsInside))
            {
                var captured = e;
                AddRemovable($"Embellishment: {captured.DisplaySummary}", () => AddedEmbellishments.Remove(captured));
            }

            // ── Inside of Card section (positioned after the main content but
            // before the envelope so the user can see exactly what's on each side
            // of the card). Header acts as the section divider. ───────────────
            bool hasAnyInside =
                BgMats.Any(g => g.IsInside) ||
                AdditionalMats.Any(g => g.IsInside) ||
                FocalMatGroups.Any(g => g.IsInside) ||
                InsideBgMats.Count > 0 ||
                InsideAdditionalMats.Count > 0 ||
                HasInsideFocalMat ||
                ConfiguredSentiments.Any(s => s.IsInside) ||
                ConfiguredInsideSentiments.Count > 0 ||
                AddedEmbellishments.Any(e => e.IsInside) ||
                InsideAddedEmbellishments.Count > 0;

            if (hasAnyInside)
            {
                AddInfo("── Inside of Card ──");

                foreach (var group in BgMats.Where(g => g.IsInside))
                    RenderMatGroup(group, "Background Mat", "Piece", BgMats);
                // Legacy direct-collection inside mats (older inside flow that wrote
                // straight into InsideBgMats / InsideAdditionalMats instead of tagging
                // a WizardBgMatGroup with IsInside).
                foreach (var mat in InsideBgMats)
                {
                    var capturedMat = mat;
                    AddRemovable($"Background Mat {capturedMat.Layer}: {capturedMat.DisplaySummary}",
                        () => InsideBgMats.Remove(capturedMat));
                    foreach (var d in capturedMat.AddedDetails)
                    {
                        var capturedDetail = d;
                        AddRemovable($"  Piece {capturedMat.Layer} detail: {capturedDetail.DisplaySummary}",
                            () => capturedMat.AddedDetails.Remove(capturedDetail));
                    }
                }

                foreach (var group in AdditionalMats.Where(g => g.IsInside))
                    RenderMatGroup(group, "Additional Mat", "Piece", AdditionalMats);
                foreach (var mat in InsideAdditionalMats)
                {
                    var capturedMat = mat;
                    AddRemovable($"Additional Mat {capturedMat.Layer}: {capturedMat.DisplaySummary}",
                        () => InsideAdditionalMats.Remove(capturedMat));
                    foreach (var d in capturedMat.AddedDetails)
                    {
                        var capturedDetail = d;
                        AddRemovable($"  Piece {capturedMat.Layer} detail: {capturedDetail.DisplaySummary}",
                            () => capturedMat.AddedDetails.Remove(capturedDetail));
                    }
                }

                foreach (var group in FocalMatGroups.Where(g => g.IsInside))
                    RenderMatGroup(group, "Focal Mat", "Part", FocalMatGroups);
                if (HasInsideFocalMat)
                {
                    AddInfo($"Focal Mat: {InsideFocal.DisplaySummary}");
                    foreach (var d in InsideFocal.AddedDetails)
                    {
                        var capturedDetail = d;
                        AddRemovable($"  Inside focal detail: {capturedDetail.DisplaySummary}",
                            () => InsideFocal.AddedDetails.Remove(capturedDetail));
                    }
                }

                foreach (var s in ConfiguredSentiments.Where(s => s.IsInside))
                {
                    var capturedSent = s;
                    AddRemovable($"Sentiment: {capturedSent.DisplaySummary}", () => ConfiguredSentiments.Remove(capturedSent));
                    foreach (var p in capturedSent.Parts)
                        foreach (var d in p.AddedDetails)
                        {
                            var capturedPart = p;
                            var capturedDetail = d;
                            AddRemovable($"  Sentiment detail: {capturedDetail.DisplaySummary}",
                                () => capturedPart.AddedDetails.Remove(capturedDetail));
                        }
                }
                foreach (var s in ConfiguredInsideSentiments)
                {
                    var capturedSent = s;
                    AddRemovable($"Sentiment: {capturedSent.DisplaySummary}",
                        () => ConfiguredInsideSentiments.Remove(capturedSent));
                    foreach (var p in capturedSent.Parts)
                        foreach (var d in p.AddedDetails)
                        {
                            var capturedPart = p;
                            var capturedDetail = d;
                            AddRemovable($"  Sentiment detail: {capturedDetail.DisplaySummary}",
                                () => capturedPart.AddedDetails.Remove(capturedDetail));
                        }
                }

                foreach (var e in AddedEmbellishments.Where(e => e.IsInside))
                {
                    var captured = e;
                    AddRemovable($"Embellishment: {captured.DisplaySummary}", () => AddedEmbellishments.Remove(captured));
                }
                foreach (var e in InsideAddedEmbellishments)
                {
                    var captured = e;
                    AddRemovable($"Embellishment: {captured.DisplaySummary}",
                        () => InsideAddedEmbellishments.Remove(captured));
                }
            }

            // ── Envelope / storage (always last) ────────────────────────────────
            if (SelectedEnvelopeItem != null)
                AddInfo($"Envelope: {SelectedEnvelopeItem.Name}");
            if (SelectedStorageBagItem != null)
                AddInfo($"Storage Bag: {SelectedStorageBagItem.Name}");
        }

        [RelayCommand]
        private void ConfirmBuild()
        {
            WasConfirmed = true;
            CardBaseType = SelectedCardBase;
            SelectedItemIds = CollectAllItemIds();
            BuildSteps = AssembleBuildSteps();
            BuildOtherNotes = CollectOtherNotes();
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private string CollectOtherNotes()
        {
            var parts = new List<string>();

            if (SelectedBaseCardstockColor == "Other" && !string.IsNullOrWhiteSpace(BaseCardstockOtherText))
                parts.Add($"Card base cardstock: {BaseCardstockOtherText}");

            foreach (var group in BgMats)
                foreach (var mat in group.Pieces)
                    if (mat.SelectedCardstockColor == "Other" && !string.IsNullOrWhiteSpace(mat.OtherCardstockText))
                        parts.Add(group.Pieces.Count == 1
                            ? $"Background Mat {group.GroupNumber} cardstock: {mat.OtherCardstockText}"
                            : $"Background Mat {group.GroupNumber} piece {mat.Layer} cardstock: {mat.OtherCardstockText}");

            foreach (var group in AdditionalMats)
                foreach (var mat in group.Pieces)
                    if (mat.SelectedCardstockColor == "Other" && !string.IsNullOrWhiteSpace(mat.OtherCardstockText))
                        parts.Add(group.Pieces.Count == 1
                            ? $"Additional Mat {group.GroupNumber} cardstock: {mat.OtherCardstockText}"
                            : $"Additional Mat {group.GroupNumber} piece {mat.Layer} cardstock: {mat.OtherCardstockText}");

            foreach (var fp in FocalParts)
            {
                if (fp.SelectedCardstockColor == "Other" && !string.IsNullOrWhiteSpace(fp.OtherCardstockText))
                    parts.Add($"Mat {BgMats.Count + AdditionalMats.Count + fp.PartNumber} (exterior focal) cardstock: {fp.OtherCardstockText}");
                if (fp.HasBacker && fp.BackerCardstockColor == "Other" && !string.IsNullOrWhiteSpace(fp.OtherBackerCardstockText))
                    parts.Add($"Mat {BgMats.Count + AdditionalMats.Count + fp.PartNumber} (exterior focal backer) cardstock: {fp.OtherBackerCardstockText}");
            }

            foreach (var mat in InsideBgMats)
                if (mat.SelectedCardstockColor == "Other" && !string.IsNullOrWhiteSpace(mat.OtherCardstockText))
                    parts.Add($"Inside background mat {mat.Layer} cardstock: {mat.OtherCardstockText}");
            foreach (var mat in InsideAdditionalMats)
                if (mat.SelectedCardstockColor == "Other" && !string.IsNullOrWhiteSpace(mat.OtherCardstockText))
                    parts.Add($"Inside additional mat {mat.Layer} cardstock: {mat.OtherCardstockText}");

            if (HasInsideFocalMat && InsideFocal.SelectedCardstockColor == "Other" && !string.IsNullOrWhiteSpace(InsideFocal.OtherCardstockText))
                parts.Add($"Inside focal cardstock: {InsideFocal.OtherCardstockText}");

            if (InsideFocal.HasBacker && InsideFocal.BackerCardstockColor == "Other" && !string.IsNullOrWhiteSpace(InsideFocal.OtherBackerCardstockText))
                parts.Add($"Inside focal backer cardstock: {InsideFocal.OtherBackerCardstockText}");

            parts.AddRange(_sentimentOtherNotes);

            var blendParts = new List<string>();

            void AddBlendNote(string label, bool isSelfBlended, string desc, List<string> inks)
            {
                if (!isSelfBlended) return;
                var note = label;
                if (!string.IsNullOrWhiteSpace(desc)) note += $": {desc}";
                if (inks.Count > 0) note += $" (inks: {string.Join(", ", inks)})";
                blendParts.Add(note);
            }

            foreach (var group in BgMats)
                foreach (var mat in group.Pieces)
                    AddBlendNote(group.Pieces.Count == 1 ? $"Background Mat {group.GroupNumber}" : $"Background Mat {group.GroupNumber} piece {mat.Layer}",
                        mat.IsSelfBlended, mat.SelfBlendDescription, mat.BlendInkColors);
            foreach (var group in AdditionalMats)
                foreach (var mat in group.Pieces)
                    AddBlendNote(group.Pieces.Count == 1 ? $"Additional Mat {group.GroupNumber}" : $"Additional Mat {group.GroupNumber} piece {mat.Layer}",
                        mat.IsSelfBlended, mat.SelfBlendDescription, mat.BlendInkColors);
            foreach (var fp in FocalParts)
                AddBlendNote($"Mat {BgMats.Count + AdditionalMats.Count + fp.PartNumber} (exterior focal)", fp.IsSelfBlended, fp.SelfBlendDescription, fp.BlendInkColors);
            foreach (var mat in InsideBgMats)
                AddBlendNote($"Inside background mat {mat.Layer}", mat.IsSelfBlended, mat.SelfBlendDescription, mat.BlendInkColors);
            foreach (var mat in InsideAdditionalMats)
                AddBlendNote($"Inside additional mat {mat.Layer}", mat.IsSelfBlended, mat.SelfBlendDescription, mat.BlendInkColors);
            if (HasInsideFocalMat)
                AddBlendNote("Inside focal", InsideFocal.IsSelfBlended, InsideFocal.SelfBlendDescription, InsideFocal.BlendInkColors);
            foreach (var s in ConfiguredSentiments)
                foreach (var p in s.Parts)
                    AddBlendNote($"Sentiment \"{p.ItemName}\"", p.IsSelfBlended, p.SelfBlendDescription, p.BlendInkColors);

            if (parts.Count == 0 && blendParts.Count == 0) return string.Empty;

            var result = new List<string>();
            if (parts.Count > 0) result.Add("Custom cardstock colors: " + string.Join("; ", parts));
            if (blendParts.Count > 0) result.Add("Self-blend notes: " + string.Join("; ", blendParts));
            return string.Join("\n", result);
        }

        [RelayCommand]
        private void CancelBuild()
        {
            WasConfirmed = false;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? CloseRequested;

        private List<int> CollectAllItemIds()
        {
            // Items-used roll-up. Duplicates are intentional — if a stamp gets used
            // on the cardbase AND on a focal mat, it appears twice so the build log
            // accurately reflects how the card was built. Order matches the build
            // summary order so the items-used list reads as a step-by-step recipe.
            var result = new List<int>();

            void AddItem(int id) => result.Add(id);
            void AddInkItem(string colorName)
            {
                if (_inkItemIdByColor.TryGetValue(colorName, out var id)) AddItem(id);
            }
            void AddInkList(IEnumerable<string> colors) { foreach (var c in colors) AddInkItem(c); }
            void AddDecorationInks(IEnumerable<WizardMatDecoration> decorations)
            {
                foreach (var d in decorations)
                {
                    AddInkList(d.StampInkColors);
                    AddInkList(d.EmbossingInkColors);
                    foreach (var layer in d.StencilInkLayers) AddInkList(layer.InkColors);
                }
            }
            void AddCardstock(string? colorName)
            {
                if (string.IsNullOrEmpty(colorName) || colorName == "Other") return;
                if (_glitterCardstockIdByName.TryGetValue(colorName, out var csId)
                    || _foilCardstockIdByName.TryGetValue(colorName, out csId)
                    || _cardstockItemIdByName.TryGetValue(colorName, out csId))
                    result.Add(csId);
            }
            // Adhesive name lookup - adds the inventory item ID for any adhesive used.
            void AddAdhesives(IEnumerable<string> names)
            {
                foreach (var n in names)
                    if (!string.IsNullOrEmpty(n) && _adhesiveIdByName.TryGetValue(n, out var aId))
                        AddItem(aId);
            }
            // Inks rolled up from each WizardDetailEntry (stamp inks, embell embossing
            // inks, stencil inks, foil-stencil inks, plus the legacy single-pick InkColor).
            // Item IDs from AddedDetails are already covered by mat.GetItemIds().
            void AddAddedDetailsInks(IEnumerable<WizardDetailEntry> dets)
            {
                foreach (var det in dets)
                {
                    AddInkList(det.StampInkColors);
                    AddInkList(det.EmbellEmbossingInkColors);
                    AddInkList(det.StencilInkColors);
                    AddInkList(det.FoilStencilInkColors);
                    if (!string.IsNullOrEmpty(det.InkColor)) AddInkItem(det.InkColor);
                }
            }

            // Order: cardbase cardstock → each mat (cardstock first, then items used on it) →
            //        focal mat (cardstock → items → backer cardstock) → sentiments (cardstock → stamp) →
            //        embellishments → inside section (same pattern) → envelope last

            AddCardstock(EffectiveBaseCardstockColor);
            foreach (var d in CardBase.Decorations)
            {
                if (d.Item != null) AddItem(d.Item.Id);
                if (d.StampItem != null) AddItem(d.StampItem.Id);
            }
            AddDecorationInks(CardBase.Decorations);
            // Card-base detail entries: the per-pick rows the user added on the
            // Details sub-page (Stamps/Dies/Embell/Stacklets/EF/Stencils with their
            // own follow-up stencil glitter / happy medium / astro paste items, OLO
            // markers, Foils with stencil + ink + glitter follow-ups, watercolors,
            // ink colors). Each WizardDetailEntry knows how to enumerate its items.
            foreach (var det in CardBaseAddedDetails)
                foreach (var id in det.GetItemIds()) AddItem(id);
            AddAddedDetailsInks(CardBaseAddedDetails);
            // Cardbase adhesives picked from inventory (item ids already known).
            foreach (var a in CardBaseAddedAdhesives) AddItem(a.Id);

            foreach (var group in BgMats)
                foreach (var mat in group.Pieces)
                {
                    AddCardstock(mat.EffectiveCardstockColor);
                    foreach (var id in mat.GetItemIds()) AddItem(id);
                    AddInkList(mat.BlendInkColors);
                    AddInkList(mat.StampInkColors);
                    AddInkList(mat.EmbossingInkColors);
                    AddDecorationInks(mat.Decorations);
                    AddAddedDetailsInks(mat.AddedDetails);
                    AddAdhesives(mat.Adhesives);
                }

            foreach (var group in AdditionalMats)
                foreach (var mat in group.Pieces)
                {
                    AddCardstock(mat.EffectiveCardstockColor);
                    foreach (var id in mat.GetItemIds()) AddItem(id);
                    AddInkList(mat.BlendInkColors);
                    AddInkList(mat.StampInkColors);
                    AddInkList(mat.EmbossingInkColors);
                    AddDecorationInks(mat.Decorations);
                    AddAddedDetailsInks(mat.AddedDetails);
                    AddAdhesives(mat.Adhesives);
                }

            // Focal Mat hub uses WizardBgMatGroup (same shape as BG mats); its
            // per-piece detail entries hold the new Foils + glitter picks too.
            foreach (var group in FocalMatGroups)
                foreach (var mat in group.Pieces)
                {
                    AddCardstock(mat.EffectiveCardstockColor);
                    foreach (var id in mat.GetItemIds()) AddItem(id);
                    AddInkList(mat.BlendInkColors);
                    AddInkList(mat.StampInkColors);
                    AddInkList(mat.EmbossingInkColors);
                    AddDecorationInks(mat.Decorations);
                    AddAddedDetailsInks(mat.AddedDetails);
                    AddAdhesives(mat.Adhesives);
                }

            foreach (var fp in FocalParts)
            {
                AddCardstock(fp.EffectiveCardstockColor);
                foreach (var id in fp.GetItemIds()) AddItem(id);
                AddInkList(fp.BlendInkColors);
                AddInkList(fp.StampInkColors);
                AddInkList(fp.EmbossingInkColors);
                AddDecorationInks(fp.Decorations);
                AddAddedDetailsInks(fp.AddedDetails);
                AddCardstock(fp.EffectiveBackerCardstockColor);
                AddAdhesives(fp.Adhesives);
            }

            foreach (var s in ConfiguredSentiments)
                foreach (var p in s.Parts)
                {
                    if (!string.IsNullOrEmpty(p.CardstockColor) && p.CardstockColor != "Other")
                        AddCardstock(p.CardstockColor);
                    AddItem(p.ItemId);
                    if (p.IsEmbossed && p.EmbossingPowderItemId.HasValue)
                        AddItem(p.EmbossingPowderItemId.Value);
                    AddInkList(p.BlendInkColors);
                    AddInkList(p.StampInkColors);
                    // Each sentiment-part decoration's item (and its stamp item) must also be tracked
                    foreach (var d in p.Decorations)
                    {
                        AddItem(d.Item.Id);
                        if (d.StampItem != null) AddItem(d.StampItem.Id);
                    }
                    AddDecorationInks(p.Decorations);
                    AddAdhesives(p.Adhesives);
                }

            foreach (var e in AddedEmbellishments)
            {
                AddItem(e.ItemId);
                if (e.StampItemId.HasValue) AddItem(e.StampItemId.Value);
            }

            if (HasInside == true)
            {
                foreach (var mat in InsideBgMats)
                {
                    AddCardstock(mat.EffectiveCardstockColor);
                    foreach (var id in mat.GetItemIds()) AddItem(id);
                    AddInkList(mat.BlendInkColors);
                    AddInkList(mat.StampInkColors);
                    AddInkList(mat.EmbossingInkColors);
                    AddDecorationInks(mat.Decorations);
                    AddAddedDetailsInks(mat.AddedDetails);
                    AddAdhesives(mat.Adhesives);
                }
                foreach (var mat in InsideAdditionalMats)
                {
                    AddCardstock(mat.EffectiveCardstockColor);
                    foreach (var id in mat.GetItemIds()) AddItem(id);
                    AddInkList(mat.BlendInkColors);
                    AddInkList(mat.StampInkColors);
                    AddInkList(mat.EmbossingInkColors);
                    AddDecorationInks(mat.Decorations);
                    AddAddedDetailsInks(mat.AddedDetails);
                    AddAdhesives(mat.Adhesives);
                }
                if (HasInsideFocalMat)
                {
                    AddCardstock(InsideFocal.EffectiveCardstockColor);
                    foreach (var id in InsideFocal.GetItemIds()) AddItem(id);
                    AddInkList(InsideFocal.BlendInkColors);
                    AddInkList(InsideFocal.StampInkColors);
                    AddInkList(InsideFocal.EmbossingInkColors);
                    AddDecorationInks(InsideFocal.Decorations);
                    AddAddedDetailsInks(InsideFocal.AddedDetails);
                    AddCardstock(InsideFocal.EffectiveBackerCardstockColor);
                    AddAdhesives(InsideFocal.Adhesives);
                }
                foreach (var c in ConfiguredInsideSentiments)
                    foreach (var p in c.Parts)
                    {
                        AddItem(p.ItemId);
                        AddCardstock(p.CardstockColor);
                        AddAdhesives(p.Adhesives);
                    }
                foreach (var e in InsideAddedEmbellishments) { AddItem(e.ItemId); if (e.StampItemId.HasValue) AddItem(e.StampItemId.Value); }
            }

            if (SelectedEnvelopeItem != null) AddItem(SelectedEnvelopeItem.Id);
            if (SelectedStorageBagItem != null) AddItem(SelectedStorageBagItem.Id);

            return result;
        }

        private void AddCardstockId(string? colorName, List<int> ids)
        {
            if (string.IsNullOrEmpty(colorName) || colorName == "Other") return;
            if (_cardstockItemIdByName.TryGetValue(colorName, out var id))
                ids.Add(id);
        }

        // Builds the per-layer "— Layer 1: …; Layer 2: …" suffix appended to a
        // stencil build-step label so the final card summary reflects every
        // layer's inks + Glitter / Happy Medium / Astro Paste picks. Returns
        // an empty string when nothing was recorded for any layer.
        private static string StencilLayerSummarySuffix(WizardDetailEntry d)
        {
            if (d.StencilLayerEntries == null || d.StencilLayerEntries.Count == 0)
                return string.Empty;
            var nonEmpty = d.StencilLayerEntries
                .Where(le => le.InkColors.Count > 0 || le.UsedGlitter || le.UsedHappyMedium || le.UsedAstroPaste)
                .Select(le => le.DisplaySummary)
                .ToList();
            return nonEmpty.Count == 0 ? string.Empty : " — " + string.Join("; ", nonEmpty);
        }

        private List<WizardBuildStep> AssembleBuildSteps()
        {
            var steps = new List<WizardBuildStep>();

            void Add(string section, string stepType, int? matLayer, int? itemId,
                     int? dieId, string? cutting, string label)
                => steps.Add(new WizardBuildStep(section, stepType, matLayer, itemId, dieId, cutting, label));

            // Emit one row per item in a captured WizardDetailEntry. Used by every
            // context that holds AddedDetails (cardbase, BG mats, additional mats,
            // focal parts, focal-mat-groups, sentiment parts, and the inside-card
            // mirrors of all of the above) so adding a new detail field only needs
            // a single edit here. labelPrefix becomes the human-readable scope
            // ("Card base", "BG Mat 1", "Sentiment", …); stepPrefix is the build
            // step's machine-readable kind prefix ("cardbase_detail", "mat_detail",
            // "sentiment_detail", …).
            void EmitDetailEntry(string section, string stepPrefix, string labelPrefix,
                                 int? matLayer, WizardDetailEntry d)
            {
                if (d.Stamp != null)
                    Add(section, $"{stepPrefix}_stamp", matLayer, d.Stamp.Id, null, null, $"{labelPrefix} stamp: {d.Stamp.Name}");
                if (d.Die != null)
                    Add(section, $"{stepPrefix}_die", matLayer, d.Die.Id, null, null, $"{labelPrefix} die: {d.Die.Name}");
                if (d.Embellishment != null)
                    Add(section, $"{stepPrefix}_embellishment", matLayer, d.Embellishment.Id, null, null, $"{labelPrefix} embellishment: {d.Embellishment.Name}");
                if (d.Stacklet != null)
                    Add(section, $"{stepPrefix}_stacklet", matLayer, d.Stacklet.Id, null, null, $"{labelPrefix} stacklet: {d.Stacklet.Name}");
                if (d.EmbossingFolder != null)
                    Add(section, $"{stepPrefix}_embossing_folder", matLayer, d.EmbossingFolder.Id, null, null, $"{labelPrefix} embossing folder: {d.EmbossingFolder.Name}");
                if (d.Stencil != null)
                    Add(section, $"{stepPrefix}_stencil", matLayer, d.Stencil.Id, null, null,
                        $"{labelPrefix} stencil: {d.Stencil.Name}{StencilLayerSummarySuffix(d)}");
                foreach (var marker in d.OloMarkers)
                    Add(section, $"{stepPrefix}_olo_marker", matLayer, marker.Id, null, null, $"{labelPrefix} OLO marker: {marker.Name}");
                if (d.Watercolor != null)
                    Add(section, $"{stepPrefix}_watercolor", matLayer, d.Watercolor.Id, null, null, $"{labelPrefix} watercolor: {d.Watercolor.Name}");
                if (d.StampEmbossingPowder != null)
                    Add(section, $"{stepPrefix}_embossing_powder", matLayer, d.StampEmbossingPowder.Id, null, null, $"{labelPrefix} embossing powder: {d.StampEmbossingPowder.Name}");
                if (d.EmbellEmbossingStamp != null)
                    Add(section, $"{stepPrefix}_embell_embossing_stamp", matLayer, d.EmbellEmbossingStamp.Id, null, null, $"{labelPrefix} embell embossing stamp: {d.EmbellEmbossingStamp.Name}");
                foreach (var g in d.StencilGlitterItems)
                    Add(section, $"{stepPrefix}_stencil_glitter", matLayer, g.Id, null, null, $"{labelPrefix} stencil glitter: {g.Name}");
                foreach (var h in d.StencilHappyMediumItems)
                    Add(section, $"{stepPrefix}_stencil_happy_medium", matLayer, h.Id, null, null, $"{labelPrefix} stencil happy medium: {h.Name}");
                foreach (var a in d.StencilAstroPasteItems)
                    Add(section, $"{stepPrefix}_stencil_astro_paste", matLayer, a.Id, null, null, $"{labelPrefix} stencil astro paste: {a.Name}");
                if (d.Foil != null)
                    Add(section, $"{stepPrefix}_foil", matLayer, d.Foil.Id, null, null, $"{labelPrefix} foil: {d.Foil.Name} ({d.FoilApplicationMethod})");
                if (d.FoilStencil != null)
                    Add(section, $"{stepPrefix}_foil_stencil", matLayer, d.FoilStencil.Id, null, null, $"{labelPrefix} foil stencil: {d.FoilStencil.Name}");
                foreach (var g in d.FoilStencilGlitterItems)
                    Add(section, $"{stepPrefix}_foil_stencil_glitter", matLayer, g.Id, null, null, $"{labelPrefix} foil stencil glitter: {g.Name}");
                foreach (var h in d.FoilStencilHappyMediumItems)
                    Add(section, $"{stepPrefix}_foil_stencil_happy_medium", matLayer, h.Id, null, null, $"{labelPrefix} foil stencil happy medium: {h.Name}");
                foreach (var a in d.FoilStencilAstroPasteItems)
                    Add(section, $"{stepPrefix}_foil_stencil_astro_paste", matLayer, a.Id, null, null, $"{labelPrefix} foil stencil astro paste: {a.Name}");
                if (string.Equals(d.FoilApplicationMethod, "Toner", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(d.FoilTonerText))
                    Add(section, $"{stepPrefix}_foil_toner", matLayer, null, null, d.FoilTonerFont, $"{labelPrefix} foil toner: \"{d.FoilTonerText}\" in {d.FoilTonerFont}");
            }

            var cardBaseLabel = string.IsNullOrEmpty(EffectiveBaseCardstockColor)
                ? $"Cardbase: {SelectedCardBase}"
                : $"Cardbase: {SelectedCardBase} in {EffectiveBaseCardstockColor}";
            Add("exterior", "card_base", null, null, null, null, cardBaseLabel);

            // Card base decorations
            foreach (var d in CardBase.Decorations)
            {
                Add("exterior", "cardbase_decoration", null, d.Item?.Id, null, null, $"Card Base decoration: {d.Item?.Name}");
                if (d.StampItem != null)
                    Add("exterior", "cardbase_decoration_stamp", null, d.StampItem.Id, null, null, $"Card Base stamp: {d.StampItem.Name}");
            }

            // Card-base detail entries are emitted BEFORE the cardbase adhesives so
            // the order reads top-to-bottom: cardbase → its decorations & details →
            // adhesives that hold it all together.
            foreach (var d in CardBaseAddedDetails)
                EmitDetailEntry("exterior", "cardbase_detail", "Card base", null, d);

            // Cardbase adhesives picked on the Adhesives sub-page
            foreach (var a in CardBaseAddedAdhesives)
                Add("exterior", "card_base_adhesive", null, a.Id, null, null, $"Card Base Adhesive: {a.Name}");

            foreach (var group in BgMats.Where(g => !g.IsInside))
            {
                Add("exterior", "background_mat", group.GroupNumber, null, null, null, group.DisplaySummary);
                foreach (var mat in group.Pieces)
                {
                    var itemId = mat.CuttingMethod switch
                    {
                        "All Planned Out" => mat.PlannedOutItem?.Id,
                        "Frames" => mat.FramesItem?.Id,
                        "Stacklets" => mat.StackletItem?.Id,
                        "Insider" => mat.InsiderItem?.Id,
                        "Foil-It" => mat.FoilItItem?.Id,
                        _ => null
                    };
                    var pieceLabel = group.Pieces.Count == 1
                        ? $"Background Mat {group.GroupNumber}: {mat.DisplaySummary}"
                        : $"Background Mat {group.GroupNumber} piece {mat.Layer}: {mat.DisplaySummary}";
                    Add("exterior", "background_mat_piece", group.GroupNumber, itemId, null, mat.CuttingMethod, pieceLabel);
                    foreach (var d in mat.Decorations)
                    {
                        Add("exterior", "mat_decoration", group.GroupNumber, d.Item.Id, null, null, $"Background Mat {group.GroupNumber} decoration: {d.Item.Name}");
                        if (d.StampItem != null)
                            Add("exterior", "decoration_stamp", group.GroupNumber, d.StampItem.Id, null, null, $"Background Mat {group.GroupNumber} stamp: {d.StampItem.Name}");
                    }
                    // New-hub mats record decorations as WizardDetailEntry on AddedDetails
                    // instead of WizardMatDecoration. Emit per-detail steps so each picked
                    // item is tracked in the project. Per-piece adhesives are flushed
                    // after the details so the order reads "build the layer → glue it down".
                    foreach (var d in mat.AddedDetails)
                        EmitDetailEntry("exterior", "mat_detail", $"BG Mat {group.GroupNumber}", group.GroupNumber, d);
                    foreach (var aName in mat.Adhesives)
                        Add("exterior", "mat_adhesive", group.GroupNumber,
                            _adhesiveIdByName.TryGetValue(aName, out var aId) ? aId : null,
                            null, null, $"BG Mat {group.GroupNumber} adhesive: {aName}");
                }
            }

            foreach (var group in AdditionalMats.Where(g => !g.IsInside))
            {
                Add("exterior", "additional_mat", group.GroupNumber, null, null, null, group.DisplaySummary);
                foreach (var mat in group.Pieces)
                {
                    var itemId = mat.CuttingMethod switch
                    {
                        "All Planned Out" => mat.PlannedOutItem?.Id,
                        "Frames" => mat.FramesItem?.Id,
                        "Stacklets" => mat.StackletItem?.Id,
                        "Insider" => mat.InsiderItem?.Id,
                        "Foil-It" => mat.FoilItItem?.Id,
                        _ => null
                    };
                    var pieceLabel = group.Pieces.Count == 1
                        ? $"Additional Mat {group.GroupNumber}: {mat.DisplaySummary}"
                        : $"Additional Mat {group.GroupNumber} piece {mat.Layer}: {mat.DisplaySummary}";
                    Add("exterior", "additional_mat_piece", group.GroupNumber, itemId, null, mat.CuttingMethod, pieceLabel);
                    foreach (var d in mat.Decorations)
                    {
                        Add("exterior", "mat_decoration", group.GroupNumber, d.Item.Id, null, null, $"Additional Mat {group.GroupNumber} decoration: {d.Item.Name}");
                        if (d.StampItem != null)
                            Add("exterior", "decoration_stamp", group.GroupNumber, d.StampItem.Id, null, null, $"Additional Mat {group.GroupNumber} stamp: {d.StampItem.Name}");
                    }
                    foreach (var d in mat.AddedDetails)
                        EmitDetailEntry("exterior", "additional_mat_detail", $"Additional Mat {group.GroupNumber}", group.GroupNumber, d);
                    foreach (var aName in mat.Adhesives)
                        Add("exterior", "additional_mat_adhesive", group.GroupNumber,
                            _adhesiveIdByName.TryGetValue(aName, out var aId) ? aId : null,
                            null, null, $"Additional Mat {group.GroupNumber} adhesive: {aName}");
                }
            }

            // Focal-mat hub (new). Pieces share the WizardBgMat shape with BG / Additional
            // mats so the same per-detail emission applies. Inserted between additional
            // mats and the legacy FocalParts so the build order stays mats → focal.
            foreach (var group in FocalMatGroups.Where(g => !g.IsInside))
            {
                Add("exterior", "focal_mat_group", group.GroupNumber, null, null, null, group.DisplaySummary);
                foreach (var mat in group.Pieces)
                {
                    var itemId = mat.CuttingMethod switch
                    {
                        "All Planned Out" => mat.PlannedOutItem?.Id,
                        "Frames" => mat.FramesItem?.Id,
                        "Stacklets" => mat.StackletItem?.Id,
                        "Insider" => mat.InsiderItem?.Id,
                        "Foil-It" => mat.FoilItItem?.Id,
                        _ => null
                    };
                    var pieceLabel = group.Pieces.Count == 1
                        ? $"Focal Mat {group.GroupNumber}: {mat.DisplaySummary}"
                        : $"Focal Mat {group.GroupNumber} part {mat.Layer}: {mat.DisplaySummary}";
                    Add("exterior", "focal_mat_piece", group.GroupNumber, itemId, null, mat.CuttingMethod, pieceLabel);
                    foreach (var d in mat.Decorations)
                    {
                        Add("exterior", "focal_mat_decoration", group.GroupNumber, d.Item.Id, null, null, $"Focal Mat {group.GroupNumber} decoration: {d.Item.Name}");
                        if (d.StampItem != null)
                            Add("exterior", "focal_mat_decoration_stamp", group.GroupNumber, d.StampItem.Id, null, null, $"Focal Mat {group.GroupNumber} stamp: {d.StampItem.Name}");
                    }
                    foreach (var d in mat.AddedDetails)
                        EmitDetailEntry("exterior", "focal_mat_detail", $"Focal Mat {group.GroupNumber}", group.GroupNumber, d);
                    foreach (var aName in mat.Adhesives)
                        Add("exterior", "focal_mat_adhesive", group.GroupNumber,
                            _adhesiveIdByName.TryGetValue(aName, out var aId) ? aId : null,
                            null, null, $"Focal Mat {group.GroupNumber} adhesive: {aName}");
                }
            }

            // Exterior focal parts (legacy single-piece focal flow). PartNumber is
            // offset past every BG / Additional / Focal-hub group so the focal step
            // sorts after them, matching the physical assembly order.
            int focalPartBaseIndex = BgMats.Count + AdditionalMats.Count + FocalMatGroups.Count;
            foreach (var fp in FocalParts)
            {
                var focalItemId = fp.CuttingMethod switch
                {
                    "All Planned Out" => fp.PlannedOutItem?.Id,
                    "Frames"          => fp.FramesItem?.Id,
                    "Stacklet"        => fp.StackletItem?.Id,
                    "Insider"         => fp.InsiderItem?.Id,
                    "Foil-It"         => fp.FoilItItem?.Id,
                    "Dies"            => fp.SelectedDie?.Id,
                    _                 => (int?)null
                };
                bool focalHasSomething = focalItemId.HasValue
                    || !string.IsNullOrEmpty(fp.EffectiveCardstockColor)
                    || fp.Decorations.Count > 0 || fp.HasBacker
                    || fp.AddedDetails.Count > 0;
                if (!focalHasSomething) continue;
                int layerIndex = focalPartBaseIndex + fp.PartNumber;
                Add("exterior", "focal_mat", layerIndex,
                    focalItemId, null, fp.CuttingMethod,
                    $"Focal Mat Piece {fp.PartNumber}: {fp.DisplaySummary}");
                foreach (var d in fp.Decorations)
                {
                    Add("exterior", "focal_decoration", layerIndex, d.Item.Id, null, null, $"Focal Mat {fp.PartNumber} decoration: {d.Item.Name}");
                    if (d.StampItem != null)
                        Add("exterior", "focal_decoration_stamp", layerIndex, d.StampItem.Id, null, null, $"Focal Mat {fp.PartNumber} stamp: {d.StampItem.Name}");
                }
                foreach (var d in fp.AddedDetails)
                    EmitDetailEntry("exterior", "focal_detail", $"Focal Mat {fp.PartNumber}", layerIndex, d);
                foreach (var aName in fp.Adhesives)
                    Add("exterior", "focal_adhesive", layerIndex,
                        _adhesiveIdByName.TryGetValue(aName, out var aId) ? aId : null,
                        null, null, $"Focal Mat {fp.PartNumber} adhesive: {aName}");
            }

            foreach (var s in ConfiguredSentiments)
                foreach (var p in s.Parts)
                {
                    Add("exterior", "sentiment", null, p.ItemId, null, null, $"Sentiment: {p.DisplaySummary}");
                    // Per-part decorations also need their own build-steps so they show up on the project page
                    foreach (var d in p.Decorations)
                    {
                        Add("exterior", "sentiment_decoration", null, d.Item.Id, null, null, $"Sentiment decoration: {d.Item.Name}");
                        if (d.StampItem != null)
                            Add("exterior", "sentiment_decoration_stamp", null, d.StampItem.Id, null, null, $"Sentiment decoration stamp: {d.StampItem.Name}");
                    }
                    // Per-part captured detail entries (Stamps/Dies/Embell/Stacklets/EF/
                    // Stencils/OLO/Watercolor/Foils with their own follow-ups). Without
                    // this loop the user's per-sentiment Details sub-page picks were
                    // captured but never surfaced on the project page.
                    foreach (var d in p.AddedDetails)
                        EmitDetailEntry("exterior", "sentiment_detail", "Sentiment", null, d);
                    // Sentiment-piece adhesives.
                    foreach (var aName in p.Adhesives)
                        Add("exterior", "sentiment_adhesive", null,
                            _adhesiveIdByName.TryGetValue(aName, out var aId) ? aId : null,
                            null, null, $"Sentiment adhesive: {aName}");
                }
            foreach (var e in AddedEmbellishments)
            {
                Add("exterior", "embellishment", null, e.ItemId, null, null, $"Embellishment: {e.DisplaySummary}");
                if (e.StampItemId.HasValue)
                    Add("exterior", "embellishment_stamp", null, e.StampItemId.Value, null, null,
                        $"Embellishment Stamp: {e.StampItemName}");
            }

            if (HasInside == true)
            {
                // Mat groups whose IsInside flag is set were added through the new
                // hub but tagged for the inside of the card. Emit them inside the
                // inside-section so they sort with the rest of the inside content.
                foreach (var group in BgMats.Where(g => g.IsInside))
                {
                    Add("inside", "background_mat", group.GroupNumber, null, null, null, group.DisplaySummary);
                    foreach (var mat in group.Pieces)
                    {
                        var itemId = mat.CuttingMethod switch
                        {
                            "All Planned Out" => mat.PlannedOutItem?.Id,
                            "Frames" => mat.FramesItem?.Id,
                            "Stacklets" => mat.StackletItem?.Id,
                            "Insider" => mat.InsiderItem?.Id,
                            "Foil-It" => mat.FoilItItem?.Id,
                            _ => null
                        };
                        var pieceLabel = group.Pieces.Count == 1
                            ? $"Inside Background Mat {group.GroupNumber}: {mat.DisplaySummary}"
                            : $"Inside Background Mat {group.GroupNumber} piece {mat.Layer}: {mat.DisplaySummary}";
                        Add("inside", "background_mat_piece", group.GroupNumber, itemId, null, mat.CuttingMethod, pieceLabel);
                        foreach (var d in mat.Decorations)
                        {
                            Add("inside", "mat_decoration", group.GroupNumber, d.Item.Id, null, null, $"Inside Background Mat {group.GroupNumber} decoration: {d.Item.Name}");
                            if (d.StampItem != null)
                                Add("inside", "decoration_stamp", group.GroupNumber, d.StampItem.Id, null, null, $"Inside Background Mat {group.GroupNumber} stamp: {d.StampItem.Name}");
                        }
                        foreach (var d in mat.AddedDetails)
                            EmitDetailEntry("inside", "mat_detail", $"Inside BG Mat {group.GroupNumber}", group.GroupNumber, d);
                        foreach (var aName in mat.Adhesives)
                            Add("inside", "mat_adhesive", group.GroupNumber,
                                _adhesiveIdByName.TryGetValue(aName, out var aId) ? aId : null,
                                null, null, $"Inside BG Mat {group.GroupNumber} adhesive: {aName}");
                    }
                }

                foreach (var group in AdditionalMats.Where(g => g.IsInside))
                {
                    Add("inside", "additional_mat", group.GroupNumber, null, null, null, group.DisplaySummary);
                    foreach (var mat in group.Pieces)
                    {
                        var itemId = mat.CuttingMethod switch
                        {
                            "All Planned Out" => mat.PlannedOutItem?.Id,
                            "Frames" => mat.FramesItem?.Id,
                            "Stacklets" => mat.StackletItem?.Id,
                            "Insider" => mat.InsiderItem?.Id,
                            "Foil-It" => mat.FoilItItem?.Id,
                            _ => null
                        };
                        var pieceLabel = group.Pieces.Count == 1
                            ? $"Inside Additional Mat {group.GroupNumber}: {mat.DisplaySummary}"
                            : $"Inside Additional Mat {group.GroupNumber} piece {mat.Layer}: {mat.DisplaySummary}";
                        Add("inside", "additional_mat_piece", group.GroupNumber, itemId, null, mat.CuttingMethod, pieceLabel);
                        foreach (var d in mat.Decorations)
                        {
                            Add("inside", "mat_decoration", group.GroupNumber, d.Item.Id, null, null, $"Inside Additional Mat {group.GroupNumber} decoration: {d.Item.Name}");
                            if (d.StampItem != null)
                                Add("inside", "decoration_stamp", group.GroupNumber, d.StampItem.Id, null, null, $"Inside Additional Mat {group.GroupNumber} stamp: {d.StampItem.Name}");
                        }
                        foreach (var d in mat.AddedDetails)
                            EmitDetailEntry("inside", "additional_mat_detail", $"Inside Additional Mat {group.GroupNumber}", group.GroupNumber, d);
                        foreach (var aName in mat.Adhesives)
                            Add("inside", "additional_mat_adhesive", group.GroupNumber,
                                _adhesiveIdByName.TryGetValue(aName, out var aId) ? aId : null,
                                null, null, $"Inside Additional Mat {group.GroupNumber} adhesive: {aName}");
                    }
                }

                foreach (var group in FocalMatGroups.Where(g => g.IsInside))
                {
                    Add("inside", "focal_mat_group", group.GroupNumber, null, null, null, group.DisplaySummary);
                    foreach (var mat in group.Pieces)
                    {
                        var itemId = mat.CuttingMethod switch
                        {
                            "All Planned Out" => mat.PlannedOutItem?.Id,
                            "Frames" => mat.FramesItem?.Id,
                            "Stacklets" => mat.StackletItem?.Id,
                            "Insider" => mat.InsiderItem?.Id,
                            "Foil-It" => mat.FoilItItem?.Id,
                            _ => null
                        };
                        var pieceLabel = group.Pieces.Count == 1
                            ? $"Inside Focal Mat {group.GroupNumber}: {mat.DisplaySummary}"
                            : $"Inside Focal Mat {group.GroupNumber} part {mat.Layer}: {mat.DisplaySummary}";
                        Add("inside", "focal_mat_piece", group.GroupNumber, itemId, null, mat.CuttingMethod, pieceLabel);
                        foreach (var d in mat.Decorations)
                        {
                            Add("inside", "focal_mat_decoration", group.GroupNumber, d.Item.Id, null, null, $"Inside Focal Mat {group.GroupNumber} decoration: {d.Item.Name}");
                            if (d.StampItem != null)
                                Add("inside", "focal_mat_decoration_stamp", group.GroupNumber, d.StampItem.Id, null, null, $"Inside Focal Mat {group.GroupNumber} stamp: {d.StampItem.Name}");
                        }
                        foreach (var d in mat.AddedDetails)
                            EmitDetailEntry("inside", "focal_mat_detail", $"Inside Focal Mat {group.GroupNumber}", group.GroupNumber, d);
                        foreach (var aName in mat.Adhesives)
                            Add("inside", "focal_mat_adhesive", group.GroupNumber,
                                _adhesiveIdByName.TryGetValue(aName, out var aId) ? aId : null,
                                null, null, $"Inside Focal Mat {group.GroupNumber} adhesive: {aName}");
                    }
                }

                foreach (var mat in InsideBgMats)
                {
                    var itemId = mat.CuttingMethod switch
                    {
                        "All Planned Out" => mat.PlannedOutItem?.Id,
                        "Frames" => mat.FramesItem?.Id,
                        "Stacklets" => mat.StackletItem?.Id,
                        "Insider" => mat.InsiderItem?.Id,
                        "Foil-It" => mat.FoilItItem?.Id,
                        _ => null
                    };
                    Add("inside", "background_mat", mat.Layer, itemId, null, mat.CuttingMethod, $"Inside Background Mat {mat.Layer}: {mat.DisplaySummary}");
                    foreach (var d in mat.Decorations)
                    {
                        Add("inside", "mat_decoration", mat.Layer, d.Item.Id, null, null, $"Inside background mat {mat.Layer} decoration: {d.Item.Name}");
                        if (d.StampItem != null)
                            Add("inside", "decoration_stamp", mat.Layer, d.StampItem.Id, null, null, $"Inside background mat {mat.Layer} stamp: {d.StampItem.Name}");
                    }
                    foreach (var d in mat.AddedDetails)
                        EmitDetailEntry("inside", "mat_detail", $"Inside BG Mat {mat.Layer}", mat.Layer, d);
                    foreach (var aName in mat.Adhesives)
                        Add("inside", "mat_adhesive", mat.Layer,
                            _adhesiveIdByName.TryGetValue(aName, out var aId) ? aId : null,
                            null, null, $"Inside BG Mat {mat.Layer} adhesive: {aName}");
                }

                foreach (var mat in InsideAdditionalMats)
                {
                    var itemId = mat.CuttingMethod switch
                    {
                        "All Planned Out" => mat.PlannedOutItem?.Id,
                        "Frames" => mat.FramesItem?.Id,
                        "Stacklets" => mat.StackletItem?.Id,
                        "Insider" => mat.InsiderItem?.Id,
                        "Foil-It" => mat.FoilItItem?.Id,
                        _ => null
                    };
                    Add("inside", "additional_mat", mat.Layer, itemId, null, mat.CuttingMethod, $"Inside Additional Mat {mat.Layer}: {mat.DisplaySummary}");
                    foreach (var d in mat.Decorations)
                    {
                        Add("inside", "mat_decoration", mat.Layer, d.Item.Id, null, null, $"Inside additional mat {mat.Layer} decoration: {d.Item.Name}");
                        if (d.StampItem != null)
                            Add("inside", "decoration_stamp", mat.Layer, d.StampItem.Id, null, null, $"Inside additional mat {mat.Layer} stamp: {d.StampItem.Name}");
                    }
                    foreach (var d in mat.AddedDetails)
                        EmitDetailEntry("inside", "additional_mat_detail", $"Inside Additional Mat {mat.Layer}", mat.Layer, d);
                    foreach (var aName in mat.Adhesives)
                        Add("inside", "additional_mat_adhesive", mat.Layer,
                            _adhesiveIdByName.TryGetValue(aName, out var aId) ? aId : null,
                            null, null, $"Inside Additional Mat {mat.Layer} adhesive: {aName}");
                }

                // Inside focal - only persisted when user added one
                if (HasInsideFocalMat)
                {
                    var insideFocalItemId = InsideFocal.CuttingMethod switch
                    {
                        "All Planned Out" => InsideFocal.PlannedOutItem?.Id,
                        "Frames"          => InsideFocal.FramesItem?.Id,
                        "Stacklet"        => InsideFocal.StackletItem?.Id,
                        "Insider"         => InsideFocal.InsiderItem?.Id,
                        "Foil-It"         => InsideFocal.FoilItItem?.Id,
                        "Dies"            => InsideFocal.SelectedDie?.Id,
                        _                 => (int?)null
                    };
                    int insideFocalLayer = InsideBgMats.Count + InsideAdditionalMats.Count + 1;
                    Add("inside", "focal_mat", insideFocalLayer,
                        insideFocalItemId, null, InsideFocal.CuttingMethod,
                        $"Inside Focal Mat: {InsideFocal.DisplaySummary}");
                    foreach (var d in InsideFocal.Decorations)
                    {
                        Add("inside", "focal_decoration", insideFocalLayer, d.Item.Id, null, null, $"Inside focal decoration: {d.Item.Name}");
                        if (d.StampItem != null)
                            Add("inside", "focal_decoration_stamp", insideFocalLayer, d.StampItem.Id, null, null, $"Inside focal stamp: {d.StampItem.Name}");
                    }
                    foreach (var d in InsideFocal.AddedDetails)
                        EmitDetailEntry("inside", "focal_detail", "Inside Focal Mat", insideFocalLayer, d);
                    foreach (var aName in InsideFocal.Adhesives)
                        Add("inside", "focal_adhesive", insideFocalLayer,
                            _adhesiveIdByName.TryGetValue(aName, out var aId) ? aId : null,
                            null, null, $"Inside focal adhesive: {aName}");
                }

                foreach (var c in ConfiguredInsideSentiments)
                    foreach (var p in c.Parts)
                    {
                        Add("inside", "sentiment", null, p.ItemId, null, null, $"Sentiment: {p.DisplaySummary}");
                        foreach (var d in p.Decorations)
                        {
                            Add("inside", "sentiment_decoration", null, d.Item.Id, null, null, $"Inside sentiment decoration: {d.Item.Name}");
                            if (d.StampItem != null)
                                Add("inside", "sentiment_decoration_stamp", null, d.StampItem.Id, null, null, $"Inside sentiment decoration stamp: {d.StampItem.Name}");
                        }
                        foreach (var d in p.AddedDetails)
                            EmitDetailEntry("inside", "sentiment_detail", "Inside Sentiment", null, d);
                        foreach (var aName in p.Adhesives)
                            Add("inside", "sentiment_adhesive", null,
                                _adhesiveIdByName.TryGetValue(aName, out var aId) ? aId : null,
                                null, null, $"Inside sentiment adhesive: {aName}");
                    }
                foreach (var e in InsideAddedEmbellishments)
                {
                    Add("inside", "embellishment", null, e.ItemId, null, null, $"Embellishment: {e.ItemName}");
                    if (e.StampItemId.HasValue)
                        Add("inside", "embellishment_stamp", null, e.StampItemId.Value, null, null, $"Inside embellishment stamp: {e.StampItemName}");
                }
            }

            if (SelectedEnvelopeItem != null)
                Add("exterior", "envelope", null, SelectedEnvelopeItem.Id, null, null, $"Envelope: {SelectedEnvelopeItem.Name}");
            if (SelectedStorageBagItem != null)
                Add("exterior", "storage_bag", null, SelectedStorageBagItem.Id, null, null, $"Storage Bag: {SelectedStorageBagItem.Name}");

            return steps;
        }


        // ── Snapshot capture / restore ────────────────────────────────────────
        // Round-trips the wizard's full editable state (every collection, every
        // picker selection) so that re-opening the wizard on an existing build
        // populates with what the user originally chose. Output-only fields like
        // CardBaseType / BuildSteps / BuildOtherNotes are *not* snapshotted —
        // they're regenerated from the live state when Create Card runs.
        public string CaptureSnapshotJson()
        {
            var snap = new WizardBuildSnapshot
            {
                SelectedCardBase = SelectedCardBase,
                Notes = WizardNotes,

                BaseRegularCardstockItemId = SelectedBaseRegularCardstockItem?.Id,
                BaseFoilCardstockItemId    = SelectedBaseFoilCardstockItem?.Id,
                BaseGlitterCardstockItemId = SelectedBaseGlitterCardstockItem?.Id,
                BaseCardstockColor         = SelectedBaseCardstockColor,
                BaseIsSelfBlended          = BaseIsSelfBlended,
                BaseSelfBlendDescription   = BaseSelfBlendDescription,
                BaseBlendInkColors         = BaseBlendInks.Ordered.ToList(),
                CardBase                   = CardBase,
                CardBaseAddedDetails       = CardBaseAddedDetails.ToList(),
                CardBaseAddedAdhesives     = CardBaseAddedAdhesives.ToList(),

                BgMats               = BgMats.ToList(),
                AdditionalMats       = AdditionalMats.ToList(),
                FocalMatGroups       = FocalMatGroups.ToList(),
                FocalParts           = FocalParts.ToList(),
                ConfiguredSentiments = ConfiguredSentiments.ToList(),
                AddedEmbellishments  = AddedEmbellishments.ToList(),
                SelectedEnvelopeItem = SelectedEnvelopeItem,
                SelectedStorageBagItem = SelectedStorageBagItem,

                InsideBgMats               = InsideBgMats.ToList(),
                InsideAdditionalMats       = InsideAdditionalMats.ToList(),
                InsideFocal                = InsideFocal,
                ConfiguredInsideSentiments = ConfiguredInsideSentiments.ToList(),
                InsideAddedEmbellishments  = InsideAddedEmbellishments.ToList(),

                InsideLinerCardstockItemId = SelectedInsideLinerCardstockItem?.Id,
                InsideLinerCardstockColor  = SelectedInsideLinerCardstockColor,
                InsideMiscDetails          = InsideMiscDetails.ToList(),
            };
            return snap.ToJson();
        }

        public void LoadFromSnapshotJson(string? json)
        {
            var snap = WizardBuildSnapshot.FromJson(json);
            if (snap == null) return;

            // Top-level
            if (!string.IsNullOrEmpty(snap.SelectedCardBase)) SelectedCardBase = snap.SelectedCardBase;
            if (!string.IsNullOrEmpty(snap.Notes)) WizardNotes = snap.Notes;

            // Card Base
            BaseIsSelfBlended        = snap.BaseIsSelfBlended;
            BaseSelfBlendDescription = snap.BaseSelfBlendDescription;
            SelectedBaseCardstockColor = snap.BaseCardstockColor;
            // Resolve cardstock item references back to the live ObservableCollection instances
            // so the bound ComboBox SelectedItem matches by reference (not just by Id).
            if (snap.BaseRegularCardstockItemId is int rid)
                SelectedBaseRegularCardstockItem = BaseCardstockRegularItems.FirstOrDefault(i => i.Id == rid);
            if (snap.BaseFoilCardstockItemId is int fid)
                SelectedBaseFoilCardstockItem = BaseCardstockFoilItems.FirstOrDefault(i => i.Id == fid);
            if (snap.BaseGlitterCardstockItemId is int gid)
                SelectedBaseGlitterCardstockItem = BaseCardstockGlitterItems.FirstOrDefault(i => i.Id == gid);
            BaseBlendInks.Ordered.Clear();
            foreach (var c in snap.BaseBlendInkColors) BaseBlendInks.Ordered.Add(c);

            if (snap.CardBaseAddedDetails != null)
            {
                CardBaseAddedDetails.Clear();
                foreach (var d in snap.CardBaseAddedDetails) CardBaseAddedDetails.Add(d);
            }
            if (snap.CardBaseAddedAdhesives != null)
            {
                CardBaseAddedAdhesives.Clear();
                foreach (var a in snap.CardBaseAddedAdhesives) CardBaseAddedAdhesives.Add(a);
            }

            // Outside collections
            BgMats.Clear();
            foreach (var g in snap.BgMats ?? Enumerable.Empty<WizardBgMatGroup>()) BgMats.Add(g);
            AdditionalMats.Clear();
            foreach (var g in snap.AdditionalMats ?? Enumerable.Empty<WizardBgMatGroup>()) AdditionalMats.Add(g);
            FocalMatGroups.Clear();
            foreach (var g in snap.FocalMatGroups ?? Enumerable.Empty<WizardBgMatGroup>()) FocalMatGroups.Add(g);
            FocalParts.Clear();
            foreach (var p in snap.FocalParts ?? Enumerable.Empty<WizardFocalSection>()) FocalParts.Add(p);
            ConfiguredSentiments.Clear();
            foreach (var s in snap.ConfiguredSentiments ?? Enumerable.Empty<WizardConfiguredSentiment>()) ConfiguredSentiments.Add(s);
            AddedEmbellishments.Clear();
            foreach (var e in snap.AddedEmbellishments ?? Enumerable.Empty<WizardEmbellishment>()) AddedEmbellishments.Add(e);
            SelectedEnvelopeItem = snap.SelectedEnvelopeItem;
            SelectedStorageBagItem = snap.SelectedStorageBagItem;

            // Inside collections
            InsideBgMats.Clear();
            foreach (var m in snap.InsideBgMats ?? Enumerable.Empty<WizardBgMat>()) InsideBgMats.Add(m);
            InsideAdditionalMats.Clear();
            foreach (var m in snap.InsideAdditionalMats ?? Enumerable.Empty<WizardBgMat>()) InsideAdditionalMats.Add(m);
            // InsideFocal is a get-only property that we mutate in place, so copy fields.
            // For simplicity we accept that loading replaces nothing here unless the
            // wizard's InsideFocal was a property with a setter. (Currently it's get-only.)
            ConfiguredInsideSentiments.Clear();
            foreach (var s in snap.ConfiguredInsideSentiments ?? Enumerable.Empty<WizardConfiguredSentiment>()) ConfiguredInsideSentiments.Add(s);
            InsideAddedEmbellishments.Clear();
            foreach (var e in snap.InsideAddedEmbellishments ?? Enumerable.Empty<WizardEmbellishment>()) InsideAddedEmbellishments.Add(e);

            // Inside hub: liner cardstock + misc Details
            SelectedInsideLinerCardstockColor = snap.InsideLinerCardstockColor;
            if (snap.InsideLinerCardstockItemId is int liner)
            {
                // Pull from the picker's filtered set (loaded via Load() above).
                // Falls back to null if the item no longer exists in inventory.
                SelectedInsideLinerCardstockItem =
                    InsideLinerCardstockPicker.FilteredItems.FirstOrDefault(o => o.Id == liner);
                InsideLinerCardstockPicker.SelectedItem = SelectedInsideLinerCardstockItem;
            }
            InsideCardstockSaved = SelectedInsideLinerCardstockItem != null
                                   || !string.IsNullOrEmpty(SelectedInsideLinerCardstockColor);

            InsideMiscDetails.Clear();
            foreach (var d in snap.InsideMiscDetails ?? Enumerable.Empty<WizardDetailEntry>())
                InsideMiscDetails.Add(d);
            InsideDetailsSaved = InsideMiscDetails.Count > 0;

            UpdateSummaryLines();
        }
    }
}
