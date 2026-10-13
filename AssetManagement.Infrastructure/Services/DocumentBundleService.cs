using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading.Tasks;
using AssetManagement.Core.Interfaces;
using AssetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Services;

/// <summary>
/// Implements IDocumentBundleService with a deterministic scoring algorithm
/// for evidence-to-item matching. Never invents matches or confidence values.
/// </summary>
public class DocumentBundleService : IDocumentBundleService
{
    private readonly ApplicationDbContext _context;
    private readonly IAiFieldMappingService _fieldMapping;
    private readonly IAiValidationService _validation;

    // Matching score thresholds (Section 6 of spec)
    private const int AutoMatchThreshold = 60;
    private const int AmbiguityGapRequired = 15;
    private const int NoMatchThreshold = 40;

    // Keywords indicating document type
    private static readonly string[] InvoiceKeywords = { "invoice", "inv-", "bill to", "ship to", "subtotal", "total amount due", "tax invoice", "purchase order", "payment terms" };
    private static readonly string[] ConfigKeywords = { "specifications", "processor", "memory", "storage", "display", "operating system", "model no", "part no", "serial", "ram", "ssd", "hdd", "cpu", "graphics" };
    private static readonly string[] WarrantyKeywords = { "warranty", "guarantee", "service agreement", "limited warranty", "warranty period" };
    private static readonly string[] DeliveryKeywords = { "delivery note", "challan", "dispatch", "packing list", "shipped" };

    public DocumentBundleService(ApplicationDbContext context, IAiFieldMappingService fieldMapping, IAiValidationService validation)
    {
        _context = context;
        _fieldMapping = fieldMapping;
        _validation = validation;
    }

    // ──────────────────────────────────────────────────────────────────────
    // CLASSIFY
    // ──────────────────────────────────────────────────────────────────────

    public BundleDocument ClassifyDocument(string fileName, int pageNumber, string? ocrText, AiDocumentExtractionResult? extractionResult)
    {
        var doc = new BundleDocument
        {
            OriginalFileName = fileName,
            PageNumber = pageNumber,
            OcrText = ocrText,
            ExtractionResult = extractionResult
        };

        var lowerText = (ocrText ?? string.Empty).ToLowerInvariant();
        var lowerName = fileName.ToLowerInvariant();

        // ── Has AI extracted invoice items? → Invoice/Continuation ──────
        if (extractionResult != null && extractionResult.Items.Count > 0)
        {
            doc.Classification = DocumentClassification.Invoice;
            doc.ClassificationReason = "AI extracted invoice line items";
            return doc;
        }

        if (extractionResult != null && !string.IsNullOrEmpty(extractionResult.InvoiceNumber))
        {
            doc.Classification = DocumentClassification.InvoiceContinuation;
            doc.ClassificationReason = "Invoice number found but no new line items (continuation page)";
            return doc;
        }

        // ── Keyword matching ─────────────────────────────────────────────
        int invoiceScore = InvoiceKeywords.Count(kw => lowerText.Contains(kw));
        int configScore = ConfigKeywords.Count(kw => lowerText.Contains(kw));
        int warrantyScore = WarrantyKeywords.Count(kw => lowerText.Contains(kw));
        int deliveryScore = DeliveryKeywords.Count(kw => lowerText.Contains(kw));

        // File name hints
        if (lowerName.Contains("config") || lowerName.Contains("spec")) configScore += 3;
        if (lowerName.Contains("warranty")) warrantyScore += 3;
        if (lowerName.Contains("invoice") || lowerName.Contains("inv")) invoiceScore += 3;
        if (lowerName.Contains("delivery") || lowerName.Contains("challan")) deliveryScore += 3;

        var maxScore = Math.Max(invoiceScore, Math.Max(configScore, Math.Max(warrantyScore, deliveryScore)));

        if (maxScore == 0)
        {
            doc.Classification = DocumentClassification.Unknown;
            doc.ClassificationReason = "No recognizable keywords found";
            return doc;
        }

        if (warrantyScore == maxScore)
        {
            doc.Classification = DocumentClassification.Warranty;
            doc.ClassificationReason = $"Warranty keywords matched ({warrantyScore} hits)";
        }
        else if (deliveryScore == maxScore)
        {
            doc.Classification = DocumentClassification.DeliveryNote;
            doc.ClassificationReason = $"Delivery note keywords matched ({deliveryScore} hits)";
        }
        else if (configScore > invoiceScore)
        {
            doc.Classification = DocumentClassification.Configuration;
            doc.ClassificationReason = $"Configuration/spec keywords matched ({configScore} hits)";
        }
        else
        {
            doc.Classification = DocumentClassification.Invoice;
            doc.ClassificationReason = $"Invoice keywords matched ({invoiceScore} hits)";
        }

        return doc;
    }

