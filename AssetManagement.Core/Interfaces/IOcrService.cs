using System.IO;
using System.Threading.Tasks;

namespace AssetManagement.Core.Interfaces;

public class OcrResult
{
    public bool IsSuccess { get; set; }
    public string? Text { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IOcrService
{
    Task<OcrResult> ExtractTextAsync(Stream fileStream, string fileName, string contentType);
}
