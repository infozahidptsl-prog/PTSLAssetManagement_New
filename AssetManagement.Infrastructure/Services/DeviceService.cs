using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AssetManagement.Core.Entities;
using AssetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Services;

public class DeviceService
{
    private readonly ApplicationDbContext _context;

    public DeviceService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Device>> GetDevicesAsync()
    {
        var categories = await _context.DeviceCategories.AsNoTracking().ToListAsync();
        CategoryTypeRegistry.Sync(categories);
        return await _context.Devices.AsNoTracking().ToListAsync();
    }

    public async Task<Device?> GetDeviceByIdAsync(string id)
    {
        var categories = await _context.DeviceCategories.AsNoTracking().ToListAsync();
        CategoryTypeRegistry.Sync(categories);

        return await _context.Devices
            .AsNoTracking()
            .Include(d => d.PurchaseItem)
            .ThenInclude(pi => pi.Purchase)
            .FirstOrDefaultAsync(d => d.Id == id || d.AssetTag == id || d.Serial == id);
    }
    
    public async Task<Device?> GetDeviceByQrTokenAsync(string tokenHash)
    {
        return await _context.Devices
            .AsNoTracking()
            .Include(d => d.PurchaseItem)
            .ThenInclude(pi => pi.Purchase)
            .FirstOrDefaultAsync(d => d.QrTokenHash == tokenHash && d.QrTokenRevokedAt == null);
    }
    
    public async Task<List<Device>> GetChildComponentsAsync(string parentId)
    {
        return await _context.Devices.AsNoTracking().Where(d => d.ParentDeviceId == parentId).ToListAsync();
    }

    public async Task AddDeviceAsync(Device device)
    {
        if (string.IsNullOrEmpty(device.Id))
        {
            device.Id = Guid.NewGuid().ToString();
        }

        if (!device.IsComponent && !string.IsNullOrWhiteSpace(device.Category))
        {
            var catType = CategoryTypeRegistry.GetType(device.Category);
            if (catType.Equals("Component", StringComparison.OrdinalIgnoreCase))
            {
                device.IsComponent = true;
            }
        }
        
        if (string.IsNullOrEmpty(device.AssetTag))
        {
            device.AssetTag = await GenerateAssetTagAsync(device);
        }

        device.QrTokenCreatedAt ??= DateTime.UtcNow;
        
        _context.Devices.Add(device);
        await _context.SaveChangesAsync();
    }

    public async Task<Device> RegisterPhysicalAssetFromPurchaseAsync(Device device, string purchaseItemId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var exists = await _context.Devices.AnyAsync(d => d.Serial == device.Serial);
            if (exists)
            {
                throw new InvalidOperationException($"A device with serial '{device.Serial}' is already registered.");
            }

            var pItem = await _context.PurchaseItems.FirstOrDefaultAsync(p => p.Id == purchaseItemId);
            if (pItem == null) throw new InvalidOperationException("Purchase item not found.");
            
            if (pItem.RemainingQuantity <= 0)
            {
                throw new InvalidOperationException("Cannot register: Quantity limit reached.");
            }

            if (string.IsNullOrEmpty(device.Id)) device.Id = Guid.NewGuid().ToString();
            if (string.IsNullOrEmpty(device.AssetTag))
            {
                device.AssetTag = await GenerateAssetTagAsync(device);
            }

            device.QrTokenCreatedAt ??= DateTime.UtcNow;

            device.PurchaseItemId = purchaseItemId;

            _context.Devices.Add(device);
            pItem.RegisteredQuantity += 1;
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return device;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateDeviceAsync(Device device)
    {
        var existingDevice = await _context.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.Id == device.Id);
        if (existingDevice != null)
        {
            bool handoverChanged = (device.From != existingDevice.From || device.To != existingDevice.To) 
                && (!string.IsNullOrEmpty(device.From) || !string.IsNullOrEmpty(device.To));
                
            if (handoverChanged)
            {
                var handover = new Handover
                {
                    Id = Guid.NewGuid().ToString(),
                    DeviceId = device.Id,
                    From = device.From ?? string.Empty,
                    To = device.To ?? string.Empty,
                    Released = device.Released,
                    ReturnDate = device.ReturnDate,
                    Condition = device.Condition
                };
                _context.Handovers.Add(handover);
            }
        }

        _context.Devices.Update(device);
        await _context.SaveChangesAsync();
    }