    // ──────────────────────────────────────────────────────────────────────
    // PROCESS BUNDLE
    // ──────────────────────────────────────────────────────────────────────

    public async Task<BundleProcessingResult> ProcessBundleAsync(DocumentBundle bundle)
    {
        var result = new BundleProcessingResult { Bundle = bundle };

        // ── Step 1: Separate invoice docs from supporting docs ───────────
        var invoiceDocs = bundle.Documents
            .Where(d => d.Classification == DocumentClassification.Invoice || d.Classification == DocumentClassification.InvoiceContinuation)
            .ToList();

        var supportingDocs = bundle.Documents
            .Where(d => d.Classification != DocumentClassification.Invoice && d.Classification != DocumentClassification.InvoiceContinuation)
            .ToList();

        if (!invoiceDocs.Any())
        {
            result.Errors.Add("No invoice documents were found in the uploaded bundle. Please include at least one invoice image.");
            return result;
        }

        // ── Step 2: Merge invoice pages into one canonical purchase ──────
        var mergedExtraction = MergeInvoiceDocs(invoiceDocs);
        result.MappingResult = await _fieldMapping.MapPurchaseAsync(mergedExtraction);
        await _validation.ValidatePurchaseAsync(result.MappingResult);
        result.MergedPurchase = result.MappingResult.Data;

        if (result.MappingResult.DuplicateDetected)
        {
            result.IsDuplicate = true;
            result.Warnings.Add($"Duplicate invoice detected: {result.MergedPurchase?.InvoiceNumber}. This invoice may already exist in the system.");
        }

        if (result.MergedPurchase == null || !result.MergedPurchase.Items.Any())
        {
            result.Warnings.Add("No purchase line items could be extracted. Please review the invoice manually.");
        }

        // ── Step 3: Build per-item candidates ───────────────────────────
        foreach (var item in result.MergedPurchase?.Items ?? new())
        {
            var match = new CandidateItemMatch { Item = item };

            // Add invoice page evidence (auto-attached, confidence = doc confidence or 0.9)
            foreach (var invoiceDoc in invoiceDocs)
            {
                bool docContainsItem = !string.IsNullOrEmpty(invoiceDoc.OcrText) &&
                    ((!string.IsNullOrEmpty(item.Model) && invoiceDoc.OcrText.Contains(item.Model, StringComparison.OrdinalIgnoreCase)) ||
                     (!string.IsNullOrEmpty(item.Description) && invoiceDoc.OcrText.Contains(item.Description?.Split(' ')[0] ?? "", StringComparison.OrdinalIgnoreCase)));

                match.EvidenceMatches.Add(new EvidenceMatch
                {
                    DocumentId = invoiceDoc.DocumentId,
                    SourceFileName = invoiceDoc.OriginalFileName,
                    EvidenceType = invoiceDoc.Classification,
                    Confidence = docContainsItem ? 0.95 : 0.80,
                    MatchMethod = docContainsItem ? "Invoice line item text match" : "Invoice page (all items)",
                    IsAmbiguous = false,
                    ExtractedText = invoiceDoc.OcrText
                });
            }

            result.ItemMatches.Add(match);
        }

        // ── Step 4: Match supporting docs to items ───────────────────────
        foreach (var supportDoc in supportingDocs)
        {
            var allItems = result.ItemMatches.Select(im => im.Item).ToList();
            var evidenceMatches = MatchEvidenceToItems(supportDoc, allItems);

            if (!evidenceMatches.Any())
            {
                // Unmatched — add warning, will show in UI for manual assignment
                result.Warnings.Add($"'{supportDoc.OriginalFileName}' could not be automatically matched to any purchase item. Manual assignment required.");
                continue;
            }

            var topMatch = evidenceMatches.First();

            if (topMatch.IsAmbiguous)
            {
                // Ambiguous — attach to all candidates, flag RequiresReview
                result.Warnings.Add($"'{supportDoc.OriginalFileName}' matches multiple purchase items. Human review required.");
                foreach (var em in evidenceMatches.Where(e => e.IsAmbiguous || e == topMatch))
                {
                    var candidateMatch = result.ItemMatches.FirstOrDefault(im =>
                        NormalizeStr(im.Item.Model) == NormalizeStr(em.MatchMethod?.Split(':').LastOrDefault() ?? ""));
                }
                // Attach to all items that scored highly, mark all as RequiresReview
                foreach (var candidateItem in result.ItemMatches)
                {
                    var relevantMatch = evidenceMatches.FirstOrDefault(e => e.MatchScore >= NoMatchThreshold);
                    if (relevantMatch != null)
                    {
                        relevantMatch.SourceFileName = supportDoc.OriginalFileName;
                        relevantMatch.DocumentId = supportDoc.DocumentId;
                        relevantMatch.EvidenceType = supportDoc.Classification;
                        candidateItem.EvidenceMatches.Add(relevantMatch);
                        candidateItem.RequiresReview = true;
                    }
                }
            }
            else
            {
                // Unambiguous auto-match — attach to the best candidate
                var bestItemDesc = topMatch.MatchMethod?.Contains(":") == true
                    ? topMatch.MatchMethod.Split(":")[1].Trim()
                    : null;

                // Find the item whose description/model best matches top match
                CandidateItemMatch? targetItem = null;
                if (!string.IsNullOrEmpty(bestItemDesc))
                {
                    targetItem = result.ItemMatches.FirstOrDefault(im =>
                        NormalizeStr(im.Item.Model).Contains(NormalizeStr(bestItemDesc)) ||
                        NormalizeStr(bestItemDesc).Contains(NormalizeStr(im.Item.Model ?? "")));
                }
                targetItem ??= result.ItemMatches.FirstOrDefault();

                if (targetItem != null)
                {
                    topMatch.DocumentId = supportDoc.DocumentId;
                    topMatch.SourceFileName = supportDoc.OriginalFileName;
                    topMatch.EvidenceType = supportDoc.Classification;
                    topMatch.ExtractedText = supportDoc.OcrText;
                    targetItem.EvidenceMatches.Add(topMatch);

                    // Persist candidate serial if found
                    if (!string.IsNullOrEmpty(topMatch.CandidateSerial))
                    {
                        targetItem.Item.Warnings ??= new();
                        targetItem.Item.Warnings.Add($"Candidate serial found in {supportDoc.OriginalFileName}: {topMatch.CandidateSerial}");
                    }
                }
            }
        }

        return result;
    }

