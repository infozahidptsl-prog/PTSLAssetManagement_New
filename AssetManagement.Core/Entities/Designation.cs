namespace AssetManagement.Core.Entities;

public class Designation
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
}