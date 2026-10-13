using System;
using System.Collections.Generic;

namespace AssetManagement.Core.Entities;

public class PurchaseItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string PurchaseId { get; set; } = string.Empty;
    public Purchase? Purchase { get; set; }

    public string Manufacturer { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    
    public decimal? UnitCost { get; set; }
    public int? Quantity { get; set; }
    
    // Default properties for the physical assets
    public string? Processor { get; set; }
    public string? Ram { get; set; }
    public string? Ssd { get; set; }
    public int? WarrantyMonths { get; set; }
    public string OwnerCompany { get; set; } = "PrimeTech Solutions Ltd.";

    /// <summary>HDD size (from config image or manual entry).</summary>
    public string? Hdd { get; set; }

    /// <summary>Candidate serial extracted from a config/spec image.
    /// NOT used to create a Device automatically — only pre-fills /device-register.</summary>
    public string? CandidateSerial { get; set; }

    /// <summary>True when this item came from a low-confidence AI/OCR extraction.</summary>
    public bool IsLowConfidence { get; set; }
    
    // Track how many physical devices have been registered against this line item
    public int RegisteredQuantity { get; set; } = 0;
    public int RemainingQuantity => (Quantity ?? 0) - RegisteredQuantity;
    
    // Devices created from this item
    public ICollection<Device> Devices { get; set; } = new List<Device>();

    // Evidence documents linked to this item
    public ICollection<PurchaseItemEvidence> Evidences { get; set; } = new List<PurchaseItemEvidence>();
}
