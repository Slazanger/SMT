using System;

namespace WpfHelpers.WpfControls.Paging
{
    /// <summary>
    ///     Provides context for the <see cref="PagingController.CurrentPageChanged" /> event.
    /// </summary>
    public class CurrentPageChangedEventArgs : EventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="CurrentPageChangedEventArgs" /> class.
        /// </summary>
        /// <param name="startIndex">The index of the first item in the current page..</param>
        /// <param name="itemCount">The count of items in the current page.</param>
        public CurrentPageChangedEventArgs(int startIndex, int itemCount)
        {
            StartIndex = startIndex;
            ItemCount = itemCount;
        }

        /// <summary>
        ///     Gets the index of the first item in the current page.
        /// </summary>
        /// <value>The index of the first item.</value>
        public int StartIndex { get; private set; }

        /// <summary>
        ///     Gets the count of items in the current page.
        /// </summary>
        /// <value>The item count.</value>
        public int ItemCount { get; private set; }
    }
}