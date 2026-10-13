USE [AI-Powered_AssetManagementSystem];

-- Migrate Employees
INSERT INTO Employees (Id, Name, EmployeeId, Designation, Email, Contact, IsActive, IsTeamAccount, About, BloodGroup, Department, JoiningDate, LinkedInUrl, Location, Manager, ProfileImageUrl, Team)
SELECT 
    NEWID(),
    ISNULL(FullName, 'Unknown'), 
    ISNULL(EmployeeID, 'UNKNOWN-ID'), 
    ISNULL(Designation, 'Unknown'), 
    ISNULL(Email, ''), 
    ISNULL(PhoneNumber, ''),
    ISNULL(IsActive, 1),
    0, -- IsTeamAccount
    '', '', '', '', '', '', '', '', ''
FROM [AssetManagementSystem].[dbo].[SystemUsers];

-- Migrate Devices
INSERT INTO Devices (Id, Serial, Category, Status, AssetTag, Brand, DeviceName, [From], [To], AssetAmount, Condition, Contact, Designation, Email, EmployeeId, HasWarranty, Hdd, InvoiceFilePath, IsComponent, MonitorDisplay, Note, OwnerCompany, Processor, ProcessorFamily, ProcurementDate, Ram, ReceivedDate, Released, ReturnDate, Ssd, UserName, WarrantyMonths)
SELECT 
    NEWID(),
    ISNULL(SerialNumber, 'Unknown'),
    ISNULL(DeviceType, 'Unknown'),
    CASE WHEN StatusID = 1 THEN 0 WHEN StatusID = 2 THEN 1 WHEN StatusID = 3 THEN 2 ELSE 3 END,
    'MIGRATE-' + CAST(AssetID as varchar),
    'Unknown',
    ISNULL(DeviceType, '') + ' ' + ISNULL(SerialNumber, ''),
    '', '', 
    0, '', '', '', '', '', 0, '', '', 0, '', '', '', 
    'Unknown', 'Unknown', -- Processor fields
    '', '', '', '', '', '', '', 0
FROM [AssetManagementSystem].[dbo].[CoreAssets];

