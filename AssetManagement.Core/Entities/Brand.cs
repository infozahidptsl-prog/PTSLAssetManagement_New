namespace AssetManagement.Core.Entities;

public class Brand
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
}