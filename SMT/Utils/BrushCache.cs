using System;
using System.Collections.Concurrent;
using System.Windows.Media;

namespace SMT.Utils
{
    /// <summary>
    /// A thread-safe cache for brushes and pens that automatically freezes them for optimal performance.
    /// Freezing WPF objects like brushes and pens improves rendering performance by making them immutable.
    /// </summary>
    public static class BrushCache
    {
        #region Private Fields

        private static readonly ConcurrentDictionary<Color, SolidColorBrush> _solidBrushCache 
            = new ConcurrentDictionary<Color, SolidColorBrush>();

        private static readonly ConcurrentDictionary<string, Pen> _penCache 
            = new ConcurrentDictionary<string, Pen>();

        private static readonly ConcurrentDictionary<string, LinearGradientBrush> _gradientBrushCache 
            = new ConcurrentDictionary<string, LinearGradientBrush>();

        #endregion

        #region Solid Color Brushes

        /// <summary>
        /// Gets a cached and frozen SolidColorBrush for the specified color.
        /// </summary>
        /// <param name="color">The color of the brush</param>
        /// <returns>A frozen SolidColorBrush</returns>
        public static SolidColorBrush GetSolidBrush(Color color)
        {
            return _solidBrushCache.GetOrAdd(color, c =>
            {
                var brush = new SolidColorBrush(c);
                brush.Freeze();
                return brush;
            });
        }

        /// <summary>
        /// Gets a cached and frozen SolidColorBrush for the specified color components.
        /// </summary>
        /// <param name="a">Alpha component (0-255)</param>
        /// <param name="r">Red component (0-255)</param>
        /// <param name="g">Green component (0-255)</param>
        /// <param name="b">Blue component (0-255)</param>
        /// <returns>A frozen SolidColorBrush</returns>
        public static SolidColorBrush GetSolidBrush(byte a, byte r, byte g, byte b)
        {
            return GetSolidBrush(Color.FromArgb(a, r, g, b));
        }

        /// <summary>
        /// Gets a cached and frozen SolidColorBrush for the specified RGB color components with full opacity.
        /// </summary>
        /// <param name="r">Red component (0-255)</param>
        /// <param name="g">Green component (0-255)</param>
        /// <param name="b">Blue component (0-255)</param>
        /// <returns>A frozen SolidColorBrush</returns>
        public static SolidColorBrush GetSolidBrush(byte r, byte g, byte b)
        {
            return GetSolidBrush(Color.FromRgb(r, g, b));
        }

        #endregion

        #region Pens

        /// <summary>
        /// Gets a cached and frozen Pen with the specified brush and thickness.
        /// </summary>
        /// <param name="brush">The brush for the pen</param>
        /// <param name="thickness">The thickness of the pen</param>
        /// <returns>A frozen Pen</returns>
        public static Pen GetPen(Brush brush, double thickness)
        {
            return GetPen(brush, thickness, null, PenLineCap.Flat, PenLineCap.Flat, PenLineJoin.Miter, 10.0);
        }

        /// <summary>
        /// Gets a cached and frozen Pen with the specified color and thickness.
        /// </summary>
        /// <param name="color">The color of the pen</param>
        /// <param name="thickness">The thickness of the pen</param>
        /// <returns>A frozen Pen</returns>
        public static Pen GetPen(Color color, double thickness)
        {
            return GetPen(GetSolidBrush(color), thickness);
        }

        /// <summary>
        /// Gets a cached and frozen Pen with the specified parameters.
        /// </summary>
        /// <param name="brush">The brush for the pen</param>
        /// <param name="thickness">The thickness of the pen</param>
        /// <param name="dashArray">The dash pattern (null for solid line)</param>
        /// <param name="startLineCap">The cap style at the start of the line</param>
        /// <param name="endLineCap">The cap style at the end of the line</param>
        /// <param name="lineJoin">The join style for line segments</param>
        /// <param name="miterLimit">The miter limit</param>
        /// <returns>A frozen Pen</returns>
        public static Pen GetPen(Brush brush, double thickness, DoubleCollection dashArray, 
            PenLineCap startLineCap, PenLineCap endLineCap, PenLineJoin lineJoin, double miterLimit)
        {
            // Create a unique key for this pen configuration
            string key = $"{brush?.GetHashCode() ?? 0}_{thickness}_{dashArray?.GetHashCode() ?? 0}_{startLineCap}_{endLineCap}_{lineJoin}_{miterLimit}";

            return _penCache.GetOrAdd(key, k =>
            {
                var pen = new Pen(brush, thickness)
                {
                    StartLineCap = startLineCap,
                    EndLineCap = endLineCap,
                    LineJoin = lineJoin,
                    MiterLimit = miterLimit
                };

                if (dashArray != null)
                {
                    pen.DashArray = dashArray;
                }

                pen.Freeze();
                return pen;
            });
        }

