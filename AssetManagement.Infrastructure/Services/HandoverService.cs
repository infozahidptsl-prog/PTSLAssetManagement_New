using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AssetManagement.Core.Entities;
using AssetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Services;

public class HandoverService
{
    private readonly ApplicationDbContext _context;

    public HandoverService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Handover>> GetHandoversAsync()
    {
        var handoversFromDb = await _context.Handovers.AsNoTracking().Include(h => h.Device).ToListAsync();
        var devicesWithRealHandovers = handoversFromDb.Select(h => h.DeviceId).Distinct().ToList();

        var devicesWithLegacyHandovers = await _context.Devices
            .AsNoTracking()
            .Where(d => (!string.IsNullOrEmpty(d.From) || !string.IsNullOrEmpty(d.To)) && !devicesWithRealHandovers.Contains(d.Id))
            .ToListAsync();

        var derivedHandovers = devicesWithLegacyHandovers.Select(d => new Handover
        {
            Id = $"legacy-{d.Id}",
            DeviceId = d.Id,
            Device = d,
            From = d.From ?? string.Empty,
            To = d.To ?? string.Empty,
            Released = d.Released,
            ReturnDate = d.ReturnDate,
            Condition = d.Condition
        }).ToList();

        handoversFromDb.AddRange(derivedHandovers);
        return handoversFromDb;
    }

    public async Task AddHandoverAsync(Handover handover)
    {
        _context.Handovers.Add(handover);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateHandoverAsync(Handover handover)
    {
        if (handover.Id.StartsWith("legacy-"))
        {
            handover.Id = Guid.NewGuid().ToString();
            _context.Handovers.Add(handover);
        }
        else
        {
            var existing = await _context.Handovers.FindAsync(handover.Id);
            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(handover);
            }
            else
            {
                _context.Handovers.Update(handover);
            }
        }
        await _context.SaveChangesAsync();
    }

    public async Task DeleteHandoverAsync(string id)
    {
        if (id.StartsWith("legacy-"))
        {
            var deviceId = id.Replace("legacy-", "");
            var device = await _context.Devices.FindAsync(deviceId);
            if (device != null)
            {
                device.From = null;
                device.To = null;
                device.Released = null;
                device.ReturnDate = null;
                await _context.SaveChangesAsync();
            }
            return;
        }

        var handover = await _context.Handovers.FindAsync(id);
        if (handover != null)
        {
            _context.Handovers.Remove(handover);
            await _context.SaveChangesAsync();
        }
    }
}
