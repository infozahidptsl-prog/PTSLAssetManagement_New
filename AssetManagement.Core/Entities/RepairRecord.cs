using System;

namespace AssetManagement.Core.Entities;

public class RepairRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DeviceId { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public string IssueDescription { get; set; } = string.Empty;
    public string SentDate { get; set; } = string.Empty;
    public string? ExpectedReturnDate { get; set; }
    public string? ActualReturnDate { get; set; }
    public string Status { get; set; } = "In Repair";
    public decimal? Cost { get; set; }
    public string? Remarks { get; set; }

    public Device? Device { get; set; }
}