        /// <summary>
        /// Gets a cached and frozen dashed Pen.
        /// </summary>
        /// <param name="color">The color of the pen</param>
        /// <param name="thickness">The thickness of the pen</param>
        /// <param name="dashLength">The length of dashes</param>
        /// <param name="gapLength">The length of gaps between dashes</param>
        /// <returns>A frozen dashed Pen</returns>
        public static Pen GetDashedPen(Color color, double thickness, double dashLength = 2.0, double gapLength = 2.0)
        {
            var dashArray = new DoubleCollection { dashLength, gapLength };
            dashArray.Freeze();
            return GetPen(GetSolidBrush(color), thickness, dashArray, PenLineCap.Flat, PenLineCap.Flat, PenLineJoin.Miter, 10.0);
        }

        #endregion

        #region Gradient Brushes

        /// <summary>
        /// Gets a cached and frozen LinearGradientBrush.
        /// </summary>
        /// <param name="startColor">The starting color of the gradient</param>
        /// <param name="endColor">The ending color of the gradient</param>
        /// <param name="angle">The angle of the gradient in degrees (0 = horizontal left to right)</param>
        /// <returns>A frozen LinearGradientBrush</returns>
        public static LinearGradientBrush GetLinearGradientBrush(Color startColor, Color endColor, double angle = 0)
        {
            string key = $"{startColor}_{endColor}_{angle}";

            return _gradientBrushCache.GetOrAdd(key, k =>
            {
                var brush = new LinearGradientBrush(startColor, endColor, angle);
                brush.Freeze();
                return brush;
            });
        }

        /// <summary>
        /// Gets a cached and frozen LinearGradientBrush with custom gradient stops.
        /// </summary>
        /// <param name="gradientStops">The gradient stops</param>
        /// <param name="angle">The angle of the gradient in degrees</param>
        /// <returns>A frozen LinearGradientBrush</returns>
        public static LinearGradientBrush GetLinearGradientBrush(GradientStopCollection gradientStops, double angle = 0)
        {
            string key = $"{gradientStops?.GetHashCode() ?? 0}_{angle}";

            return _gradientBrushCache.GetOrAdd(key, k =>
            {
                var brush = new LinearGradientBrush(gradientStops, angle);
                brush.Freeze();
                return brush;
            });
        }

        #endregion

        #region Common Brushes

        /// <summary>
        /// Gets common system brushes that are pre-cached and frozen.
        /// </summary>
        public static class CommonBrushes
        {
            public static SolidColorBrush Black => GetSolidBrush(Colors.Black);
            public static SolidColorBrush White => GetSolidBrush(Colors.White);
            public static SolidColorBrush Red => GetSolidBrush(Colors.Red);
            public static SolidColorBrush Green => GetSolidBrush(Colors.Green);
            public static SolidColorBrush Blue => GetSolidBrush(Colors.Blue);
            public static SolidColorBrush Yellow => GetSolidBrush(Colors.Yellow);
            public static SolidColorBrush Orange => GetSolidBrush(Colors.Orange);
            public static SolidColorBrush Purple => GetSolidBrush(Colors.Purple);
            public static SolidColorBrush Gray => GetSolidBrush(Colors.Gray);
            public static SolidColorBrush LightGray => GetSolidBrush(Colors.LightGray);
            public static SolidColorBrush DarkGray => GetSolidBrush(Colors.DarkGray);
            public static SolidColorBrush Transparent => GetSolidBrush(Colors.Transparent);
        }

        /// <summary>
        /// Gets common system pens that are pre-cached and frozen.
        /// </summary>
        public static class CommonPens
        {
            public static Pen Black => GetPen(Colors.Black, 1.0);
            public static Pen White => GetPen(Colors.White, 1.0);
            public static Pen Red => GetPen(Colors.Red, 1.0);
            public static Pen Green => GetPen(Colors.Green, 1.0);
            public static Pen Blue => GetPen(Colors.Blue, 1.0);
            
            public static Pen BlackThick => GetPen(Colors.Black, 2.0);
            public static Pen WhiteThick => GetPen(Colors.White, 2.0);
            
            public static Pen BlackDashed => GetDashedPen(Colors.Black, 1.0);
            public static Pen WhiteDashed => GetDashedPen(Colors.White, 1.0);
        }

        #endregion

        #region Cache Management

        /// <summary>
        /// Gets the current number of cached brushes.
        /// </summary>
        public static int CachedBrushCount => _solidBrushCache.Count + _gradientBrushCache.Count;

        /// <summary>
        /// Gets the current number of cached pens.
        /// </summary>
        public static int CachedPenCount => _penCache.Count;

        /// <summary>
        /// Clears all cached brushes and pens. Use with caution as this may impact performance
        /// if the brushes/pens need to be recreated frequently.
        /// </summary>
        public static void ClearCache()
        {
            _solidBrushCache.Clear();
            _penCache.Clear();
            _gradientBrushCache.Clear();
        }

        #endregion
    }
}
