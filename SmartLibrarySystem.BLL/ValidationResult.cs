using System.Collections.Generic;
using System.Linq;

namespace SmartLibrarySystem.BLL
{
    public class ValidationResult
    {
        private readonly List<string> errors = new List<string>();

        public bool IsValid => !errors.Any();
        public IEnumerable<string> Errors => errors;

        public void AddError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                errors.Add(message);
            }
        }
    }
}
