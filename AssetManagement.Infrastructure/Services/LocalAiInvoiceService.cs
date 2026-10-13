using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AssetManagement.Core.Interfaces;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace AssetManagement.Infrastructure.Services;

public class LocalAiInvoiceService : IInvoiceAutomationService
{
    public async Task<InvoiceExtractionResult> ExtractInvoiceDataAsync(Stream fileStream, string fileName, string contentType)
    {
        if (contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            var text = await ExtractTextFromPdfAsync(fileStream);
            return await AnalyzeTextAsync(text);
        }
        else
        {
            // For images, we expect the frontend to extract text via OCR (Tesseract.js) and call AnalyzeTextAsync directly.
            // If it hits here, it means backend OCR wasn't implemented for images.
            return new InvoiceExtractionResult 
            { 
                IsSuccess = false, 
                ErrorMessage = "Image OCR is not supported directly on the backend. Please use the frontend scanner." 
            };
        }
    }

    private Task<string> ExtractTextFromPdfAsync(Stream stream)
    {
        return Task.Run(() => 
        {
            try
            {
                using var document = PdfDocument.Open(stream);
                var fullText = new System.Text.StringBuilder();
                
                foreach (var page in document.GetPages())
                {
                    var text = ContentOrderTextExtractor.GetText(page);
                    fullText.AppendLine(text);
                }
                
                return fullText.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to read PDF: {ex.Message}");
            }
        });
    }

    public Task<InvoiceExtractionResult> AnalyzeTextAsync(string text)
    {
        // 1. Try to use Local AI (Ollama)
        // 2. If it fails or is unavailable, use RegEx fallback parser.
        // For this environment, since Ollama failed to start, we directly fallback to Regex pattern matching.
        
        var result = new InvoiceExtractionResult
        {
            IsSuccess = true,
            RawText = text,
            UsedAi = false // Indicate we fell back to regex parser
        };
        
        try 
        {
            // Simple mock extraction based on Regex for testing
            result.Supplier = ExtractRegex(text, @"(?:Vendor|Supplier|From):\s*(.*?)(?=\r|\n|$)") ?? "Unknown Supplier";
            result.InvoiceNumber = ExtractRegex(text, @"(?:Invoice\s*No|INV-?#?):\s*([A-Za-z0-9-]+)") ?? "INV-0000";
            result.PoNumber = ExtractRegex(text, @"(?:PO\s*No|PO-?#?):\s*([A-Za-z0-9-]+)");
            
            if (DateTime.TryParse(ExtractRegex(text, @"(?:Date):\s*(\d{2,4}[-/]\d{1,2}[-/]\d{1,4})"), out var dt))
                result.InvoiceDate = dt;
            else
                result.InvoiceDate = null; // Do not invent dates!
                
            // Very naive line item extraction
            // E.g. "Dell Latitude 5000 5 1000.00 5000.00"
            var lineItemMatches = Regex.Matches(text, @"(?<desc>[A-Za-z\s0-9-]+)\s+(?<qty>\d+)\s+(?<price>\d+\.\d{2})\s+(?<total>\d+\.\d{2})");
            foreach (Match match in lineItemMatches)
            {
                if (match.Success)
                {
                    result.Items.Add(new InvoiceExtractionItem
                    {
                        Description = match.Groups["desc"].Value.Trim(),
                        Manufacturer = "Unknown",
                        Model = "Unknown",
                        Quantity = int.Parse(match.Groups["qty"].Value),
                        UnitPrice = decimal.Parse(match.Groups["price"].Value),
                        TotalPrice = decimal.Parse(match.Groups["total"].Value),
                        IsLowConfidence = true
                    });
                }
            }
            
            // If no items found, we do NOT mock one for testing. We leave it empty as per user instructions.
            // if (result.Items.Count == 0) { ... }

            result.Subtotal = result.Items.Sum(i => i.TotalPrice);
            result.Total = result.Subtotal; // Ignoring tax for mock
            result.Currency = "USD";
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = $"Failed to parse text: {ex.Message}";
        }

        return Task.FromResult(result);
    }
    
    private string? ExtractRegex(string text, string pattern)
    {
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}
