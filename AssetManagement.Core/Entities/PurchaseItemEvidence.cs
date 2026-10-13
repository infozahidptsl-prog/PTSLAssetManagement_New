using System;

namespace AssetManagement.Core.Entities;

/// <summary>
/// Persists the relationship between a PurchaseItem and the source
/// document/image that provided evidence for it (invoice page,
/// configuration image, warranty document, etc.).
/// </summary>
public class PurchaseItemEvidence
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string PurchaseItemId { get; set; } = string.Empty;
    public PurchaseItem? PurchaseItem { get; set; }

    /// <summary>Original uploaded filename (e.g. "Invoice_Page_1.jpg")</summary>
    public string SourceFileName { get; set; } = string.Empty;

    /// <summary>Invoice | Configuration | Specification | Warranty | DeliveryNote | Other</summary>
    public string EvidenceType { get; set; } = string.Empty;

    /// <summary>Full OCR text extracted from this document.</summary>
    public string? OcrText { get; set; }

    /// <summary>JSON snapshot of key fields extracted from this document.</summary>
    public string? ExtractedFields { get; set; }

    /// <summary>Matching confidence 0-1.</summary>
    public double Confidence { get; set; }

    /// <summary>Human-readable record of WHY this evidence was matched.
    /// E.g. "Manufacturer + Model exact match", "Serial number match"</summary>
    public string? MatchMethod { get; set; }

    /// <summary>True when automatic matching was ambiguous and user must confirm.</summary>
    public bool RequiresReview { get; set; }

    /// <summary>
    /// Serial number found in a configuration/specification image.
    /// NOT used to auto-create a Device - only surfaced during /device-register.
    /// </summary>
    public string? CandidateSerial { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
