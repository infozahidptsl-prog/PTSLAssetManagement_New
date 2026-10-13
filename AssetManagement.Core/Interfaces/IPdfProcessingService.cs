using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AssetManagement.Core.Interfaces
{
    public class PdfPageResult
    {
        public int PageNumber { get; set; }
        public byte[] ImageBytes { get; set; } = Array.Empty<byte>();
        public string OcrStatus { get; set; } = string.Empty;
        public List<DecodedBarcodeResult> Barcodes { get; set; } = new List<DecodedBarcodeResult>();
        public string AiStatus { get; set; } = string.Empty;
        public string ExtractionResultJson { get; set; } = string.Empty;
    }

    public interface IPdfProcessingService
    {
        Task<List<PdfPageResult>> ProcessPdfAsync(Stream pdfStream);
    }
}
