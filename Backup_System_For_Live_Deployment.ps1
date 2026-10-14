# ==============================================================================
# PrimeTech Asset Management - Live Deployment Backup Script
# Generates a complete SQL Server .bak file and packages all uploaded media/invoices
# ==============================================================================

$ErrorActionPreference = "Stop"

$ProjectRoot = "g:\Development\AssetManagement_New"
$DatabaseName = "AI-Powered_AssetManagementSystem"
$ServerInstance = ".\SQLEXPRESS"
$BackupDate = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupDir = Join-Path $ProjectRoot "DeploymentBackup"
$TempDir = Join-Path $BackupDir "AssetManagement_LiveBundle_$BackupDate"
$BakFile = Join-Path $TempDir "$($DatabaseName).bak"
$UploadsSource = Join-Path $ProjectRoot "AssetManagement.Web\wwwroot\uploads"
$UploadsTarget = Join-Path $TempDir "uploads"
$ZipTarget = Join-Path $BackupDir "AssetManagement_ReadyForLive_$BackupDate.zip"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Starting Backup for Live IIS & SQL Server Deployment     " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Ensure Directories Exist
if (!(Test-Path $TempDir)) {
    New-Item -ItemType Directory -Path $TempDir -Force | Out-Null
}

# 2. Backup SQL Server Database
Write-Host "`n[1/3] Backing up SQL Server database '$DatabaseName'..." -ForegroundColor Yellow
$SqlConnection = New-Object System.Data.SqlClient.SqlConnection("Server=$ServerInstance;Database=master;Trusted_Connection=True;TrustServerCertificate=True;")
try {
    $SqlConnection.Open()
    $SqlCommand = $SqlConnection.CreateCommand()
    $SqlCommand.CommandTimeout = 120
    $SqlCommand.CommandText = "BACKUP DATABASE [$DatabaseName] TO DISK = '$BakFile' WITH INIT, FORMAT, STATS = 10"
    $SqlCommand.ExecuteNonQuery() | Out-Null
    Write-Host "  -> SQL Database backup completed successfully: $BakFile" -ForegroundColor Green
}
catch {
    Write-Host "  -> Error backing up SQL Server: $_" -ForegroundColor Red
    throw $_
}
finally {
    if ($SqlConnection.State -eq 'Open') { $SqlConnection.Close() }
}

# 3. Copy Physical Uploads (Invoices, Challans, Requisitions)
Write-Host "`n[2/3] Collecting all uploaded files (Invoices, Challans, Attachments)..." -ForegroundColor Yellow
if (Test-Path $UploadsSource) {
    Copy-Item -Path $UploadsSource -Destination $TempDir -Recurse -Force
    $fileCount = (Get-ChildItem -Path $UploadsTarget -Recurse -File).Count
    Write-Host "  -> Copied $fileCount upload file(s) into bundle." -ForegroundColor Green
} else {
    Write-Host "  -> Notice: uploads directory not found at $UploadsSource." -ForegroundColor DarkYellow
}

# 4. Create Step-by-Step Restoration Guide
$ReadmeContent = @"
========================================================================
PRIMETECH ASSET MANAGEMENT - LIVE SERVER DEPLOYMENT GUIDE
========================================================================
Bundle Generated: $(Get-Date -Format "dd-MMM-yyyy HH:mm:ss")
Database Backup : $($DatabaseName).bak
Uploads Folder  : uploads\

------------------------------------------------------------------------
STEP 1: RESTORE SQL SERVER DATABASE
------------------------------------------------------------------------
1. Copy '$($DatabaseName).bak' to your Live SQL Server machine.
2. Open SQL Server Management Studio (SSMS).
3. Right-click 'Databases' -> 'Restore Database...'.
4. Select 'Device' -> Click '...' -> Add '$($DatabaseName).bak'.
5. In Options:
   - Check 'Overwrite the existing database (WITH REPLACE)'.
   - Verify the .mdf and .ldf file paths for your live server.
6. Click OK to complete restore.
7. Verify all Users, Assets, Employees, and Settings are intact.
   (Note: Employee profile pictures are stored inside the database
   and are 100% restored automatically).

------------------------------------------------------------------------
STEP 2: DEPLOY WEB APPLICATION TO IIS
------------------------------------------------------------------------
1. Publish the project:
   dotnet publish AssetManagement.Web\AssetManagement.Web.csproj -c Release -o C:\inetpub\wwwroot\AssetManagement
2. Copy the 'uploads\' folder from this bundle into:
   C:\inetpub\wwwroot\AssetManagement\wwwroot\uploads\
3. In IIS Manager:
   - Ensure the Application Pool is .NET CLR Version: 'No Managed Code'.
   - Ensure Application Pool Identity has WRITE & MODIFY permissions on:
     'C:\inetpub\wwwroot\AssetManagement\wwwroot\uploads'
     (Right-click uploads folder -> Properties -> Security -> Add 'IIS_IUSRS' or your AppPool Identity, check 'Modify' and 'Write').
4. In appsettings.json on the live server:
   - Update 'DefaultConnection' to point to your live SQL Server instance and credentials.
5. Browse your site in IIS!
========================================================================
"@

Set-Content -Path (Join-Path $TempDir "README_RESTORE_INSTRUCTIONS.txt") -Value $ReadmeContent

# 5. Compress Everything Into a Single Zip File
Write-Host "`n[3/3] Packaging entire bundle into a deployment ZIP..." -ForegroundColor Yellow
if (Test-Path $ZipTarget) { Remove-Item $ZipTarget -Force }
Compress-Archive -Path "$TempDir\*" -DestinationPath $ZipTarget -Force
Write-Host "  -> Zip archive created: $ZipTarget" -ForegroundColor Green

# 6. Summary
$zipSize = (Get-Item $ZipTarget).Length / 1MB
Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host " BACKUP COMPLETE! READY FOR LIVE DEPLOYMENT               " -ForegroundColor Cyan
Write-Host " Archive file: $ZipTarget" -ForegroundColor White
Write-Host " Total size  : $([math]::Round($zipSize, 2)) MB" -ForegroundColor White
Write-Host " Includes    : Full SQL .bak + All Uploaded Invoices/Images" -ForegroundColor White
Write-Host "==========================================================" -ForegroundColor Cyan
