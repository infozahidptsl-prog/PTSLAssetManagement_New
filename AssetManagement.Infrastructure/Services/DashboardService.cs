using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AssetManagement.Core.Entities;
using AssetManagement.Core.Enums;
using AssetManagement.Core.Interfaces;
using AssetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardKpiOverview> GetOverviewAsync()
    {
        var total = await _context.Devices.CountAsync();
        var issued = await _context.Devices.CountAsync(d => d.Status == DeviceStatus.InUse);
        var available = await _context.Devices.CountAsync(d => d.Status == DeviceStatus.InStore);
        var attention = await _context.AutomationFindings.CountAsync(f => f.Status == FindingStatus.Open);
        
        return new DashboardKpiOverview
        {
            TotalAssets = total,
            UtilizationPercentage = total == 0 ? 0 : Math.Round(((double)issued / total) * 100, 1),
            AvailableAssets = available,
            NeedsAttentionCount = attention
        };
    }

    public async Task<AssetStatusCounts> GetAssetStatusAsync()
    {
        var statuses = await _context.Devices
            .GroupBy(d => d.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return new AssetStatusCounts
        {
            InUse = statuses.FirstOrDefault(s => s.Status == DeviceStatus.InUse)?.Count ?? 0,
            InStore = statuses.FirstOrDefault(s => s.Status == DeviceStatus.InStore)?.Count ?? 0,
            Returned = statuses.FirstOrDefault(s => s.Status == DeviceStatus.Returned)?.Count ?? 0,
            Faulty = statuses.FirstOrDefault(s => s.Status == DeviceStatus.Faulty)?.Count ?? 0,
            Scrapped = statuses.FirstOrDefault(s => s.Status == DeviceStatus.WrittenOff)?.Count ?? 0
        };
    }

    public async Task<List<CategoryCount>> GetCategoryBreakdownAsync()
    {
        return await _context.Devices
            .Where(d => !string.IsNullOrEmpty(d.Category))
            .GroupBy(d => d.Category!)
            .Select(g => new CategoryCount { Category = g.Key, Count = g.Count() })
            .OrderByDescending(c => c.Count)
            .Take(10)
            .ToListAsync();
    }

    public async Task<UtilizationMetrics> GetUtilizationAsync()
    {
        var total = await _context.Devices.CountAsync();
        var inUse = await _context.Devices.CountAsync(d => d.Status == DeviceStatus.InUse);
        var inStore = await _context.Devices.CountAsync(d => d.Status == DeviceStatus.InStore);
        var returned = await _context.Devices.CountAsync(d => d.Status == DeviceStatus.Returned);

        return new UtilizationMetrics
        {
            Percentage = total == 0 ? 0 : Math.Round(((double)inUse / total) * 100, 1),
            Assigned = inUse,
            Available = inStore,
            Returned = returned,
            Other = total - inUse - inStore - returned
        };
    }

    public async Task<List<ActivityTrendData>> GetActivityTrendAsync(int months = 6)
    {
        var startDate = DateTime.UtcNow.Date.AddMonths(-months + 1);
        startDate = new DateTime(startDate.Year, startDate.Month, 1);

        var data = new List<ActivityTrendData>();
        
        // This is a naive client-side evaluation due to complex date string parsing from DB
        var handovers = await _context.Handovers.ToListAsync();
        var repairs = await _context.Repairs.ToListAsync();
        
        for (int i = 0; i < months; i++)
        {
            var monthStart = startDate.AddMonths(i);
            var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
            var monthName = monthStart.ToString("MMM yyyy");
            
            var monthHandovers = handovers.Where(h => {
                if (string.IsNullOrEmpty(h.Released)) return false;
                if (DateTime.TryParse(h.Released, out var date)) return date.Year == monthStart.Year && date.Month == monthStart.Month;
                return false;
            }).ToList();
                
            var issues = monthHandovers.Count(h => string.IsNullOrEmpty(h.From) || h.From.Equals("Store", StringComparison.OrdinalIgnoreCase) || h.From.Equals("Store/IT", StringComparison.OrdinalIgnoreCase) || h.From.Equals("HR/Store", StringComparison.OrdinalIgnoreCase));
            var returns = monthHandovers.Count(h => !string.IsNullOrEmpty(h.To) && (h.To.Equals("Store", StringComparison.OrdinalIgnoreCase) || h.To.Equals("Store/IT", StringComparison.OrdinalIgnoreCase) || h.To.Equals("HR/Store", StringComparison.OrdinalIgnoreCase)));
            var transfers = monthHandovers.Count - issues - returns;
            if (transfers < 0) transfers = 0;
            
            var monthRepairs = repairs.Where(r => {
                if (string.IsNullOrEmpty(r.SentDate)) return false;
                if (DateTime.TryParse(r.SentDate, out var date)) return date.Year == monthStart.Year && date.Month == monthStart.Month;
                return false;
            }).Count();
                
            data.Add(new ActivityTrendData
            {
                Month = monthName,
                Issues = issues,
                Returns = returns,
                Transfers = transfers,
                Repairs = monthRepairs
            });
        }
        
        return data;
    }

    public async Task<LifecycleRiskMetrics> GetLifecycleRiskAsync()
    {
        var devices = await _context.Devices.ToListAsync(); // Because ProcurementDate is sometimes string, we do it in-memory
        var now = DateTime.UtcNow;
        var in30Days = now.AddDays(30);
        var in90Days = now.AddDays(90);
        
        int expired = 0;
        int exp30 = 0;
        int exp90 = 0;
        
        foreach (var d in devices)
        {
            if (d.HasWarranty && d.ProcurementDate.HasValue && d.WarrantyMonths.HasValue && d.WarrantyMonths.Value > 0)
            {
                var endDate = d.ProcurementDate.Value.AddMonths(d.WarrantyMonths.Value);
                if (endDate < now) expired++;
                else if (endDate <= in30Days) exp30++;
                else if (endDate <= in90Days) exp90++;
            }
        }
        
        var faulty = await _context.Devices.CountAsync(d => d.Status == DeviceStatus.Faulty);
        var scrapped = await _context.Devices.CountAsync(d => d.Status == DeviceStatus.WrittenOff);

        return new LifecycleRiskMetrics
        {
            WarrantyExpired = expired,
            WarrantyExpiring30Days = exp30,
            WarrantyExpiring90Days = exp90,
            Faulty = faulty,
            Scrapped = scrapped
        };
    }

    public async Task<RepairTrendMetrics> GetRepairTrendAsync()
    {
        var repairs = await _context.Repairs.ToListAsync();
        var open = repairs.Count(r => r.Status != "Completed" && r.Status != "Returned");
        var completed = repairs.Count(r => r.Status == "Completed" || r.Status == "Returned");
        var faulty = await _context.Devices.CountAsync(d => d.Status == DeviceStatus.Faulty);

        return new RepairTrendMetrics
        {
            OpenRepairs = open,
            CompletedRepairs = completed,
            FaultyAssets = faulty
        };
    }

    public async Task<List<GroupedAttentionFinding>> GetNeedsAttentionGroupedAsync()
    {
        var findings = await _context.AutomationFindings
            .Where(f => f.Status == FindingStatus.Open)
            .GroupBy(f => f.RuleType)
            .Select(g => new
            {
                RuleType = g.Key,
                Count = g.Count(),
                HighestSeverity = g.Max(f => f.Severity),
                SampleTitle = g.OrderByDescending(f => f.CreatedAt).FirstOrDefault()!.Title
            })
            .OrderByDescending(g => g.HighestSeverity)
            .ThenByDescending(g => g.Count)
            .Take(5)
            .ToListAsync();
            
        return findings.Select(f => new GroupedAttentionFinding
        {
            RuleType = f.RuleType.ToString(),
            Count = f.Count,
            HighestSeverity = f.HighestSeverity,
            Title = GetFriendlyTitleForRule(f.RuleType.ToString())
        }).ToList();
    }
    
    private string GetFriendlyTitleForRule(string ruleType)
    {
        return ruleType switch
        {
            "DuplicateSerial" => "Duplicate Serial Numbers",
            "MissingWarranty" => "Missing Warranty Info",
            "UnassignedActiveDevice" => "Unassigned Devices in Active State",
            "InactiveEmployeeWithDevices" => "Inactive Employees with Devices",
            _ => ruleType
        };
    }
}
