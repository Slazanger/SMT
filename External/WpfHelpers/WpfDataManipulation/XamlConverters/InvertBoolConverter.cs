using System;

namespace WpfHelpers.WpfDataManipulation.XamlConverters
{
    public class InvertBoolConverter : BoolToAnySwitchConverter<Boolean>
    {
        public InvertBoolConverter()
        {
            IfTrue = false;
            IfFalse = true;
        }
    }
}
