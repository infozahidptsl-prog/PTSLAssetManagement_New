using AssetManagement.Core.Entities;
using AssetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AssetManagement.Infrastructure.Services;

public class PurchaseService
{
    private readonly ApplicationDbContext _context;

    public PurchaseService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Purchase>> GetAllPurchasesAsync()
    {
        return await _context.Purchases
            .Include(p => p.Items)
            .OrderByDescending(p => p.PurchaseDate)
            .ToListAsync();
    }

    public async Task<Purchase?> GetPurchaseByIdAsync(string id)
    {
        return await _context.Purchases
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id || p.PurchaseNumber == id);
    }

    public async Task<PurchaseItem?> GetPurchaseItemByIdAsync(string id)
    {
        return await _context.PurchaseItems
            .Include(i => i.Purchase)
            .Include(i => i.Devices)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task AddPurchaseAsync(Purchase purchase)
    {
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> CheckDuplicateInvoiceAsync(string supplier, string invoiceNumber)
    {
        return await _context.Purchases.AnyAsync(p => p.Supplier == supplier && p.InvoiceNumber == invoiceNumber);
    }

    public async Task UpdatePurchaseItemAsync(PurchaseItem item)
    {
        _context.PurchaseItems.Update(item);
        await _context.SaveChangesAsync();
    }

    public async Task UpdatePurchaseAsync(Purchase purchase)
    {
        var existing = await _context.Purchases.FirstOrDefaultAsync(p => p.Id == purchase.Id);
        if (existing == null) return;

        existing.Supplier = purchase.Supplier;
        existing.InvoiceNumber = purchase.InvoiceNumber;
        existing.PurchaseDate = purchase.PurchaseDate;
        existing.Notes = purchase.Notes;

        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeletePurchaseAsync(string id)
    {
        var purchase = await _context.Purchases
            .Include(p => p.Items)
                .ThenInclude(i => i.Devices)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (purchase == null) return false;

        bool hasRegisteredDevices = purchase.Items.Any(i => i.RegisteredQuantity > 0 || (i.Devices != null && i.Devices.Any()));
        if (hasRegisteredDevices)
        {
            throw new InvalidOperationException("Cannot delete this purchase because physical devices have already been registered against it. Please delete the registered devices first.");
        }

        if (!string.IsNullOrEmpty(purchase.InvoiceFilePath))
        {
            try
            {
                var fullPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", purchase.InvoiceFilePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch { }
        }

        _context.PurchaseItems.RemoveRange(purchase.Items);
        _context.Purchases.Remove(purchase);
        await _context.SaveChangesAsync();
        return true;
    }
}

