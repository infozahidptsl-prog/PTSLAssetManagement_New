using System;
using System.Linq;
using System.Threading.Tasks;
using AssetManagement.Core.Interfaces;
using AssetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Services;

public class AiFieldMappingService : IAiFieldMappingService
{
    private readonly ApplicationDbContext _context;

    public AiFieldMappingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MappingValidationResult<CanonicalPurchaseDto>> MapPurchaseAsync(AiDocumentExtractionResult extractionResult)
    {
        var result = new MappingValidationResult<CanonicalPurchaseDto>
        {
            Data = new CanonicalPurchaseDto()
        };

        if (extractionResult == null) return result;

        result.Data.DocumentType = extractionResult.DocumentType;
        result.Data.InvoiceNumber = !string.IsNullOrWhiteSpace(extractionResult.InvoiceNumber) ? extractionResult.InvoiceNumber : extractionResult.ChallanNumber;
        result.Data.PurchaseOrderNumber = extractionResult.PurchaseOrderNumber;
        result.Data.ChallanNumber = extractionResult.ChallanNumber;
        result.Data.InvoiceDate = extractionResult.DocumentDate;
        result.Data.Currency = string.IsNullOrWhiteSpace(extractionResult.Currency) ? "BDT" : extractionResult.Currency;
        result.Data.VendorAddress = extractionResult.VendorAddress;
        result.Data.VendorContact = extractionResult.VendorContact;
        result.Data.PaymentTerms = extractionResult.PaymentTerms;
        result.Data.Notes = extractionResult.Notes;

        // Vendor Mapping
        result.Data.Vendor = extractionResult.Vendor;
        if (!string.IsNullOrWhiteSpace(extractionResult.Vendor))
        {
            var knownSuppliers = await _context.Purchases
                .Select(p => p.Supplier)
                .Distinct()
                .ToListAsync();

            var matches = knownSuppliers
                .Where(s => s.Contains(extractionResult.Vendor, StringComparison.OrdinalIgnoreCase) ||
                            extractionResult.Vendor.Contains(s, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 1)
            {
                result.Data.Vendor = matches[0];
            }
            else if (matches.Count > 1)
            {
                result.Warnings.Add($"Multiple potential matches found for Vendor '{extractionResult.Vendor}'. Please select manually.");
                result.RequiresReview = true;
            }
        }

        // Map Items
        if (extractionResult.Items != null)
        {
            foreach (var item in extractionResult.Items)
            {
                var canonicalItem = new CanonicalPurchaseItemDto
                {
                    Description = item.Description ?? string.Empty,
                    Manufacturer = item.Manufacturer ?? string.Empty,
                    Model = item.Model ?? string.Empty,
                    SerialNumber = item.SerialNumber,
                    SerialNumbers = item.SerialNumbers.ToList(),
                    Quantity = item.Quantity ?? 0,
                    UnitPrice = item.UnitPrice ?? 0m,
                    LineTotal = item.LineTotal ?? (item.Quantity ?? 0) * (item.UnitPrice ?? 0m),
                    Warranty = item.Warranty,
                    Processor = item.Processor,
                    Ram = item.Ram,
                    Ssd = item.Ssd,
                    Confidence = extractionResult.Confidence
                };

                result.Data.Items.Add(canonicalItem);
            }
        }

        // Subtotal & Totals
        decimal computedSubtotal = result.Data.Items.Sum(i => i.LineTotal);
        result.Data.Subtotal = extractionResult.Subtotal ?? computedSubtotal;
        result.Data.Tax = extractionResult.Tax ?? 0m;
        result.Data.Discount = extractionResult.Discount ?? 0m;
        result.Data.ShippingCharges = extractionResult.ShippingCharges ?? 0m;
        result.Data.Total = extractionResult.TotalAmount ?? (result.Data.Subtotal + result.Data.Tax + result.Data.ShippingCharges - result.Data.Discount);

        return result;
    }

    public async Task<MappingValidationResult<CanonicalDeviceDto>> MapDeviceAsync(AiDocumentExtractionResult extractionResult)
    {
        var result = new MappingValidationResult<CanonicalDeviceDto>
        {
            Data = new CanonicalDeviceDto()
        };

        if (extractionResult == null) return result;

        result.Data.Manufacturer = extractionResult.Manufacturer;
        result.Data.Model = extractionResult.Model;
        result.Data.SerialNumber = extractionResult.SerialNumber;
        result.Data.SNID = extractionResult.SNID;
        result.Data.Confidence = extractionResult.Confidence;
        result.Data.CandidateSerials = extractionResult.CandidateSerials.ToList();

        // Hardware specs
        result.Data.Processor = extractionResult.Processor;
        result.Data.Ram = extractionResult.Ram;
        result.Data.Ssd = extractionResult.Ssd;
        result.Data.Hdd = extractionResult.Hdd;
        result.Data.Display = extractionResult.Display;

        // Manufacturer Canonical Mapping
        if (!string.IsNullOrWhiteSpace(extractionResult.Manufacturer))
        {
            var knownBrands = await _context.Devices
                .Select(d => d.Brand)
                .Distinct()
                .ToListAsync();

            var matches = knownBrands
                .Where(b => b.Contains(extractionResult.Manufacturer, StringComparison.OrdinalIgnoreCase) ||
                            extractionResult.Manufacturer.Contains(b, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 1)
            {
                result.Data.Manufacturer = matches[0];
            }
            else if (matches.Count > 1)
            {
                result.Warnings.Add($"Multiple potential matches found for Manufacturer '{extractionResult.Manufacturer}'. Please select manually.");
                result.RequiresReview = true;
            }
        }

        return result;
    }
}
