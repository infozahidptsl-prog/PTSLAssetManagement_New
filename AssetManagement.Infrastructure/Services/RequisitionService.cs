using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AssetManagement.Core.Entities;
using AssetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Services;

public class RequisitionService
{
    private readonly ApplicationDbContext _context;

    public RequisitionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Requisition>> GetAllAsync()
    {
        return await _context.Requisitions
            .AsNoTracking()
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();
    }

    public async Task<Requisition?> GetByIdAsync(string id)
    {
        return await _context.Requisitions.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Requisition> CreateAsync(Requisition requisition)
    {
        // Generate human-readable number: REQ-001, REQ-002, etc.
        var count = await _context.Requisitions.CountAsync();
        requisition.RequisitionNumber = $"REQ-{(count + 1):D3}";
        requisition.RequestedAt = DateTime.UtcNow;
        requisition.Status = "Pending";

        _context.Requisitions.Add(requisition);
        await _context.SaveChangesAsync();
        return requisition;
    }

    public async Task ApproveAsync(string id, string approverName, string? note)
    {
        var r = await _context.Requisitions.FindAsync(id);
        if (r == null) return;
        r.Status = "Approved";
        r.ApprovedByName = approverName;
        r.ApprovalNote = note;
        r.ApprovedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task RejectAsync(string id, string approverName, string? note)
    {
        var r = await _context.Requisitions.FindAsync(id);
        if (r == null) return;
        r.Status = "Rejected";
        r.ApprovedByName = approverName;
        r.ApprovalNote = note;
        r.ApprovedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task LinkPurchaseAsync(string id, string deviceId)
    {
        var r = await _context.Requisitions.FindAsync(id);
        if (r == null) return;
        r.PurchasedDeviceId = deviceId;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var r = _context.Requisitions.Local.FirstOrDefault(x => x.Id == id) 
                ?? await _context.Requisitions.FindAsync(id);
        if (r != null)
        {
            _context.Requisitions.Remove(r);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateAsync(Requisition requisition)
    {
        var tracked = _context.Requisitions.Local.FirstOrDefault(r => r.Id == requisition.Id);
        if (tracked != null)
        {
            _context.Entry(tracked).CurrentValues.SetValues(requisition);
        }
        else
        {
            var existing = await _context.Requisitions.FindAsync(requisition.Id);
            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(requisition);
            }
            else
            {
                _context.Requisitions.Update(requisition);
            }
        }
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetPendingCountAsync()
        => await _context.Requisitions.CountAsync(r => r.Status != "Done");
}

