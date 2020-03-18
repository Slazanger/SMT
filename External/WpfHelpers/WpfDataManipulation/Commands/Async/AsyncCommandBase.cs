using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WpfHelpers.WpfDataManipulation.Commands.Async
{
    public abstract class AsyncCommandBase : IAsyncCommand
    {
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public abstract bool CanExecute(object parameter);

        public async void Execute(object parameter)
        {
            await ExecuteAsync(parameter);
        }

        public abstract Task ExecuteAsync(object parameter);

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}