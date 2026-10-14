using System;
using AssetManagement.Core.Entities;

namespace AssetManagement.Web.Helpers
{
    public static class CategoryColorHelper
    {
        public static (string mainClass, string badgeClass, string IconClass, string TypeBadgeClass) GetCategoryColors(string? category)
        {
            if (string.IsNullOrWhiteSpace(category)) 
                return ("bg-slate-50 text-slate-700 ring-slate-600/20", "bg-slate-200 text-slate-800", "text-slate-500", "bg-slate-50 text-slate-700 ring-slate-600/20");
            
            var trimmed = category.Trim();
            var lower = trimmed.ToLowerInvariant();

            switch (lower)
            {
                case "laptop":
                    return ("bg-blue-50 text-blue-700 ring-blue-600/20", "bg-blue-100 text-blue-800", "text-blue-500", "bg-blue-50 text-blue-700 ring-blue-600/20");
                case "desktop":
                case "mini-pc":
                case "minipc":
                case "pc":
                case "pc-server":
                    return ("bg-indigo-50 text-indigo-700 ring-indigo-600/20", "bg-indigo-100 text-indigo-800", "text-indigo-500", "bg-indigo-50 text-indigo-700 ring-indigo-600/20");
                case "monitor":
                    return ("bg-purple-50 text-purple-700 ring-purple-600/20", "bg-purple-100 text-purple-800", "text-purple-500", "bg-purple-50 text-purple-700 ring-purple-600/20");
                case "test server":
                case "server":
                    return ("bg-rose-50 text-rose-700 ring-rose-600/20", "bg-rose-100 text-rose-800", "text-rose-500", "bg-rose-50 text-rose-700 ring-rose-600/20");
                case "mobile":
                case "phone":
                case "smartphone":
                case "tablet":
                    return ("bg-emerald-50 text-emerald-700 ring-emerald-600/20", "bg-emerald-100 text-emerald-800", "text-emerald-500", "bg-emerald-50 text-emerald-700 ring-emerald-600/20");
                case "ssd":
                    return ("bg-cyan-50 text-cyan-700 ring-cyan-600/20", "bg-cyan-100 text-cyan-800", "text-cyan-500", "bg-cyan-50 text-cyan-700 ring-cyan-600/20");
                case "hdd":
                    return ("bg-teal-50 text-teal-700 ring-teal-600/20", "bg-teal-100 text-teal-800", "text-teal-500", "bg-teal-50 text-teal-700 ring-teal-600/20");
                case "ram":
                    return ("bg-fuchsia-50 text-fuchsia-700 ring-fuchsia-600/20", "bg-fuchsia-100 text-fuchsia-800", "text-fuchsia-500", "bg-fuchsia-50 text-fuchsia-700 ring-fuchsia-600/20");
                case "power supply":
                    return ("bg-orange-50 text-orange-700 ring-orange-600/20", "bg-orange-100 text-orange-800", "text-orange-500", "bg-orange-50 text-orange-700 ring-orange-600/20");
                case "peripheral":
                case "keyboard":
                case "mouse":
                case "headphone":
                case "printer":
                case "scanner":
                case "printer & scanner":
                case "ups":
                case "cable":
                    return ("bg-amber-50 text-amber-700 ring-amber-600/20", "bg-amber-100 text-amber-800", "text-amber-500", "bg-amber-50 text-amber-700 ring-amber-600/20");
            }

            // Dynamic type-based colors for any custom category
            var devType = CategoryTypeRegistry.GetType(trimmed);
            if (string.Equals(devType, "System", StringComparison.OrdinalIgnoreCase))
            {
                return ("bg-indigo-50 text-indigo-700 ring-indigo-600/20", "bg-indigo-100 text-indigo-800", "text-indigo-500", "bg-indigo-50 text-indigo-700 ring-indigo-600/20");
            }
            if (string.Equals(devType, "Component", StringComparison.OrdinalIgnoreCase))
            {
                return ("bg-cyan-50 text-cyan-700 ring-cyan-600/20", "bg-cyan-100 text-cyan-800", "text-cyan-500", "bg-cyan-50 text-cyan-700 ring-cyan-600/20");
            }
            if (string.Equals(devType, "Peripheral", StringComparison.OrdinalIgnoreCase))
            {
                return ("bg-amber-50 text-amber-700 ring-amber-600/20", "bg-amber-100 text-amber-800", "text-amber-500", "bg-amber-50 text-amber-700 ring-amber-600/20");
            }

            return ("bg-slate-50 text-slate-700 ring-slate-600/20", "bg-slate-200 text-slate-800", "text-slate-500", "bg-slate-50 text-slate-700 ring-slate-600/20");
        }
    }
}
