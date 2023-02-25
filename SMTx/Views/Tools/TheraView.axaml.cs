using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SMTx.Views.Tools
{
    public partial class TheraView : UserControl
    {
        public TheraView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
