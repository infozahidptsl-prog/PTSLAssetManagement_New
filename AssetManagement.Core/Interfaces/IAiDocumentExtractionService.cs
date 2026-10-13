using System;
using System.Collections.Generic;

namespace AssetManagement.Core.Interfaces;

public class AiDocumentExtractionResult
{
    public bool IsSuccess { get; set; }
    public string? DocumentType { get; set; }
    public string? Vendor { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public string? ChallanNumber { get; set; }
    public DateTime? DocumentDate { get; set; }
    public List<AiDocumentExtractionItem> Items { get; set; } = new();
    
    public string? VendorAddress { get; set; }
    public string? VendorContact { get; set; }
    public string? Currency { get; set; } = "BDT";
    public string? PaymentTerms { get; set; }
    public decimal? Subtotal { get; set; }
    public decimal? Tax { get; set; }
    public decimal? Discount { get; set; }
    public decimal? ShippingCharges { get; set; }
    public decimal? TotalAmount { get; set; }
    public decimal? NetPayable { get; set; }
    public string? Notes { get; set; }
    
    // Physical Label / Specification specific fields
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? Version { get; set; }
    public string? PartNumber { get; set; }
    public string? SerialNumber { get; set; }
    public string? EAN { get; set; }
    public string? SNID { get; set; }
    public string? ManufacturingDate { get; set; }
    public string? Processor { get; set; }
    public string? Ram { get; set; }
    public string? Ssd { get; set; }
    public string? Hdd { get; set; }
    public string? Display { get; set; }
    public string? DeviceType { get; set; }
    public string? Warranty { get; set; }
    public List<string> CandidateSerials { get; set; } = new();

    public double Confidence { get; set; }
    public List<string> Warnings { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class AiDocumentExtractionItem
{
    public string? Description { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public List<string> SerialNumbers { get; set; } = new();
    public string? PartNumber { get; set; }
    public int? Quantity { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? Discount { get; set; }
    public decimal? Tax { get; set; }
    public decimal? LineTotal { get; set; }
    public string? Warranty { get; set; }
    public int? WarrantyMonths { get; set; }
    public string? Processor { get; set; }
    public string? Ram { get; set; }
    public string? Ssd { get; set; }
    public string? Hdd { get; set; }
}

public interface IAiDocumentExtractionService
{
    Task<AiDocumentExtractionResult> ExtractDocumentAsync(string rawText, string imageBase64);
}
