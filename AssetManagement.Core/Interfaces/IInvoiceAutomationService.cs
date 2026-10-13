using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AssetManagement.Core.Interfaces;

public class InvoiceExtractionItem
{
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    
    // Confidence Flags
    public bool IsLowConfidence { get; set; }
}

public class InvoiceExtractionResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public bool UsedAi { get; set; }
    public string? RawText { get; set; } // The raw OCR text

    public string? Supplier { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? PoNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string? Currency { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    
    public List<InvoiceExtractionItem> Items { get; set; } = new();
    
    // Confidence Flags
    public bool IsLowConfidence { get; set; }
}

public interface IInvoiceAutomationService
{
    Task<InvoiceExtractionResult> ExtractInvoiceDataAsync(Stream fileStream, string fileName, string contentType);
    Task<InvoiceExtractionResult> AnalyzeTextAsync(string text);
}
