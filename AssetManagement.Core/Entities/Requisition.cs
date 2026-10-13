using System;

namespace AssetManagement.Core.Entities;

public class Requisition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Auto-generated human-readable ID, e.g. REQ-001</summary>
    public string RequisitionNumber { get; set; } = string.Empty;

    // Who requested
    public string RequestedByName { get; set; } = string.Empty;
    public string? RequestedByEmployeeId { get; set; }

    // What they want
    public string AssetCategory { get; set; } = string.Empty;  // System, Component, Peripheral
    public string AssetType { get; set; } = string.Empty;
    public string? AssetSpecification { get; set; }
    public int Quantity { get; set; } = 1;
    public string Priority { get; set; } = "Normal";  // High, Normal, Low
    public string? JustificationNote { get; set; }

    // Approval
    public string Status { get; set; } = "Pending";   // Pending, Approved, Rejected
    public string? ProjectManagerName { get; set; }    // Who approved
    public string? ApprovedByName { get; set; }
    public string? ApprovalNote { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Dates
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RequisitionReceivedDate { get; set; }   // Date PM received the requisition

    // Attachment (email screenshot or image)
    public string? AttachmentPath { get; set; }         // Server path to stored file
    public string? AttachmentOriginalName { get; set; } // Original filename for display

    // Link to purchase
    public string? PurchasedDeviceId { get; set; }
}
