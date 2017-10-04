using System.Threading.Tasks;
using System.Windows.Input;

namespace WpfHelpers.WpfDataManipulation.Commands.Async
{
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync(object parameter);
    }
}