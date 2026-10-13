using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AssetManagement.Core.Entities;
using AssetManagement.Core.Enums;

namespace AssetManagement.Core.Interfaces
{
    public interface IAutomationService
    {
        Task RunScanAsync();
        Task<List<AutomationFinding>> GetFindingsAsync(FindingStatus? status = null, FindingSeverity? severity = null, AutomationRuleType? ruleType = null, string searchTerm = "");
        Task<AutomationFinding?> GetFindingByIdAsync(int id);
        Task<bool> ResolveFindingAsync(int id, string resolvedBy, string? resolutionNote);
        Task<bool> DismissFindingAsync(int id, string dismissedBy, string dismissalReason);
        Task<AutomationSettings> GetSettingsAsync();
        Task UpdateSettingsAsync(AutomationSettings settings);
        
        Task<int> GetOpenFindingCountAsync();
        Task<int> GetCriticalFindingCountAsync();
        Task<int> GetWarningFindingCountAsync();
        Task<int> GetInfoFindingCountAsync();
        Task<List<AutomationFinding>> GetDashboardFindingsAsync(int top = 5);
    }
}