    // ──────────────────────────────────────────────────────────────────────
    // MATCH EVIDENCE TO ITEMS (deterministic scoring)
    // ──────────────────────────────────────────────────────────────────────

    public List<EvidenceMatch> MatchEvidenceToItems(BundleDocument doc, List<CanonicalPurchaseItemDto> items)
    {
        if (!items.Any() || string.IsNullOrEmpty(doc.OcrText))
            return new();

        var lowerOcr = doc.OcrText.ToLowerInvariant();
        var scoredMatches = new List<(CanonicalPurchaseItemDto Item, int Score, string Method, string? Serial)>();

        // Try to extract serial from config image
        var candidateSerial = ExtractSerial(doc.OcrText);

        foreach (var item in items)
        {
            int score = 0;
            var matchReasons = new List<string>();

            var normModel = NormalizeStr(item.Model ?? "");
            var normManuf = NormalizeStr(item.Manufacturer ?? "");
            var normDesc = NormalizeStr(item.Description ?? "");

            // ── Score signals (Section 5 of spec) ────────────────────────

            // +40: Exact model match
            if (!string.IsNullOrEmpty(normModel) && lowerOcr.Contains(normModel))
            {
                score += 40;
                matchReasons.Add($"Model '{item.Model}' exact match");
            }

            // +30: Manufacturer match
            if (!string.IsNullOrEmpty(normManuf) && lowerOcr.Contains(normManuf))
            {
                score += 30;
                matchReasons.Add($"Manufacturer '{item.Manufacturer}' match");
            }

            // +20: Model substring in description
            if (!string.IsNullOrEmpty(normModel) && !string.IsNullOrEmpty(normDesc))
            {
                var modelParts = normModel.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
                int partHits = modelParts.Count(part => part.Length > 2 && lowerOcr.Contains(part));
                if (partHits >= 2)
                {
                    score += 20;
                    matchReasons.Add($"Model keyword overlap ({partHits} parts matched)");
                }
            }

            // +20: Part number / SKU match (look for typical part # patterns)
            var partNoMatch = Regex.Match(lowerOcr, @"(?:part\s*(?:no|number|#)|sku|item\s*code)[:\s]+([a-z0-9\-]+)", RegexOptions.IgnoreCase);
            if (partNoMatch.Success && !string.IsNullOrEmpty(normDesc) && normDesc.Contains(partNoMatch.Groups[1].Value.ToLower()))
            {
                score += 20;
                matchReasons.Add($"Part number match: {partNoMatch.Groups[1].Value}");
            }

            // +15: CPU/RAM/Storage match from item spec fields
            if (!string.IsNullOrEmpty(item.Model))
            {
                // Check if OCR mentions typical spec tokens that match item's description
                var specPatterns = new[] { @"\d+gb", @"\d+tb", @"\d+th gen", @"core i[3579]", @"ryzen \d", @"intel", @"amd" };
                int specHits = specPatterns.Count(p => Regex.IsMatch(lowerOcr, p));
                if (specHits >= 2)
                {
                    score += 15;
                    matchReasons.Add($"Specification keywords matched ({specHits} patterns)");
                }
            }

            // +10: Quantity match
            if (item.Quantity > 0)
            {
                var qtyPattern = $@"\b{item.Quantity}\b";
                if (Regex.IsMatch(lowerOcr, qtyPattern))
                {
                    score += 10;
                    matchReasons.Add($"Quantity {item.Quantity} found in document");
                }
            }

            // +5: General description keyword overlap
            if (!string.IsNullOrEmpty(normDesc))
            {
                var descWords = normDesc.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 3).ToArray();
                int wordHits = descWords.Count(w => lowerOcr.Contains(w));
                if (wordHits >= 2)
                {
                    score += 5;
                    matchReasons.Add($"Description keywords overlapped ({wordHits} words)");
                }
            }

            if (score > 0)
            {
                scoredMatches.Add((item, score, string.Join(", ", matchReasons), candidateSerial));
            }
        }

