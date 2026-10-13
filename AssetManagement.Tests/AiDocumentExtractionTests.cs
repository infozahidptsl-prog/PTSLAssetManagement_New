using System.IO;
using System.Threading.Tasks;
using AssetManagement.Core.Entities;
using AssetManagement.Infrastructure.Services;
using AssetManagement.Tests.Helpers;
using Xunit;

namespace AssetManagement.Tests;

public class AiDocumentExtractionTests
{
    private readonly OllamaAiDocumentExtractionService _extractionService;
    private readonly string _ocrOutputText;

    public AiDocumentExtractionTests()
    {
        _extractionService = new OllamaAiDocumentExtractionService(new System.Net.Http.HttpClient());
        
        // Load the Global Brand PLC OCR output from root directory
        var ocrPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "ocr_output.txt");
        if (!File.Exists(ocrPath))
        {
            ocrPath = Path.Combine(Directory.GetCurrentDirectory(), "ocr_output.txt");
        }
        
        if (File.Exists(ocrPath))
        {
            _ocrOutputText = File.ReadAllText(ocrPath);
        }
        else
        {
            // Fallback inline sample
            _ocrOutputText = @"
Global Brand PLC
Date: 03-Aug-2026
PON: PNL/GBPL/03/08/26-094
CHL-GBPAB-2026 012957
1 | ASUS EXPERTBOOK B3 > BSOSCCA-LYI806 | 1 Pes 107,500.00 107,500.00
SIN: WINXLPO2D 79603.
2 | ASUS EXPERTBOOK?S GI> PMOSCCALYOZ9 | 2 Po: 11S900.00 230,000.00
NetAmoust: Tk 337,500.00
";
        }
    }

    [Fact]
    public async Task ExtractDocument_GlobalBrandInvoice_ExtractsAllExpectedFields()
    {
        // Act
        var result = await _extractionService.ExtractDocumentAsync(_ocrOutputText, "");

        // Assert Header
        Assert.True(result.IsSuccess);
        Assert.Equal("Global Brand PLC", result.Vendor);
        Assert.Equal("INV-GBPAB-2026-012957", result.InvoiceNumber);
        Assert.Equal("PNL/GBPL/03/08/26-094", result.PurchaseOrderNumber);
        Assert.Equal("BDT", result.Currency);
        Assert.NotNull(result.DocumentDate);
        Assert.Equal(8, result.DocumentDate.Value.Month);
        Assert.Equal(2026, result.DocumentDate.Value.Year);

        // Assert Line Items
        Assert.Equal(2, result.Items.Count);

        // Item 1
        var item1 = result.Items[0];
        Assert.Equal("ASUS ExpertBook B3", item1.Description);
        Assert.Equal("ASUS", item1.Manufacturer);
        Assert.Equal("B3405CCA-LY1806", item1.Model);
        Assert.Equal(1, item1.Quantity);
        Assert.Equal(107500.00m, item1.UnitPrice);
        Assert.Equal(107500.00m, item1.LineTotal);
        Assert.Equal("WINXLP02D79603E", item1.SerialNumber);

        // Item 2
        var item2 = result.Items[1];
        Assert.Equal("ASUS ExpertBook P3 G1", item2.Description);
        Assert.Equal("ASUS", item2.Manufacturer);
        Assert.Equal("P3406CCA-LY0279", item2.Model);
        Assert.Equal(2, item2.Quantity);
        Assert.Equal(115000.00m, item2.UnitPrice);
        Assert.Equal(230000.00m, item2.LineTotal);
        Assert.Contains("W4NXLP074930175", item2.SerialNumbers);
        Assert.Contains("W4NXLP074932176", item2.SerialNumbers);

        // Assert Grand Total
        Assert.Equal(337500.00m, result.TotalAmount);
    }

    [Fact]
    public async Task ValidationService_TotalReconciliation_DetectsDiscrepancies()
    {
        using var context = TestDbContextFactory.Create();
        var mappingService = new AiFieldMappingService(context);
        var validationService = new AiValidationService(context);

        var extraction = await _extractionService.ExtractDocumentAsync(_ocrOutputText, "");
        var mappingResult = await mappingService.MapPurchaseAsync(extraction);

        // Manually introduce discrepancy
        mappingResult.Data!.Total = 999999.00m;
        await validationService.ValidatePurchaseAsync(mappingResult);

        Assert.True(mappingResult.RequiresReview);
        Assert.Contains(mappingResult.Warnings, w => w.Contains("does not reconcile"));
    }

    [Fact]
    public async Task ValidationService_DuplicateInvoice_DetectsExistingPurchase()
    {
        using var context = TestDbContextFactory.Create();
        context.Purchases.Add(new Purchase
        {
            PurchaseNumber = "PUR-TEST-100",
            Supplier = "Global Brand PLC",
            InvoiceNumber = "INV-GBPAB-2026-012957"
        });
        await context.SaveChangesAsync();

        var mappingService = new AiFieldMappingService(context);
        var validationService = new AiValidationService(context);

        var extraction = await _extractionService.ExtractDocumentAsync(_ocrOutputText, "");
        var mappingResult = await mappingService.MapPurchaseAsync(extraction);

        await validationService.ValidatePurchaseAsync(mappingResult);

        Assert.True(mappingResult.DuplicateDetected);
        Assert.True(mappingResult.RequiresReview);
        Assert.Contains(mappingResult.Warnings, w => w.Contains("Duplicate invoice detected"));
    }

    [Fact]
    public async Task DeviceService_PhysicalAssetRegistration_EnforcesRemainingQuantityAndUniqueSerial()
    {
        using var context = TestDbContextFactory.Create();
        var deviceService = new DeviceService(context);

        var purchase = new Purchase
        {
            PurchaseNumber = "PUR-2601",
            Supplier = "Global Brand PLC",
            InvoiceNumber = "INV-001"
        };
        var purchaseItem = new PurchaseItem
        {
            Purchase = purchase,
            Manufacturer = "ASUS",
            ModelName = "ExpertBook B3",
            Quantity = 1,
            RegisteredQuantity = 0
        };
        context.Purchases.Add(purchase);
        context.PurchaseItems.Add(purchaseItem);
        await context.SaveChangesAsync();

        // 1. First registration should succeed
        var dev1 = new Device
        {
            Serial = "WINXLP02D79603E",
            Brand = "ASUS",
            DeviceName = "ExpertBook B3",
            Category = "Laptop"
        };
        var registered1 = await deviceService.RegisterPhysicalAssetFromPurchaseAsync(dev1, purchaseItem.Id);
        Assert.NotNull(registered1);
        Assert.Contains("LPT-", registered1.AssetTag);
        Assert.StartsWith("PSL-", registered1.AssetTag);
        Assert.Equal(1, purchaseItem.RegisteredQuantity);
        Assert.Equal(0, purchaseItem.RemainingQuantity);

        // 2. Second registration with same serial must fail with duplicate serial exception
        var devDuplicateSerial = new Device
        {
            Serial = "WINXLP02D79603E",
            Brand = "ASUS",
            DeviceName = "ExpertBook B3"
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            deviceService.RegisterPhysicalAssetFromPurchaseAsync(devDuplicateSerial, purchaseItem.Id));

        // 3. Third registration with different serial must fail because remaining quantity is 0
        var devQuantityExceeded = new Device
        {
            Serial = "NEWSERIAL12345",
            Brand = "ASUS",
            DeviceName = "ExpertBook B3"
        };
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            deviceService.RegisterPhysicalAssetFromPurchaseAsync(devQuantityExceeded, purchaseItem.Id));
        Assert.Contains("Quantity limit reached", ex.Message);
    }

    [Fact]
    public async Task ExtractDocument_SouthBanglaCiscoChallan_ExtractsVendorModelAndSerials()
    {
        var sampleText = @"
SBC SOUTH BANGLA
computers
Customer Name | PRIME NET LIMITED. Delivery From: HEAD OFFICE.
CHL-SBCHO-2026-005533
Code | cus-suctio-2018-002520
Address TK BHABAN (7TH FLOOR),13 KAZI NAZRUL ISLAM AVENUE KAWRAN BAZAR ,DHAKA-1215 | TNV-SBCHO-2026-005533
Product Name Packing Qty
CISCO Catalyst 1300 24-port GE, 4x1G SEP MODEL:C1300-24T-4G 2.00 Pes
| | 1DT33003080Y 1DT33003082V
Fring Due & Time : 27-Aug-2026 12.02 pom
";
        var result = await _extractionService.ExtractDocumentAsync(sampleText, "");

        Assert.True(result.IsSuccess);
        Assert.Equal("South Bangla Computers", result.Vendor);
        Assert.Equal("INV-SBCHO-2026-005533", result.InvoiceNumber);
        Assert.Equal("CHL-SBCHO-2026-005533", result.ChallanNumber);
        Assert.NotNull(result.DocumentDate);
        Assert.Equal(8, result.DocumentDate.Value.Month);
        Assert.Equal(2026, result.DocumentDate.Value.Year);

        Assert.Single(result.Items);
        var item = result.Items[0];
        Assert.Equal("Cisco", item.Manufacturer);
        Assert.Equal("C1300-24T-4G", item.Model);
        Assert.Equal(2, item.Quantity);
        Assert.Contains("DT33003080Y", item.SerialNumbers);
        Assert.Contains("DT33003082V", item.SerialNumbers);
    }

    [Fact]
    public async Task ExtractDocument_AcerMonitorSticker_ExtractsModelSerialSnid()
    {
        var imgPath = @"C:\Users\ZAHID_PTSL_2\Desktop\Invoice\Serial.png";
        var ocr = new TesseractOcrService();
        using var stream = File.OpenRead(imgPath);
        var ocrRes = await ocr.ExtractTextAsync(stream, "serial.png", "image/png");
        
        System.Console.WriteLine("=== ACTUAL OCR TEXT ===");
        System.Console.WriteLine(ocrRes.Text);
        System.Console.WriteLine("=== END OCR TEXT ===");

        var result = await _extractionService.ExtractDocumentAsync(ocrRes.Text ?? "", "");

        Assert.Equal("Acer", result.Manufacturer);
        Assert.Equal("K202HQL", result.Model);
        Assert.Equal("MMT1KSS0173250CE194228", result.SerialNumber);
    }

    [Fact]
    public async Task ExtractDocument_GlobalBrandDeliveryChallan_ExtractsPoChallanDateAndItems()
    {
        var imgPath = @"C:\Users\ZAHID_PTSL_2\Desktop\Invoice\WhatsApp Image 2026-10-06 at 5.26.25 PM.jpeg";
        var ocr = new TesseractOcrService();
        using var stream = File.OpenRead(imgPath);
        var ocrRes = await ocr.ExtractTextAsync(stream, "challan.jpg", "image/jpeg");
        
        System.Console.WriteLine("=== GLOBAL BRAND CHALLAN OCR TEXT ===");
        System.Console.WriteLine(ocrRes.Text);
        System.Console.WriteLine("=== END CHALLAN OCR TEXT ===");

        var result = await _extractionService.ExtractDocumentAsync(ocrRes.Text ?? "", "");

        Assert.Equal("Global Brand PLC", result.Vendor);
        Assert.Equal("PNL/GBPL/03/08/26-094", result.PurchaseOrderNumber);
        Assert.Contains("SO-GBPAB-SALAHUDDIN-2026-0357", result.ChallanNumber);
        Assert.NotNull(result.DocumentDate);
        Assert.Equal(9, result.DocumentDate.Value.Month);
        Assert.Equal(2026, result.DocumentDate.Value.Year);
    }
}
