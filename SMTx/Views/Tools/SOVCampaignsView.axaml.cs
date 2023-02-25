using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SMTx.Views.Tools
{
    public partial class SOVCampaignsView : UserControl
    {
        public SOVCampaignsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
