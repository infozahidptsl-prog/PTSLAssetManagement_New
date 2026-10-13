using System;

namespace AssetManagement.Core.Enums
{
    public enum FindingSeverity
    {
        Info = 1,
        Warning = 2,
        Critical = 3
    }

    public enum FindingStatus
    {
        Open = 1,
        Resolved = 2,
        Dismissed = 3
    }

    public enum AutomationRuleType
    {
        DeadAsset = 1,
        AgingDevice = 2,
        DamageRepair = 3,
        WarrantyExpiring = 4,
        MissingInformation = 5,
        DuplicateSerial = 6,
        PurchaseRegistrationMismatch = 7,
        UnallocatedAsset = 8,
        AllocationAnomaly = 9,
        ReturnRepairAnomaly = 10
    }
}
