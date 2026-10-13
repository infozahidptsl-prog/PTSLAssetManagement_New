using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AssetManagement.Core.Interfaces;
using Docnet.Core;
using Docnet.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AssetManagement.Infrastructure.Services
{
    public class PdfProcessingService : IPdfProcessingService
    {
        private readonly IQrBarcodeDetectionService _barcodeService;
        private readonly IOcrService _ocrService;
        private readonly IAiDocumentExtractionService _aiService;

        public PdfProcessingService(
            IQrBarcodeDetectionService barcodeService,
            IOcrService ocrService,
            IAiDocumentExtractionService aiService)
        {
            _barcodeService = barcodeService;
            _ocrService = ocrService;
            _aiService = aiService;
        }

        public async Task<List<PdfPageResult>> ProcessPdfAsync(Stream pdfStream)
        {
            var results = new List<PdfPageResult>();

            byte[] pdfBytes;
            using (var memoryStream = new MemoryStream())
            {
                await pdfStream.CopyToAsync(memoryStream);
                pdfBytes = memoryStream.ToArray();
            }

            using var docReader = DocLib.Instance.GetDocReader(pdfBytes, new PageDimensions(1.0));
            int pageCount = docReader.GetPageCount();

            for (int i = 0; i < pageCount; i++)
            {
                using var pageReader = docReader.GetPageReader(i);
                
                var width = pageReader.GetPageWidth();
                var height = pageReader.GetPageHeight();
                var rawBytes = pageReader.GetImage(); // BGRA

                // Convert raw bytes to PNG using ImageSharp
                using var image = Image.LoadPixelData<Bgra32>(rawBytes, width, height);
                using var imageStream = new MemoryStream();
                image.SaveAsPng(imageStream);
                imageStream.Position = 0;

                var imageBytes = imageStream.ToArray();
                var base64Image = Convert.ToBase64String(imageBytes);

                // Barcode Detection
                imageStream.Position = 0;
                var barcodes = await _barcodeService.DetectAsync(imageStream);

                // OCR Stage
                imageStream.Position = 0;
                var ocrResult = await _ocrService.ExtractTextAsync(imageStream, $"page_{i}.png", "image/png");
                
                string ocrStatus = ocrResult.IsSuccess ? "SUCCESS" : (ocrResult.ErrorMessage?.Contains("OCR_DEPENDENCY_MISSING") == true ? "OCR_DEPENDENCY_MISSING" : "FAILED");
                string extractedText = ocrResult.Text ?? string.Empty;

                // AI Stage
                string aiStatus = "PENDING";
                string extractionJson = string.Empty;

                var aiResult = await _aiService.ExtractDocumentAsync(extractedText, base64Image);
                if (aiResult.ErrorMessage?.Contains("AI_UNAVAILABLE") == true)
                {
                    aiStatus = "AI_UNAVAILABLE";
                }
                else if (aiResult.IsSuccess)
                {
                    aiStatus = "SUCCESS";
                    extractionJson = JsonSerializer.Serialize(aiResult);
                }
                else
                {
                    aiStatus = "FAILED";
                }

                results.Add(new PdfPageResult
                {
                    PageNumber = i + 1,
                    ImageBytes = imageBytes,
                    OcrStatus = ocrStatus,
                    Barcodes = barcodes,
                    AiStatus = aiStatus,
                    ExtractionResultJson = extractionJson
                });
            }

            return results;
        }
    }
}
