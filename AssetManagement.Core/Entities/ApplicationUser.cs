using Microsoft.AspNetCore.Identity;

namespace AssetManagement.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string? EmployeeId { get; set; }
    public string? FullName { get; set; }
}
