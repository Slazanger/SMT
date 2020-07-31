using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WpfHelpers.WpfDataManipulation.Commands.Async
{
    /// <summary>
    ///
    /// </summary>
    public class AsyncCommand : AsyncCommandBase, INotifyPropertyChanged
    {
        private readonly CancelAsyncCommand _cancelCommand;
        private readonly Func<bool> _canExecute = () => true;
        private readonly Func<CancellationToken, Task> _command;
        private NotifyTaskCompletionBase _execution;

        public AsyncCommand(Func<CancellationToken, Task> command)
        {
            _command = command;
            _cancelCommand = new CancelAsyncCommand();
        }

        public AsyncCommand(Func<CancellationToken, Task> command, Func<bool> canExecute)
            : this(command)
        {
            _canExecute = canExecute;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand CancelCommand
        {
            get { return _cancelCommand; }
        }

        public NotifyTaskCompletionBase Execution
        {
            get
            {
                return _execution;
            }
            private set
            {
                _execution = value;
                OnPropertyChanged();
            }
        }

        public override bool CanExecute(object parameter)
        {
            return _canExecute() && (Execution == null || Execution.IsCompleted);
        }

        public override async Task ExecuteAsync(object parameter)
        {
            _cancelCommand.NotifyCommandStarting();
            Execution = new NotifyTaskCompletionBase(_command(_cancelCommand.Token));
            RaiseCanExecuteChanged();
            await Execution.TaskCompletion;
            _cancelCommand.NotifyCommandFinished();
            RaiseCanExecuteChanged();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private sealed class CancelAsyncCommand : ICommand
        {
            private bool _commandExecuting;
            private CancellationTokenSource _cts = new CancellationTokenSource();

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public CancellationToken Token
            {
                get { return _cts.Token; }
            }

            bool ICommand.CanExecute(object parameter)
            {
                return _commandExecuting && !_cts.IsCancellationRequested;
            }

            void ICommand.Execute(object parameter)
            {
                _cts.Cancel();
                RaiseCanExecuteChanged();
            }

            public void NotifyCommandFinished()
            {
                _commandExecuting = false;
                RaiseCanExecuteChanged();
            }

            public void NotifyCommandStarting()
            {
                _commandExecuting = true;
                if (!_cts.IsCancellationRequested)
                    return;
                _cts = new CancellationTokenSource();
                RaiseCanExecuteChanged();
            }

            private void RaiseCanExecuteChanged()
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}