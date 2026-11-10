using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Threading;
using EVEDataUtils;
using Newtonsoft.Json;

namespace SMT
{
    /// <summary>
    /// --perftest[=results.json]: once the main window has rendered, drive the region map through a fixed script,
    /// write the timings as JSON and close. The run changes the region, toggles and layout on purpose, so in this
    /// mode SMT saves nothing on exit except the characters (refreshed ESI tokens must not be lost).
    /// To see what a change did, run it on the build before and the build after, a few times each, and give the two
    /// folders of results to tools/PerfBench: "PerfBench compare before after".
    /// </summary>
    public partial class MainWindow
    {
        private static readonly string[] PerfTestRegions =
        {
            "Delve", "The Forge", "Catch", "Fountain", "Providence", "Querious", "Pure Blind", "Esoteria", "Detorid", "Vale of the Silent"
        };

        private string perfTestOutputPath;

        private bool IsPerfTestRun => perfTestOutputPath != null;

        private void ReadPerfTestArguments()
        {
            foreach(string arg in Environment.GetCommandLineArgs().Skip(1))
            {
                if(arg == "--perftest")
                {
                    perfTestOutputPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SMT-perftest.json");
                }
                else if(arg.StartsWith("--perftest=", StringComparison.Ordinal))
                {
                    perfTestOutputPath = arg.Substring("--perftest=".Length);
                }
            }
        }

        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            ContentRendered -= MainWindow_ContentRendered;
            PerfMetrics.MarkStartup(PerfMetrics.FirstFrame);
            AppLog.Metrics("Startup", PerfMetrics.StartupSummary());

            if(IsPerfTestRun)
            {
                _ = RunPerfTestAsync();
            }
        }

