using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SMTx.Views.Documents
{
    public partial class UniverseView : UserControl
    {
        public UniverseView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
