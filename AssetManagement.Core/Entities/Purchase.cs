using System;
using System.Collections.Generic;

namespace AssetManagement.Core.Entities;

public class Purchase
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    // Stable identifier for QR code, e.g. "PUR-1001"
    public string PurchaseNumber { get; set; } = string.Empty;
    
    public string Supplier { get; set; } = string.Empty;
    public string? InvoiceNumber { get; set; }
    public DateTime? PurchaseDate { get; set; }
    
    // Requisition link if it came from a request
    public string? RequisitionId { get; set; }
    public Requisition? Requisition { get; set; }
    
    public string? InvoiceFilePath { get; set; }
    public string? Notes { get; set; }

    public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
}
