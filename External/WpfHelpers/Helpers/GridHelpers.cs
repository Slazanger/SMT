using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfHelpers.Helpers
{
    public class GridHelpers
    {
        private static void SetStarColumns(Grid grid)
        {
            var starColumns =
                GetStarColumns(grid).Split(',');

            for (var i = 0; i < grid.ColumnDefinitions.Count; i++)
            {
                if (starColumns.Contains(i.ToString()))
                    grid.ColumnDefinitions[i].Width =
                        new GridLength(1, GridUnitType.Star);
            }
        }

        private static void SetStarRows(Grid grid)
        {
            var starRows =
                GetStarRows(grid).Split(',');

            for (var i = 0; i < grid.RowDefinitions.Count; i++)
            {
                if (starRows.Contains(i.ToString()))
                    grid.RowDefinitions[i].Height =
                        new GridLength(1, GridUnitType.Star);
            }
        }

        #region RowCount Property

        /// <summary>
        ///     Adds the specified number of Rows to RowDefinitions.
        ///     Default Height is Auto
        /// </summary>
        public static readonly DependencyProperty RowCountProperty =
            DependencyProperty.RegisterAttached(
                "RowCount", typeof (int), typeof (GridHelpers),
                new PropertyMetadata(-1, RowCountChanged));

        // Get
        public static int GetRowCount(DependencyObject obj)
        {
            return (int) obj.GetValue(RowCountProperty);
        }

        // Set
        public static void SetRowCount(DependencyObject obj, int value)
        {
            obj.SetValue(RowCountProperty, value);
        }

        // Change Event - Adds the Rows
        public static void RowCountChanged(
            DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is Grid) || (int) e.NewValue < 0)
                return;

            var grid = (Grid) obj;
            grid.RowDefinitions.Clear();

            for (var i = 0; i < (int) e.NewValue; i++)
                grid.RowDefinitions.Add(
                    new RowDefinition {Height = new GridLength(1.0, GridUnitType.Star)});

            SetStarRows(grid);
        }

        #endregion

        #region ColumnCount Property

        /// <summary>
        ///     Adds the specified number of Columns to ColumnDefinitions.
        ///     Default Width is Auto
        /// </summary>
        public static readonly DependencyProperty ColumnCountProperty =
            DependencyProperty.RegisterAttached(
                "ColumnCount", typeof (int), typeof (GridHelpers),
                new PropertyMetadata(-1, ColumnCountChanged));

        // Get
        public static int GetColumnCount(DependencyObject obj)
        {
            return (int) obj.GetValue(ColumnCountProperty);
        }

        // Set
        public static void SetColumnCount(DependencyObject obj, int value)
        {
            obj.SetValue(ColumnCountProperty, value);
        }

        // Change Event - Add the Columns
        public static void ColumnCountChanged(
            DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is Grid) || (int) e.NewValue < 0)
                return;

            var grid = (Grid) obj;
            grid.ColumnDefinitions.Clear();

            for (var i = 0; i < (int) e.NewValue; i++)
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition {Width = new GridLength(1.0, GridUnitType.Star)});

            SetStarColumns(grid);
        }

        #endregion

        #region StarRows Property

        /// <summary>
        ///     Makes the specified Row's Height equal to Star.
        ///     Can set on multiple Rows
        /// </summary>
        public static readonly DependencyProperty StarRowsProperty =
            DependencyProperty.RegisterAttached(
                "StarRows", typeof (string), typeof (GridHelpers),
                new PropertyMetadata(string.Empty, StarRowsChanged));

        // Get
        public static string GetStarRows(DependencyObject obj)
        {
            return (string) obj.GetValue(StarRowsProperty);
        }

        // Set
        public static void SetStarRows(DependencyObject obj, string value)
        {
            obj.SetValue(StarRowsProperty, value);
        }

        // Change Event - Makes specified Row's Height equal to Star
        public static void StarRowsChanged(
            DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is Grid) || string.IsNullOrEmpty(e.NewValue.ToString()))
                return;

            SetStarRows((Grid) obj);
        }

        #endregion

        #region StarColumns Property

        /// <summary>
        ///     Makes the specified Column's Width equal to Star.
        ///     Can set on multiple Columns
        /// </summary>
        public static readonly DependencyProperty StarColumnsProperty =
            DependencyProperty.RegisterAttached(
                "StarColumns", typeof (string), typeof (GridHelpers),
                new PropertyMetadata(string.Empty, StarColumnsChanged));

        // Get
        public static string GetStarColumns(DependencyObject obj)
        {
            return (string) obj.GetValue(StarColumnsProperty);
        }

        // Set
        public static void SetStarColumns(DependencyObject obj, string value)
        {
            obj.SetValue(StarColumnsProperty, value);
        }

        // Change Event - Makes specified Column's Width equal to Star
        public static void StarColumnsChanged(
            DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is Grid) || string.IsNullOrEmpty(e.NewValue.ToString()))
                return;

            SetStarColumns((Grid) obj);
        }

        #endregion
    }
}