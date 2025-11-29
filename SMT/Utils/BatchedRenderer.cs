using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace SMT.Utils
{
    /// <summary>
    /// Provides high-performance batched rendering by grouping multiple draw operations into fewer DrawingVisuals.
    /// This dramatically reduces the number of visual objects created, improving rendering performance.
    /// </summary>
    public static class BatchedRenderer
    {
        #region Configuration

        /// <summary>
        /// Default number of items to batch into a single DrawingVisual
        /// </summary>
        public const int DEFAULT_BATCH_SIZE = 50;

        /// <summary>
        /// Maximum number of items per batch to prevent individual DrawingVisuals from becoming too complex
        /// </summary>
        public const int MAX_BATCH_SIZE = 200;

        #endregion

        #region Core Batching Methods

        /// <summary>
        /// Renders a collection of items using batched DrawingVisuals for optimal performance
        /// </summary>
        /// <typeparam name="T">Type of items to render</typeparam>
        /// <param name="items">Collection of items to render</param>
        /// <param name="drawAction">Action that draws a single item to the DrawingContext</param>
        /// <param name="batchSize">Number of items per DrawingVisual (default: 50)</param>
        /// <param name="enableBitmapCaching">Whether to enable bitmap caching on DrawingVisuals</param>
        /// <param name="cacheScale">Scale for bitmap cache (1.0 = normal, lower = more memory efficient)</param>
        /// <returns>Collection of batched DrawingVisuals ready to be added to canvas</returns>
        public static IEnumerable<DrawingVisual> CreateBatchedVisuals<T>(
            IEnumerable<T> items,
            Action<DrawingContext, T> drawAction,
            int batchSize = DEFAULT_BATCH_SIZE,
            bool enableBitmapCaching = true,
            double cacheScale = 2.0)  // Higher default cache scale for better quality
        {
            if (items == null) yield break;
            if (drawAction == null) throw new ArgumentNullException(nameof(drawAction));

            // Clamp batch size to reasonable limits
            batchSize = Math.Max(1, Math.Min(batchSize, MAX_BATCH_SIZE));

            var itemsList = items.ToList();
            if (itemsList.Count == 0) yield break;

            // Process items in batches
            for (int i = 0; i < itemsList.Count; i += batchSize)
            {
                var batch = itemsList.Skip(i).Take(batchSize);
                var drawingVisual = CreateBatchDrawingVisual(batch, drawAction, enableBitmapCaching, cacheScale);
                if (drawingVisual != null)
                {
                    yield return drawingVisual;
                }
            }
        }

        /// <summary>
        /// Renders items with conditional filtering for better performance
        /// </summary>
        /// <typeparam name="T">Type of items to render</typeparam>
        /// <param name="items">Collection of items to render</param>
        /// <param name="filterPredicate">Predicate to filter items (e.g., viewport culling)</param>
        /// <param name="drawAction">Action that draws a single item</param>
        /// <param name="batchSize">Number of items per DrawingVisual</param>
        /// <param name="enableBitmapCaching">Whether to enable bitmap caching</param>
        /// <returns>Collection of batched DrawingVisuals</returns>
        public static IEnumerable<DrawingVisual> CreateFilteredBatchedVisuals<T>(
            IEnumerable<T> items,
            Func<T, bool> filterPredicate,
            Action<DrawingContext, T> drawAction,
            int batchSize = DEFAULT_BATCH_SIZE,
            bool enableBitmapCaching = true,
            double cacheScale = 2.0)
        {
            var filteredItems = items?.Where(filterPredicate) ?? Enumerable.Empty<T>();
            return CreateBatchedVisuals(filteredItems, drawAction, batchSize, enableBitmapCaching, cacheScale);
        }

        #endregion

        #region Specialized Rendering Methods

        /// <summary>
        /// Batched rendering for systems with ellipse/circle shapes
        /// </summary>
        public static IEnumerable<DrawingVisual> RenderSystemEllipses<T>(
            IEnumerable<T> systems,
            Func<T, Point> getPosition,
            Func<T, double> getRadius,
            Func<T, Brush> getFillBrush,
            Func<T, Pen> getStrokePen = null,
            int batchSize = DEFAULT_BATCH_SIZE)
        {
            return CreateBatchedVisuals(systems, (dc, system) =>
            {
                var position = getPosition(system);
                var radius = getRadius(system);
                var fill = getFillBrush(system);
                var stroke = getStrokePen?.Invoke(system);

                dc.DrawEllipse(fill, stroke, position, radius, radius);
            }, batchSize);
        }

        /// <summary>
        /// Batched rendering for systems with rectangle shapes
        /// </summary>
        public static IEnumerable<DrawingVisual> RenderSystemRectangles<T>(
            IEnumerable<T> systems,
            Func<T, Rect> getRectangle,
            Func<T, Brush> getFillBrush,
            Func<T, Pen> getStrokePen = null,
            int batchSize = DEFAULT_BATCH_SIZE)
        {
            return CreateBatchedVisuals(systems, (dc, system) =>
            {
                var rect = getRectangle(system);
                var fill = getFillBrush(system);
                var stroke = getStrokePen?.Invoke(system);

                dc.DrawRectangle(fill, stroke, rect);
            }, batchSize);
        }

        /// <summary>
        /// Batched rendering for connection lines between systems
        /// </summary>
        public static IEnumerable<DrawingVisual> RenderSystemConnections<T>(
            IEnumerable<T> connections,
            Func<T, Point> getStartPoint,
            Func<T, Point> getEndPoint,
            Func<T, Pen> getLinePen,
            int batchSize = DEFAULT_BATCH_SIZE)
        {
            return CreateBatchedVisuals(connections, (dc, connection) =>
            {
                var startPoint = getStartPoint(connection);
                var endPoint = getEndPoint(connection);
                var pen = getLinePen(connection);

                dc.DrawLine(pen, startPoint, endPoint);
            }, batchSize);
        }

        /// <summary>
        /// Batched rendering for text labels
        /// </summary>
        public static IEnumerable<DrawingVisual> RenderSystemText<T>(
            IEnumerable<T> systems,
            Func<T, Point> getPosition,
            Func<T, string> getText,
            Func<T, Brush> getTextBrush,
            Typeface typeface,
            double fontSize,
            int batchSize = DEFAULT_BATCH_SIZE)
        {
            return CreateBatchedVisuals(systems, (dc, system) =>
            {
                var position = getPosition(system);
                var text = getText(system);
                var brush = getTextBrush(system);

                if (!string.IsNullOrEmpty(text))
                {
                    var formattedText = new FormattedText(
                        text,
                        System.Globalization.CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        fontSize,
                        brush,
                        VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip);

                    dc.DrawText(formattedText, position);
                }
            }, batchSize);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a single DrawingVisual containing multiple rendered items
        /// </summary>
        private static DrawingVisual CreateBatchDrawingVisual<T>(
            IEnumerable<T> batch,
            Action<DrawingContext, T> drawAction,
            bool enableBitmapCaching,
            double cacheScale)
        {
            var drawingVisual = new DrawingVisual();

            using (var drawingContext = drawingVisual.RenderOpen())
            {
                foreach (var item in batch)
                {
                    try
                    {
                        drawAction(drawingContext, item);
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue rendering other items
                        System.Diagnostics.Debug.WriteLine($"Error rendering item: {ex.Message}");
                    }
                }
            }

            // Enable bitmap caching for better performance with complex visuals
            if (enableBitmapCaching)
            {
                drawingVisual.CacheMode = new BitmapCache(cacheScale);
            }

            return drawingVisual;
        }

        /// <summary>
        /// Efficiently adds batched DrawingVisuals to a VisualHost, clearing existing content
        /// </summary>
        public static void AddBatchedVisualsToHost<T>(
            IEnumerable<T> items,
            Action<DrawingContext, T> drawAction,
            object visualHost, // Works with existing VisualHost class
            int batchSize = DEFAULT_BATCH_SIZE,
            bool enableBitmapCaching = true,
            double cacheScale = 2.0) // Higher default cache scale for better quality
        {
            if (visualHost == null) throw new ArgumentNullException(nameof(visualHost));

            // Use reflection to call ClearAllChildren - works with VisualHost
            var clearMethod = visualHost.GetType().GetMethod("ClearAllChildren");
            if (clearMethod != null)
            {
                clearMethod.Invoke(visualHost, null);
            }

            // Add new batched visuals
            var batchedVisuals = CreateBatchedVisuals(items, drawAction, batchSize, enableBitmapCaching, cacheScale);
            var addChildMethod = visualHost.GetType().GetMethod("AddChild", new[] { typeof(Visual) });
            var addChildWithContextMethod = visualHost.GetType().GetMethod("AddChild", new[] { typeof(Visual), typeof(object) });

            foreach (var visual in batchedVisuals)
            {
                if (addChildWithContextMethod != null)
                {
                    // Call AddChild(Visual, object) with null context
                    addChildWithContextMethod.Invoke(visualHost, new object[] { visual, null });
                }
                else if (addChildMethod != null)
                {
                    // Call AddChild(Visual)
                    addChildMethod.Invoke(visualHost, new object[] { visual });
                }
                else
                {
                    throw new InvalidOperationException("VisualHost does not have a compatible AddChild method");
                }
            }
        }

        /// <summary>
        /// Calculates optimal batch size based on the number of items
        /// </summary>
        public static int GetOptimalBatchSize(int itemCount)
        {
            if (itemCount <= 0) return DEFAULT_BATCH_SIZE;
            
            // For very small counts, use smaller batches
            if (itemCount < 100) return Math.Max(10, itemCount / 5);
            
            // For large counts, use larger batches but cap at MAX_BATCH_SIZE
            if (itemCount > 5000) return MAX_BATCH_SIZE;
            
            // For medium counts, aim for 20-50 batches total
            return Math.Min(MAX_BATCH_SIZE, Math.Max(DEFAULT_BATCH_SIZE, itemCount / 25));
        }

        #endregion

        #region Performance Monitoring

        /// <summary>
        /// Statistics for monitoring batched rendering performance
        /// </summary>
        public class RenderingStats
        {
            public int TotalItems { get; set; }
            public int BatchCount { get; set; }
            public int ItemsPerBatch => BatchCount > 0 ? TotalItems / BatchCount : 0;
            public TimeSpan RenderTime { get; set; }
            public double ItemsPerSecond => RenderTime.TotalSeconds > 0 ? TotalItems / RenderTime.TotalSeconds : 0;

            public override string ToString()
            {
                return $"Rendered {TotalItems} items in {BatchCount} batches ({ItemsPerBatch} items/batch) " +
                       $"in {RenderTime.TotalMilliseconds:F1}ms ({ItemsPerSecond:F0} items/sec)";
            }
        }

        /// <summary>
        /// Renders items with performance monitoring
        /// </summary>
        public static (IEnumerable<DrawingVisual> visuals, RenderingStats stats) CreateBatchedVisualsWithStats<T>(
            IEnumerable<T> items,
            Action<DrawingContext, T> drawAction,
            int batchSize = DEFAULT_BATCH_SIZE,
            bool enableBitmapCaching = true)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var itemsList = items?.ToList() ?? new List<T>();
            var visuals = CreateBatchedVisuals(itemsList, drawAction, batchSize, enableBitmapCaching).ToList();
            
            stopwatch.Stop();

            var stats = new RenderingStats
            {
                TotalItems = itemsList.Count,
                BatchCount = visuals.Count,
                RenderTime = stopwatch.Elapsed
            };

            return (visuals, stats);
        }

        #endregion
    }
}
