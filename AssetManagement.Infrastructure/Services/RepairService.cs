using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AssetManagement.Core.Entities;
using AssetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Services;

public class RepairService
{
    private readonly ApplicationDbContext _context;

    public RepairService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RepairRecord>> GetRepairsAsync()
    {
        return await _context.Repairs.Include(r => r.Device).ToListAsync();
    }

    public async Task<RepairRecord?> GetRepairByIdAsync(string id)
    {
        return await _context.Repairs.Include(r => r.Device).FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task AddRepairAsync(RepairRecord repair)
    {
        _context.Repairs.Add(repair);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRepairAsync(RepairRecord repair)
    {
        var existing = await _context.Repairs.FindAsync(repair.Id);
        if (existing != null)
        {
            _context.Entry(existing).CurrentValues.SetValues(repair);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteRepairAsync(string id)
    {
        var repair = await _context.Repairs.FindAsync(id);
        if (repair != null)
        {
            _context.Repairs.Remove(repair);
            await _context.SaveChangesAsync();
        }
    }
}
