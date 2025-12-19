using System.Collections.Generic;

namespace SmartLibrarySystem.Models
{
    public static class RequestStatus
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Delivered = "Delivered";
        public const string Returned = "Returned";

        public static IReadOnlyList<string> OrderedFlow => new[] { Pending, Approved, Delivered, Returned };

        private static readonly IReadOnlyDictionary<string, string> displayNameMap = new Dictionary<string, string>
        {
            { Pending, "Beklemede" },
            { Approved, "Onaylandı" },
            { Delivered, "Teslim Edildi" },
            { Returned, "İade Edildi" }
        };

        public static IReadOnlyDictionary<string, string> DisplayNames => displayNameMap;

        public static string ToDisplay(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return string.Empty;
            }

            return displayNameMap.TryGetValue(status, out var text) ? text : status;
        }
    }
}
