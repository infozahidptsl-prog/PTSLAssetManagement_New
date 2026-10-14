using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AssetManagement.Core.Entities;
using AssetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Services;

public class EmployeeService
{
    private readonly ApplicationDbContext _context;

    public EmployeeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Employee>> GetEmployeesAsync(bool includeInactive = false)
    {
        var devices = await _context.Devices.AsNoTracking().ToListAsync();
        var employeesFromDb = await _context.Employees.AsNoTracking().ToListAsync();
        
        var dynamicEmployees = devices
            .Where(d => !string.IsNullOrEmpty(d.UserName))
            .Select(d => new Employee
            {
                Id = !string.IsNullOrEmpty(d.EmployeeId) ? d.EmployeeId : d.UserName!.Replace(" ", "-").ToLower(),
                Name = d.UserName,
                EmployeeId = d.EmployeeId,
                Email = d.Email,
                Contact = d.Contact,
                Designation = d.Designation
            }).ToList();

        var baseEmployees = new List<Employee>();
        baseEmployees.AddRange(employeesFromDb);
        baseEmployees.AddRange(dynamicEmployees);

        var mergedEmployees = baseEmployees
            .Where(e => !string.IsNullOrEmpty(e.Name))
            .GroupBy(e => !string.IsNullOrEmpty(e.EmployeeId) 
                ? $"id:{e.EmployeeId.Trim().ToUpperInvariant()}" 
                : $"name:{e.Name!.Trim().ToLowerInvariant()}")
            .Select(g =>
            {
                var first = g.OrderByDescending(e => !string.IsNullOrEmpty(e.Email))
                             .ThenByDescending(e => !string.IsNullOrEmpty(e.EmployeeId))
                             .First();
                
                var merged = new Employee
                {
                    Id = first.Id,
                    Name = first.Name,
                    EmployeeId = !string.IsNullOrEmpty(first.EmployeeId) ? first.EmployeeId : g.FirstOrDefault(e => !string.IsNullOrEmpty(e.EmployeeId))?.EmployeeId,
                    Email = !string.IsNullOrEmpty(first.Email) ? first.Email : g.FirstOrDefault(e => !string.IsNullOrEmpty(e.Email))?.Email,
                    Contact = !string.IsNullOrEmpty(first.Contact) ? first.Contact : g.FirstOrDefault(e => !string.IsNullOrEmpty(e.Contact))?.Contact,
                    Designation = !string.IsNullOrEmpty(first.Designation) ? first.Designation : g.FirstOrDefault(e => !string.IsNullOrEmpty(e.Designation))?.Designation,
                    Department = !string.IsNullOrEmpty(first.Department) ? first.Department : g.FirstOrDefault(e => !string.IsNullOrEmpty(e.Department))?.Department,
                    JoiningDate = first.JoiningDate.HasValue ? first.JoiningDate : g.FirstOrDefault(e => e.JoiningDate.HasValue)?.JoiningDate,
                    IsActive = first.IsActive,
                    ProfileImageUrl = !string.IsNullOrEmpty(first.ProfileImageUrl) ? first.ProfileImageUrl : g.FirstOrDefault(e => !string.IsNullOrEmpty(e.ProfileImageUrl))?.ProfileImageUrl,
                    Team = !string.IsNullOrEmpty(first.Team) ? first.Team : g.FirstOrDefault(e => !string.IsNullOrEmpty(e.Team))?.Team,
                    Manager = !string.IsNullOrEmpty(first.Manager) ? first.Manager : g.FirstOrDefault(e => !string.IsNullOrEmpty(e.Manager))?.Manager,
                    Location = !string.IsNullOrEmpty(first.Location) ? first.Location : g.FirstOrDefault(e => !string.IsNullOrEmpty(e.Location))?.Location,
                    BloodGroup = !string.IsNullOrEmpty(first.BloodGroup) ? first.BloodGroup : g.FirstOrDefault(e => !string.IsNullOrEmpty(e.BloodGroup))?.BloodGroup,
                    LinkedInUrl = !string.IsNullOrEmpty(first.LinkedInUrl) ? first.LinkedInUrl : g.FirstOrDefault(e => !string.IsNullOrEmpty(e.LinkedInUrl))?.LinkedInUrl,
                    About = !string.IsNullOrWhiteSpace(first.About) ? first.About : g.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.About))?.About,
                    IsTeamAccount = first.IsTeamAccount || g.Any(e => e.IsTeamAccount)
                };

                // Assign devices: strictly match EmployeeId if available, otherwise match Name
                var assignedDevices = devices.Where(d => 
                    (!string.IsNullOrEmpty(merged.EmployeeId) && !string.IsNullOrEmpty(d.EmployeeId) && string.Equals(d.EmployeeId, merged.EmployeeId, StringComparison.OrdinalIgnoreCase)) || 
                    (string.IsNullOrEmpty(merged.EmployeeId) && !string.IsNullOrEmpty(merged.Name) && string.Equals(d.UserName, merged.Name, StringComparison.OrdinalIgnoreCase))).ToList();

                merged.DeviceIds = assignedDevices.Select(d => d.Id).ToList();
                merged.ActiveCount = assignedDevices.Count(d => d.Status == AssetManagement.Core.Enums.DeviceStatus.InUse);

                return merged;
            })
            .OrderBy(e => e.Name)
            .ToList();

        if (!includeInactive)
        {
            mergedEmployees = mergedEmployees.Where(e => e.IsActive).ToList();
        }

        return mergedEmployees;
    }

    public async Task<Employee?> GetEmployeeByIdAsync(string id)
    {
        var employees = await GetEmployeesAsync(includeInactive: true);
        return employees.FirstOrDefault(e => e.Id == id);
    }
    
    public async Task<Employee?> GetEmployeeByQrTokenAsync(string tokenHash)
    {
        return await _context.Employees.FirstOrDefaultAsync(e => e.QrTokenHash == tokenHash && e.QrTokenRevokedAt == null);
    }
    
    public async Task<string> GetNextEmployeeIdAsync()
    {
        // Only consider 3-digit PTSL IDs (format PTSLxxx) — excludes test/dummy entries like PTSL999
        var dbEmployees = await _context.Employees.AsNoTracking().ToListAsync();
        var ptids = dbEmployees
            .Where(e => !string.IsNullOrEmpty(e.EmployeeId) && e.EmployeeId.StartsWith("PTSL", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.EmployeeId!.Substring(4).Trim())
            .Where(s => int.TryParse(s, out _) && s.Length == 3)   // strictly 3-digit format only
            .Select(int.Parse)
            .ToList();

        // Also scan device.EmployeeId fields for 3-digit PTSL numbers
        var deviceIds = await _context.Devices
            .Where(d => d.EmployeeId != null && d.EmployeeId.StartsWith("PTSL"))
            .Select(d => d.EmployeeId!)
            .Distinct()
            .ToListAsync();
        var devicePtids = deviceIds
            .Select(id => id.Substring(4).Trim())
            .Where(s => int.TryParse(s, out _) && s.Length == 3)   // strictly 3-digit format only
            .Select(int.Parse)
            .ToList();

        var all = ptids.Concat(devicePtids).ToList();
        if (!all.Any()) return "PTSL001";
        var next = all.Max() + 1;
        return $"PTSL{next:D3}";
    }

    public async Task OffboardEmployeeAsync(string employeeId, string? excludeDeviceId = null)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);
        
        // If employee is not in DB yet (dynamically created), add them so we can track their inactive state
        if (employee == null)
        {
            var employees = await GetEmployeesAsync(includeInactive: true);
            var dynamicEmp = employees.FirstOrDefault(e => e.Id == employeeId);
            if (dynamicEmp != null)
            {
                employee = new Employee
                {
                    Id = dynamicEmp.Id,
                    Name = dynamicEmp.Name,
                    EmployeeId = dynamicEmp.EmployeeId,
                    Email = dynamicEmp.Email,
                    Contact = dynamicEmp.Contact,
                    Designation = dynamicEmp.Designation,
                    Department = dynamicEmp.Department,
                    JoiningDate = dynamicEmp.JoiningDate,
                    IsActive = false,
                    LinkedInUrl = dynamicEmp.LinkedInUrl,
                    About = dynamicEmp.About
                };
                _context.Employees.Add(employee);
            }
            else 
            {
                return;
            }
        }
        else
        {
            employee.IsActive = false;
        }

        // Unassign devices
        var assignedDevicesQuery = _context.Devices
            .Where(d => (!string.IsNullOrEmpty(employee.EmployeeId) && d.EmployeeId == employee.EmployeeId) || 
                        (!string.IsNullOrEmpty(employee.Name) && d.UserName == employee.Name));

        if (!string.IsNullOrEmpty(excludeDeviceId))
        {
            assignedDevicesQuery = assignedDevicesQuery.Where(d => d.Id != excludeDeviceId);
        }

        var assignedDevices = await assignedDevicesQuery.ToListAsync();

        foreach (var device in assignedDevices)
        {
            if (device.Status == AssetManagement.Core.Enums.DeviceStatus.InUse)
            {
                // Create handover record
                var handover = new Handover
                {
                    Id = Guid.NewGuid().ToString(),
                    DeviceId = device.Id,
                    From = employee.Name,
                    To = "HR/Store",
                    Released = DateTime.Now.ToString("dd-MMM-yyyy"),
                    ReturnDate = DateTime.Now.ToString("dd-MMM-yyyy"),
                    Condition = device.Condition
                };
                _context.Handovers.Add(handover);

                // Update device
                device.Status = AssetManagement.Core.Enums.DeviceStatus.InStore;
                device.UserName = null;
                device.EmployeeId = null;
                device.Email = null;
                device.Contact = null;
                device.Designation = null;
                device.ReceivedDate = null;
            }
        }

        await _context.SaveChangesAsync();
    }
    
    public async Task<bool> DeleteEmployeeAsync(string id)
    {
        var existing = await _context.Employees.FindAsync(id)
            ?? await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == id);

        if (existing != null)
        {
            _context.Employees.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task AddEmployeeAsync(Employee employee)
    {
        if (string.IsNullOrEmpty(employee.Id))
        {
            employee.Id = string.IsNullOrEmpty(employee.EmployeeId) 
                ? employee.Name.Replace(" ", "-").ToLower() + "-" + Guid.NewGuid().ToString().Substring(0, 4)
                : employee.EmployeeId;
        }
        
        // Detach any previously tracked entity with the same key to avoid EF Core tracking conflicts
        var alreadyTracked = _context.ChangeTracker.Entries<Employee>()
            .FirstOrDefault(e => e.Entity.Id == employee.Id);
        if (alreadyTracked != null)
        {
            alreadyTracked.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        }

        // Check if it already exists in DB (avoid duplicate insert)
        var existing = await _context.Employees.FindAsync(employee.Id);
        if (existing != null)
        {
            _context.Entry(existing).CurrentValues.SetValues(employee);
        }
        else
        {
            _context.Employees.Add(employee);
        }
        await _context.SaveChangesAsync();
    }

    public async Task UpdateEmployeeAsync(Employee employee)
    {
        if (string.IsNullOrEmpty(employee.Id))
        {
            employee.Id = !string.IsNullOrEmpty(employee.EmployeeId) ? employee.EmployeeId : Guid.NewGuid().ToString();
        }

        var alreadyTracked = _context.ChangeTracker.Entries<Employee>()
            .FirstOrDefault(e => e.Entity.Id == employee.Id);
        if (alreadyTracked != null && alreadyTracked.Entity != employee)
        {
            alreadyTracked.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        }

        var existing = await _context.Employees.FindAsync(employee.Id)
            ?? await _context.Employees.FirstOrDefaultAsync(e => (!string.IsNullOrEmpty(employee.EmployeeId) && e.EmployeeId == employee.EmployeeId) ||
                                                                 (!string.IsNullOrEmpty(employee.Name) && e.Name.ToLower() == employee.Name.ToLower()));
        if (existing != null)
        {
            _context.Entry(existing).CurrentValues.SetValues(employee);
        }
        else
        {
            _context.Employees.Add(employee);
        }

        // Clean up or sync any other duplicate records with matching name or employee ID
        var targetId = existing != null ? existing.Id : employee.Id;
        var duplicates = await _context.Employees
            .Where(e => e.Id != targetId && 
                       ((!string.IsNullOrEmpty(employee.EmployeeId) && e.EmployeeId == employee.EmployeeId) ||
                        (!string.IsNullOrEmpty(employee.Name) && e.Name.Trim().ToLower() == employee.Name.Trim().ToLower())))
            .ToListAsync();

        foreach (var dup in duplicates)
        {
            if (string.IsNullOrEmpty(dup.Email) && string.IsNullOrEmpty(dup.EmployeeId))
            {
                _context.Employees.Remove(dup);
            }
            else
            {
                dup.Designation = employee.Designation;
                dup.Department = employee.Department;
            }
        }

        await _context.SaveChangesAsync();

        var assignedDevices = await _context.Devices
            .Where(d => (!string.IsNullOrEmpty(employee.EmployeeId) && d.EmployeeId == employee.EmployeeId) || 
                        (!string.IsNullOrEmpty(employee.Name) && d.UserName == employee.Name))
            .ToListAsync();
        
        foreach(var device in assignedDevices)
        {
            device.UserName = employee.Name;
            device.EmployeeId = employee.EmployeeId;
            device.Email = employee.Email;
            device.Contact = employee.Contact;
            device.Designation = employee.Designation;
        }
        
        if (assignedDevices.Any())
        {
            await _context.SaveChangesAsync();
        }
    }
}
