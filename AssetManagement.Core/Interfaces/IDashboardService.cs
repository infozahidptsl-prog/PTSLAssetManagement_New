using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AssetManagement.Core.Enums;

namespace AssetManagement.Core.Interfaces;

public class DashboardKpiOverview
{
    public int TotalAssets { get; set; }
    public double UtilizationPercentage { get; set; }
    public int AvailableAssets { get; set; }
    public int NeedsAttentionCount { get; set; }
}

public class AssetStatusCounts
{
    public int InUse { get; set; }
    public int InStore { get; set; }
    public int Returned { get; set; }
    public int Faulty { get; set; }
    public int Scrapped { get; set; }
}

public class CategoryCount
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class UtilizationMetrics
{
    public double Percentage { get; set; }
    public int Assigned { get; set; }
    public int Available { get; set; }
    public int Returned { get; set; }
    public int Other { get; set; }
}

public class LifecycleRiskMetrics
{
    public int WarrantyExpired { get; set; }
    public int WarrantyExpiring30Days { get; set; }
    public int WarrantyExpiring90Days { get; set; }
    public int Faulty { get; set; }
    public int Scrapped { get; set; }
}

public class RepairTrendMetrics
{
    public int OpenRepairs { get; set; }
    public int CompletedRepairs { get; set; }
    public int FaultyAssets { get; set; }
}

public class ActivityTrendData
{
    public string Month { get; set; } = string.Empty;
    public int Issues { get; set; }
    public int Returns { get; set; }
    public int Transfers { get; set; }
    public int Repairs { get; set; }
}

public class GroupedAttentionFinding
{
    public string Title { get; set; } = string.Empty;
    public int Count { get; set; }
    public string RuleType { get; set; } = string.Empty;
    public FindingSeverity HighestSeverity { get; set; }
}

public interface IDashboardService
{
    Task<DashboardKpiOverview> GetOverviewAsync();
    Task<AssetStatusCounts> GetAssetStatusAsync();
    Task<List<CategoryCount>> GetCategoryBreakdownAsync();
    Task<UtilizationMetrics> GetUtilizationAsync();
    Task<List<ActivityTrendData>> GetActivityTrendAsync(int months = 6);
    Task<LifecycleRiskMetrics> GetLifecycleRiskAsync();
    Task<RepairTrendMetrics> GetRepairTrendAsync();
    Task<List<GroupedAttentionFinding>> GetNeedsAttentionGroupedAsync();
}
