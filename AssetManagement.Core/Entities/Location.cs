namespace AssetManagement.Core.Entities;

public class Location
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
}
