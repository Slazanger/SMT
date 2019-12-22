using System.Globalization;
using System.Net;
using System.Windows.Controls;

namespace WpfHelpers.WpfDataManipulation.Rules
{
    /// <summary>
    /// Used to check if incoming string is ip address, or not
    /// </summary>
    public class IpRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo ci)
        {
            IPAddress ip = null;

            try
            {
                if (!IPAddress.TryParse((string)value, out ip))
                    return new ValidationResult(false, "Not valid.");
            }
            catch
            {
                return new ValidationResult(false, "Not valid.");
            }

            return new ValidationResult(true, null);
        }
    }

}
