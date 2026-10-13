using System;
using System.Linq;
using System.Threading.Tasks;
using AssetManagement.Core.Interfaces;
using AssetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Services;

public class AiValidationService : IAiValidationService
{
    private readonly ApplicationDbContext _context;

    public AiValidationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task ValidatePurchaseAsync(MappingValidationResult<CanonicalPurchaseDto> mappedResult)
    {
        if (mappedResult.Data == null) return;
        var data = mappedResult.Data;

        // 1. Missing Critical Fields
        if (string.IsNullOrWhiteSpace(data.InvoiceNumber))
        {
            if (!string.IsNullOrWhiteSpace(data.ChallanNumber))
            {
                data.InvoiceNumber = data.ChallanNumber;
            }
            else
            {
                mappedResult.Warnings.Add("Missing critical field: Invoice Number.");
                mappedResult.RequiresReview = true;
            }
        }
        if (string.IsNullOrWhiteSpace(data.Vendor))
        {
            mappedResult.Warnings.Add("Missing critical field: Vendor.");
            mappedResult.RequiresReview = true;
        }

        // 2. Numeric Validation
        if (data.Subtotal < 0) { mappedResult.Warnings.Add("Subtotal cannot be negative."); mappedResult.RequiresReview = true; }
        if (data.Tax < 0) { mappedResult.Warnings.Add("Tax cannot be negative."); mappedResult.RequiresReview = true; }
        if (data.Total < 0) { mappedResult.Warnings.Add("Total cannot be negative."); mappedResult.RequiresReview = true; }

        var expectedTotal = data.Subtotal + data.Tax + data.ShippingCharges - data.Discount;
        if (Math.Abs(data.Total - expectedTotal) > 0.01m)
        {
            mappedResult.Warnings.Add($"Invoice total ({data.Total:F2}) does not reconcile with calculated total ({expectedTotal:F2}).");
            mappedResult.RequiresReview = true;
        }

        if (data.Items == null || !data.Items.Any())
        {
            mappedResult.Warnings.Add("Purchase document contains no line items.");
            mappedResult.RequiresReview = true;
        }
        else
        {
            decimal sumLineTotals = 0m;
            foreach (var item in data.Items)
            {
                if (item.Quantity <= 0) { mappedResult.Warnings.Add($"Item '{item.Description}' quantity must be > 0."); mappedResult.RequiresReview = true; }
                if (item.UnitPrice < 0) { mappedResult.Warnings.Add($"Item '{item.Description}' unit price cannot be negative."); mappedResult.RequiresReview = true; }
                if (item.LineTotal < 0) { mappedResult.Warnings.Add($"Item '{item.Description}' line total cannot be negative."); mappedResult.RequiresReview = true; }

                var expectedLineTotal = item.Quantity * item.UnitPrice;
                if (Math.Abs(item.LineTotal - expectedLineTotal) > 0.01m)
                {
                    mappedResult.Warnings.Add($"Item '{item.Description}' line total ({item.LineTotal:F2}) does not match Quantity × UnitPrice ({expectedLineTotal:F2}).");
                    mappedResult.RequiresReview = true;
                }

                sumLineTotals += item.LineTotal;
            }

            if (data.Subtotal > 0 && Math.Abs(sumLineTotals - data.Subtotal) > 0.01m)
            {
                mappedResult.Warnings.Add($"Sum of line totals ({sumLineTotals:F2}) does not match subtotal ({data.Subtotal:F2}).");
                mappedResult.RequiresReview = true;
            }
        }

        // 3. Duplicate Detection
        if (!string.IsNullOrWhiteSpace(data.Vendor) && !string.IsNullOrWhiteSpace(data.InvoiceNumber))
        {
            var existingPurchase = await _context.Purchases
                .FirstOrDefaultAsync(p => p.Supplier == data.Vendor && p.InvoiceNumber == data.InvoiceNumber);

            if (existingPurchase != null)
            {
                mappedResult.DuplicateDetected = true;
                mappedResult.RequiresReview = true;
                mappedResult.ExistingReference = existingPurchase.PurchaseNumber;
                mappedResult.Warnings.Add($"Duplicate invoice detected. Existing purchase found: {existingPurchase.PurchaseNumber} (Vendor: {existingPurchase.Supplier}, Invoice: {existingPurchase.InvoiceNumber})");
            }
        }
    }

    public async Task ValidateDeviceRegistrationAsync(MappingValidationResult<CanonicalDeviceDto> mappedResult)
    {
        if (mappedResult.Data == null) return;
        var data = mappedResult.Data;

        // 1. Missing Critical Fields
        if (string.IsNullOrWhiteSpace(data.SerialNumber))
        {
            mappedResult.Warnings.Add("Missing critical field: Serial Number.");
            mappedResult.RequiresReview = true;
        }
        if (string.IsNullOrWhiteSpace(data.Manufacturer))
        {
            mappedResult.Warnings.Add("Missing critical field: Manufacturer.");
            mappedResult.RequiresReview = true;
        }

        // 2. Serial Number Validation & Duplicate Detection
        if (!string.IsNullOrWhiteSpace(data.SerialNumber))
        {
            var existingDevice = await _context.Devices
                .FirstOrDefaultAsync(d => d.Serial == data.SerialNumber);

            if (existingDevice != null)
            {
                mappedResult.RegistrationBlocked = true;
                mappedResult.RequiresReview = true;
                mappedResult.ExistingReference = existingDevice.AssetTag;
                mappedResult.Warnings.Add($"Duplicate serial number detected. Device already exists with Asset Tag: {existingDevice.AssetTag}");
            }
        }

        // 3. Quantity Enforcement for Purchase Item integration
        if (!string.IsNullOrWhiteSpace(data.PurchaseItemId))
        {
            var purchaseItem = await _context.PurchaseItems
                .FirstOrDefaultAsync(pi => pi.Id == data.PurchaseItemId);

            if (purchaseItem != null)
            {
                if (purchaseItem.RemainingQuantity <= 0)
                {
                    mappedResult.RegistrationBlocked = true;
                    mappedResult.RequiresReview = true;
                    mappedResult.Warnings.Add($"Registration blocked: Purchase Item '{purchaseItem.ModelName}' has already met its quantity limit ({purchaseItem.Quantity}).");
                }
            }
        }
    }
}
