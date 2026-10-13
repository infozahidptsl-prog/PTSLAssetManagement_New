using System;
using System.Collections.Generic;

namespace AssetManagement.Web.Helpers
{
    public class MonitorSpec
    {
        public string Brand { get; set; } = "";
        public string Model { get; set; } = "";
        public string Size { get; set; } = "";
        public string Serial { get; set; } = "";
        public bool IsLegacyFormat { get; set; } = false;
        public string LegacyText { get; set; } = "";
    }

    public static class MonitorSpecHelper
    {
        public static MonitorSpec Parse(string display)
        {
            var spec = new MonitorSpec();
            if (string.IsNullOrWhiteSpace(display)) return spec;

            if (display.Contains("Brand: ") || display.Contains("Model: ") || display.Contains("Size: ") || display.Contains("S/N: "))
            {
                var parts = display.Split('|', StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    var part = p.Trim();
                    if (part.StartsWith("Brand: ", StringComparison.OrdinalIgnoreCase))
                        spec.Brand = part.Substring(7).Trim();
                    else if (part.StartsWith("Model: ", StringComparison.OrdinalIgnoreCase))
                        spec.Model = part.Substring(7).Trim();
                    else if (part.StartsWith("Size: ", StringComparison.OrdinalIgnoreCase))
                        spec.Size = part.Substring(6).Trim();
                    else if (part.StartsWith("S/N: ", StringComparison.OrdinalIgnoreCase))
                        spec.Serial = part.Substring(5).Trim();
                }
            }
            else
            {
                spec.IsLegacyFormat = true;
                spec.LegacyText = display;
            }

            return spec;
        }

        public static string Build(MonitorSpec spec)
        {
            if (spec.IsLegacyFormat && !string.IsNullOrWhiteSpace(spec.LegacyText))
                return spec.LegacyText;

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(spec.Brand)) parts.Add($"Brand: {spec.Brand.Trim()}");
            if (!string.IsNullOrWhiteSpace(spec.Model)) parts.Add($"Model: {spec.Model.Trim()}");
            if (!string.IsNullOrWhiteSpace(spec.Size)) parts.Add($"Size: {spec.Size.Trim()}");
            if (!string.IsNullOrWhiteSpace(spec.Serial)) parts.Add($"S/N: {spec.Serial.Trim()}");
            
            return string.Join(" | ", parts);
        }
    }
}
