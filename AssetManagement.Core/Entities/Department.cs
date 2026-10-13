namespace AssetManagement.Core.Entities;

public class Department
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
}
