using System;
using System.ComponentModel.DataAnnotations;
using AssetManagement.Core.Enums;

namespace AssetManagement.Core.Entities;

public class Device
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? AssetTag { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Serial { get; set; } = string.Empty;
    public string Processor { get; set; } = string.Empty;
    public string ProcessorFamily { get; set; } = string.Empty;
    public string Ram { get; set; } = string.Empty;
    public string? Ssd { get; set; }
    public string? Hdd { get; set; }
    public string? MonitorDisplay { get; set; }
    public string? EmployeeId { get; set; }
    public string? UserName { get; set; }
    public string? Contact { get; set; }
    public string? Email { get; set; }
    
    // Network Identifiers
    public string? MacAddress { get; set; }
    public string? Imei { get; set; }
    public string? Designation { get; set; }
    public string? ReceivedDate { get; set; }
    public string? ReturnDate { get; set; }
    public string? Condition { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public string? Released { get; set; }
    public string? Note { get; set; }
    public decimal? AssetAmount { get; set; }
    public DeviceStatus Status { get; set; }
    public string Category { get; set; } = "Laptop";
    
    [Required(ErrorMessage = "Owner / Company is required")]
    public string OwnerCompany { get; set; } = "PrimeTech Solutions Ltd."; // Defaults to PrimeTech Solutions Ltd.
    
    public string? Location { get; set; } // Physical office / branch location (e.g. TK Bhaban, BSEC Bhaban)
    
    // Warranty and Procurement
    public DateTime? ProcurementDate { get; set; }
    public bool HasWarranty { get; set; }
    public string? InvoiceFilePath { get; set; }
    public int? WarrantyMonths { get; set; }
    
    // Purchase Integration
    public string? PurchaseItemId { get; set; }
    public PurchaseItem? PurchaseItem { get; set; }
    
    // Component Tracking
    public bool IsComponent { get; set; } = false;
    public string? ParentDeviceId { get; set; }
    public Device? ParentDevice { get; set; }
    
    public string DeviceTypeDisplay 
    {
        get
        {
            if (IsComponent) return "Component";
            return CategoryTypeRegistry.GetType(Category);
        }
    }

    public static void SyncCategoryTypes(IEnumerable<DeviceCategoryModel>? categories)
        => CategoryTypeRegistry.Sync(categories);

    public static void SetCategoryType(string categoryName, string categoryType)
        => CategoryTypeRegistry.SetType(categoryName, categoryType);

    public static void RemoveCategoryType(string categoryName)
        => CategoryTypeRegistry.Remove(categoryName);

    public static string GetDeviceTypeForCategory(string? category)
        => CategoryTypeRegistry.GetType(category);

    
    public DateTime? WarrantyEndDate 
    { 
        get 
        {
            if (HasWarranty && ProcurementDate.HasValue && WarrantyMonths.HasValue)
            {
                return ProcurementDate.Value.AddMonths(WarrantyMonths.Value);
            }
            return null;
        }
    }
    
    // QR Token Security
    public string? QrTokenHash { get; set; }
    public DateTime? QrTokenCreatedAt { get; set; }
    public DateTime? QrTokenRevokedAt { get; set; }
    public int QrTokenVersion { get; set; } = 1;
}
