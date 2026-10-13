namespace AssetManagement.Core.Entities;

public class Handover
{
    public string Id { get; set; } = string.Empty;
    
    public string DeviceId { get; set; } = string.Empty;
    public Device Device { get; set; } = null!;
    
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    
    public string? Released { get; set; }
    public string? ReturnDate { get; set; }
    public string? Condition { get; set; }
}
