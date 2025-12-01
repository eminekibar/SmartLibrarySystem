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
    }
}
