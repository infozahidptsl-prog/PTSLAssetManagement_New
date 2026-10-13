USE [AI-Powered_AssetManagementSystem];

-- Migrate Employees
INSERT INTO Employees (Name, EmployeeId, Designation, Email, Contact, IsActive, IsTeamAccount)
SELECT 
    FullName, 
    EmployeeID, 
    Designation, 
    Email, 
    PhoneNumber,
    IsActive,
    0
FROM [AIPoweredAssetManagementSystem].[dbo].[SystemUsers];

-- Migrate Devices
INSERT INTO Devices (Serial, Category, Status, AssetTag, Id, Brand, DeviceName, [From], [To])
SELECT 
    SerialNumber,
    DeviceType,
    CASE WHEN StatusID = 1 THEN 0 WHEN StatusID = 2 THEN 1 WHEN StatusID = 3 THEN 2 ELSE 3 END,
    'MIGRATE-' + CAST(AssetID as varchar),
    NEWID(),
    'Unknown Brand',
    DeviceType + ' ' + SerialNumber,
    '',
    ''
FROM [AIPoweredAssetManagementSystem].[dbo].[CoreAssets];
