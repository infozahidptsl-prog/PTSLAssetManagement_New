using System;
using System.Collections.Generic;
using AssetManagement.Core.Entities;

namespace AssetManagement.Core.Interfaces;

public enum FieldStatus
{
    Extracted,
    NeedsReview,
    Missing,
    Conflicting,
    ManuallyCorrected
}

public class ExtractedField<T>
{
    public T? Value { get; set; }
    public FieldStatus Status { get; set; } = FieldStatus.Extracted;
    public double? Confidence { get; set; }
    public string? SourceDocument { get; set; }
    public int? SourcePage { get; set; }
    public string? SupportingText { get; set; }

    public ExtractedField() { }
    public ExtractedField(T? value, FieldStatus status = FieldStatus.Extracted, double? confidence = null, string? sourceDoc = null, int? page = null, string? text = null)
    {
        Value = value;
        Status = status;
        Confidence = confidence;
        SourceDocument = sourceDoc;
        SourcePage = page;
        SupportingText = text;
    }
}

public class CanonicalPurchaseDto
{
    public string? DocumentType { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public string? ChallanNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string? Vendor { get; set; }
    public string? VendorAddress { get; set; }
    public string? VendorContact { get; set; }
    public string? Currency { get; set; } = "BDT";
    public string? PaymentTerms { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Discount { get; set; }
    public decimal ShippingCharges { get; set; }
    public decimal Total { get; set; }
    public decimal NetPayable { get; set; }
    public string? Notes { get; set; }
    
    public List<CanonicalPurchaseItemDto> Items { get; set; } = new();
}

public class CanonicalPurchaseItemDto
{
    public string? Description { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? PartNumber { get; set; }
    public string? SerialNumber { get; set; }
    public List<string> SerialNumbers { get; set; } = new();
    public int Quantity { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal LineTotal { get; set; }
    public string? Warranty { get; set; }
    public int? WarrantyMonths { get; set; }
    public string? Processor { get; set; }
    public string? Ram { get; set; }
    public string? Ssd { get; set; }
    public string? Hdd { get; set; }
    public string? PurchaseReference { get; set; }
    public double Confidence { get; set; }
    public int? SourcePage { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public class CanonicalDeviceDto
{
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? DeviceName { get; set; }
    public string? SerialNumber { get; set; }
    public List<string> CandidateSerials { get; set; } = new();
    public string? PartNumber { get; set; }
    public string? SNID { get; set; }
    public string? EAN { get; set; }
    public string? AssetType { get; set; }
    public string? Category { get; set; } = "Laptop";
    public string? Processor { get; set; }
    public string? ProcessorFamily { get; set; }
    public string? Ram { get; set; }
    public string? Ssd { get; set; }
    public string? Hdd { get; set; }
    public string? Display { get; set; }
    public int? WarrantyMonths { get; set; }
    public decimal? AssetAmount { get; set; }
    public string OwnerCompany { get; set; } = "PrimeTech Solutions Ltd.";
    public string? Condition { get; set; } = "New";
    public string? PurchaseItemId { get; set; }
    public double Confidence { get; set; }
    public int? SourcePage { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public class MappingValidationResult<T>
{
    public T? Data { get; set; }
    public bool RequiresReview { get; set; }
    public bool DuplicateDetected { get; set; }
    public bool RegistrationBlocked { get; set; }
    public List<string> Warnings { get; set; } = new();
    public string? ExistingReference { get; set; }
}

public interface IAiFieldMappingService
{
    Task<MappingValidationResult<CanonicalPurchaseDto>> MapPurchaseAsync(AiDocumentExtractionResult extractionResult);
    Task<MappingValidationResult<CanonicalDeviceDto>> MapDeviceAsync(AiDocumentExtractionResult extractionResult);
}