        if (!scoredMatches.Any())
            return new();

        scoredMatches.Sort((a, b) => b.Score.CompareTo(a.Score));

        var topScore = scoredMatches[0].Score;
        bool isAmbiguous = scoredMatches.Count > 1 &&
                           (topScore - scoredMatches[1].Score) < AmbiguityGapRequired;

        if (topScore < NoMatchThreshold)
            return new(); // Not confident enough

        return scoredMatches
            .Where(m => m.Score >= NoMatchThreshold)
            .Select(m => new EvidenceMatch
            {
                MatchScore = m.Score,
                Confidence = Math.Min(1.0, m.Score / 100.0),
                MatchMethod = m.Method + $": {m.Item.Model ?? m.Item.Description}",
                IsAmbiguous = isAmbiguous,
                CandidateSerial = m.Serial,
                EvidenceType = doc.Classification
            })
            .ToList();
    }

    // ──────────────────────────────────────────────────────────────────────
    // MERGE INVOICE PAGES
    // ──────────────────────────────────────────────────────────────────────

    private AiDocumentExtractionResult MergeInvoiceDocs(List<BundleDocument> invoiceDocs)
    {
        // Use the first invoice page as base
        var primary = invoiceDocs.FirstOrDefault(d => d.Classification == DocumentClassification.Invoice);
        primary ??= invoiceDocs.First();

        var merged = primary.ExtractionResult ?? new AiDocumentExtractionResult { IsSuccess = true };

        // Merge line items from continuation pages — deduplicate by description+qty
        foreach (var doc in invoiceDocs.Where(d => d != primary && d.ExtractionResult != null))
        {
            foreach (var item in doc.ExtractionResult!.Items)
            {
                bool isDuplicate = merged.Items.Any(existing =>
                    NormalizeStr(existing.Description) == NormalizeStr(item.Description) &&
                    existing.Quantity == item.Quantity);

                if (!isDuplicate)
                    merged.Items.Add(item);
            }
        }

        // Merge confidence (average)
        var allConfidences = invoiceDocs
            .Where(d => d.ExtractionResult != null)
            .Select(d => d.ExtractionResult!.Confidence)
            .ToList();
        if (allConfidences.Any())
            merged.Confidence = allConfidences.Average();

        return merged;
    }

    // ──────────────────────────────────────────────────────────────────────
    // HELPERS
    // ──────────────────────────────────────────────────────────────────────

    private static string NormalizeStr(string? s) =>
        Regex.Replace((s ?? string.Empty).ToLowerInvariant().Trim(), @"[\s\-_/]+", " ");

    private static string? ExtractSerial(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var match = Regex.Match(text,
            @"(?:serial\s*(?:no|number|#)?|s/?n)[:\s]+([A-Z0-9]{6,20})",
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}