        private async Task RunPerfTestAsync()
        {
            Dictionary<string, object> results = new Dictionary<string, object>();

            try
            {
                // timings taken with the JIT's tiering switched off don't compare with timings taken with it on
                results["tieredCompilation"] = Environment.GetEnvironmentVariable("DOTNET_TieredCompilation") != "0";
                results["startupMs"] = PerfMetrics.StartupMarks.ToDictionary(m => m.Key, m => Math.Round(m.Value));

                // let the startup background work (ESI, zKillboard connection, storms) settle
                await Task.Delay(TimeSpan.FromSeconds(5));

                // the characters found in the Local logs (or from ESI) by then, and how many have a known system
                List<EVEData.LocalCharacter> characters = EVEManager.LocalCharacters.ToList();
                results["charactersAfterStartup"] = new Dictionary<string, object>
                {
                    ["count"] = characters.Count,
                    ["withSystem"] = characters.Count(c => !string.IsNullOrEmpty(c.Location)),
                };

                if(RegionLayoutDoc != null)
                {
                    RegionLayoutDoc.IsSelected = true;
                }

                await WaitForIdleAsync();

                List<string> regions = PerfTestRegions.Where(name => EVEManager.GetRegion(name) != null).ToList();
                results["regionSwitch"] = await MeasureAsync(regions, 2, name => RegionUC.SelectRegion(name));

                RegionUC.SelectRegion("Delve");
                await WaitForIdleAsync();
                results["regionCanvasChildren"] = RegionUC.MainCanvas.Children.Count;
                results["regionCanvasVisuals"] = CountVisuals(RegionUC.MainCanvas);
                results["systemNamePixels"] = SystemNamePixelHashes(out int namesDrawn);
                results["systemNamesDrawn"] = namesDrawn;
                SaveRegionMapImage(System.IO.Path.ChangeExtension(perfTestOutputPath, ".png"));
                results["fullRedraw"] = await MeasureAsync(Enumerable.Range(0, 10).ToList(), 1, _ => RegionUC.ReDrawMap(true));
                results["partialRedraw"] = await MeasureAsync(Enumerable.Range(0, 20).ToList(), 1, _ => RegionUC.ReDrawMap(false));
                results["regionCanvasChildrenAfterPartialRedraws"] = RegionUC.MainCanvas.Children.Count;

                // left-clicks on 20 systems through the system shapes' own MouseDown handler
                List<Shape> systemShapes = RegionUC.MainCanvas.Children.OfType<Shape>().Where(s => s.DataContext is EVEData.MapSystem).Take(20).ToList();
                results["systemClick"] = await MeasureAsync(systemShapes, 1, ClickShape);

                // hovering systems: the popup and highlights, then leaving, with a full intel list
                results["systemHover"] = await MeasureHoverAsync(systemShapes);

                // a data overlay toggle (an even number of toggles leaves it as it was)
                results["dataOverlayToggle"] = await MeasureAsync(Enumerable.Range(0, 6).ToList(), 1, _ => RegionUC.ShowNPCKills = !RegionUC.ShowNPCKills);

                // a setting that has nothing to do with the region map (intel window flash)
                results["unrelatedSettingChange"] = await MeasureAsync(Enumerable.Range(0, 6).ToList(), 1, _ => MapConf.FlashWindow = !MapConf.FlashWindow);

                // sitting idle with the region map in view
                results["idleRegionMap"] = await MeasureIdleAsync(TimeSpan.FromSeconds(20));

                // an overlay window for a test character: 30 refreshes
                results["overlay"] = await MeasureOverlayAsync();

                // where idle time goes: the region map without a selected system (no highlight animation), then the universe map
                RegionUC.SelectedSystem = string.Empty;
                RegionUC.ReDrawMap(false);
                await WaitForIdleAsync();
                results["idleRegionMapNoSelection"] = await MeasureIdleAsync(TimeSpan.FromSeconds(15));

                // a region with jump bridges (from this profile's list), nothing selected
                string bridgeRegion = EVEManager.JumpBridges
                    .SelectMany(jb => new[] { EVEManager.GetEveSystem(jb.From)?.Region, EVEManager.GetEveSystem(jb.To)?.Region })
                    .Where(region => region != null)
                    .GroupBy(region => region)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault();
                if(bridgeRegion != null)
                {
                    RegionUC.SelectRegion(bridgeRegion);
                    RegionUC.SelectedSystem = string.Empty;
                    RegionUC.ReDrawMap(false);
                    await WaitForIdleAsync();
                    Dictionary<string, object> bridgeIdle = await MeasureIdleAsync(TimeSpan.FromSeconds(15));
                    bridgeIdle["region"] = bridgeRegion;
                    bridgeIdle["animatedLines"] = RegionUC.MainCanvas.Children.OfType<Shape>().Count(s => s.HasAnimatedProperties);
                    results["idleJumpBridgeRegion"] = bridgeIdle;
                }

                if(UniverseLayoutDoc != null)
                {
                    UniverseLayoutDoc.IsSelected = true;
                    await WaitForIdleAsync();
                    results["idleUniverseMap"] = await MeasureIdleAsync(TimeSpan.FromSeconds(15));
                }

                // the universe map's right-click lookup: a point just off each system's centre must find that system
                int probed = 0;
                int found = 0;
                foreach(EVEData.System sys in EVEManager.Systems)
                {
                    probed++;
                    if(UniverseUC.FindSystemAt(new Point(sys.UniverseX + 0.6, sys.UniverseY - 0.6)) == sys)
                    {
                        found++;
                    }
                }

                results["universeRightClickLookup"] = new Dictionary<string, object> { ["probed"] = probed, ["foundItsSystem"] = found };
            }
            catch(Exception exception)
            {
                results["error"] = exception.ToString();
            }

            File.WriteAllText(perfTestOutputPath, JsonConvert.SerializeObject(results, Formatting.Indented));
            Close();
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        /// <summary>
        /// Puts a --perftest window in the middle of a secondary monitor when there is one, out of the way of the main
        /// screen, at the same size (physical pixels) on every run so results compare.
        /// </summary>
        private static void PlaceOnPerfTestScreen(Window window, int width, int height)
        {
            System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.AllScreens.FirstOrDefault(s => !s.Primary) ?? System.Windows.Forms.Screen.PrimaryScreen;
            System.Drawing.Rectangle area = screen.WorkingArea;
            width = Math.Min(width, area.Width);
            height = Math.Min(height, area.Height);
            IntPtr handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            SetWindowPos(handle, IntPtr.Zero, area.Left + ((area.Width - width) / 2), area.Top + ((area.Height - height) / 2), width, height, SWP_NOZORDER | SWP_NOACTIVATE);
        }

        private void ClosePerfTestRun()
        {
            try
            {
                EVEManager.SaveCharacters();
            }
            catch(Exception exception)
            {
                AppLog.Error("Save characters", exception);
            }

            EVEManager.ShutDown();

            waveOutEvent?.Stop();
            waveOutEvent?.Dispose();
            audioFileReader?.Dispose();
            nIcon.Visible = false;
            nIcon.Dispose();
        }

        private static void ClickShape(Shape shape)
        {
            MouseButtonEventArgs args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseDownEvent,
                Source = shape,
            };

            // the handler only acts on single clicks, and ClickCount has an internal setter
            typeof(MouseButtonEventArgs).GetProperty(nameof(MouseButtonEventArgs.ClickCount)).GetSetMethod(true).Invoke(args, new object[] { 1 });
            shape.RaiseEvent(args);
        }

