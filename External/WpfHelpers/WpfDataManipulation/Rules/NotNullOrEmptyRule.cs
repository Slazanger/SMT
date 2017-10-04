using System.Globalization;
using System.Windows.Controls;

namespace WpfHelpers.WpfDataManipulation.Rules
{
    /// <summary>
    /// Used to check if incoming value is not empty
    /// </summary>
    public class NotNullOrEmptyRule : ValidationRule
    {
        public string IfFailedMessage { get; set; } = "Cannot be empty";

        public override ValidationResult Validate(object value, CultureInfo ci)
        {
            if (string.IsNullOrWhiteSpace(value?.ToString()))
                return new ValidationResult(false, IfFailedMessage);

            return new ValidationResult(true, null);
        }
    }
}