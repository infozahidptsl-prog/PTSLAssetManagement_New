using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AssetManagement.Core.Enums;

namespace AssetManagement.Core.Entities
{
    public class AutomationFinding
    {
        [Key]
        public int Id { get; set; }

        public AutomationRuleType RuleType { get; set; }
        public FindingSeverity Severity { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? RecommendedAction { get; set; }

        public string? DeviceId { get; set; }
        [ForeignKey("DeviceId")]
        public virtual Device? Device { get; set; }

        public string? EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        public string? PurchaseId { get; set; }
        [ForeignKey("PurchaseId")]
        public virtual Purchase? Purchase { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        public FindingStatus Status { get; set; } = FindingStatus.Open;

        public DateTime? ResolvedAt { get; set; }
        public string? ResolvedBy { get; set; }
        public string? ResolutionNote { get; set; }

        public DateTime? DismissedAt { get; set; }
        public string? DismissedBy { get; set; }
        public string? DismissalReason { get; set; }

        // A unique hash or identifier to prevent duplicating the exact same finding while open
        [MaxLength(255)]
        public string? FindingHash { get; set; }
    }
}