        private static async Task WaitForIdleAsync()
        {
            await System.Windows.Threading.Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        }

        /// <summary>
        /// Runs the action for each item: the time until it returns plus a layout pass, the time until the UI
        /// thread is idle again, and how many region map redraws it caused (the 2 s refresh can add a few).
        /// </summary>
        private async Task<Dictionary<string, object>> MeasureAsync<T>(IList<T> items, int passes, Action<T> action)
        {
            List<double> syncMs = new List<double>();
            List<double> settledMs = new List<double>();
            long fullBefore = PerfMetrics.RegionFullRedraws;
            long partialBefore = PerfMetrics.RegionPartialRedraws;
            double fullMsBefore = PerfMetrics.RegionFullRedrawMs;
            int gen0Before = GC.CollectionCount(0);
            int gen1Before = GC.CollectionCount(1);
            int gen2Before = GC.CollectionCount(2);
            long allocatedBefore = GC.GetTotalAllocatedBytes();

            for(int pass = 0; pass < passes; pass++)
            {
                foreach(T item in items)
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    action(item);
                    UpdateLayout();
                    syncMs.Add(sw.Elapsed.TotalMilliseconds);
                    await WaitForIdleAsync();
                    settledMs.Add(sw.Elapsed.TotalMilliseconds);
                }
            }

