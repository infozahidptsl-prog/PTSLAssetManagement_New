using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AssetManagement.Core.Interfaces
{
    public class DecodedBarcodeResult
    {
        public string Format { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public interface IQrBarcodeDetectionService
    {
        Task<List<DecodedBarcodeResult>> DetectAsync(Stream imageStream);
    }
}
