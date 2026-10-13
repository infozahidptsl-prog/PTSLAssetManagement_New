namespace AssetManagement.Core.Entities
{
    public class Team
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        
        // Mapping to Project Manager
        public int? ProjectManagerId { get; set; }
        public ProjectManager? ProjectManager { get; set; }
    }
}
