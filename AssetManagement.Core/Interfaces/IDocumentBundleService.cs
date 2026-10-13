using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AssetManagement.Core.Interfaces;

// ──────────────────────────────────────────────────────────────────────────
// DOCUMENT CLASSIFICATION
// ──────────────────────────────────────────────────────────────────────────

public enum DocumentClassification
{
    Unknown,
    Invoice,
    InvoiceContinuation,
    Configuration,
    Specification,
    Warranty,
    DeliveryNote,
    Other
}

// ──────────────────────────────────────────────────────────────────────────
// BUNDLE — in-memory, NOT persisted to DB
// ──────────────────────────────────────────────────────────────────────────

/// <summary>One capture session containing all uploaded files.</summary>
public class DocumentBundle
{
    public string BundleId { get; set; } = Guid.NewGuid().ToString();
    public List<BundleDocument> Documents { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>One uploaded file/page within a bundle, after OCR and classification.</summary>
public class BundleDocument
{
    public string DocumentId { get; set; } = Guid.NewGuid().ToString();
    public string OriginalFileName { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public DocumentClassification Classification { get; set; } = DocumentClassification.Unknown;
    public string? ClassificationReason { get; set; }

    public string? OcrText { get; set; }
    public double OcrConfidence { get; set; }

    /// <summary>Full structured AI/OCR extraction result for this document.</summary>
    public AiDocumentExtractionResult? ExtractionResult { get; set; }

    // Processing status for the UI pipeline display
    public string OcrStatus { get; set; } = "Pending";
    public string AiStatus { get; set; } = "Pending";
    public string MatchStatus { get; set; } = "Pending";
    public string? ProcessingError { get; set; }
}

// ──────────────────────────────────────────────────────────────────────────
// EVIDENCE MATCH — result of matching a BundleDocument to a PurchaseItem
// ──────────────────────────────────────────────────────────────────────────

/// <summary>
/// Records WHY a document was matched to a specific PurchaseItem candidate.
/// Follows the auditability requirement (Section 8 of spec).
/// </summary>
public class EvidenceMatch
{
    public string DocumentId { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;
    public DocumentClassification EvidenceType { get; set; }

    /// <summary>0–1 confidence in this match.</summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Human-readable match reason, e.g.
    /// "Manufacturer + Model exact match", "Serial number exact match",
    /// "Invoice line description + normalized model match"
    /// </summary>
    public string? MatchMethod { get; set; }

    /// <summary>True when two or more items scored within 15 pts of each other.</summary>
    public bool IsAmbiguous { get; set; }

    /// <summary>Serial found in config image. NOT used to create Device automatically.</summary>
    public string? CandidateSerial { get; set; }

    public string? ExtractedText { get; set; }

    /// <summary>Raw matching score for debugging.</summary>
    public int MatchScore { get; set; }
}

// ──────────────────────────────────────────────────────────────────────────
// CANDIDATE ITEM MATCH — one purchase line item + its evidence
// ──────────────────────────────────────────────────────────────────────────

public class CandidateItemMatch
{
    public CanonicalPurchaseItemDto Item { get; set; } = new();
    public List<EvidenceMatch> EvidenceMatches { get; set; } = new();

    /// <summary>True when any evidence match is ambiguous or AI confidence is low.</summary>
    public bool RequiresReview { get; set; }
}

// ──────────────────────────────────────────────────────────────────────────
// BUNDLE PROCESSING RESULT — output of ProcessBundleAsync
// ──────────────────────────────────────────────────────────────────────────

public class BundleProcessingResult
{
    public DocumentBundle Bundle { get; set; } = new();

    /// <summary>Merged, deduplicated purchase extracted from all invoice documents.</summary>
    public CanonicalPurchaseDto? MergedPurchase { get; set; }
    public MappingValidationResult<CanonicalPurchaseDto>? MappingResult { get; set; }

    /// <summary>Per-item candidates, each with their evidence attachments.</summary>
    public List<CandidateItemMatch> ItemMatches { get; set; } = new();

    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();

    public bool HasAmbiguousMatches => ItemMatches.Any(m => m.RequiresReview);
    public bool IsDuplicate { get; set; }
    public string? ExistingPurchaseId { get; set; }
}

// ──────────────────────────────────────────────────────────────────────────
// SERVICE INTERFACE
// ──────────────────────────────────────────────────────────────────────────

public interface IDocumentBundleService
{
    /// <summary>
    /// Classify a single document given its OCR text.
    /// Classification is deterministic (keyword-based), not hallucinated AI.
    /// </summary>
    BundleDocument ClassifyDocument(string fileName, int pageNumber, string? ocrText, AiDocumentExtractionResult? extractionResult);

    /// <summary>
    /// Process an entire DocumentBundle:
    /// 1. Merge invoice pages into one CanonicalPurchaseDto
    /// 2. Match configuration/specification images to PurchaseItem candidates
    /// 3. Flag ambiguous matches for human review
    /// </summary>
    Task<BundleProcessingResult> ProcessBundleAsync(DocumentBundle bundle);

    /// <summary>
    /// Deterministic scoring-based evidence matching.
    /// Returns matches sorted by score descending.
    /// Never silently makes potentially incorrect associations.
    /// </summary>
    List<EvidenceMatch> MatchEvidenceToItems(BundleDocument doc, List<CanonicalPurchaseItemDto> items);
}
