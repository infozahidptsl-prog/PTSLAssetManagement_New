using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AssetManagement.Core.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ZXing;
using ZXing.ImageSharp;

namespace AssetManagement.Infrastructure.Services
{
    public class QrBarcodeDetectionService : IQrBarcodeDetectionService
    {
        public async Task<List<DecodedBarcodeResult>> DetectAsync(Stream imageStream)
        {
            var results = new List<DecodedBarcodeResult>();

            try
            {
                // Reset stream position if needed
                if (imageStream.CanSeek)
                {
                    imageStream.Position = 0;
                }

                // Load image using ImageSharp
                using var image = await Image.LoadAsync<Rgba32>(imageStream);

                var reader = new ZXing.ImageSharp.BarcodeReader<Rgba32>
                {
                    AutoRotate = true,
                    Options = new ZXing.Common.DecodingOptions
                    {
                        TryHarder = true,
                        PossibleFormats = new List<BarcodeFormat>
                        {
                            BarcodeFormat.QR_CODE,
                            BarcodeFormat.CODE_128,
                            BarcodeFormat.EAN_13,
                            BarcodeFormat.EAN_8
                        }
                    }
                };

                // Decode
                var barcodeResult = reader.Decode(image);

                if (barcodeResult != null)
                {
                    results.Add(new DecodedBarcodeResult
                    {
                        Format = barcodeResult.BarcodeFormat.ToString(),
                        Value = barcodeResult.Text
                    });
                }
            }
            catch (Exception)
            {
                // Return empty list on failure to parse
            }

            return results;
        }
    }
}
