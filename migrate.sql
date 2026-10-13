USE [AI-Powered_AssetManagementSystem];

-- Migrate Employees
INSERT INTO Employees (Name, EmployeeId, Designation, Email, Contact, IsActive)
SELECT 
    FullName, 
    EmployeeID, 
    Designation, 
    Email, 
    PhoneNumber,
    IsActive
FROM [AIPoweredAssetManagementSystem].[dbo].[SystemUsers];

-- Migrate Devices
INSERT INTO Devices (Serial, Category, Status, AssetTag, Id)
SELECT 
    SerialNumber,
    DeviceType,
    CASE WHEN StatusID = 1 THEN 0 WHEN StatusID = 2 THEN 1 WHEN StatusID = 3 THEN 2 ELSE 3 END,
    'MIGRATE-' + CAST(AssetID as varchar),
    NEWID()
FROM [AIPoweredAssetManagementSystem].[dbo].[CoreAssets];
