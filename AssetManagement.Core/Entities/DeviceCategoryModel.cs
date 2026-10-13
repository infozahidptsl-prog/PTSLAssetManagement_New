namespace AssetManagement.Core.Entities;

public class DeviceCategoryModel
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "System"; // e.g. System, Component, Peripheral
}