    public async Task TransferDeviceAsync(string deviceId, string transferType, Device newAssignmentData = null, string? previousUser = null)
    {
        var device = await _context.Devices.FirstOrDefaultAsync(d => d.Id == deviceId);
        if (device == null) return;

        string oldUser = !string.IsNullOrEmpty(previousUser)
            ? previousUser
            : (!string.IsNullOrEmpty(device.UserName) ? device.UserName : "IT inventory");

        if (transferType == "TransferToUser")
        {
            var handover = new Handover
            {
                Id = Guid.NewGuid().ToString(),
                DeviceId = device.Id,
                From = oldUser,
                To = newAssignmentData?.UserName ?? "New User",
                Released = DateTime.Now.ToString("yyyy-MM-dd"),
                ReturnDate = null,
                Condition = device.Condition
            };
            _context.Handovers.Add(handover);

            device.UserName = newAssignmentData?.UserName;
            device.EmployeeId = newAssignmentData?.EmployeeId;
            device.Email = newAssignmentData?.Email;
            device.Contact = newAssignmentData?.Contact;
            device.Designation = newAssignmentData?.Designation;
            device.ReceivedDate = DateTime.Now.ToString("yyyy-MM-dd");
            device.ReturnDate = null;
            device.Status = AssetManagement.Core.Enums.DeviceStatus.InUse;
            device.From = handover.From;
            device.To = handover.To;
        }
        else if (transferType == "ReturnToStore")
        {
            var handover = new Handover
            {
                Id = Guid.NewGuid().ToString(),
                DeviceId = device.Id,
                From = oldUser,
                To = "Store/IT",
                Released = DateTime.Now.ToString("yyyy-MM-dd"),
                ReturnDate = DateTime.Now.ToString("yyyy-MM-dd"),
                Condition = device.Condition
            };
            _context.Handovers.Add(handover);

            device.UserName = null;
            device.EmployeeId = null;
            device.Email = null;
            device.Contact = null;
            device.Designation = null;
            device.ReceivedDate = null;
            device.ReturnDate = DateTime.Now.ToString("yyyy-MM-dd");
            device.Status = AssetManagement.Core.Enums.DeviceStatus.InStore;
            device.From = handover.From;
            device.To = handover.To;
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateDevicesAsync(IEnumerable<Device> devices)
    {
        _context.Devices.UpdateRange(devices);
        await _context.SaveChangesAsync();
    }
    
    public async Task InstallComponentAsync(string componentId, string parentId)
    {
        var component = await _context.Devices.FirstOrDefaultAsync(d => d.Id == componentId);
        var parent = await _context.Devices.FirstOrDefaultAsync(d => d.Id == parentId);
        
        if (component != null && parent != null)
        {
            var handover = new Handover
            {
                Id = Guid.NewGuid().ToString(),
                DeviceId = component.Id,
                From = component.From ?? "Store",
                To = $"Device: {parent.AssetTag ?? parent.Id}",
                Released = DateTime.Now.ToString("yyyy-MM-dd"),
                ReturnDate = null,
                Condition = component.Condition
            };
            _context.Handovers.Add(handover);

            component.ParentDeviceId = parent.Id;
            component.Status = AssetManagement.Core.Enums.DeviceStatus.InUse;
            component.ReturnDate = null;
            component.From = handover.From;
            component.To = handover.To;
            
            _context.Devices.Update(component);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task UninstallComponentAsync(string componentId)
    {
        var component = await _context.Devices.FirstOrDefaultAsync(d => d.Id == componentId);
        if (component != null && component.ParentDeviceId != null)
        {
            var oldParent = await _context.Devices.FirstOrDefaultAsync(d => d.Id == component.ParentDeviceId);
            
            var handover = new Handover
            {
                Id = Guid.NewGuid().ToString(),
                DeviceId = component.Id,
                From = $"Device: {oldParent?.AssetTag ?? component.ParentDeviceId}",
                To = "Store/IT",
                Released = DateTime.Now.ToString("yyyy-MM-dd"),
                ReturnDate = DateTime.Now.ToString("yyyy-MM-dd"),
                Condition = component.Condition
            };
            _context.Handovers.Add(handover);

            component.ParentDeviceId = null;
            component.Status = AssetManagement.Core.Enums.DeviceStatus.InStore;
            component.From = handover.From;
            component.To = handover.To;
            
            _context.Devices.Update(component);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteDeviceAsync(string id)
    {
        var device = await _context.Devices.FirstOrDefaultAsync(d => d.Id == id);
        if (device != null)
        {
            // 1. Remove automation findings linked to this device
            var findings = await _context.AutomationFindings.Where(f => f.DeviceId == id).ToListAsync();
            if (findings.Any())
            {
                _context.AutomationFindings.RemoveRange(findings);
            }

            // 2. Remove handovers & repair records linked to this device
            var handovers = await _context.Handovers.Where(h => h.DeviceId == id).ToListAsync();
            if (handovers.Any())
            {
                _context.Handovers.RemoveRange(handovers);
            }

            var repairs = await _context.Repairs.Where(r => r.DeviceId == id).ToListAsync();
            if (repairs.Any())
            {
                _context.Repairs.RemoveRange(repairs);
            }

            // 3. Detach any child components attached to this device
            var attachedComponents = await _context.Devices.Where(d => d.ParentDeviceId == id).ToListAsync();
            foreach (var comp in attachedComponents)
            {
                comp.ParentDeviceId = null;
                comp.Status = AssetManagement.Core.Enums.DeviceStatus.InStore;
            }

            // 4. If this device was created against a purchase item, decrement registered quantity and free up slot
            if (!string.IsNullOrEmpty(device.PurchaseItemId) && int.TryParse(device.PurchaseItemId, out var pItemId))
            {
                var purchaseItem = await _context.PurchaseItems.FindAsync(pItemId);
                if (purchaseItem != null && purchaseItem.RegisteredQuantity > 0)
                {
                    purchaseItem.RegisteredQuantity--;
                }
            }

            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();
        }
    }

    public async Task AssignMissingAssetTagsAsync()
    {
        var devicesWithOldTags = await _context.Devices.Where(d => d.AssetTag != null && d.AssetTag.StartsWith("PTSL-")).ToListAsync();
        foreach (var device in devicesWithOldTags)
        {
            device.AssetTag = device.AssetTag.Replace("PTSL-", "PT-");
            _context.Devices.Update(device);
        }
        if (devicesWithOldTags.Any())
        {
            await _context.SaveChangesAsync();
        }

        var devicesWithoutTags = await _context.Devices.Where(d => string.IsNullOrEmpty(d.AssetTag)).ToListAsync();
        if (!devicesWithoutTags.Any()) return;

        foreach (var device in devicesWithoutTags)
        {
            device.AssetTag = await GenerateAssetTagAsync(device);
            _context.Devices.Update(device);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<string> GenerateAssetTagAsync(Device device)
    {
        string categoryCode = "SYS";
        if (!string.IsNullOrWhiteSpace(device.Category))
        {
            var catUpper = device.Category.ToUpperInvariant().Trim();
            if (catUpper.StartsWith("LAPTOP")) categoryCode = "LPT";
            else if (catUpper.StartsWith("DESKTOP")) categoryCode = "DSK";
            else if (catUpper.StartsWith("MOBILE") || catUpper.StartsWith("PHONE")) categoryCode = "MOB";
            else if (catUpper.StartsWith("SERVER")) categoryCode = "SRV";
            else if (catUpper.StartsWith("MACBOOK")) categoryCode = "MAC";
            else if (catUpper.StartsWith("MONITOR")) categoryCode = "MON";
            else categoryCode = catUpper.Substring(0, Math.Min(3, catUpper.Length)).ToUpperInvariant();
        }
        
        if (device.IsComponent) categoryCode = "CMP";
        
        string companyPrefix = await GenerateCompanyPrefixAsync(device.OwnerCompany);
        string tagPrefix = string.IsNullOrEmpty(companyPrefix) ? $"{categoryCode}-" : $"{companyPrefix}-{categoryCode}-";
        
        var existingTags = await _context.Devices
            .Where(d => d.AssetTag != null && d.AssetTag.StartsWith(tagPrefix))
            .Select(d => d.AssetTag)
            .ToListAsync();
            
        int nextId = 1;
        if (existingTags.Any())
        {
            var numbers = existingTags
                .Select(t => int.TryParse(t!.Split('-').LastOrDefault(), out int n) ? n : 0)
                .ToList();
            if (numbers.Any())
                nextId = numbers.Max() + 1;
        }
        
        return $"{tagPrefix}{nextId:D3}";
    }

    private async Task<string> GenerateCompanyPrefixAsync(string? companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            return "";

        var company = await _context.OwnerCompanies.FirstOrDefaultAsync(c => c.Name == companyName || (companyName.Contains("Prime") && c.Name.Contains("Prime")));
        if (company != null && !string.IsNullOrWhiteSpace(company.Prefix))
        {
            return company.Prefix.Trim();
        }

        if (companyName.Equals("PTSL", StringComparison.OrdinalIgnoreCase))
            return "PSL";

        var words = companyName.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 1)
        {
            return string.Concat(words.Take(3).Select(w => w[0])).ToUpper();
        }
        
        string word = words[0];
        var upperChars = word.Where(char.IsUpper).ToList();
        if (upperChars.Count >= 2)
        {
            return string.Concat(upperChars.Take(3)).ToUpper();
        }
        
        return word.Substring(0, Math.Min(word.Length, 3)).ToUpper();
    }
}
