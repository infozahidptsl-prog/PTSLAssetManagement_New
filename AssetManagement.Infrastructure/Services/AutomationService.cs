using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AssetManagement.Core.Entities;
using AssetManagement.Core.Enums;
using AssetManagement.Core.Interfaces;
using AssetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Services
{
    public class AutomationService : IAutomationService
    {
        private readonly ApplicationDbContext _context;

        public AutomationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AutomationSettings> GetSettingsAsync()
        {
            var settings = await _context.AutomationSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new AutomationSettings();
                _context.AutomationSettings.Add(settings);
                await _context.SaveChangesAsync();
            }
            return settings;
        }

        public async Task UpdateSettingsAsync(AutomationSettings settings)
        {
            _context.AutomationSettings.Update(settings);
            await _context.SaveChangesAsync();
        }

        public async Task RunScanAsync()
        {
            var settings = await GetSettingsAsync();
            var newFindings = new List<AutomationFinding>();
            var now = DateTime.UtcNow;

            var activeDevices = await _context.Devices
                .AsNoTracking()
                .ToListAsync();
                
            var employeesById = await _context.Employees
                .AsNoTracking()
                .ToDictionaryAsync(e => e.Id);
                
            var employeesByCustomIdList = await _context.Employees
                .AsNoTracking()
                .Where(e => !string.IsNullOrWhiteSpace(e.EmployeeId))
                .ToListAsync();
            
            var employeesByCustomId = employeesByCustomIdList
                .GroupBy(e => e.EmployeeId)
                .ToDictionary(g => g.Key!, g => g.First());

            // Load hashes for active or explicitly ignored findings to prevent duplicates.
            // We EXCLUDE Resolved findings so that if a problem recurs (or was falsely resolved), it creates a new alert.
            var allFindings = await _context.AutomationFindings
                .AsNoTracking()
                .Where(f => f.Status == FindingStatus.Open || f.Status == FindingStatus.Dismissed)
                .Select(f => f.FindingHash)
                .Where(h => !string.IsNullOrEmpty(h))
                .ToListAsync();
                
            var existingHashes = new HashSet<string>(allFindings!);

            void AddFinding(AutomationRuleType ruleType, FindingSeverity severity, string title, string desc, string hash, string? recommendedAction = null, string? deviceId = null, string? customEmployeeId = null, string? purchaseId = null)
            {
                if (!existingHashes.Contains(hash))
                {
                    string? actualEmployeeId = null;
                    if (!string.IsNullOrEmpty(customEmployeeId) && employeesByCustomId.TryGetValue(customEmployeeId, out var emp))
                    {
                        actualEmployeeId = emp.Id;
                    }

                    newFindings.Add(new AutomationFinding
                    {
                        RuleType = ruleType,
                        Severity = severity,
                        Title = title,
                        Description = desc,
                        FindingHash = hash,
                        RecommendedAction = recommendedAction,
                        DeviceId = deviceId,
                        EmployeeId = actualEmployeeId,
                        PurchaseId = purchaseId,
                        CreatedAt = now,
                        Status = FindingStatus.Open
                    });
                    existingHashes.Add(hash); // prevent duplicates
                }
            }

            string? GetEmpName(string? customEmpId) => !string.IsNullOrEmpty(customEmpId) && employeesByCustomId.TryGetValue(customEmpId, out var emp) ? emp.Name : null;

            // RULE A - Dead / Failed Asset
            var deadStatuses = new[] { DeviceStatus.Faulty, DeviceStatus.WrittenOff, DeviceStatus.Scrapped };
            foreach (var device in activeDevices.Where(d => deadStatuses.Contains(d.Status)))
            {
                AddFinding(AutomationRuleType.DeadAsset, FindingSeverity.Critical, $"{device.Brand} — {device.Status}",
                    $"Asset {device.AssetTag} ({device.Serial}) is marked as {device.Status}.",
                    $"RULE_A_{device.Id}",
                    "Inspect asset / Send to repair / Scrap / Replace",
                    device.Id, device.EmployeeId);
            }

            // RULE B - Old / Aging Device
            foreach (var device in activeDevices.Where(d => d.ProcurementDate.HasValue))
            {
                var ageMonths = (now.Year - device.ProcurementDate!.Value.Year) * 12 + now.Month - device.ProcurementDate.Value.Month;
                if (ageMonths >= settings.AgingDeviceThresholdMonths)
                {
                    AddFinding(AutomationRuleType.AgingDevice, FindingSeverity.Warning, $"{device.Brand ?? "Unknown"} — Aging Device",
                        $"Asset {device.AssetTag} is {ageMonths / 12} years and {ageMonths % 12} months old.",
                        $"RULE_B_{device.Id}",
                        "Review replacement eligibility.",
                        device.Id, device.EmployeeId);
                }
            }

            // RULE C - Body Damage / Repair Required
            var repairStatuses = new[] { DeviceStatus.InRepair };
            foreach (var device in activeDevices.Where(d => (d.Condition != null && d.Condition.Contains("Damage")) || repairStatuses.Contains(d.Status)))
            {
                AddFinding(AutomationRuleType.DamageRepair, FindingSeverity.Warning, $"{device.Brand ?? "Asset"} — Repair Required",
                        $"Asset {device.AssetTag} is marked as {device.Status} or damaged.",
                        $"RULE_C_{device.Id}",
                        "Schedule repair or assess damage.",
                        device.Id, device.EmployeeId);
            }

            // RULE D - Warranty Expiring
            foreach (var device in activeDevices.Where(d => d.ProcurementDate.HasValue && d.HasWarranty && d.WarrantyMonths > 0))
            {
                var expiryDate = device.ProcurementDate!.Value.AddMonths(device.WarrantyMonths!.Value);
                var daysRemaining = (expiryDate - now).TotalDays;
                
                if (daysRemaining <= settings.WarrantyExpiringWarningDays && daysRemaining >= 0)
                {
                    AddFinding(AutomationRuleType.WarrantyExpiring, FindingSeverity.Warning, $"{device.Brand ?? "Asset"} — Warranty Expiring",
                        $"Warranty expires in {Math.Ceiling(daysRemaining)} days on {expiryDate:MMM dd, yyyy}.",
                        $"RULE_D_{device.Id}",
                        "Review for any necessary final claims.",
                        device.Id, device.EmployeeId);
                }
                else if (daysRemaining < 0 && daysRemaining > -365) // recently expired
                {
                    AddFinding(AutomationRuleType.WarrantyExpiring, FindingSeverity.Info, $"{device.Brand ?? "Asset"} — Warranty Expired",
                        $"Warranty expired {Math.Abs(Math.Ceiling(daysRemaining))} days ago.",
                        $"RULE_D_EXPIRED_{device.Id}",
                        "Update asset status or renew if applicable.",
                        device.Id, device.EmployeeId);
                }
            }

            // RULE E - Missing Asset Information
            foreach (var device in activeDevices)
            {
                var missingFields = new List<string>();
                if (string.IsNullOrWhiteSpace(device.Serial)) missingFields.Add("Serial");
                if (string.IsNullOrWhiteSpace(device.Brand)) missingFields.Add("Manufacturer");
                if (string.IsNullOrWhiteSpace(device.DeviceName)) missingFields.Add("Model");
                if (!device.ProcurementDate.HasValue) missingFields.Add("Purchase Date");

                if (missingFields.Any())
                {
                    AddFinding(AutomationRuleType.MissingInformation, FindingSeverity.Warning, $"{device.AssetTag ?? device.Id} — Missing Data",
                        $"Asset is missing: {string.Join(", ", missingFields)}.",
                        $"RULE_E_{device.Id}",
                        "Update asset details.",
                        device.Id, device.EmployeeId);
                }
            }

            // RULE F - Duplicate Serial
            var duplicateSerials = activeDevices
                .Where(d => !string.IsNullOrWhiteSpace(d.Serial))
                .GroupBy(d => d.Serial!)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in duplicateSerials)
            {
                var ids = string.Join(", ", group.Select(d => d.AssetTag ?? d.Id));
                foreach (var device in group)
                {
                    AddFinding(AutomationRuleType.DuplicateSerial, FindingSeverity.Critical, $"Duplicate Serial: {group.Key}",
                        $"Serial number {group.Key} is shared by assets: {ids}.",
                        $"RULE_F_{device.Id}",
                        "Investigate duplicate registration.",
                        device.Id, device.EmployeeId);
                }
            }

            // RULE G - Purchase / Registration Mismatch
            var purchaseItems = await _context.PurchaseItems
                .Include(p => p.Purchase)
                .AsNoTracking()
                .ToListAsync();

            foreach (var item in purchaseItems)
            {
                var registeredCount = activeDevices.Count(d => d.PurchaseItemId == item.Id);
                if (registeredCount > item.Quantity)
                {
                    AddFinding(AutomationRuleType.PurchaseRegistrationMismatch, FindingSeverity.Critical, $"Over-registration on Purchase {item.Purchase?.PurchaseNumber}",
                        $"Purchased quantity is {item.Quantity}, but {registeredCount} assets are registered.",
                        $"RULE_G_OVER_{item.Id}",
                        "Review purchase registration records.",
                        purchaseId: item.PurchaseId);
                }
                else if (registeredCount < item.Quantity && item.Purchase != null)
                {
                    AddFinding(AutomationRuleType.PurchaseRegistrationMismatch, FindingSeverity.Info, $"Pending Registrations: {item.Purchase.PurchaseNumber}",
                        $"Purchased: {item.Quantity}, Registered: {registeredCount}. {item.Quantity - registeredCount} remaining to register.",
                        $"RULE_G_REMAINING_{item.Id}",
                        "Register remaining physical assets.",
                        purchaseId: item.PurchaseId);
                }
            }

            // RULE H - Unallocated Asset
            var availableStatuses = new[] { DeviceStatus.InStore };
            var unallocatedAssets = activeDevices.Where(d => string.IsNullOrEmpty(d.EmployeeId) && availableStatuses.Contains(d.Status)).ToList();
            if (unallocatedAssets.Count > 0)
            {
                AddFinding(AutomationRuleType.UnallocatedAsset, FindingSeverity.Info, "Unallocated Assets Available",
                    $"{unallocatedAssets.Count} registered assets are currently available and unallocated.",
                    $"RULE_H_SUMMARY",
                    "Ready for allocation when needed.");
            }

            // RULE I - Allocation Anomalies
            foreach (var device in activeDevices.Where(d => !string.IsNullOrEmpty(d.EmployeeId)))
            {
                if (!employeesByCustomId.ContainsKey(device.EmployeeId!))
                {
                    AddFinding(AutomationRuleType.AllocationAnomaly, FindingSeverity.Critical, $"{device.AssetTag} — Invalid Allocation",
                        $"Asset is allocated to an invalid or non-existent employee ID ({device.EmployeeId}).",
                        $"RULE_I_{device.Id}",
                        "Reassign or unallocate asset.",
                        device.Id);
                }
                else if (device.Status != DeviceStatus.InUse)
                {
                    AddFinding(AutomationRuleType.AllocationAnomaly, FindingSeverity.Warning, $"{device.AssetTag} — Allocation Status Mismatch",
                        $"Asset is allocated to {GetEmpName(device.EmployeeId)} but status is marked as '{device.Status}'.",
                        $"RULE_I_STATUS_{device.Id}",
                        "Correct the device status to match allocation.",
                        device.Id, device.EmployeeId);
                }
            }

            // RULE J - Return / Repair Anomalies
            foreach (var device in activeDevices)
            {
                if (device.Status == DeviceStatus.Returned && !string.IsNullOrEmpty(device.EmployeeId))
                {
                    AddFinding(AutomationRuleType.ReturnRepairAnomaly, FindingSeverity.Critical, $"{device.AssetTag} — Return Anomaly",
                        $"Asset is marked 'Returned' but is still allocated to {GetEmpName(device.EmployeeId)}.",
                        $"RULE_J_RETURN_{device.Id}",
                        "Process handover return properly.",
                        device.Id, device.EmployeeId);
                }
                if (device.Status == DeviceStatus.InRepair && !string.IsNullOrEmpty(device.EmployeeId))
                {
                    AddFinding(AutomationRuleType.ReturnRepairAnomaly, FindingSeverity.Warning, $"{device.AssetTag} — Repair Anomaly",
                        $"Asset is marked 'In Repair' but is assigned to {GetEmpName(device.EmployeeId)}.",
                        $"RULE_J_REPAIR_{device.Id}",
                        "Unallocate device if it is physically in repair.",
                        device.Id, device.EmployeeId);
                }
            }

            if (newFindings.Any())
            {
                _context.AutomationFindings.AddRange(newFindings);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<AutomationFinding>> GetFindingsAsync(FindingStatus? status = null, FindingSeverity? severity = null, AutomationRuleType? ruleType = null, string searchTerm = "")
        {
            var query = _context.AutomationFindings
                .Include(f => f.Device)
                .Include(f => f.Employee)
                .Include(f => f.Purchase)
                .AsQueryable();

            if (status.HasValue) query = query.Where(f => f.Status == status.Value);
            if (severity.HasValue) query = query.Where(f => f.Severity == severity.Value);
            if (ruleType.HasValue) query = query.Where(f => f.RuleType == ruleType.Value);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(f => 
                    f.Title.ToLower().Contains(searchTerm) || 
                    (f.Description != null && f.Description.ToLower().Contains(searchTerm)) ||
                    (f.Device != null && f.Device.AssetTag != null && f.Device.AssetTag.ToLower().Contains(searchTerm)) ||
                    (f.Device != null && f.Device.Serial != null && f.Device.Serial.ToLower().Contains(searchTerm)) ||
                    (f.Employee != null && f.Employee.Name.ToLower().Contains(searchTerm)) ||
                    (f.Purchase != null && f.Purchase.PurchaseNumber != null && f.Purchase.PurchaseNumber.ToLower().Contains(searchTerm))
                );
            }

            return await query.OrderByDescending(f => f.CreatedAt).ToListAsync();
        }

        public async Task<AutomationFinding?> GetFindingByIdAsync(int id)
        {
            return await _context.AutomationFindings
                .Include(f => f.Device)
                .Include(f => f.Employee)
                .Include(f => f.Purchase)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<bool> ResolveFindingAsync(int id, string resolvedBy, string? resolutionNote)
        {
            var finding = await _context.AutomationFindings.FindAsync(id);
            if (finding == null || finding.Status != FindingStatus.Open) return false;

            finding.Status = FindingStatus.Resolved;
            finding.ResolvedAt = DateTime.UtcNow;
            finding.ResolvedBy = resolvedBy;
            finding.ResolutionNote = resolutionNote;
            finding.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DismissFindingAsync(int id, string dismissedBy, string dismissalReason)
        {
            var finding = await _context.AutomationFindings.FindAsync(id);
            if (finding == null || finding.Status != FindingStatus.Open) return false;

            finding.Status = FindingStatus.Dismissed;
            finding.DismissedAt = DateTime.UtcNow;
            finding.DismissedBy = dismissedBy;
            finding.DismissalReason = dismissalReason;
            finding.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetOpenFindingCountAsync() => await _context.AutomationFindings.CountAsync(f => f.Status == FindingStatus.Open);
        public async Task<int> GetCriticalFindingCountAsync() => await _context.AutomationFindings.CountAsync(f => f.Status == FindingStatus.Open && f.Severity == FindingSeverity.Critical);
        public async Task<int> GetWarningFindingCountAsync() => await _context.AutomationFindings.CountAsync(f => f.Status == FindingStatus.Open && f.Severity == FindingSeverity.Warning);
        public async Task<int> GetInfoFindingCountAsync() => await _context.AutomationFindings.CountAsync(f => f.Status == FindingStatus.Open && f.Severity == FindingSeverity.Info);

        public async Task<List<AutomationFinding>> GetDashboardFindingsAsync(int top = 5)
        {
            return await _context.AutomationFindings
                .Include(f => f.Device)
                .Include(f => f.Employee)
                .Where(f => f.Status == FindingStatus.Open)
                .OrderByDescending(f => f.Severity) // Critical first
                .ThenByDescending(f => f.CreatedAt)
                .Take(top)
                .ToListAsync();
        }
    }
}
