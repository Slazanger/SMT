using System.Diagnostics;
using System.Windows.Input;
using SMTx.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Controls;
using Dock.Model.Core;


namespace SMTx.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        private IFactory _factory;
        private IDock _layout;
        private string _currentView;

        public IFactory Factory
        {
            get => _factory;
            set => SetProperty(ref _factory, value);
        }

        public IDock Layout
        {
            get => _layout;
            set => SetProperty(ref _layout, value);
        }

        public string CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }
    }
}
