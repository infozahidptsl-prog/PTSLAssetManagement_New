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

public class DepartmentAssetStat
{
    public string Department { get; set; } = string.Empty;
    public int Deployed { get; set; }
    public int InStore { get; set; }
}

public class MonthlyFlowData
{
    public string Month { get; set; } = string.Empty;
    public int InUse { get; set; }
    public int ReserveStock { get; set; }
    public int Acquisition { get => InUse; set => InUse = value; }
    public int Disposals { get => ReserveStock; set => ReserveStock = value; }
}

public class DepartmentDeploymentStat
{
    public string Department { get; set; } = string.Empty;
    public string Category { get => Department; set => Department = value; }
    public int Active { get; set; }
    public int InStore { get; set; }
    public int Maintenance { get; set; }
    public int Total => Active + InStore + Maintenance;
}

public class ExecutiveDashboardExtras
{
    public decimal TotalBookValue { get; set; }
    public double MomGrowthPercentage { get; set; } = 4.0;
    public int PendingApprovalsCount { get; set; }
    public int DuplicateSerialsCount { get; set; }
    public int FleetHealthScore { get; set; } = 87;
    public int HealthyCount { get; set; }
    public int AtRiskCount { get; set; }
    public int ObsoleteCount { get; set; }
    public List<DepartmentAssetStat> DepartmentStats { get; set; } = new();
    public List<MonthlyFlowData> MonthlyFlows { get; set; } = new();
    public List<DepartmentDeploymentStat> DepartmentDeployments { get; set; } = new();
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
    Task<ExecutiveDashboardExtras> GetExecutiveExtrasAsync();
}

