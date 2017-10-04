using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace WpfHelpers.WpfDataManipulation.Rules.Converters
{
    /// <summary>
    ///     The following converter combines a list of ValidationErrors into a string.
    ///     <example>
    ///         <ControlTemplate x:Key="TextBoxErrorTemplate" TargetType="Control">
    ///             <Grid ClipToBounds="False">
    ///                 < Image HorizontalAlignment="Right" VerticalAlignment="Top"
    ///                     Width="16" Height="16" Margin="0,-8,-8,0"
    ///                     Source="{StaticResource ErrorImage}"
    ///                     ToolTip="{Binding ElementName=adornedElement, 
    ///                          Path=AdornedElement.(Validation.Errors), 
    ///                          Converter={k:ValidationErrorsToStringConverter}}" />
    ///                 <Border BorderBrush="Red" BorderThickness="1" Margin="-1">
    ///                     <AdornedElementPlaceholder Name="adornedElement" />
    ///                 </Border>
    ///             </Grid>
    ///         </ControlTemplate>
    ///     </example>
    /// </summary>
    [ValueConversion(typeof(ReadOnlyObservableCollection<ValidationError>), typeof(string))]
    public class ValidationErrorsToStringConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            var errors =
                value as ReadOnlyObservableCollection<ValidationError>;

            if (errors == null)
                return string.Empty;

            return string.Join("\n", (from e in errors
                select e.ErrorContent as string).ToArray());
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new ValidationErrorsToStringConverter();
        }
    }
}