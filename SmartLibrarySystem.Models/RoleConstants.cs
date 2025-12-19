using System.Collections.Generic;

namespace SmartLibrarySystem.Models
{
    public static class RoleConstants
    {
        public const string Student = "Student";
        public const string Staff = "Staff";
        public const string Admin = "Admin";

        private static readonly IReadOnlyDictionary<string, string> displayNameMap = new Dictionary<string, string>
        {
            { Student, "Öğrenci" },
            { Staff, "Personel" },
            { Admin, "Yönetici" }
        };

        public static IReadOnlyDictionary<string, string> DisplayNames => displayNameMap;

        public static string ToDisplay(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return string.Empty;
            }

            return displayNameMap.TryGetValue(role, out var text) ? text : role;
        }
    }
}
