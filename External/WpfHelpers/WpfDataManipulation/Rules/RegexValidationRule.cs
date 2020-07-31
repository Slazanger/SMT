using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace WpfHelpers.WpfDataManipulation.Rules
{
    /// <summary>
    /// Validates a text against a regular expression (https://wpftutorial.net/DataValidation.html)
    /// </summary>
    public class RegexValidationRule : ValidationRule
    {
        private string _pattern;
        private Regex _regex;

        public RegexValidationRule()
        {
        }

        public string Pattern
        {
            get
            {
                return _pattern;
            }
            set
            {
                _pattern = value;
                _regex = new Regex(_pattern, RegexOptions.IgnoreCase);
            }
        }

        public override ValidationResult Validate(object value, CultureInfo ultureInfo)
        {
            if (value == null || !_regex.Match(value.ToString()).Success)
            {
                return new ValidationResult(false, "The value is not a valid e-mail address");
            }
            else
            {
                return new ValidationResult(true, null);
            }
        }
    }
}