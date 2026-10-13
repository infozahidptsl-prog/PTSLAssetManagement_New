namespace AssetManagement.Core.Entities
{
    public class OwnerCompany
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Prefix { get; set; }
    }
}
