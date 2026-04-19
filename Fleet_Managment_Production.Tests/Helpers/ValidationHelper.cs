using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Fleet_Managment_Production.Tests.Helpers
{
    public static class ValidationHelper
    {
        public static IList<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var validationContext = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, validationContext, results, true);
            return results; ;
        }
    }
}
