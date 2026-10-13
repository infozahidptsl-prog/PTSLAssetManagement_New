using System.Collections.Generic;

namespace AssetManagement.Core.Entities;

public class Employee
{
    public string Id { get; set; } = string.Empty;
    public string? EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? Email { get; set; }
    public string? Contact { get; set; }
    public string? Department { get; set; }
    public DateTime? JoiningDate { get; set; }
    public string? Location { get; set; }
    public string? Team { get; set; }
    public string? Manager { get; set; }
    public string? BloodGroup { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? About { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsTeamAccount { get; set; } = false;
    
    // Non-mapped or handled differently since we compute this from Devices in React
    public List<string> DeviceIds { get; set; } = new();
    public int ActiveCount { get; set; }
    
    // QR Token Security
    public string? QrTokenHash { get; set; }
    public DateTime? QrTokenCreatedAt { get; set; }
    public DateTime? QrTokenRevokedAt { get; set; }
    public int QrTokenVersion { get; set; } = 1;
}