            double count = syncMs.Count;
            return new Dictionary<string, object>
            {
                ["count"] = syncMs.Count,
                ["medianMs"] = Median(syncMs),
                ["meanMs"] = Math.Round(syncMs.Average(), 2),
                ["maxMs"] = Math.Round(syncMs.Max(), 2),
                ["settledMedianMs"] = Median(settledMs),
                ["fullRedrawsPerAction"] = Math.Round((PerfMetrics.RegionFullRedraws - fullBefore) / count, 2),
                ["partialRedrawsPerAction"] = Math.Round((PerfMetrics.RegionPartialRedraws - partialBefore) / count, 2),
                // time inside ReDrawMap(true) itself, without the layout pass and rendering that follow
                ["fullRedrawOwnMsPerAction"] = Math.Round((PerfMetrics.RegionFullRedrawMs - fullMsBefore) / count, 2),
                ["allocatedMBPerAction"] = Math.Round((GC.GetTotalAllocatedBytes() - allocatedBefore) / 1048576.0 / count, 2),
                ["gcGen0"] = GC.CollectionCount(0) - gen0Before,
                ["gcGen1"] = GC.CollectionCount(1) - gen1Before,
                ["gcGen2"] = GC.CollectionCount(2) - gen2Before,
            };
        }

        /// <summary>
        /// Opens an overlay for a test character in 1DQ1-A and refreshes it 30 times. Reports the refresh time, how
        /// many ToolTipOpening handlers one system shape carries afterwards, and what opening its tooltip costs.
        /// The test character isn't ESI-linked, so it is never saved; the overlay's placement isn't stored.
        /// </summary>
        private async Task<Dictionary<string, object>> MeasureOverlayAsync()
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            EVEData.LocalCharacter pilot = new EVEData.LocalCharacter("SMT perftest pilot", string.Empty, "1DQ1-A");
            EVEData.LocalCharacter previousCharacter = ActiveCharacter;
            Overlay.SuppressPlacementSaving = true;
            Overlay.PlacementOverride = window => PlaceOnPerfTestScreen(window, 500, 500);
            EVEManager.AddCharacter(pilot);
            ActiveCharacter = pilot;
            Overlay overlay = new Overlay(this);
            try
            {
                overlay.Show();
                await Task.Delay(TimeSpan.FromSeconds(1));
                await WaitForIdleAsync();

                List<double> refreshMs = new List<double>();
                for(int i = 0; i < 30; i++)
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    overlay.RefreshCurrentView();
                    overlay.UpdateLayout();
                    refreshMs.Add(sw.Elapsed.TotalMilliseconds);
                    await WaitForIdleAsync();
                }

                result["refreshes"] = refreshMs.Count;
                result["refreshFirst5MedianMs"] = Median(refreshMs.Take(5).ToList());
                result["refreshLast5MedianMs"] = Median(refreshMs.Skip(25).ToList());

                // the test character's own system shape
                overlay.systemData.TryGetValue("1DQ1-A", out OverlaySystemData pilotSystem);
                Shape shape = pilotSystem?.systemCanvasElement;
                result["systemsOnOverlay"] = overlay.systemData.Count;
                if(shape != null)
                {
                    // WPF keeps a shape's handlers to itself; if this way in ever stops working, the value is left
                    // out, which PerfBench compare shows, and not reported as no handlers
                    object store = typeof(UIElement).GetProperty("EventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(shape);
                    MethodInfo getHandlers = store?.GetType().GetMethod("GetRoutedEventHandlers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if(getHandlers?.Invoke(store, new object[] { ToolTipService.ToolTipOpeningEvent }) is Array handlers)
                    {
                        result["tooltipOpeningHandlersOnOneSystem"] = handlers.Length;
                    }

                    // what opening that system's tooltip costs now (each handler runs)
                    ConstructorInfo openingArgs = typeof(ToolTipEventArgs).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(bool) }, null);
                    if(openingArgs != null)
                    {
                        Stopwatch sw = Stopwatch.StartNew();
                        for(int i = 0; i < 10; i++)
                        {
                            ToolTipEventArgs args = (ToolTipEventArgs)openingArgs.Invoke(new object[] { true });
                            args.RoutedEvent = ToolTipService.ToolTipOpeningEvent;
                            args.Source = shape;
                            shape.RaiseEvent(args);
                        }

                        result["tooltipOpenMs"] = Math.Round(sw.Elapsed.TotalMilliseconds / 10, 3);
                    }
                }
            }
            finally
            {
                overlay.Close();
                ActiveCharacter = previousCharacter;
                EVEManager.RemoveCharacter(pilot);
            }

            return result;
        }

        /// <summary>
        /// Hovers each system through the region map's own popup code: entering (the popup with the gate and jump
        /// bridge highlights) and leaving. Every hover checks each intel row, so the intel list is first filled to its
        /// 250-row limit with test rows, some naming the hovered systems; they are removed afterwards.
        /// </summary>
        private async Task<Dictionary<string, object>> MeasureHoverAsync(List<Shape> systemShapes)
        {
            if(systemShapes.Count == 0)
            {
                return null;
            }

            List<string> names = systemShapes.Select(s => ((EVEData.MapSystem)s.DataContext).Name).ToList();
            List<EVEData.IntelData> testRows = new List<EVEData.IntelData>();
            for(int i = IntelCache.Count; i < 250; i++)
            {
                EVEData.IntelData row = new EVEData.IntelData("[ 2026.11.09 12:00:00 ] SMT perftest > test", "perftest");
                row.IntelString = "perftest row " + i;
                if(i % 5 == 0)
                {
                    row.Systems.Add(names[i % names.Count]);
                }

                testRows.Add(row);
                IntelCache.Add(row);
            }

            int intelRowCount = IntelCache.Count;
            await WaitForIdleAsync();

            List<double> enterMs = new List<double>();
            List<double> enterSettledMs = new List<double>();
            List<double> leaveMs = new List<double>();
            List<double> leaveSettledMs = new List<double>();
            List<double> intelRowsMs = new List<double>();
            List<string> popupSizes = new List<string>();
            int realizedIntelRows = 0;
            double allocatedMBPerHover = 0;
            try
            {
                long allocatedBefore = GC.GetTotalAllocatedBytes();
                foreach(Shape shape in systemShapes)
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    RegionUC.UpdateSystemPopup(shape, true);
                    UpdateLayout();
                    enterMs.Add(sw.Elapsed.TotalMilliseconds);
                    popupSizes.Add(string.Format(CultureInfo.InvariantCulture, "{0:0.#}x{1:0.#}", RegionUC.SystemInfoPopupSP.ActualWidth, RegionUC.SystemInfoPopupSP.ActualHeight));
                    await WaitForIdleAsync();
                    enterSettledMs.Add(sw.Elapsed.TotalMilliseconds);

                    sw.Restart();
                    RegionUC.UpdateSystemPopup(shape, false);
                    UpdateLayout();
                    leaveMs.Add(sw.Elapsed.TotalMilliseconds);
                    await WaitForIdleAsync();
                    leaveSettledMs.Add(sw.Elapsed.TotalMilliseconds);
                }

                allocatedMBPerHover = (GC.GetTotalAllocatedBytes() - allocatedBefore) / 1048576.0 / systemShapes.Count;

                // the intel row highlight on its own (it runs on entering and on leaving)
                foreach(string name in names)
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    RegionUC_SystemHoverEvent(name);
                    intelRowsMs.Add(sw.Elapsed.TotalMilliseconds);
                }

                for(int i = 0; i < IntelCache.Count; i++)
                {
                    if(RawIntelBox.ItemContainerGenerator.ContainerFromIndex(i) != null)
                    {
                        realizedIntelRows++;
                    }
                }

                RegionUC_SystemHoverEvent(string.Empty);
            }
            finally
            {
                foreach(EVEData.IntelData row in testRows)
                {
                    IntelCache.Remove(row);
                }
            }

            return new Dictionary<string, object>
            {
                ["count"] = enterMs.Count,
                ["popupOnInProfile"] = MapConf.ShowSystemPopup,
                ["jumpBridgesShown"] = RegionUC.ShowJumpBridges,
                ["enterMedianMs"] = Median(enterMs),
                ["enterSettledMedianMs"] = Median(enterSettledMs),
                ["enterMaxMs"] = Math.Round(enterMs.Max(), 2),
                ["leaveMedianMs"] = Median(leaveMs),
                ["leaveSettledMedianMs"] = Median(leaveSettledMs),
                ["allocatedMBPerHover"] = Math.Round(allocatedMBPerHover, 3),
                ["intelRows"] = intelRowCount,
                ["realizedIntelRows"] = realizedIntelRows,
                ["intelRowHighlightMedianMs"] = Median(intelRowsMs),
                // the popup's laid-out size per hovered system, to compare its content between builds
                ["popupSizes"] = string.Join(" ", popupSizes),
            };
        }

        private async Task<Dictionary<string, object>> MeasureIdleAsync(TimeSpan duration)
        {
            Dictionary<string, object> idle = await MeasureIdleCoreAsync(duration);
            idle["animatedShapesOnRegionMap"] = RegionUC.MainCanvas.Children.OfType<Shape>()
                .Count(s => s.HasAnimatedProperties || (s.RenderTransform?.HasAnimatedProperties ?? false));
            return idle;
        }

        private static async Task<Dictionary<string, object>> MeasureIdleCoreAsync(TimeSpan duration)
        {
            using Process process = Process.GetCurrentProcess();
            int gen0 = GC.CollectionCount(0);
            int gen1 = GC.CollectionCount(1);
            int gen2 = GC.CollectionCount(2);
            long allocated = GC.GetTotalAllocatedBytes();
            long partialRedraws = PerfMetrics.RegionPartialRedraws;
            double partialRedrawMs = PerfMetrics.RegionPartialRedrawMs;
            TimeSpan cpu = process.TotalProcessorTime;

            await Task.Delay(duration);

            process.Refresh();
            double seconds = duration.TotalSeconds;
            return new Dictionary<string, object>
            {
                ["seconds"] = seconds,
                ["cpuMsPerSecond"] = Math.Round((process.TotalProcessorTime - cpu).TotalMilliseconds / seconds, 1),
                ["allocatedMBPerSecond"] = Math.Round((GC.GetTotalAllocatedBytes() - allocated) / 1048576.0 / seconds, 2),
                ["gcGen0"] = GC.CollectionCount(0) - gen0,
                ["gcGen1"] = GC.CollectionCount(1) - gen1,
                ["gcGen2"] = GC.CollectionCount(2) - gen2,
                ["partialRedraws"] = PerfMetrics.RegionPartialRedraws - partialRedraws,
                ["partialRedrawMsPerSecond"] = Math.Round((PerfMetrics.RegionPartialRedrawMs - partialRedrawMs) / seconds, 2),
            };
        }

        /// <summary>
        /// Renders each system name block on the region map (a Grid on the canvas, with a margin for text that overflows
        /// it) and hashes its pixels, so two builds' text can be compared name by name. <paramref name="drawn"/> is
        /// how many of the renders show anything at all: all of them, unless the capture itself has stopped working.
        /// </summary>
        private string SystemNamePixelHashes(out int drawn)
        {
            drawn = 0;
            List<string> hashes = new List<string>();
            foreach(Grid grid in RegionUC.MainCanvas.Children.OfType<Grid>())
            {
                // the frame is given in the canvas's coordinates (see RenderBitmap): the block's place on it, and a margin
                Vector position = System.Windows.Media.VisualTreeHelper.GetOffset(grid);
                Rect frame = new Rect(position.X - 40, position.Y - 20, grid.ActualWidth + 80, grid.ActualHeight + 60);
                byte[] pixels = RenderPixels(grid, frame);
                if(pixels.Any(value => value != 0))
                {
                    drawn++;
                }

                hashes.Add(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(pixels)).Substring(0, 8));
            }

            return string.Join(" ", hashes);
        }

        /// <summary>
        /// The region map's canvas as a PNG, to look at after a rendering change.
        /// </summary>
        private void SaveRegionMapImage(string path)
        {
            Rect frame = new Rect(0, 0, RegionUC.MainCanvas.ActualWidth, RegionUC.MainCanvas.ActualHeight);
            System.Windows.Media.Imaging.RenderTargetBitmap bitmap = RenderBitmap(RegionUC.MainCanvas, frame);
            System.Windows.Media.Imaging.PngBitmapEncoder encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            using FileStream file = File.Create(path);
            encoder.Save(file);
        }

        private static byte[] RenderPixels(System.Windows.Media.Visual visual, Rect frame)
        {
            System.Windows.Media.Imaging.RenderTargetBitmap bitmap = RenderBitmap(visual, frame);
            byte[] pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
            bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
            return pixels;
        }

        /// <summary>
        /// Renders what a visual draws inside frame at 96 dpi, through a VisualBrush, which leaves out everything else
        /// on the canvas. The frame is in the coordinates of the visual's parent: the brush draws a visual where it
        /// sits in its parent, so a frame at the visual's own origin shows nothing of a visual placed elsewhere.
        /// </summary>
        private static System.Windows.Media.Imaging.RenderTargetBitmap RenderBitmap(System.Windows.Media.Visual visual, Rect frame)
        {
            int width = Math.Max(1, (int)Math.Ceiling(frame.Width));
            int height = Math.Max(1, (int)Math.Ceiling(frame.Height));
            System.Windows.Media.VisualBrush brush = new System.Windows.Media.VisualBrush(visual)
            {
                ViewboxUnits = System.Windows.Media.BrushMappingMode.Absolute,
                Viewbox = frame,
                Stretch = System.Windows.Media.Stretch.Fill,
            };
            System.Windows.Media.DrawingVisual drawing = new System.Windows.Media.DrawingVisual();
            using(System.Windows.Media.DrawingContext context = drawing.RenderOpen())
            {
                context.DrawRectangle(brush, null, new Rect(0, 0, frame.Width, frame.Height));
            }

            System.Windows.Media.Imaging.RenderTargetBitmap bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            bitmap.Render(drawing);
            return bitmap;
        }

        /// <summary>
        /// Every visual below root: the elements layout and rendering have to visit, templates included.
        /// </summary>
        private static int CountVisuals(DependencyObject root)
        {
            int count = 0;
            int children = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
            for(int i = 0; i < children; i++)
            {
                count += 1 + CountVisuals(System.Windows.Media.VisualTreeHelper.GetChild(root, i));
            }

            return count;
        }

        private static double Median(List<double> values)
        {
            List<double> sorted = values.OrderBy(v => v).ToList();
            return Math.Round(sorted[sorted.Count / 2], 2);
        }
    }
}
