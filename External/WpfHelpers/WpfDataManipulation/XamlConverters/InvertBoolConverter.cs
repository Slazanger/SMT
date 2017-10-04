using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
