using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AssetManagement.Core.Interfaces;
using Docnet.Core;
using Docnet.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tesseract;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace AssetManagement.Infrastructure.Services;

/// <summary>
/// Real OCR implementation.
/// - Text-based PDFs: text layer is read with PdfPig.
/// - Scanned PDFs (no/very little text layer): each page is rendered with Docnet (pdfium) and OCR'd with Tesseract.
/// - Images: OCR'd directly with Tesseract.
/// Never returns fabricated/mock text. If OCR cannot run, an error is returned instead.
/// </summary>
public class TesseractOcrService : IOcrService
{
    private static readonly object EngineLock = new();
    private readonly string _tessDataPath;

    // Below this many non-whitespace characters, a PDF text layer is considered absent (scanned PDF).
    private const int MinPdfTextChars = 40;
    // Render scale for scanned PDFs (1.0 = 72 DPI). 3.0 ~= 216 DPI which is good for Tesseract.
    private const double PdfRenderScale = 3.0;

    public TesseractOcrService()
    {
        var candidates = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata"),
            Path.Combine(Directory.GetCurrentDirectory(), "tessdata"),
            Path.Combine(Directory.GetCurrentDirectory(), "AssetManagement.Web", "tessdata"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "AssetManagement.Web", "tessdata"),
            @"G:\Development\AssetManagement_New\AssetManagement.Web\tessdata"
        };
        _tessDataPath = candidates.FirstOrDefault(p => File.Exists(Path.Combine(p, "eng.traineddata")))
                        ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
    }

    public async Task<OcrResult> ExtractTextAsync(Stream fileStream, string fileName, string contentType)
    {
        byte[] bytes;
        using (var ms = new MemoryStream())
        {
            if (fileStream.CanSeek) fileStream.Position = 0;
            await fileStream.CopyToAsync(ms);
            bytes = ms.ToArray();
        }

        if (bytes.Length == 0)
            return new OcrResult { IsSuccess = false, ErrorMessage = "Empty file." };

        bool isPdf = (contentType ?? "").Contains("pdf", StringComparison.OrdinalIgnoreCase)
                     || (fileName ?? "").EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

        return await Task.Run(() => isPdf ? ExtractFromPdf(bytes) : ExtractFromImage(bytes));
    }

    private OcrResult ExtractFromPdf(byte[] pdfBytes)
    {
        // 1) Try the embedded text layer first
        string textLayer = "";
        try
        {
            var sb = new StringBuilder();
            using var document = PdfDocument.Open(pdfBytes);
            foreach (var page in document.GetPages())
            {
                sb.AppendLine(ContentOrderTextExtractor.GetText(page));
            }
            textLayer = sb.ToString();
        }
        catch
        {
            // Fall through to rendering + OCR
        }

        if (CountNonWhitespace(textLayer) >= MinPdfTextChars)
            return new OcrResult { IsSuccess = true, Text = textLayer };

        // 2) Scanned PDF: render each page and OCR it
        try
        {
            var sb = new StringBuilder();
            using var docReader = DocLib.Instance.GetDocReader(pdfBytes, new PageDimensions(PdfRenderScale));
            int pageCount = docReader.GetPageCount();

            for (int i = 0; i < pageCount; i++)
            {
                using var pageReader = docReader.GetPageReader(i);
                int width = pageReader.GetPageWidth();
                int height = pageReader.GetPageHeight();
                var bgra = pageReader.GetImage();

                FlattenOnWhite(bgra);

                using var image = Image.LoadPixelData<Bgra32>(bgra, width, height);
                using var png = new MemoryStream();
                image.SaveAsPng(png);

                var pageText = RunTesseract(png.ToArray());
                sb.AppendLine(pageText);
            }

            var text = sb.ToString();
            if (CountNonWhitespace(text) == 0)
                return new OcrResult { IsSuccess = false, ErrorMessage = "No readable text found in PDF." };

            return new OcrResult { IsSuccess = true, Text = text };
        }
        catch (Exception ex)
        {
            return new OcrResult { IsSuccess = false, ErrorMessage = $"OCR_FAILED: {ex.Message}" };
        }
    }

    private OcrResult ExtractFromImage(byte[] imageBytes)
    {
        try
        {
            var text = RunTesseract(imageBytes);
            if (CountNonWhitespace(text) == 0)
                return new OcrResult { IsSuccess = false, ErrorMessage = "No readable text found in image." };

            return new OcrResult { IsSuccess = true, Text = text };
        }
        catch (Exception ex)
        {
            return new OcrResult { IsSuccess = false, ErrorMessage = $"OCR_FAILED: {ex.Message}" };
        }
    }

    private string RunTesseract(byte[] imageBytes)
    {
        if (!File.Exists(Path.Combine(_tessDataPath, "eng.traineddata")))
            throw new InvalidOperationException($"OCR_DEPENDENCY_MISSING: eng.traineddata not found in {_tessDataPath}");

        // TesseractEngine is not thread-safe; serialize access.
        lock (EngineLock)
        {
            using var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);
            engine.DefaultPageSegMode = PageSegMode.Auto;
            // Keep spacing so column-based tables (Qty / Unit Price / Total) stay on one line
            engine.SetVariable("preserve_interword_spaces", "1");

            using var pix = Pix.LoadFromMemory(imageBytes);
            using var page = engine.Process(pix);
            return page.GetText() ?? string.Empty;
        }
    }

    /// <summary>pdfium renders with a transparent background; composite onto white so OCR sees black-on-white.</summary>
    private static void FlattenOnWhite(byte[] bgra)
    {
        for (int i = 0; i + 3 < bgra.Length; i += 4)
        {
            int a = bgra[i + 3];
            if (a == 255) continue;
            bgra[i]     = (byte)((bgra[i]     * a + 255 * (255 - a)) / 255);
            bgra[i + 1] = (byte)((bgra[i + 1] * a + 255 * (255 - a)) / 255);
            bgra[i + 2] = (byte)((bgra[i + 2] * a + 255 * (255 - a)) / 255);
            bgra[i + 3] = 255;
        }
    }

    private static int CountNonWhitespace(string? s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        int n = 0;
        foreach (var c in s) if (!char.IsWhiteSpace(c)) n++;
        return n;
    }
}
