using System.ComponentModel.DataAnnotations;

namespace AssetManagement.Core.Entities
{
    public class AutomationSettings
    {
        [Key]
        public int Id { get; set; }

        public int AgingDeviceThresholdMonths { get; set; } = 36; // 3 years default
        public int WarrantyExpiringWarningDays { get; set; } = 90; // 90 days default
        
        // Add more thresholds as needed
    }
}
