using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AssetManagement.Core.Entities;

public static class CategoryTypeRegistry
{
    private static readonly ConcurrentDictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase);

    static CategoryTypeRegistry()
    {
        InitializeDefaults();
    }

    private static void InitializeDefaults()
    {
        // Default System categories
        _map["Laptop"] = "System";
        _map["Desktop"] = "System";
        _map["Mini-PC"] = "System";
        _map["Minipc"] = "System";
        _map["PC"] = "System";
        _map["Server"] = "System";
        _map["PC-Server"] = "System";
        _map["MacBook"] = "System";
        _map["Workstation"] = "System";
        _map["Mobile"] = "System";
        _map["Phone"] = "System";
        _map["Smartphone"] = "System";
        _map["Tablet"] = "System";

        // Default Component categories
        _map["RAM"] = "Component";
        _map["SSD"] = "Component";
        _map["HDD"] = "Component";
        _map["Power Supply"] = "Component";
        _map["Processor"] = "Component";
        _map["Graphics Card"] = "Component";
        _map["GPU"] = "Component";

        // Default Peripheral categories
        _map["Monitor"] = "Peripheral";
        _map["Keyboard"] = "Peripheral";
        _map["Mouse"] = "Peripheral";
        _map["Printer"] = "Peripheral";
        _map["Scanner"] = "Peripheral";
        _map["Printer & Scanner"] = "Peripheral";
        _map["UPS"] = "Peripheral";
        _map["Headphone"] = "Peripheral";
        _map["Headset"] = "Peripheral";
        _map["Cable"] = "Peripheral";
        _map["Speaker"] = "Peripheral";
        _map["Webcam"] = "Peripheral";
    }

    public static void Sync(IEnumerable<DeviceCategoryModel>? categories)
    {
        if (categories == null) return;
        foreach (var c in categories)
        {
            if (!string.IsNullOrWhiteSpace(c.Name) && !string.IsNullOrWhiteSpace(c.Type))
            {
                _map[c.Name.Trim()] = c.Type.Trim();
            }
        }
    }

    public static void SetType(string categoryName, string categoryType)
    {
        if (!string.IsNullOrWhiteSpace(categoryName) && !string.IsNullOrWhiteSpace(categoryType))
        {
            _map[categoryName.Trim()] = categoryType.Trim();
        }
    }

    public static void Remove(string categoryName)
    {
        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            _map.TryRemove(categoryName.Trim(), out _);
        }
    }

    public static string GetType(string? categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName)) return "Peripheral";
        var trimmed = categoryName.Trim();
        if (_map.TryGetValue(trimmed, out var dynamicType) && !string.IsNullOrWhiteSpace(dynamicType))
        {
            return dynamicType;
        }

        // Heuristic fallback if unknown
        var lower = trimmed.ToLowerInvariant();
        if (lower.Contains("laptop") || lower.Contains("desktop") || lower.Contains("server") ||
            lower.Contains("macbook") || lower.Contains("mini-pc") || lower.Contains("minipc") ||
            lower.Contains("workstation") || lower == "pc" || lower.Contains("mobile") ||
            lower.Contains("phone") || lower.Contains("tablet"))
        {
            return "System";
        }

        if (lower.Contains("ram") || lower.Contains("ssd") || lower.Contains("hdd") ||
            lower.Contains("nvme") || lower.Contains("component") || lower.Contains("processor") ||
            lower.Contains("power supply"))
        {
            return "Component";
        }

        return "Peripheral";
    }
}
