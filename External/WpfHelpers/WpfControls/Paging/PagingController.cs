using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WpfHelpers.WpfDataManipulation.Commands.Sync;

namespace WpfHelpers.WpfControls.Paging
{
    /// <summary>
    ///     Utility class that helps coordinate paged access to a data store.
    /// </summary>
    public sealed class PagingController : INotifyPropertyChanged
    {
        /// <summary>
        ///     The current page.
        /// </summary>
        private int currentPage;

        /// <summary>
        ///     The count of items to be divided into pages.
        /// </summary>
        private int itemCount;

        /// <summary>
        ///     The length (number of items) of each page.
        /// </summary>
        private int pageSize;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PagingController" /> class.
        /// </summary>
        /// <param name="itemCount">The item count.</param>
        /// <param name="pageSize">The size of each page.</param>
        public PagingController(int itemCount, int pageSize)
        {
            Contract.Requires(itemCount >= 0);
            Contract.Requires(pageSize > 0);

            this.itemCount = itemCount;
            this.pageSize = pageSize;
            currentPage = this.itemCount == 0 ? 0 : 1;

            GotoFirstPageCommand = new RelayCommand(() => CurrentPage = 1, () => ItemCount != 0 && CurrentPage > 1);
            GotoLastPageCommand = new RelayCommand(() => CurrentPage = PageCount,
                () => ItemCount != 0 && CurrentPage < PageCount);
            GotoNextPageCommand = new RelayCommand(() => ++CurrentPage, () => ItemCount != 0 && CurrentPage < PageCount);
            GotoPreviousPageCommand = new RelayCommand(() => --CurrentPage, () => ItemCount != 0 && CurrentPage > 1);
        }

        /// <summary>
        ///     Gets the command that, when executed, sets <see cref="CurrentPage" /> to 1.
        /// </summary>
        /// <value>The command that changes the current page.</value>
        public ICommand GotoFirstPageCommand { get; }

        /// <summary>
        ///     Gets the command that, when executed, decrements <see cref="CurrentPage" /> by 1.
        /// </summary>
        /// <value>The command that changes the current page.</value>
        public ICommand GotoPreviousPageCommand { get; }

        /// <summary>
        ///     Gets the command that, when executed, increments <see cref="CurrentPage" /> by 1.
        /// </summary>
        /// <value>The command that changes the current page.</value>
        public ICommand GotoNextPageCommand { get; }

        /// <summary>
        ///     Gets the command that, when executed, sets <see cref="CurrentPage" /> to <see cref="PageCount" />.
        /// </summary>
        /// <value>The command that changes the current page.</value>
        public ICommand GotoLastPageCommand { get; }

        /// <summary>
        ///     Gets or sets the total number of items to be divided into pages.
        /// </summary>
        /// <value>The item count.</value>
        public int ItemCount
        {
            get { return itemCount; }

            set
            {
                Contract.Requires(value >= 0);

                itemCount = value;
                RaisePropertyChanged(nameof(ItemCount));
                RaisePropertyChanged(nameof(PageCount));
                RaiseCanExecuteChanged(GotoLastPageCommand, GotoNextPageCommand);

                if (CurrentPage > PageCount)
                    CurrentPage = PageCount;
            }
        }

        /// <summary>
        ///     Gets or sets the number of items that each page contains.
        /// </summary>
        /// <value>The size of the page.</value>
        public int PageSize
        {
            get { return pageSize; }

            set
            {
                Contract.Requires(value > 0);

                var oldStartIndex = CurrentPageStartIndex;
                pageSize = value;
                RaisePropertyChanged(nameof(PageSize));
                RaisePropertyChanged(nameof(PageCount));
                RaisePropertyChanged(nameof(CurrentPageStartIndex));
                RaiseCanExecuteChanged(GotoLastPageCommand, GotoNextPageCommand);

                if (oldStartIndex >= 0)
                    CurrentPage = GetPageFromIndex(oldStartIndex);
            }
        }

        /// <summary>
        ///     Gets the number of pages required to contain all items.
        /// </summary>
        /// <value>The page count.</value>
        public int PageCount
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() == 0 || itemCount > 0);
                Contract.Ensures(Contract.Result<int>() > 0 || itemCount == 0);

                if (itemCount == 0)
                    return 0;

                var ceil = (int) Math.Ceiling((double) itemCount / pageSize);

                Contract.Assume(ceil > 0);
                    // Math.Ceiling makes the static checker unable to prove the postcondition without help
                return ceil;
            }
        }

        /// <summary>
        ///     Gets or sets the current page.
        /// </summary>
        /// <value>The current page.</value>
        public int CurrentPage
        {
            get { return currentPage; }

            set
            {
                Contract.Requires(value == 0 || PageCount != 0);
                Contract.Requires(value > 0 || PageCount == 0);
                Contract.Requires(value <= PageCount);

                currentPage = value;
                RaisePropertyChanged(nameof(CurrentPage));
                RaisePropertyChanged(nameof(CurrentPageStartIndex));
                RaiseCanExecuteChanged(GotoLastPageCommand, GotoNextPageCommand);
                RaiseCanExecuteChanged(GotoFirstPageCommand, GotoPreviousPageCommand);

                var handler = CurrentPageChanged;
                if (handler != null)
                    handler(this, new CurrentPageChangedEventArgs(CurrentPageStartIndex, PageSize));
            }
        }

        /// <summary>
        ///     Gets the index of the first item belonging to the current page.
        /// </summary>
        /// <value>The index of the first item belonging to the current page.</value>
        public int CurrentPageStartIndex
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() == -1 || PageCount != 0);
                Contract.Ensures(Contract.Result<int>() >= 0 || PageCount == 0);
                Contract.Ensures(Contract.Result<int>() < ItemCount);
                return PageCount == 0 ? -1 : (CurrentPage - 1) * PageSize;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     Occurs when the value of <see cref="CurrentPage" /> changes.
        /// </summary>
        public event EventHandler<CurrentPageChangedEventArgs> CurrentPageChanged;

        /// <summary>
        ///     Calls RaiseCanExecuteChanged on any number of DelegateCommand instances.
        /// </summary>
        /// <param name="commands">The commands.</param>
        [Pure]
        private static void RaiseCanExecuteChanged(params ICommand[] commands)
        {
            Contract.Requires(commands != null);
            foreach (var command in commands.Cast<RelayCommand>())
                command.RaiseCanExecuteChanged();
        }

        /// <summary>
        ///     Gets the number of the page to which the item with the specified index belongs.
        /// </summary>
        /// <param name="itemIndex">The index of the item in question.</param>
        /// <returns>The number of the page in which the item with the specified index belongs.</returns>
        [Pure]
        private int GetPageFromIndex(int itemIndex)
        {
            Contract.Requires(itemIndex >= 0);
            Contract.Requires(itemIndex < itemCount);
            Contract.Ensures(Contract.Result<int>() >= 1);
            Contract.Ensures(Contract.Result<int>() <= PageCount);

            var result = (int) Math.Floor((double) itemIndex / PageSize) + 1;
            Contract.Assume(result >= 1);
                // Math.Floor makes the static checker unable to prove the postcondition without help
            Contract.Assume(result <= PageCount); // Ditto
            return result;
        }

        /// <summary>
        ///     Defines the invariant for object of this class.
        /// </summary>
        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(currentPage == 0 || PageCount != 0);
            Contract.Invariant(currentPage > 0 || PageCount == 0);
            Contract.Invariant(currentPage <= PageCount);
            Contract.Invariant(pageSize > 0);
            Contract.Invariant(itemCount >= 0);
        }

        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}